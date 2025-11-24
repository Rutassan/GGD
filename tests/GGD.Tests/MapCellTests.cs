using NUnit.Framework;
using Raylib_cs;

[TestFixture]
public class MapCellTests
{
    [Test]
    public void BuiltStoneWall_HasBlockAppearanceAndIsWall()
    {
        MapCell wall = MapCell.BuiltStoneWall();

        Assert.That(wall.DisplayChar, Is.EqualTo('█'));
        Assert.That(wall.IsWall, Is.True);
        Assert.That(wall.CellColor, Is.EqualTo(new Color(100, 100, 100, 255)));
    }

    [Test]
    public void RenderColor_ReturnsDoorColorWhenDoorPresent()
    {
        DoorTile door = new DoorTile(false);
        MapCell cell = MapCell.DoorCell(door);

        Assert.That(cell.HasDoor, Is.True);
        Assert.That(cell.RenderColor, Is.EqualTo(door.CurrentColor));
        Assert.That(cell.RenderChar, Is.EqualTo(door.CurrentDisplayChar));
    }

    [Test]
    public void RenderColor_ReturnsCellColorWhenDoorMissing()
    {
        MapCell cell = MapCell.Empty();
        Assert.That(cell.RenderColor, Is.EqualTo(cell.CellColor));
        Assert.That(cell.RenderChar, Is.EqualTo(cell.DisplayChar));
    }
}
