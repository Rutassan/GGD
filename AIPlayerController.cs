using Raylib_cs;
using System;
using System.Collections.Generic; // Для List

public class AIPlayerController : IPlayerController
{
    private enum AIState
    {
        Idle,
        ExecutingPath,
        SearchingForResource,
        Mining,
        Building
    }

    private readonly Random _random = new Random();
    private int _actionDelayCounter = 0;
    private const int _actionDelayMax = 20;

    private List<(int, int)> _path = new List<(int, int)>(); 
    private (int x, int y)? _targetResourcePosition = null;
    private AIState _currentState = AIState.Idle; // Текущее состояние хранится скорее для дебага, чем для строгой логики
    private bool _justMined = false;
    private readonly Func<GameMap, int, int, int, int, List<(int, int)>?> _pathfinder;

    public AIPlayerController(Func<GameMap, int, int, int, int, List<(int, int)>?>? pathfinder = null)
    {
        _pathfinder = pathfinder ?? Pathfinder.FindPath;
    }

    public void Update(Player player, GameMap map)
    {
        _actionDelayCounter++;

        if (_actionDelayCounter < _actionDelayMax)
        {
            return;
        }

        _actionDelayCounter = 0;
        bool minedPreviousTurn = _justMined;
        _justMined = false;

        // Инвалидация цели: если ресурс закончился, перестаем идти по пути
        if (_targetResourcePosition.HasValue)
        {
            var target = _targetResourcePosition.Value;
            MapCell targetCell = map.GetCell(target.x, target.y);
            if (targetCell.Resource == null || targetCell.Resource.Health <= 0)
            {
                _targetResourcePosition = null;
                _path.Clear();
            }
        }

        // 1. Приоритет — добыча, если стоим на ресурсе
        if (TryMineCurrentCell(player, map))
        {
            _justMined = true;
            return;
        }

        // 2. Если есть актуальный маршрут — двигаемся
        if (_path.Count > 0)
        {
            _currentState = AIState.ExecutingPath;
            ExecuteNextStep(player, map);
            return;
        }

        MapCell currentCell = map.GetCell(player.X, player.Y);

        // 3. Пытаемся построить, если есть камень и клетка пустая
        if (!minedPreviousTurn 
            && player.PlayerInventory.HasItem("Stone", 1) 
            && !currentCell.IsWall 
            && currentCell.Resource == null)
        {
            _currentState = AIState.Building;
            player.Build(map, "Stone");
            return;
        }

        // 4. Планируем путь к ресурсу; если ничего не нашли — переходим в Idle
        if (!player.PlayerInventory.HasItem("Stone", 1))
        {
            _currentState = AIState.SearchingForResource;
            if (TryPlanPathToNearestResource(player, map))
            {
                _currentState = AIState.ExecutingPath;
                ExecuteNextStep(player, map);
                return;
            }
        }

        // 5. Режим ожидания — случайное блуждание, чтобы не застревать
        if (minedPreviousTurn)
        {
            _currentState = AIState.Idle;
            return;
        }
        WanderRandomly(player, map);
    }

    // Вспомогательный метод для поиска ближайшего ресурса
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
                    minDistance = currDist;
                    nearestResource = (currX, currY);
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
        if (currentCell.Resource != null && currentCell.Resource.ResourceType == "Stone" && currentCell.Resource.Health > 0)
        {
            _currentState = AIState.Mining;
            player.Mine(map);
            _targetResourcePosition = null;
            _path.Clear();
            return true;
        }
        return false;
    }

    private bool TryPlanPathToNearestResource(Player player, GameMap map)
    {
        _targetResourcePosition = FindNearestResource(map, player.X, player.Y, "Stone");
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
            _path.RemoveAt(0); // Игнорируем текущую клетку в маршруте
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
}
