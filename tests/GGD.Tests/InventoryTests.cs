using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class InventoryTests
{
    private Inventory _inventory;

    [SetUp]
    public void SetUp()
    {
        _inventory = new Inventory();
    }

    [Test]
    public void AddItem_AddsNewResourceCorrectly()
    {
        // Act
        _inventory.AddItem("Stone", 10);

        // Assert
        Assert.That(_inventory.Resources.ContainsKey("Stone"), Is.True);
        Assert.That(_inventory.Resources["Stone"], Is.EqualTo(10));
    }

    [Test]
    public void AddItem_IncrementsExistingResourceCorrectly()
    {
        // Arrange
        _inventory.AddItem("Wood", 5);

        // Act
        _inventory.AddItem("Wood", 3);

        // Assert
        Assert.That(_inventory.Resources.ContainsKey("Wood"), Is.True);
        Assert.That(_inventory.Resources["Wood"], Is.EqualTo(8));
    }

    [Test]
    public void RemoveItem_RemovesResourceCorrectly()
    {
        // Arrange
        _inventory.AddItem("Iron", 7);

        // Act
        bool result = _inventory.RemoveItem("Iron", 7);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_inventory.Resources.ContainsKey("Iron"), Is.False);
    }

    [Test]
    public void RemoveItem_DecrementsResourceCorrectly()
    {
        // Arrange
        _inventory.AddItem("Copper", 10);

        // Act
        bool result = _inventory.RemoveItem("Copper", 4);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_inventory.Resources.ContainsKey("Copper"), Is.True);
        Assert.That(_inventory.Resources["Copper"], Is.EqualTo(6));
    }

    [Test]
    public void RemoveItem_ReturnsFalse_WhenResourceNotEnough()
    {
        // Arrange
        _inventory.AddItem("Silver", 2);

        // Act
        bool result = _inventory.RemoveItem("Silver", 5);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(_inventory.Resources.ContainsKey("Silver"), Is.True);
        Assert.That(_inventory.Resources["Silver"], Is.EqualTo(2));
    }

    [Test]
    public void RemoveItem_ReturnsFalse_WhenResourceNotFound()
    {
        // Act
        bool result = _inventory.RemoveItem("Gold", 1);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(_inventory.Resources.ContainsKey("Gold"), Is.False);
    }

    [Test]
    public void HasItem_ReturnsTrue_WhenResourceExistsAndEnough()
    {
        // Arrange
        _inventory.AddItem("Stone", 15);

        // Act
        bool result = _inventory.HasItem("Stone", 10);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void HasItem_ReturnsFalse_WhenResourceExistsButNotEnough()
    {
        // Arrange
        _inventory.AddItem("Stone", 5);

        // Act
        bool result = _inventory.HasItem("Stone", 10);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasItem_ReturnsFalse_WhenResourceDoesNotExist()
    {
        // Act
        bool result = _inventory.HasItem("Iron", 1);

        // Assert
        Assert.That(result, Is.False);
    }
}
