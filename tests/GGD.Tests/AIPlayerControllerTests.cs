using NUnit.Framework;
using Raylib_cs; // For Color
using System;

// Re-using TestPlayer from ManualPlayerControllerTests (assuming it's defined globally or in a common test helper)
// If not, it needs to be included here or referenced. For now, assuming it's available.
// public class TestPlayer : Player
// {
//     public int LastDx { get; private set; }
//     public int LastDy { get; private set; }
//     public int MoveCallCount { get; private set; }

//     public TestPlayer(int x, int y, char character, Color color) : base(x, y, character, color) { }

//     public override void Move(int dx, int dy, GameMap map)
//     {
//         LastDx = dx;
//         LastDy = dy;
//         MoveCallCount++;
//     }
// }

// Specialized TestGameMap for AIControllerTests to simulate walls
public class AITestGameMap : GameMap
{
    private bool _alwaysWall;

    public AITestGameMap(bool alwaysWall = false) : base() 
    {
        _alwaysWall = alwaysWall;
    }

    public override bool IsWall(int x, int y)
    {
        if (_alwaysWall) return true;
        // For more complex tests, you could define a small map here
        return false; 
    }
}


[TestFixture]
public class AIPlayerControllerTests
{
    private AIPlayerController _controller;
    private TestPlayer _player; // Using the TestPlayer stub
    private AITestGameMap _gameMap;

    [SetUp]
    public void SetUp()
    {
        _controller = new AIPlayerController();
        _player = new TestPlayer(0, 0, '@', new Color(0, 121, 241, 255)); // Использовать new Color(0, 121, 241, 255) для BLUE
        _gameMap = new AITestGameMap();
    }

    [Test]
    public void Update_BeforeDelayExpires_DoesNotMovePlayer()
    {
        for (int i = 0; i < 19; i++) 
        {
            _controller.Update(_player, _gameMap);
        }
        Assert.That(_player.MoveCallCount, Is.EqualTo(0));
    }

    [Test]
    public void Update_AfterDelayExpires_MovesPlayer()
    {
        for (int i = 0; i < 20; i++) 
        {
            _controller.Update(_player, _gameMap);
        }
        Assert.That(_player.MoveCallCount, Is.EqualTo(1));
        Assert.That(_player.LastDx + _player.LastDy, Is.Not.EqualTo(0));
    }

    [Test]
    public void Update_WhenTryingToMoveIntoWall_PlayerDoesNotMove()
    {
        _gameMap = new AITestGameMap(true); 

        for (int i = 0; i < 20; i++) 
        {
            _controller.Update(_player, _gameMap);
        }

        Assert.That(_player.MoveCallCount, Is.EqualTo(1)); 
    }
}
