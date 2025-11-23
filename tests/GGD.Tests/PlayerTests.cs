using NUnit.Framework;
using Raylib_cs;

namespace GGD.Tests;

public class PlayerTests
{
    [Test]
    public void Player_Constructor_ShouldSetInitialPosition()
    {
        // Arrange
        var player = new Player(10, 20, '@', Color.White);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(player.X, Is.EqualTo(10));
            Assert.That(player.Y, Is.EqualTo(20));
        });
    }

    [Test]
    public void Move_WhenDestinationIsFloor_ShouldUpdatePosition()
    {
        var player = new Player(1, 1, '@', Color.White);
        var map = new GameMap();

        player.Move(1, 0, map);

        Assert.Multiple(() =>
        {
            Assert.That(player.X, Is.EqualTo(2));
            Assert.That(player.Y, Is.EqualTo(1));
        });
    }

    [Test]
    public void Move_WhenDestinationIsWall_ShouldKeepPosition()
    {
        var player = new Player(1, 1, '@', Color.White);
        var map = new GameMap();

        player.Move(-1, 0, map);

        Assert.Multiple(() =>
        {
            Assert.That(player.X, Is.EqualTo(1));
            Assert.That(player.Y, Is.EqualTo(1));
        });
    }
}
