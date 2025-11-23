using NUnit.Framework;
using Raylib_cs; // For KeyboardKey

// Manual stub for Player to track move calls
public class TestPlayer : Player
{
    public int LastDx { get; private set; }
    public int LastDy { get; private set; }
    public int MoveCallCount { get; private set; }

    public TestPlayer(int x, int y, char character, Color color) : base(x, y, character, color) { }

    // Override Move to just record the call, not actually change position.
    public override void Move(int dx, int dy, GameMap map)
    {
        LastDx = dx;
        LastDy = dy;
        MoveCallCount++;
        // No base.Move call, as we're isolating controller logic.
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
        _controller.Update(_player, _gameMap);
        Assert.That(_player.MoveCallCount, Is.EqualTo(0));
    }
    
    [Test]
    public void Update_RightKeyPressed_MovesPlayerRight()
    {
        _mockInputService.KeyPressed = (KeyboardKey)262; // KEY_RIGHT
        _controller.Update(_player, _gameMap);
        Assert.That(_player.MoveCallCount, Is.EqualTo(1));
        Assert.That(_player.LastDx, Is.EqualTo(1));
        Assert.That(_player.LastDy, Is.EqualTo(0));
    }

    [Test]
    public void Update_LeftKeyPressed_MovesPlayerLeft()
    {
        _mockInputService.KeyPressed = (KeyboardKey)263; // KEY_LEFT
        _controller.Update(_player, _gameMap);
        Assert.That(_player.MoveCallCount, Is.EqualTo(1));
        Assert.That(_player.LastDx, Is.EqualTo(-1));
        Assert.That(_player.LastDy, Is.EqualTo(0));
    }

    [Test]
    public void Update_UpKeyPressed_MovesPlayerUp()
    {
        _mockInputService.KeyPressed = (KeyboardKey)265; // KEY_UP
        _controller.Update(_player, _gameMap);
        Assert.That(_player.MoveCallCount, Is.EqualTo(1));
        Assert.That(_player.LastDx, Is.EqualTo(0));
        Assert.That(_player.LastDy, Is.EqualTo(-1));
    }

    [Test]
    public void Update_DownKeyPressed_MovesPlayerDown()
    {
        _mockInputService.KeyPressed = (KeyboardKey)264; // KEY_DOWN
        _controller.Update(_player, _gameMap);
        Assert.That(_player.MoveCallCount, Is.EqualTo(1));
        Assert.That(_player.LastDx, Is.EqualTo(0));
        Assert.That(_player.LastDy, Is.EqualTo(1));
    }
}
