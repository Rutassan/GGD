using NUnit.Framework;
using Raylib_cs;

[TestFixture]
public class ResourceTileTests
{
    [Test]
    public void ResourceTile_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        char displayChar = '$';
        Color tileColor = new Color(150, 150, 150, 255);
        string resourceType = "Stone";
        int health = 5;

        // Act
        ResourceTile resourceTile = new ResourceTile(displayChar, tileColor, resourceType, health);

        // Assert
        Assert.That(resourceTile.DisplayChar, Is.EqualTo(displayChar));
        Assert.That(resourceTile.TileColor.R, Is.EqualTo(tileColor.R));
        Assert.That(resourceTile.TileColor.G, Is.EqualTo(tileColor.G));
        Assert.That(resourceTile.TileColor.B, Is.EqualTo(tileColor.B));
        Assert.That(resourceTile.TileColor.A, Is.EqualTo(tileColor.A));
        Assert.That(resourceTile.ResourceType, Is.EqualTo(resourceType));
        Assert.That(resourceTile.Health, Is.EqualTo(health));
    }
}
