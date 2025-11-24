using NUnit.Framework;
using Raylib_cs;

[TestFixture]
public class MapCellTests
{
    [Test]
    public void MapCell_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        char displayChar = 'X';
        Color cellColor = new Color(255, 0, 0, 255);
        bool isWall = true;
        ResourceTile resource = new ResourceTile('$', new Color(100, 100, 100, 255), "Gold", 1);

        // Act
        MapCell mapCell = new MapCell(displayChar, cellColor, isWall, resource);

        // Assert
        Assert.That(mapCell.DisplayChar, Is.EqualTo(displayChar));
        Assert.That(mapCell.CellColor.R, Is.EqualTo(cellColor.R));
        Assert.That(mapCell.CellColor.G, Is.EqualTo(cellColor.G));
        Assert.That(mapCell.CellColor.B, Is.EqualTo(cellColor.B));
        Assert.That(mapCell.CellColor.A, Is.EqualTo(cellColor.A));
        Assert.That(mapCell.IsWall, Is.EqualTo(isWall));
        Assert.That(mapCell.Resource, Is.EqualTo(resource));
    }

    [Test]
    public void MapCell_Constructor_WithNullResource_SetsResourceToNull()
    {
        // Arrange
        char displayChar = 'X';
        Color cellColor = new Color(255, 0, 0, 255);
        bool isWall = true;

        // Act
        MapCell mapCell = new MapCell(displayChar, cellColor, isWall, null);

        // Assert
        Assert.That(mapCell.Resource, Is.Null);
    }

    [Test]
    public void Empty_FactoryMethod_ReturnsCorrectMapCell()
    {
        // Act
        MapCell emptyCell = MapCell.Empty();

        // Assert
        Assert.That(emptyCell.DisplayChar, Is.EqualTo('.'));
        Assert.That(emptyCell.CellColor.R, Is.EqualTo(80)); // DARKGRAY R
        Assert.That(emptyCell.CellColor.G, Is.EqualTo(80)); // DARKGRAY G
        Assert.That(emptyCell.CellColor.B, Is.EqualTo(80)); // DARKGRAY B
        Assert.That(emptyCell.CellColor.A, Is.EqualTo(255)); // DARKGRAY A
        Assert.That(emptyCell.IsWall, Is.False);
        Assert.That(emptyCell.Resource, Is.Null);
    }

    [Test]
    public void Wall_FactoryMethod_ReturnsCorrectMapCell()
    {
        // Act
        MapCell wallCell = MapCell.Wall();

        // Assert
        Assert.That(wallCell.DisplayChar, Is.EqualTo('#'));
        Assert.That(wallCell.CellColor.R, Is.EqualTo(121)); // GRAY R
        Assert.That(wallCell.CellColor.G, Is.EqualTo(121)); // GRAY G
        Assert.That(wallCell.CellColor.B, Is.EqualTo(121)); // GRAY B
        Assert.That(wallCell.CellColor.A, Is.EqualTo(255)); // GRAY A
        Assert.That(wallCell.IsWall, Is.True);
        Assert.That(wallCell.Resource, Is.Null);
    }

    [Test]
    public void ResourceCell_FactoryMethod_ReturnsCorrectMapCell()
    {
        // Arrange
        ResourceTile resource = new ResourceTile('^', new Color(200, 200, 0, 255), "Iron", 2);

        // Act
        MapCell resourceCell = MapCell.ResourceCell(resource);

        // Assert
        Assert.That(resourceCell.DisplayChar, Is.EqualTo(resource.DisplayChar));
        Assert.That(resourceCell.CellColor.R, Is.EqualTo(resource.TileColor.R));
        Assert.That(resourceCell.CellColor.G, Is.EqualTo(resource.TileColor.G));
        Assert.That(resourceCell.CellColor.B, Is.EqualTo(resource.TileColor.B));
        Assert.That(resourceCell.CellColor.A, Is.EqualTo(resource.TileColor.A));
        Assert.That(resourceCell.IsWall, Is.False);
        Assert.That(resourceCell.Resource, Is.EqualTo(resource));
    }

    [Test]
    public void DoorCell_ReflectsDoorState()
    {
        DoorTile door = new DoorTile(false);
        MapCell doorCell = MapCell.DoorCell(door);

        Assert.That(doorCell.HasDoor, Is.True);
        Assert.That(doorCell.IsWall, Is.True);
        Assert.That(doorCell.RenderChar, Is.EqualTo(door.ClosedChar));

        door.Toggle();
        Assert.That(doorCell.IsWall, Is.False);
        Assert.That(doorCell.RenderChar, Is.EqualTo(door.OpenChar));
    }
}
