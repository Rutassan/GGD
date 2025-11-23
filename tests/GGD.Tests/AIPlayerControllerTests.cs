using NUnit.Framework;
using Raylib_cs;
using System;
using System.Collections.Generic; // Для GetValueOrDefault

// MockGameMap for AIPlayerControllerTests
public class AIMockGameMap : GameMap
{
    public MapCell[,] MockCells = new MapCell[MapHeight, MapWidth];

    public AIMockGameMap()
    {
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
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return true; 
        return MockCells[y, x].IsWall;
    }

    public override MapCell GetCell(int x, int y)
    {
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return MapCell.Wall(); 
        return MockCells[y, x];
    }

    public override void SetCell(int x, int y, MapCell newCell)
    {
        if (x >= 0 && x < MapWidth && y >= 0 && y < MapHeight) 
        {
            MockCells[y, x] = newCell;
        }
    }
}


[TestFixture]
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
        _gameMap = new AIMockGameMap();
    }

    [Test]
    public void Update_BeforeDelayExpires_DoesNotMovePlayerOrAct()
    {
        // Arrange
        ResourceTile stone = new ResourceTile('$', new Color(), "Stone", 1);
        _gameMap.SetCell(_player.X, _player.Y, MapCell.ResourceCell(stone));

        for (int i = 0; i < 19; i++) // 19 calls, so counter is 19
        {
            _controller.Update(_player, _gameMap);
        }
        Assert.That(_player.X, Is.EqualTo(1));
        Assert.That(_player.Y, Is.EqualTo(1));
        Assert.That(_player.PlayerInventory.Resources.Count, Is.EqualTo(0)); // No mining happened
        
        // Исправлено: Проверяем, что Resource не null перед обращением к Health
        Assert.That(_gameMap.GetCell(_player.X, _player.Y).Resource, Is.Not.Null);
        Assert.That(_gameMap.GetCell(_player.X, _player.Y).Resource!.Health, Is.EqualTo(1)); // Resource not mined
    }

    [Test]
    public void Update_AfterDelayExpires_AIAttemptsToMineResourceAtCurrentPosition()
    {
        // Arrange
        ResourceTile stone = new ResourceTile('$', new Color(150, 150, 150, 255), "Stone", 1); // Добавил цвет
        _gameMap.SetCell(_player.X, _player.Y, MapCell.ResourceCell(stone));
        
        // Act
        for (int i = 0; i < 20; i++) // 20 calls, action should occur
        {
            _controller.Update(_player, _gameMap);
        }

        // Assert
        Assert.That(stone.Health, Is.EqualTo(0)); // Resource should be mined
        Assert.That(_player.PlayerInventory.HasItem("Stone", 1), Is.True);
        Assert.That(_gameMap.GetCell(_player.X, _player.Y).DisplayChar, Is.EqualTo('.')); // Cell should be empty
    }

    [Test]
    public void Update_AfterDelayExpires_AIAttemptsToBuildIfHasResourcesAndEmptyCell()
    {
        // Arrange
        _player.PlayerInventory.AddItem("Stone", 1); // Give AI a stone
        _gameMap.SetCell(_player.X, _player.Y, MapCell.Empty()); // Ensure current cell is empty

        // Act
        for (int i = 0; i < 20; i++) // 20 calls, action should occur
        {
            _controller.Update(_player, _gameMap);
        }

        // Assert
        Assert.That(_player.PlayerInventory.HasItem("Stone", 0), Is.False); // Stone should be consumed
        Assert.That(_gameMap.GetCell(_player.X, _player.Y).DisplayChar, Is.EqualTo('#')); // Cell should be a wall
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
            _controller.Update(_player, _gameMap);
        }

        // Assert - Player should have moved
        // Исправлено: синтаксис Or в NUnit
        Assert.That(_player.X, Is.Not.EqualTo(initialX).Or.Not.EqualTo(_player.Y)); // Проверяем, что изменилась хотя бы одна координата
    }
}
