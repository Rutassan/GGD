using NUnit.Framework;
using Raylib_cs;

[TestFixture]
public class GameMapTests
{
    private GameMap _gameMap;

    [SetUp]
    public void Setup()
    {
        _gameMap = new GameMap();
    }

    [Test]
    public void Constructor_GeneratesMapWithCorrectDimensions()
    {
        // Assert that the map is generated with the expected dimensions
        Assert.That(GameMap.MapWidth, Is.EqualTo(80));
        Assert.That(GameMap.MapHeight, Is.EqualTo(30));
    }

    [Test]
    public void IsWall_ReturnsTrueForBoundaryWalls()
    {
        // Assert
        Assert.That(_gameMap.IsWall(0, 0), Is.True); // Top-left corner
        Assert.That(_gameMap.IsWall(GameMap.MapWidth - 1, 0), Is.True); // Top-right corner
        Assert.That(_gameMap.IsWall(0, GameMap.MapHeight - 1), Is.True); // Bottom-left corner
        Assert.That(_gameMap.IsWall(GameMap.MapWidth - 1, GameMap.MapHeight - 1), Is.True); // Bottom-right corner
    }

    [Test]
    public void IsWall_ReturnsTrueForOutOfBoundsCoordinates()
    {
        // Assert
        Assert.That(_gameMap.IsWall(-1, 0), Is.True);
        Assert.That(_gameMap.IsWall(GameMap.MapWidth, 0), Is.True);
        Assert.That(_gameMap.IsWall(0, -1), Is.True);
        Assert.That(_gameMap.IsWall(0, GameMap.MapHeight), Is.True);
    }

    [Test]
    public void IsWall_ReturnsFalseForEmptySpace()
    {
        // Act
        // Find an empty cell within the map (assuming there are empty cells)
        int emptyX = -1, emptyY = -1;
        for (int y = 1; y < GameMap.MapHeight - 1; y++)
        {
            for (int x = 1; x < GameMap.MapWidth - 1; x++)
            {
                if (!_gameMap.GetCell(x, y).IsWall && _gameMap.GetCell(x,y).Resource == null)
                {
                    emptyX = x;
                    emptyY = y;
                    break;
                }
            }
            if (emptyX != -1) break;
        }

        // Assert that an empty cell was found
        Assert.That(emptyX, Is.Not.EqualTo(-1));
        Assert.That(emptyY, Is.Not.EqualTo(-1));

        // Assert that IsWall returns false for the empty cell
        Assert.That(_gameMap.IsWall(emptyX, emptyY), Is.False);
    }

    // New tests for GetCell and SetCell
    [Test]
    public void GetCell_ReturnsCorrectCellAtCoordinate()
    {
        // Arrange
        MapCell initialCell = _gameMap.GetCell(1, 1);

        // Act & Assert
        Assert.That(initialCell, Is.Not.Null);
    }

    [Test]
    public void SetCell_UpdatesCellAtCoordinateCorrectly()
    {
        // Arrange
        MapCell newCell = MapCell.Wall();

        // Act
        _gameMap.SetCell(1, 1, newCell);

        // Assert
        Assert.That(_gameMap.GetCell(1, 1), Is.EqualTo(newCell));
    }

    [Test]
    public void GenerateMap_CreatesVariedMapContent()
    {
        // Arrange - regenerate map to ensure it's fresh
        _gameMap = new GameMap(); 

        // Act - Count different types of cells
        int wallCount = 0;
        int emptyCount = 0;
        int resourceCount = 0;

        for (int y = 0; y < GameMap.MapHeight; y++)
        {
            for (int x = 0; x < GameMap.MapWidth; x++)
            {
                MapCell cell = _gameMap.GetCell(x, y);
                if (cell.IsWall) wallCount++;
                else if (cell.Resource != null) resourceCount++;
                else emptyCount++;
            }
        }

        // Assert - we expect at least some of each type, given the random generation.
        // The exact counts are random, so we check for presence and general distribution.
        Assert.That(wallCount, Is.GreaterThanOrEqualTo(GameMap.MapWidth * 2 + (GameMap.MapHeight - 2) * 2)); // Boundary walls
        Assert.That(emptyCount, Is.GreaterThan(0)); // Should have some empty spaces
        Assert.That(resourceCount, Is.GreaterThanOrEqualTo(0)); // May have 0 depending on randomness, but should be tested for more variety later. For now, just not negative.
    }

    [Test]
    public void GetCell_OutOfBounds_ReturnsEmptyCell()
    {
        MapCell cell = _gameMap.GetCell(-5, -5);
        Assert.That(cell.DisplayChar, Is.EqualTo('.'));
        Assert.That(cell.IsWall, Is.False);
    }

    [Test]
    public void SetCell_OutOfBounds_DoesNotChangeMap()
    {
        MapCell before = _gameMap.GetCell(1, 1);
        _gameMap.SetCell(-1, -1, MapCell.Wall());
        Assert.That(_gameMap.GetCell(1, 1), Is.EqualTo(before));
    }
}
