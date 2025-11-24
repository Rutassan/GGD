using NUnit.Framework;
using Raylib_cs; // For KeyboardKey

// Manual stub for Player to track move calls
    public class TestPlayer : Player
    {
        public int LastDx { get; private set; }
        public int LastDy { get; private set; }
        public int MoveCallCount { get; private set; }
        public int MineCallCount { get; private set; }

        public TestPlayer(int x, int y, char character, Color color) : base(x, y, character, color) { }

    // Override Move to just record the call, not actually change position.
        public override bool Move(int dx, int dy, GameMap map)
        {
            LastDx = dx;
            LastDy = dy;
            MoveCallCount++;
            return true; // Always return true for the test stub
        }

        public new void Mine(GameMap map)
        {
            MineCallCount++;
            base.Mine(map);
        }
    }

// Manual stub for GameMap
    public class TestGameMap : GameMap
    {
        public TestGameMap() : base() { }

    // Override IsWall to always return false for simplicity in controller tests
    public override bool IsWall(int x, int y)
    {
        return false; 
    }
}

// Manual mock for IInputService
public class MockInputService : IInputService
{
    public KeyboardKey? KeyPressed { get; set; } = null;

    public bool IsKeyPressed(KeyboardKey key)
    {
        return KeyPressed == key;
    }
}

[TestFixture]
public class ManualPlayerControllerTests
{
    private ManualPlayerController _controller;
    private TestPlayer _player;
    private TestGameMap _gameMap;
    private MockInputService _mockInputService;

    [SetUp]
    public void SetUp()
    {
        _mockInputService = new MockInputService();
        _controller = new ManualPlayerController(_mockInputService);
        _player = new TestPlayer(0, 0, '@', new Color(0, 255, 0, 255)); // Использовать new Color(0, 255, 0, 255) для GREEN
        _gameMap = new TestGameMap();
    }

    [Test]
    public void Update_NoKeyPressed_DoesNotMovePlayer()
    {
        _mockInputService.KeyPressed = null; // No key pressed
        _controller.Update(_player, _gameMap, true, 0L);
        Assert.That(_player.MoveCallCount, Is.EqualTo(0));
    }
    
    [Test]
    public void Update_RightKeyPressed_MovesPlayerRight()
    {
        _mockInputService.KeyPressed = (KeyboardKey)262;
        _controller.Update(_player, _gameMap, true, 0L);
        Assert.That(_player.MoveCallCount, Is.EqualTo(1));
        Assert.That(_player.LastDx, Is.EqualTo(1));
        Assert.That(_player.LastDy, Is.EqualTo(0));
    }

    [Test]
    public void Update_LeftKeyPressed_MovesPlayerLeft()
    {
        _mockInputService.KeyPressed = (KeyboardKey)263;
        _controller.Update(_player, _gameMap, true, 0L);
        Assert.That(_player.MoveCallCount, Is.EqualTo(1));
        Assert.That(_player.LastDx, Is.EqualTo(-1));
        Assert.That(_player.LastDy, Is.EqualTo(0));
    }

    [Test]
    public void Update_UpKeyPressed_MovesPlayerUp()
    {
        _mockInputService.KeyPressed = (KeyboardKey)265;
        _controller.Update(_player, _gameMap, true, 0L);
        Assert.That(_player.MoveCallCount, Is.EqualTo(1));
        Assert.That(_player.LastDx, Is.EqualTo(0));
        Assert.That(_player.LastDy, Is.EqualTo(-1));
    }

    [Test]
    public void Update_DownKeyPressed_MovesPlayerDown()
    {
        _mockInputService.KeyPressed = (KeyboardKey)264;
        _controller.Update(_player, _gameMap, true, 0L);
        Assert.That(_player.MoveCallCount, Is.EqualTo(1));
        Assert.That(_player.LastDx, Is.EqualTo(0));
        Assert.That(_player.LastDy, Is.EqualTo(1));
    }

    [Test]
    public void Update_MKeyPressed_MinesResource()
    {
        var player = new Player(1, 1, '@', new Color(0, 255, 0, 255));
        var map = new AIMockGameMap();
        var stone = new ResourceTile('$', new Color(), "Stone", 1);
        map.SetCell(1, 1, MapCell.ResourceCell(stone));

        _mockInputService.KeyPressed = (KeyboardKey)77;
        _controller.Update(player, map, true, 0L);

        Assert.That(stone.Health, Is.EqualTo(0));
        Assert.That(player.PlayerInventory.HasItem("Stone", 1), Is.True);
        Assert.That(map.GetCell(1, 1).IsWall, Is.False);
        Assert.That(map.GetCell(1, 1).Resource, Is.Null);
    }

    [Test]
    public void Update_BKeyPressed_BuildsWallAndConsumesStone()
    {
        var player = new Player(2, 2, '@', new Color(0, 255, 0, 255));
        var map = new AIMockGameMap();
        map.SetCell(2, 2, MapCell.Empty());
        player.PlayerInventory.AddItem("Stone", 1);

        _mockInputService.KeyPressed = (KeyboardKey)66;
        _controller.Update(player, map, true, 0L);

        Assert.That(player.PlayerInventory.HasItem("Stone", 1), Is.False);
        Assert.That(map.GetCell(2, 2).IsWall, Is.True);
    }

    [Test]
    public void Update_FKeyPressed_BuildsDoorAndConsumesStone()
    {
        var player = new Player(2, 2, '@', new Color(0, 255, 0, 255));
        var map = new AIMockGameMap();
        map.SetCell(2, 2, MapCell.Empty());
        player.PlayerInventory.AddItem("Stone", 1);

        _mockInputService.KeyPressed = (KeyboardKey)70;
        _controller.Update(player, map, true, 0L);

        Assert.That(map.HasDoorAt(2, 2), Is.True);
        Assert.That(player.PlayerInventory.HasItem("Stone", 1), Is.False);
    }

    [Test]
    public void Update_OKeyPressed_TogglesAdjacentDoor()
    {
        var player = new Player(2, 2, '@', new Color(0, 255, 0, 255));
        var map = new AIMockGameMap();
        map.PlaceDoor(2, 1); // Place a door above the player

        _mockInputService.KeyPressed = (KeyboardKey)79;
        _controller.Update(player, map, true, 0L);

        Assert.That(map.GetCell(2, 1).Door?.IsOpen, Is.True);
    }
}
