using System;
using NUnit.Framework;
using Raylib_cs;

// MockGameMap to simulate game map interactions for Player tests
public class MockGameMap : GameMap
{
    public MapCell[,] MockCells = new MapCell[MapHeight, MapWidth];

    public MockGameMap()
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
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return MapCell.Wall(); // Out of bounds
        return MockCells[y, x];
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
public class PlayerTests
{
    private Player _player;
    private MockGameMap _gameMap;

    [SetUp]
    public void Setup()
    {
        _player = new Player(1, 1, '@', new Color(0, 255, 0, 255));
        _gameMap = new MockGameMap();
    }

    [Test]
    public void Move_ValidMove_UpdatesPlayerPosition()
    {
        _player.Move(1, 0, _gameMap);
        Assert.That(_player.X, Is.EqualTo(2));
        Assert.That(_player.Y, Is.EqualTo(1));
    }

    [Test]
    public void Move_IntoWall_DoesNotUpdatePlayerPosition()
    {
        _gameMap.MockCells[1, 2] = MapCell.Wall(); // Set a wall at (2,1)
        _player.Move(1, 0, _gameMap);
        Assert.That(_player.X, Is.EqualTo(1));
        Assert.That(_player.Y, Is.EqualTo(1));
    }

    // New tests for Mine method
    [Test]
    public void Mine_ResourceWithHealth_DecrementsHealth()
    {
        // Arrange
        ResourceTile stone = new ResourceTile('$', new Color(150, 150, 150, 255), "Stone", 2);
        _gameMap.SetCell(_player.X, _player.Y, MapCell.ResourceCell(stone));

        // Act
        _player.Mine(_gameMap);

        // Assert
        Assert.That(stone.Health, Is.EqualTo(1));
        Assert.That(_player.PlayerInventory.HasItem("Stone", 1), Is.False); // Not added yet
    }

    [Test]
    public void Mine_ResourceDepleted_AddsItemToInventoryAndReplacesCell()
    {
        // Arrange
        ResourceTile stone = new ResourceTile('$', new Color(150, 150, 150, 255), "Stone", 1);
        _gameMap.SetCell(_player.X, _player.Y, MapCell.ResourceCell(stone));
        
        // Act
        _player.Mine(_gameMap); // Deplete resource

        // Assert
        Assert.That(stone.Health, Is.EqualTo(0));
        Assert.That(_player.PlayerInventory.HasItem("Stone", 1), Is.True);
        Assert.That(_gameMap.GetCell(_player.X, _player.Y).DisplayChar, Is.EqualTo('.')); // Should be empty
    }

    [Test]
    public void Mine_NoResourceAtLocation_DoesNothing()
    {
        // Arrange
        _gameMap.SetCell(_player.X, _player.Y, MapCell.Empty());

        // Act
        _player.Mine(_gameMap);

        // Assert
        Assert.That(_player.PlayerInventory.Resources.Count, Is.EqualTo(0));
        Assert.That(_gameMap.GetCell(_player.X, _player.Y).DisplayChar, Is.EqualTo('.')); // Remains empty
    }

    // New tests for Build method
    [Test]
    public void Build_WithEnoughResourcesAndEmptyCell_BuildsWallAndRemovesResource()
    {
        // Arrange
        _player.PlayerInventory.AddItem("Stone", 5);
        // Act
        bool result = _player.Build(_gameMap, "Stone", _player.X, _player.Y);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_player.PlayerInventory.HasItem("Stone", 4), Is.True); // 1 less stone
        Assert.That(_gameMap.GetCell(_player.X, _player.Y).DisplayChar, Is.EqualTo('#')); // Cell is now a wall
        Assert.That(_gameMap.GetCell(_player.X, _player.Y).IsWall, Is.True);
    }

    [Test]
    public void Build_WithoutEnoughResources_DoesNotBuildAndReturnsFalse()
    {
        // Arrange
        // _player.PlayerInventory.AddItem("Stone", 0); // No stone (this line actually adds "Stone" with 0, so HasItem returns True)
        // Ensure "Stone" is not in inventory at all, or has less than 1.
        if (_player.PlayerInventory.Resources.ContainsKey("Stone")) {
            _player.PlayerInventory.RemoveItem("Stone", _player.PlayerInventory.Resources["Stone"]);
        }
        // Act
        bool result = _player.Build(_gameMap, "Stone", _player.X, _player.Y);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(_player.PlayerInventory.Resources.ContainsKey("Stone"), Is.False); // Should not have "Stone" in inventory
        Assert.That(_gameMap.GetCell(_player.X, _player.Y).DisplayChar, Is.EqualTo('.')); // Cell remains empty
    }

    [Test]
    public void Build_OnOccupiedCell_DoesNotBuildAndReturnsFalse()
    {
        // Arrange
        _player.PlayerInventory.AddItem("Stone", 1);
        _gameMap.SetCell(_player.X, _player.Y, MapCell.Wall()); // Cell is a wall

        // Act
        bool result = _player.Build(_gameMap, "Stone", _player.X, _player.Y);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(_player.PlayerInventory.HasItem("Stone", 1), Is.True); // Stone not consumed
        Assert.That(_gameMap.GetCell(_player.X, _player.Y).DisplayChar, Is.EqualTo('#')); // Cell remains wall
    }

    [Test]
    public void UpdateHunger_DecreasesOverTime()
    {
        int initial = _player.Hunger;
        for (int i = 0; i < Player.MaxHunger; i++)
        {
            _player.UpdateHunger();
        }
        Assert.That(_player.Hunger, Is.LessThan(initial));
    }

    [Test]
    public void Eat_RestoresHungerAndConsumesBerry()
    {
        for (int i = 0; i < 300; i++)
        {
            _player.UpdateHunger();
        }

        int before = _player.Hunger;
        _player.PlayerInventory.AddItem("Berry", 1);
        bool consumed = _player.Eat("Berry");

        Assert.That(consumed, Is.True);
        Assert.That(_player.PlayerInventory.HasItem("Berry", 1), Is.False);
        Assert.That(_player.Hunger, Is.EqualTo(Math.Min(Player.MaxHunger, before + 30)));
    }

    [Test]
    public void BuildDoor_WithStonePlacesDoor()
    {
        _player.PlayerInventory.AddItem("Stone", 1);
        _gameMap.SetCell(_player.X, _player.Y, MapCell.Empty());

        bool result = _player.BuildDoor(_gameMap, _player.X, _player.Y);

        Assert.That(result, Is.True);
        Assert.That(_player.PlayerInventory.HasItem("Stone", 1), Is.False);
        Assert.That(_gameMap.HasDoorAt(_player.X, _player.Y), Is.True);
    }
}
