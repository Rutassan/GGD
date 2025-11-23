using NUnit.Framework;

namespace GGD.Tests;

public class GameMapTests
{
    [Test]
    public void IsWall_ShouldReportWallsAroundBorders()
    {
        var map = new GameMap();

        Assert.Multiple(() =>
        {
            Assert.That(map.IsWall(0, 0), Is.True);
            Assert.That(map.IsWall(19, 0), Is.True);
            Assert.That(map.IsWall(0, 9), Is.True);
            Assert.That(map.IsWall(19, 9), Is.True);
        });
    }

    [Test]
    public void IsWall_ShouldReportFloorAsPassable()
    {
        var map = new GameMap();
        Assert.That(map.IsWall(1, 1), Is.False);
    }

    [Test]
    public void IsWall_ShouldTreatOutOfBoundsAsWall()
    {
        var map = new GameMap();
        Assert.Multiple(() =>
        {
            Assert.That(map.IsWall(-1, -1), Is.True);
            Assert.That(map.IsWall(20, 5), Is.True);
            Assert.That(map.IsWall(5, 10), Is.True);
        });
    }
}
