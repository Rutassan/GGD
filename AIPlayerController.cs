using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class AIPlayerController : IPlayerController
{
    private const string StoneResourceType = "Stone";
    private const string StoneWallBlock = "StoneWall";
    private static readonly IReadOnlyDictionary<string, int> StoneWallRecipe = new Dictionary<string, int>
    {
        { StoneResourceType, 2 }
    };

    private enum AIState
    {
        Idle,
        ExecutingPath,
        SearchingForResource,
        Mining,
        Building,
        SeekingShelter,
        WaitingInShelter
    }

    private enum ShelterBuildState
    {
        None,
        FindingSpot,
        CollectingResources,
        BuildingWalls,
        EnteringShelter
    }

    private enum BuildActionType
    {
        Wall,
        Door
    }

    private struct BuildAction
    {
        public int X { get; }
        public int Y { get; }
        public BuildActionType ActionType { get; }

        public BuildAction(int x, int y, BuildActionType actionType)
        {
            X = x;
            Y = y;
            ActionType = actionType;
        }
    }

    private readonly Random _random = new Random();
    private int _actionDelayCounter = 0;
    private const int _actionDelayBase = 20;
    private const int _shelterSeekThreshold = 600;
    private const int _shelterSize = 5;

    private List<(int, int)> _path = new List<(int, int)>();
    private (int x, int y)? _targetResourcePosition = null;
    private string? _targetResourceType = null;
    private AIState _currentState = AIState.Idle;
    private readonly Func<GameMap, int, int, int, int, List<(int, int)>?> _pathfinder;

    private ShelterBuildState _shelterBuildState = ShelterBuildState.None;
    private (int x, int y)? _shelterSpotCenter = null;
    private Queue<BuildAction> _buildQueue = new Queue<BuildAction>();
    private int _requiredShelterWalls = 0;
    private (int x, int y)? _plannedDoorPosition = null;

    public AIPlayerController(Func<GameMap, int, int, int, int, List<(int, int)>?>? pathfinder = null)
    {
        _pathfinder = pathfinder ?? Pathfinder.FindPath;
    }

    public void Update(Player player, GameMap map, bool isDay, long gameTime)
    {
        int currentActionDelay = _actionDelayBase;
        if (player.IsFreezing)
        {
            currentActionDelay *= 3;
        }
        if (player.IsStarving)
        {
            currentActionDelay *= 2;
        }

        _actionDelayCounter++;
        if (_actionDelayCounter < currentActionDelay)
        {
            return;
        }
        _actionDelayCounter = 0;

        long currentCycleTime = gameTime % Program.TotalDayNightDuration;
        bool isNight = currentCycleTime >= Program.DayDuration;
        bool approachingNight = currentCycleTime >= Program.DayDuration - _shelterSeekThreshold && currentCycleTime < Program.DayDuration;
        bool nightApproaching = isNight || approachingNight;

        if (nightApproaching && !map.IsInside(player.X, player.Y))
        {
            _currentState = AIState.SeekingShelter;
            if (HandleShelterLogic(player, map))
            {
                if (_path.Count > 0)
                {
                    ExecuteNextStep(player, map);
                    return;
                }
                if (map.IsInside(player.X, player.Y))
                {
                    _shelterBuildState = ShelterBuildState.None;
                    _buildQueue.Clear();
                    _plannedDoorPosition = null;
                    return;
                }
            }
        }
        else
        {
            _shelterBuildState = ShelterBuildState.None;
            _buildQueue.Clear();
            _plannedDoorPosition = null;
        }

        if (HandleHunger(player, map))
        {
            return;
        }

        if (_targetResourcePosition.HasValue)
        {
            var target = _targetResourcePosition.Value;
            MapCell targetCell = map.GetCell(target.x, target.y);
            if (targetCell.Resource == null || targetCell.Resource.Health <= 0)
            {
                _targetResourcePosition = null;
                _targetResourceType = null;
                _path.Clear();
            }
        }

        if (TryMineCurrentCell(player, map))
        {
            return;
        }

        if (_path.Count > 0)
        {
            _currentState = AIState.ExecutingPath;
            ExecuteNextStep(player, map);
            return;
        }

        if (TryPlanPathToNearestResource(player, map, StoneResourceType))
        {
            _currentState = AIState.ExecutingPath;
            ExecuteNextStep(player, map);
            return;
        }

        MapCell currentCell = map.GetCell(player.X, player.Y);
        if (player.PlayerInventory.HasItem(StoneWallBlock, 1)
            && !currentCell.IsWall
            && currentCell.Resource == null)
        {
            _currentState = AIState.Building;
            player.Build(map, StoneWallBlock, player.X, player.Y);
            return;
        }

        WanderRandomly(player, map);
    }

    private bool HandleHunger(Player player, GameMap map)
    {
        if (!player.IsHungry && !player.IsStarving)
        {
            return false;
        }

        if (player.PlayerInventory.HasItem("Berry", 1))
        {
            player.Eat("Berry");
            return true;
        }

        if (TryPlanPathToNearestResource(player, map, "Berry"))
        {
            _currentState = AIState.ExecutingPath;
            ExecuteNextStep(player, map);
            return true;
        }

        return false;
    }

    private bool HandleShelterLogic(Player player, GameMap map)
    {
        switch (_shelterBuildState)
        {
            case ShelterBuildState.None:
                _shelterBuildState = ShelterBuildState.FindingSpot;
                _path.Clear();
                goto case ShelterBuildState.FindingSpot;

            case ShelterBuildState.FindingSpot:
                _shelterSpotCenter = FindOpenShelterSpot(map, player.X, player.Y, _shelterSize);
                if (_shelterSpotCenter.HasValue)
                {
                    _buildQueue = PlanBuildingSequence(_shelterSpotCenter.Value, _shelterSize);
                    _requiredShelterWalls = _buildQueue.Count;
                    _shelterBuildState = ShelterBuildState.CollectingResources;
                    goto case ShelterBuildState.CollectingResources;
                }
                _shelterBuildState = ShelterBuildState.None;
                return false;

            case ShelterBuildState.CollectingResources:
                if (_requiredShelterWalls > 0 && player.PlayerInventory.HasItem(StoneWallBlock, _requiredShelterWalls))
                {
                    _shelterBuildState = ShelterBuildState.BuildingWalls;
                    goto case ShelterBuildState.BuildingWalls;
                }

                if (player.PlayerInventory.HasItem(StoneResourceType, 2))
                {
                    player.Craft(StoneWallBlock, StoneWallRecipe);
                    return true;
                }

                if (TryMineCurrentCell(player, map))
                {
                    return true;
                }

                if (TryPlanPathToNearestResource(player, map, StoneResourceType))
                {
                    return true;
                }

                return false;

            case ShelterBuildState.BuildingWalls:
                if (_buildQueue.Count > 0)
                {
                    var target = _buildQueue.Peek();
                    if (player.X != target.X || player.Y != target.Y)
                    {
                        _path = _pathfinder(map, player.X, player.Y, target.X, target.Y) ?? new List<(int, int)>();
                        if (_path.Count > 0)
                        {
                            if (_path[0].Item1 == player.X && _path[0].Item2 == player.Y) _path.RemoveAt(0);
                            return true;
                        }
                        _buildQueue.Dequeue();
                        return true;
                    }

                    if (target.ActionType == BuildActionType.Door)
                    {
                        player.BuildDoor(map, target.X, target.Y);
                    }
                    else
                    {
                        player.Build(map, StoneWallBlock, target.X, target.Y);
                    }
                    _buildQueue.Dequeue();
                    return true;
                }
                _shelterBuildState = ShelterBuildState.EnteringShelter;
                goto case ShelterBuildState.EnteringShelter;

            case ShelterBuildState.EnteringShelter:
                if (map.IsInside(player.X, player.Y))
                {
                    if (_plannedDoorPosition.HasValue)
                    {
                        if (EnsureDoorState(map, false))
                        {
                            return true;
                        }
                    }
                    _shelterBuildState = ShelterBuildState.None;
                    return true;
                }

                if (_plannedDoorPosition.HasValue)
                {
                    if (EnsureDoorState(map, true))
                    {
                        return true;
                    }
                }

                if (_shelterSpotCenter.HasValue)
                {
                    _path = _pathfinder(map, player.X, player.Y, _shelterSpotCenter.Value.x, _shelterSpotCenter.Value.y) ?? new List<(int, int)>();
                    if (_path.Count > 0)
                    {
                        if (_path[0].Item1 == player.X && _path[0].Item2 == player.Y) _path.RemoveAt(0);
                        return true;
                    }
                }
                _shelterBuildState = ShelterBuildState.None;
                return false;
        }

        return false;
    }

    private Queue<BuildAction> PlanBuildingSequence((int x, int y) center, int size)
    {
        Queue<BuildAction> buildQueue = new Queue<BuildAction>();
        int startX = center.x - size / 2 - 1;
        int startY = center.y - size / 2 - 1;
        int doorX = startX + (size + 2) / 2;
        int doorY = startY + size + 1;
        _plannedDoorPosition = (doorX, doorY);

        for (int dy = 0; dy < size + 2; dy++)
        {
            for (int dx = 0; dx < size + 2; dx++)
            {
                bool isWallEdge = dx == 0 || dx == size + 1 || dy == 0 || dy == size + 1;
                if (!isWallEdge)
                {
                    continue;
                }

                int targetX = startX + dx;
                int targetY = startY + dy;
                if (targetX == doorX && targetY == doorY)
                {
                    continue;
                }

                buildQueue.Enqueue(new BuildAction(targetX, targetY, BuildActionType.Wall));
            }
        }

        buildQueue.Enqueue(new BuildAction(doorX, doorY, BuildActionType.Door));
        return buildQueue;
    }

    private (int x, int y)? FindOpenShelterSpot(GameMap map, int playerX, int playerY, int size)
    {
        int requiredWidth = size + 2;
        int requiredHeight = size + 2;

        for (int y = 0; y <= GameMap.MapHeight - requiredHeight; y++)
        {
            for (int x = 0; x <= GameMap.MapWidth - requiredWidth; x++)
            {
                bool isSpotOpen = true;
                for (int ty = 0; ty < requiredHeight; ty++)
                {
                    for (int tx = 0; tx < requiredWidth; tx++)
                    {
                        MapCell cell = map.GetCell(x + tx, y + ty);
                        if (cell.IsWall || cell.Resource != null || cell.HasDoor)
                        {
                            isSpotOpen = false;
                            break;
                        }
                    }
                    if (!isSpotOpen)
                    {
                        break;
                    }
                }

                if (isSpotOpen)
                {
                    return (x + 1 + size / 2, y + 1 + size / 2);
                }
            }
        }
        return null;
    }

    private (int x, int y)? FindNearestSafeSpot(GameMap map, int startX, int startY)
    {
        Queue<(int x, int y, int dist)> queue = new Queue<(int x, int y, int dist)>();
        HashSet<(int x, int y)> visited = new HashSet<(int x, int y)>();

        queue.Enqueue((startX, startY, 0));
        visited.Add((startX, startY));

        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            var (currX, currY, currDist) = queue.Dequeue();
            if (map.IsInside(currX, currY))
            {
                return (currX, currY);
            }

            for (int i = 0; i < 4; i++)
            {
                int nextX = currX + dx[i];
                int nextY = currY + dy[i];
                if (nextX >= 0 && nextX < GameMap.MapWidth && nextY >= 0 && nextY < GameMap.MapHeight &&
                    !map.IsWall(nextX, nextY) && !visited.Contains((nextX, nextY)))
                {
                    visited.Add((nextX, nextY));
                    queue.Enqueue((nextX, nextY, currDist + 1));
                }
            }
        }
        return null;
    }

    private (int x, int y)? FindNearestResource(GameMap map, int startX, int startY, string resourceType)
    {
        Queue<(int x, int y, int dist)> queue = new Queue<(int x, int y, int dist)>();
        HashSet<(int x, int y)> visited = new HashSet<(int x, int y)>();

        queue.Enqueue((startX, startY, 0));
        visited.Add((startX, startY));

        (int x, int y)? nearestResource = null;
        int minDistance = int.MaxValue;

        while (queue.Count > 0)
        {
            (int currX, int currY, int currDist) = queue.Dequeue();
            MapCell cell = map.GetCell(currX, currY);
            if (cell.Resource != null && cell.Resource.ResourceType == resourceType && cell.Resource.Health > 0)
            {
                if (nearestResource == null || currDist < minDistance)
                {
                    nearestResource = (currX, currY);
                    minDistance = currDist;
                }
            }

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nextX = currX + dx[i];
                int nextY = currY + dy[i];

                if (nextX >= 0 && nextX < GameMap.MapWidth && nextY >= 0 && nextY < GameMap.MapHeight &&
                    !map.IsWall(nextX, nextY) && !visited.Contains((nextX, nextY)))
                {
                    visited.Add((nextX, nextY));
                    queue.Enqueue((nextX, nextY, currDist + 1));
                }
            }
        }
        return nearestResource;
    }

    private bool TryMineCurrentCell(Player player, GameMap map)
    {
        MapCell currentCell = map.GetCell(player.X, player.Y);
        if (currentCell.Resource != null && currentCell.Resource.Health > 0)
        {
            _currentState = AIState.Mining;
            player.Mine(map);
            _targetResourcePosition = null;
            _targetResourceType = null;
            _path.Clear();
            return true;
        }
        return false;
    }

    private bool TryPlanPathToNearestResource(Player player, GameMap map, string resourceType)
    {
        _targetResourceType = resourceType;
        _targetResourcePosition = FindNearestResource(map, player.X, player.Y, resourceType);
        if (!_targetResourcePosition.HasValue)
        {
            _path.Clear();
            return false;
        }

        List<(int, int)>? newPath = _pathfinder(map, player.X, player.Y, _targetResourcePosition.Value.x, _targetResourcePosition.Value.y);
        if (newPath == null || newPath.Count == 0)
        {
            _targetResourcePosition = null;
            _path.Clear();
            return false;
        }

        _path = newPath;
        if (_path.Count > 0 && _path[0].Item1 == player.X && _path[0].Item2 == player.Y)
        {
            _path.RemoveAt(0);
        }

        if (_path.Count > 0 && map.IsWall(_path[_path.Count - 1].Item1, _path[_path.Count - 1].Item2))
        {
            _path.Clear();
            _targetResourcePosition = null;
            return false;
        }

        return _path.Count > 0;
    }

    private void ExecuteNextStep(Player player, GameMap map)
    {
        if (_path.Count == 0)
        {
            return;
        }

        int nextX = _path[0].Item1;
        int nextY = _path[0].Item2;
        int dx = nextX - player.X;
        int dy = nextY - player.Y;

        if (player.Move(dx, dy, map))
        {
            _path.RemoveAt(0);
        }
        else
        {
            _path.Clear();
            _targetResourcePosition = null;
            _currentState = AIState.Idle;
        }
    }

    private void WanderRandomly(Player player, GameMap map)
    {
        _currentState = AIState.Idle;
        int rand_dx = 0, rand_dy = 0;
        int direction = _random.Next(4);
        switch (direction)
        {
            case 0: rand_dy = -1; break;
            case 1: rand_dy = 1; break;
            case 2: rand_dx = -1; break;
            case 3: rand_dx = 1; break;
        }
        player.Move(rand_dx, rand_dy, map);
    }

    private bool EnsureDoorState(GameMap map, bool shouldBeOpen)
    {
        if (!_plannedDoorPosition.HasValue)
        {
            return false;
        }
        var (dx, dy) = _plannedDoorPosition.Value;
        if (!map.HasDoorAt(dx, dy))
        {
            return false;
        }
        MapCell cell = map.GetCell(dx, dy);
        if (cell.Door == null)
        {
            return false;
        }
        if (cell.Door.IsOpen == shouldBeOpen)
        {
            return false;
        }
        map.TryToggleDoor(dx, dy);
        return true;
    }
}
