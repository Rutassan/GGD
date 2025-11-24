using NUnit.Framework;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq; // Для LINQ-запросов
using System.Numerics; // Для Vector2, если понадобится в будущем (пока не используется напрямую для pathfinding)
using System.Reflection; // Для доступа к private полям

// MockGameMap for AIPlayerControllerTests
public class AIMockGameMap : GameMap
{
    public MapCell[,] MockCells = new MapCell[MapHeight, MapWidth];
    private string _testName;

    public AIMockGameMap(string testName = "")
    {
        _testName = testName;
        // Initialize with empty cells
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                MockCells[y, x] = MapCell.Empty();
            }
        }
    }

    public override bool IsWall(int x, int y)
    {
        bool isWall = false;
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) {
            isWall = true;
        } else {
            isWall = MockCells[y, x].IsWall;
        }
        return isWall;
    }

    public override MapCell GetCell(int x, int y)
    {
        MapCell cell = MapCell.Wall(); // Default to wall for out of bounds
        if (x >= 0 && x < MapWidth && y >= 0 && y < MapHeight) 
        {
            cell = MockCells[y, x];
        }
        return cell;
    }

    public override void SetCell(int x, int y, MapCell newCell)
    {
        if (x >= 0 && x < MapWidth && y >= 0 && y < MapHeight) 
        {
            MockCells[y, x] = newCell;
        }
    }

    public override bool PlaceDoor(int x, int y, bool initiallyOpen = false)
    {
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
        MapCell target = MockCells[y, x];
        if (target.IsWall || target.Resource != null || target.HasDoor)
        {
            return false;
        }
        DoorTile door = new DoorTile(initiallyOpen);
        MockCells[y, x] = MapCell.DoorCell(door);
        return true;
    }

    public override bool TryToggleDoor(int x, int y)
    {
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
        DoorTile? door = MockCells[y, x].Door;
        if (door == null)
        {
            return false;
        }
        door.Toggle();
        MockCells[y, x] = MapCell.DoorCell(door);
        return true;
    }

    public override bool HasDoorAt(int x, int y)
    {
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
        return MockCells[y, x].HasDoor;
    }
}


[TestFixture]
[Parallelizable(ParallelScope.None)]
public class AIPlayerControllerTests
{
    private AIPlayerController _controller;
    private Player _player; // Using actual Player for inventory and mine/build logic
    private AIMockGameMap _gameMap;

    [SetUp]
    public void SetUp()
    {
        _controller = new AIPlayerController();
        _player = new Player(1, 1, '@', new Color(0, 121, 241, 255));
        _gameMap = new AIMockGameMap(TestContext.CurrentContext.Test.Name);
    }

    [Test]
    public void Update_BeforeDelayExpires_DoesNotMovePlayerOrAct()
    {
        // Arrange
        ResourceTile stone = new ResourceTile('$', new Color(), "Stone", 1);
        _gameMap.SetCell(_player.X, _player.Y, MapCell.ResourceCell(stone));

        for (int i = 0; i < 19; i++) // 19 calls, so counter is 19
        {
            _controller.Update(_player, _gameMap, true, 0L);
        }
        Assert.That(_player.X, Is.EqualTo(1));
        Assert.That(_player.Y, Is.EqualTo(1));
        Assert.That(_player.PlayerInventory.Resources.Count, Is.EqualTo(0)); // No mining happened
        
        // Исправлено: Проверяем, что Resource не null перед обращением к Health
        ResourceTile? resource = _gameMap.GetCell(_player.X, _player.Y).Resource;
        Assert.That(resource, Is.Not.Null);
        Assert.That(resource.Health, Is.EqualTo(1)); // Resource not mined
    }

    public void Update_AfterDelayExpires_AIAttemptsToMineResourceAtCurrentPosition()
    {
        // Arrange
        ResourceTile stone = new ResourceTile('$', new Color(150, 150, 150, 255), "Stone", 1); // Добавил цвет
        for (int i = 0; i < 20; i++) // 20 calls, action should occur
        {
            _controller.Update(_player, _gameMap, true, 0L);
        }

        // Assert
        Assert.That(stone.Health, Is.EqualTo(0)); // Resource should be mined
        Assert.That(_player.PlayerInventory.Resources.GetValueOrDefault("Stone", 0), Is.EqualTo(1));
        Assert.That(_gameMap.GetCell(_player.X, _player.Y).DisplayChar, Is.EqualTo('.')); // Cell should be empty
    }

    [Test]
    public void Update_AfterDelayExpires_AIAttemptsToBuildIfHasResourcesAndEmptyCell()
    {
        // Arrange
        _player.PlayerInventory.AddItem("StoneWall", 1); // Give AI a stone wall
        _gameMap.SetCell(_player.X, _player.Y, MapCell.Empty()); // Ensure current cell is empty

        // Act
        for (int i = 0; i < 20; i++) // 20 calls, action should occur
        {
            _controller.Update(_player, _gameMap, true, 0L);
        }

        // Assert
        Assert.That(_player.PlayerInventory.Resources.GetValueOrDefault("StoneWall", 0), Is.EqualTo(0)); // Stone wall should be consumed
        Assert.That(_gameMap.GetCell(_player.X, _player.Y).DisplayChar, Is.EqualTo('█')); // Cell should be a stone wall
    }

    [Test]
    public void Update_AfterDelayExpires_AIMovesIfNoOtherActionsPossible()
    {
        // Arrange - Ensure no resource to mine and no resources to build
        _player.PlayerInventory.RemoveItem("Stone", _player.PlayerInventory.Resources.GetValueOrDefault("Stone", 0)); // Clear inventory
        _gameMap.SetCell(_player.X, _player.Y, MapCell.Empty()); 

        int initialX = _player.X;
        int initialY = _player.Y;

        // Act
        for (int i = 0; i < 20; i++) // 20 calls, action should occur
        {
            _controller.Update(_player, _gameMap, true, 0L);
        }

        // Assert - Player should have moved
        Assert.That(_player.X, Is.Not.EqualTo(initialX).Or.Not.EqualTo(_player.Y)); // Check that at least one coordinate changed
    }

    [Test]
    public void Update_AIPathfindsAndMinesResource_NoObstacles()
    {
        // Arrange
        _player.X = 1;
        _player.Y = 1;
        _player.PlayerInventory.Clear(); // Убедимся, что инвентарь пуст

        ResourceTile stone = new ResourceTile('$', new Color(150, 150, 150, 255), "Stone", 1);
        _gameMap.SetCell(5, 1, MapCell.ResourceCell(stone)); // Ресурс на (5,1)

        // Act - Даем ИИ достаточно времени, чтобы найти, дойти и добыть
        // (5-1) шагов до ресурса + 1 шаг на добычу = 5 шагов
        // Каждый шаг занимает _actionDelayMax кадров
        for (int i = 0; i < 6 * 20 + 5; i++) // 6 действий (5 move + 1 mine) * _actionDelayMax кадров/действие + запас
        {
            _controller.Update(_player, _gameMap, true, 0L);
        }

        // Assert
        var cell = _gameMap.GetCell(5, 1);
        Assert.That(cell.Resource, Is.Null);
        Assert.That(cell.DisplayChar == '.' || cell.IsWall, Is.True); // Ресурс должен быть добыт (ячейка либо пустая, либо застроена)
    }

    [Test]
    public void Update_AIPathfindsAndMinesResource_WithObstacles()
    {
        // Arrange
        _player.X = 1;
        _player.Y = 1;
        _player.PlayerInventory.Clear(); // Ensure inventory is empty

        ResourceTile stone = new ResourceTile('$', new Color(150, 150, 150, 255), "Stone", 1);
        _gameMap.SetCell(5, 1, MapCell.ResourceCell(stone)); // Resource at (5,1)

        // Place a wall at (3,1) to force pathfinding around it
        _gameMap.SetCell(3, 1, MapCell.Wall()); 
        _gameMap.SetCell(3, 0, MapCell.Empty()); // Ensure there's a path around (e.g., through (3,0) or (3,2))

        // Act - Give AI enough time to find path, move, and mine
        // Path would be longer due to obstacle
        for (int i = 1; i < 20 * 20 + 5; i++) // Увеличено количество итераций для запаса
        {
            _controller.Update(_player, _gameMap, true, 0L);
        }

        // Assert
        var cell = _gameMap.GetCell(5, 1);
        Assert.That(cell.Resource, Is.Null);
        Assert.That(cell.DisplayChar == '.' || cell.IsWall, Is.True); // Resource should be mined (either empty or built over)
        Assert.That(_gameMap.GetCell(3, 1).IsWall, Is.True); // Wall should still be there
    }

    [Test]
    public void Update_AIPathIsClearedAfterMiningResource()
    {
        // Arrange
        _player.X = 1;
        _player.Y = 1;
        _player.PlayerInventory.Clear();

        ResourceTile stone = new ResourceTile('$', new Color(150, 150, 150, 255), "Stone", 1);
        _gameMap.SetCell(2, 1, MapCell.ResourceCell(stone)); // Resource one step away

        // Act - Run updates to find path and move to resource
        for (int i = 0; i < 2 * 20; i++) 
        {
            _controller.Update(_player, _gameMap, true, 0L);
        }
        
        // Mine the resource and wait a tick
        for (int i = 0; i <= 20; i++)
        {
            _controller.Update(_player, _gameMap, true, 0L);
        }
        
        // Assert
        var pathField = typeof(AIPlayerController).GetField("_path", BindingFlags.NonPublic | BindingFlags.Instance);
        var path = (List<(int, int)>?)pathField!.GetValue(_controller);
        Assert.That(path!.Count, Is.EqualTo(0)); // Path should be cleared
        Assert.That(_player.PlayerInventory.Resources.GetValueOrDefault("Stone", 0), Is.EqualTo(1), "Stone should remain after mining because building requires StoneWall.");

    }

    [Test]
    public void Update_NoResources_AIEventuallyWandersInsteadOfStayingSearching()
    {
        _player.X = 5;
        _player.Y = 5;
        _player.PlayerInventory.Clear();
        _gameMap = new AIMockGameMap(); // Полностью пустая карта

        int startX = _player.X;
        int startY = _player.Y;
        bool hasMoved = false;

        for (int i = 0; i < 20 * 5; i++) // Несколько циклов, чтобы точно дошло до действия
        {
            _controller.Update(_player, _gameMap, true, 0L);
            if (_player.X != startX || _player.Y != startY)
            {
                hasMoved = true;
            }
        }

        Assert.That(hasMoved, Is.True, "ИИ должен выйти из поиска и начать блуждать, если ресурсов нет.");
    }

    [Test]
    public void Update_MinesResourceEvenIfPathWasPreplanned()
    {
        _player.X = 4;
        _player.Y = 4;

        ResourceTile stone = new ResourceTile('$', new Color(150, 150, 150, 255), "Stone", 1);
        _gameMap.SetCell(_player.X, _player.Y, MapCell.ResourceCell(stone));

        // Задаем фиктивный маршрут, чтобы убедиться что добыча приоритетнее
        var pathField = typeof(AIPlayerController).GetField("_path", BindingFlags.NonPublic | BindingFlags.Instance);
        pathField!.SetValue(_controller, new List<(int, int)> { (5, 4), (6, 4) });

        for (int i = 0; i < 40; i++)
        {
            _controller.Update(_player, _gameMap, true, 0L);
        }

        Assert.That(stone.Health, Is.EqualTo(0), "Ресурс должен быть добыт, даже если маршрут уже был построен.");
        Assert.That(_player.PlayerInventory.HasItem("Stone", 1), Is.True);

        var path = (List<(int, int)>?)pathField!.GetValue(_controller);
        Assert.That(path, Is.Not.Null);
        Assert.That(path!.Count, Is.EqualTo(0), "Маршрут должен очищаться после добычи ресурса.");
    }

    [Test]
    public void Update_TargetResourceDisappears_PathAndTargetCleared()
    {
        _player.X = 1;
        _player.Y = 1;
        _player.PlayerInventory.Clear();

        var pathField = typeof(AIPlayerController).GetField("_path", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var targetField = typeof(AIPlayerController).GetField("_targetResourcePosition", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Создаем ресурс и путь к нему
        _gameMap.SetCell(2, 1, MapCell.ResourceCell(new ResourceTile('$', new Color(), "Stone", 1)));

        for (int i = 0; i < 20; i++) { _controller.Update(_player, _gameMap, true, 0L); } // Построить маршрут

        // Удаляем ресурс до следующего шага ИИ
        _gameMap.SetCell(2, 1, MapCell.Empty());

        for (int i = 0; i < 20; i++) { _controller.Update(_player, _gameMap, true, 0L); }

        var path = (List<(int, int)>?)pathField.GetValue(_controller);
        var target = (ValueTuple<int, int>?)targetField.GetValue(_controller);

        Assert.That(path, Is.Not.Null);
        Assert.That(path!.Count, Is.EqualTo(0), "Путь должен сбрасываться, если цель исчезла.");
        Assert.That(target.HasValue, Is.False, "Цель должна сбрасываться, если ресурс пропал.");
    }

    [Test]
    public void HandleShelterLogic_CraftsStoneWallWhenEnoughStone()
    {
        _player.PlayerInventory.Clear();
        _player.PlayerInventory.AddItem("Stone", 2);

        var shelterStateField = typeof(AIPlayerController).GetField("_shelterBuildState", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var shelterStateType = typeof(AIPlayerController).GetNestedType("ShelterBuildState", BindingFlags.NonPublic)!;
        var collectingState = Enum.Parse(shelterStateType, "CollectingResources");
        shelterStateField.SetValue(_controller, collectingState);

        var requiredWallsField = typeof(AIPlayerController).GetField("_requiredShelterWalls", BindingFlags.NonPublic | BindingFlags.Instance)!;
        requiredWallsField.SetValue(_controller, 1);

        var handleMethod = typeof(AIPlayerController).GetMethod("HandleShelterLogic", BindingFlags.NonPublic | BindingFlags.Instance)!;
        bool handled = (bool)handleMethod.Invoke(_controller, new object[] { _player, _gameMap })!;

        Assert.That(handled, Is.True);
        Assert.That(_player.PlayerInventory.HasItem("StoneWall", 1), Is.True);
        Assert.That(_player.PlayerInventory.HasItem("Stone", 1), Is.False);
    }

    [Test]
    public void Update_PathBlocked_ClearsPathAndTarget()
    {
        _player.X = 1;
        _player.Y = 1;
        // Ресурс в (3,1), чтобы ИИ построил к нему маршрут
        _gameMap.SetCell(3, 1, MapCell.ResourceCell(new ResourceTile('$', new Color(), "Stone", 1)));

        // Шаг 1: Запускаем Update один раз, чтобы ИИ нашел ресурс и построил маршрут
        for (int i = 0; i < 20; i++) { _controller.Update(_player, _gameMap, true, 0L); }

        // Проверяем, что маршрут построен
        var pathField = typeof(AIPlayerController).GetField("_path", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var initialPath = (List<(int, int)>?)pathField.GetValue(_controller);
        Assert.That(initialPath, Is.Not.Null);
        Assert.That(initialPath!.Count, Is.GreaterThan(0), "Маршрут должен быть построен");

        // Шаг 2: Блокируем следующий шаг на маршруте
        var nextStep = initialPath[0];
        _gameMap.SetCell(nextStep.Item1, nextStep.Item2, MapCell.Wall());

        // Шаг 3: Запускаем Update еще раз, чтобы ИИ попытался сделать шаг и обнаружил преграду
        for (int i = 0; i < 20; i++) { _controller.Update(_player, _gameMap, true, 0L); }
        
        // Шаг 4: Проверяем, что маршрут и цель сброшены
        var finalPath = (List<(int, int)>?)pathField.GetValue(_controller);
        var targetField = typeof(AIPlayerController).GetField("_targetResourcePosition", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var target = (ValueTuple<int, int>?)targetField.GetValue(_controller);

        Assert.That(finalPath!.Count, Is.EqualTo(0), "Путь должен очищаться, если шаг заблокирован.");
        Assert.That(target.HasValue, Is.False, "Цель должна сбрасываться, если путь не пройти.");
    }

    [Test]
    public void ExecuteNextStep_WithEmptyPath_NoAction()
    {
        var method = typeof(AIPlayerController).GetMethod("ExecuteNextStep", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(_controller, new object[] { _player, _gameMap });
        // Просто проверяем, что метод не бросает исключения при пустом пути
    }

    [Test]
    public void Update_PathPlanningFails_ClearsTargetAndPath()
    {
        var controller = new AIPlayerController((_, _, _, _, _) => null); // Подменяем pathfinder, чтобы вернуть null
        _player.X = 1;
        _player.Y = 1;
        _player.PlayerInventory.Clear();
        _gameMap.SetCell(2, 1, MapCell.ResourceCell(new ResourceTile('$', new Color(), "Stone", 1)));

        for (int i = 0; i < 20; i++) { controller.Update(_player, _gameMap, true, 0L); }

        var pathField = typeof(AIPlayerController).GetField("_path", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var targetField = typeof(AIPlayerController).GetField("_targetResourcePosition", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var path = (List<(int, int)>?)pathField.GetValue(controller);
        var target = (ValueTuple<int, int>?)targetField.GetValue(controller);

        Assert.That(path, Is.Not.Null);
        Assert.That(path!.Count, Is.EqualTo(0), "Путь должен быть пуст, если построение маршрута вернуло null.");
        Assert.That(target.HasValue, Is.False, "Цель должна сбрасываться, если путь построить нельзя.");
    }

    private class FixedRandom : Random
    {
        private readonly int _value;
        public FixedRandom(int value) { _value = value; }
        public override int Next(int maxValue) => _value;
    }

    [Test]
    public void WanderRandomly_UsesRandomDirection()
    {
        var randomField = typeof(AIPlayerController).GetField("_random", BindingFlags.NonPublic | BindingFlags.Instance)!;
        randomField.SetValue(_controller, new FixedRandom(0)); // direction = 0 => dy = -1

        _player.X = 5;
        _player.Y = 5;
        _gameMap.SetCell(5, 4, MapCell.Empty());

        var wanderMethod = typeof(AIPlayerController).GetMethod("WanderRandomly", BindingFlags.NonPublic | BindingFlags.Instance)!;
        wanderMethod.Invoke(_controller, new object[] { _player, _gameMap });

        Assert.That(_player.Y, Is.EqualTo(4));
    }

    [Test]
    public void WanderRandomly_HandlesDifferentDirection()
    {
        var randomField = typeof(AIPlayerController).GetField("_random", BindingFlags.NonPublic | BindingFlags.Instance)!;
        randomField.SetValue(_controller, new FixedRandom(3)); // direction = 3 => dx = 1

        _player.X = 5;
        _player.Y = 5;
        _gameMap.SetCell(6, 5, MapCell.Empty());

        var wanderMethod = typeof(AIPlayerController).GetMethod("WanderRandomly", BindingFlags.NonPublic | BindingFlags.Instance)!;
        wanderMethod.Invoke(_controller, new object[] { _player, _gameMap });

        Assert.That(_player.X, Is.EqualTo(6));
    }

    [Test]
    public void Pathfinder_GeneratesCorrectPath_Simple()
    {
        // Arrange
        _player.X = 1;
        _player.Y = 1;
        _gameMap = new AIMockGameMap(); // Clear map
        // No obstacles

        int targetX = 5;
        int targetY = 1;

        // Act
        List<(int, int)>? path = Pathfinder.FindPath(_gameMap, _player.X, _player.Y, targetX, targetY);

        // Assert
        Assert.That(path, Is.Not.Null);
        // Expected path length is 5 (including start and end)
        // (1,1) -> (2,1) -> (3,1) -> (4,1) -> (5,1)
        Assert.That(path.Count, Is.EqualTo(5), $"Expected path length 5, but was {path.Count}");

        // Print path for debugging
        Console.WriteLine("Generated Path:");
        foreach (var step in path!) // Use ! because we asserted Not.Null
        {
            Console.WriteLine($"  ({step.Item1}, {step.Item2})");
        }

        // Verify path coordinates
        Assert.That(path[0].Item1, Is.EqualTo(1)); Assert.That(path[0].Item2, Is.EqualTo(1));
        Assert.That(path[1].Item1, Is.EqualTo(2)); Assert.That(path[1].Item2, Is.EqualTo(1));
        Assert.That(path[2].Item1, Is.EqualTo(3)); Assert.That(path[2].Item2, Is.EqualTo(1));
        Assert.That(path[3].Item1, Is.EqualTo(4)); Assert.That(path[3].Item2, Is.EqualTo(1));
        Assert.That(path[4].Item1, Is.EqualTo(5)); Assert.That(path[4].Item2, Is.EqualTo(1));
    }

    [Test]
    public void Pathfinder_GeneratesCorrectPath_WithObstacles()
    {
        // Arrange
        _player.X = 1;
        _player.Y = 1;
        _gameMap = new AIMockGameMap(); // Clear map

        // Wall at (3,1)
        _gameMap.SetCell(3, 1, MapCell.Wall()); 

        int targetX = 5;
        int targetY = 1;

        // Act
        List<(int, int)>? path = Pathfinder.FindPath(_gameMap, _player.X, _player.Y, targetX, targetY);

        // Assert
        Assert.That(path, Is.Not.Null);
        // Path (1,1) -> (1,0) -> (2,0) -> (3,0) -> (4,0) -> (5,0) -> (5,1)
        // Length 7
        Assert.That(path.Count, Is.EqualTo(7), $"Expected path length 7, but was {path.Count}");

        Console.WriteLine("Generated Path with Obstacles:");
        foreach (var step in path!) // Use ! because we asserted Not.Null
        {
            Console.WriteLine($"  ({step.Item1}, {step.Item2})");
        }

        // Verify path coordinates
        Assert.That(path[0].Item1, Is.EqualTo(1)); Assert.That(path[0].Item2, Is.EqualTo(1));
        Assert.That(path[1].Item1, Is.EqualTo(2)); Assert.That(path[1].Item2, Is.EqualTo(1));
        Assert.That(path[2].Item1, Is.EqualTo(2)); Assert.That(path[2].Item2, Is.EqualTo(0));
        Assert.That(path[3].Item1, Is.EqualTo(3)); Assert.That(path[3].Item2, Is.EqualTo(0));
        Assert.That(path[4].Item1, Is.EqualTo(4)); Assert.That(path[4].Item2, Is.EqualTo(0));
        Assert.That(path[5].Item1, Is.EqualTo(4)); Assert.That(path[5].Item2, Is.EqualTo(1));
        Assert.That(path[6].Item1, Is.EqualTo(5)); Assert.That(path[6].Item2, Is.EqualTo(1));
    }

    [Test]
    public void Pathfinder_ReturnsNullWhenNoPathExists()
    {
        _player.X = 1;
        _player.Y = 1;
        _gameMap = new AIMockGameMap();

        // Блокируем все соседние клетки
        _gameMap.SetCell(1, 0, MapCell.Wall());
        _gameMap.SetCell(0, 1, MapCell.Wall());
        _gameMap.SetCell(1, 2, MapCell.Wall());
        _gameMap.SetCell(2, 1, MapCell.Wall());

        var path = Pathfinder.FindPath(_gameMap, _player.X, _player.Y, 5, 5);

        Assert.That(path, Is.Null);
    }

    [Test]
    public void Pathfinder_NodeEquals_NonNodeReturnsFalse()
    {
        var nodeType = typeof(Pathfinder).GetNestedType("Node", BindingFlags.NonPublic);
        var node = Activator.CreateInstance(nodeType!, 0, 0, null, 0, 0);
        bool result = (bool)nodeType!.GetMethod("Equals")!.Invoke(node, new object?[] { null })!;
        Assert.That(result, Is.False);
    }

    [Test]
    public void FindNearestResource_FindsClosestResourceWithBFS()
    {
        // Arrange
        _player.X = 1;
        _player.Y = 1;
        _gameMap = new AIMockGameMap();

        // Resource 1: Closest by Manhattan, but behind a wall
        _gameMap.SetCell(2, 1, MapCell.Wall()); // Obstacle
        _gameMap.SetCell(3, 1, MapCell.ResourceCell(new ResourceTile('S', Raylib_cs.Color.DarkBrown, "Stone", 1))); // Target 1

        // Resource 2: Further by Manhattan, but reachable via a path
        _gameMap.SetCell(1, 3, MapCell.ResourceCell(new ResourceTile('S', Raylib_cs.Color.DarkBrown, "Stone", 1))); // Target 2

        // Expected path for Target 1 from (1,1) -> (3,1) with wall at (2,1):
        // (1,1) -> (1,2) -> (2,2) -> (3,2) -> (3,1) = 4 steps
        // Expected path for Target 2 from (1,1) -> (1,3):
        // (1,1) -> (1,2) -> (1,3) = 2 steps

        // Act - Using reflection to call the private method
        MethodInfo method = typeof(AIPlayerController).GetMethod("FindNearestResource", BindingFlags.NonPublic | BindingFlags.Instance)!; // Use BindingFlags.All to include private static
        var nearestResource = ((ValueTuple<int, int>?)method.Invoke(_controller, new object[] { _gameMap, _player.X, _player.Y, "Stone" }))!;

        // Assert
        Assert.That(nearestResource, Is.Not.Null); // Убеждаемся, что nearestResource не null
        Assert.That(nearestResource.Value.Item1, Is.EqualTo(1));
        Assert.That(nearestResource.Value.Item2, Is.EqualTo(3));
    }
}
