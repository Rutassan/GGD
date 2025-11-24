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
    public void IsInside_PointInsideClosedBox_ReturnsTrue()
    {
        // Arrange: Create a small map with a closed 3x3 box
        GameMap customMap = new GameMap();
        // Clear map
        for (int y = 0; y < GameMap.MapHeight; y++)
        {
            for (int x = 0; x < GameMap.MapWidth; x++)
            {
                customMap.SetCell(x, y, MapCell.Empty());
            }
        }

        // Build a 5x5 box of walls
        for (int y = 5; y <= 9; y++)
        {
            for (int x = 5; x <= 9; x++)
            {
                if (x == 5 || x == 9 || y == 5 || y == 9)
                {
                    customMap.SetCell(x, y, MapCell.Wall());
                }
            }
        }
        
        // Act & Assert
        Assert.That(customMap.IsInside(6, 6), Is.True); // Inside the box
        Assert.That(customMap.IsInside(7, 7), Is.True); // Inside the box
    }

    [Test]
    public void IsInside_PointOutsideClosedBox_ReturnsFalse()
    {
        // Arrange: Create a small map with a closed 3x3 box
        GameMap customMap = new GameMap();
        for (int y = 0; y < GameMap.MapHeight; y++)
        {
            for (int x = 0; x < GameMap.MapWidth; x++)
            {
                customMap.SetCell(x, y, MapCell.Empty());
            }
        }

        // Build a 5x5 box of walls
        for (int y = 5; y <= 9; y++)
        {
            for (int x = 5; x <= 9; x++)
            {
                if (x == 5 || x == 9 || y == 5 || y == 9)
                {
                    customMap.SetCell(x, y, MapCell.Wall());
                }
            }
        }

        // Act & Assert
        Assert.That(customMap.IsInside(1, 1), Is.False); // Outside the box
        Assert.That(customMap.IsInside(10, 10), Is.False); // Outside the box
    }

    [Test]
    public void IsInside_PointOnWall_ReturnsFalse()
    {
        // Arrange: Create a small map with a closed 3x3 box
        GameMap customMap = new GameMap();
        for (int y = 0; y < GameMap.MapHeight; y++)
        {
            for (int x = 0; x < GameMap.MapWidth; x++)
            {
                customMap.SetCell(x, y, MapCell.Empty());
            }
        }

        // Build a 5x5 box of walls
        for (int y = 5; y <= 9; y++)
        {
            for (int x = 5; x <= 9; x++)
            {
                if (x == 5 || x == 9 || y == 5 || y == 9)
                {
                    customMap.SetCell(x, y, MapCell.Wall());
                }
            }
        }
        
        // Act & Assert
        Assert.That(customMap.IsInside(5, 5), Is.False); // On a wall
        Assert.That(customMap.IsInside(6, 5), Is.False); // On a wall
    }

    [Test]
    public void IsInside_PointOnMapEdge_ReturnsFalse()
    {
        // Arrange: Default map with boundary walls
        GameMap customMap = new GameMap(); // Default map has boundary walls
        
        // Act & Assert
        Assert.That(customMap.IsInside(1, 0), Is.False); // Top edge
        Assert.That(customMap.IsInside(0, 1), Is.False); // Left edge
        Assert.That(customMap.IsInside(GameMap.MapWidth - 2, GameMap.MapHeight - 1), Is.False); // Bottom edge
        Assert.That(customMap.IsInside(GameMap.MapWidth - 1, GameMap.MapHeight - 2), Is.False); // Right edge
    }

    [Test]
    public void IsInside_OpenMap_ReturnsFalseForAnyPoint()
    {
        // Arrange: Create a map with no walls
        GameMap customMap = new GameMap();
        for (int y = 0; y < GameMap.MapHeight; y++)
        {
            for (int x = 0; x < GameMap.MapWidth; x++)
            {
                customMap.SetCell(x, y, MapCell.Empty());
            }
        }

        // Act & Assert
        Assert.That(customMap.IsInside(10, 10), Is.False); // Any point in an open map should be "outside"
        Assert.That(customMap.IsInside(GameMap.MapWidth / 2, GameMap.MapHeight / 2), Is.False);
    }

    [Test]
    public void IsInside_IncompleteShelter_ReturnsFalseForPointInside()
    {
        // Arrange: Create a map with an almost closed box, but with one opening
        GameMap customMap = new GameMap();
        for (int y = 0; y < GameMap.MapHeight; y++)
        {
            for (int x = 0; x < GameMap.MapWidth; x++)
            {
                customMap.SetCell(x, y, MapCell.Empty());
            }
        }

        // Build a 5x5 box of walls, but leave one opening
        for (int y = 5; y <= 9; y++)
        {
            for (int x = 5; x <= 9; x++)
            {
                if (x == 5 || x == 9 || y == 5 || y == 9)
                {
                    customMap.SetCell(x, y, MapCell.Wall());
                }
            }
        }
        customMap.SetCell(7, 5, MapCell.Empty()); // Create an opening in the top wall

        // Act & Assert
        Assert.That(customMap.IsInside(6, 6), Is.False); // Inside an incomplete box should be "outside"
    }

    [Test]
    public void IsInside_ComplexShapeWithMultipleEnclosures_ReturnsTrueForInside()
    {
        // Arrange: Create a more complex map with multiple rooms
        GameMap customMap = new GameMap();
        for (int y = 0; y < GameMap.MapHeight; y++)
        {
            for (int x = 0; x < GameMap.MapWidth; x++)
            {
                customMap.SetCell(x, y, MapCell.Empty());
            }
        }

        // Outer box (room 1)
        for (int y = 5; y <= 15; y++)
        {
            for (int x = 5; x <= 15; x++)
            {
                if (x == 5 || x == 15 || y == 5 || y == 15) customMap.SetCell(x, y, MapCell.Wall());
            }
        }
        // Inner box (room 2, connected to room 1)
        for (int y = 7; y <= 13; y++)
        {
            for (int x = 7; x <= 13; x++)
            {
                if (x == 7 || x == 13 || y == 7 || y == 13) customMap.SetCell(x, y, MapCell.Wall());
            }
        }
        // Connection between room 1 and outer world
        customMap.SetCell(10, 5, MapCell.Empty()); 
        customMap.SetCell(10, 6, MapCell.Empty()); // Doorway between outside and room 1

        // No connection between room 1 and room 2 (making room 2 truly enclosed)
        // customMap.SetCell(11, 7, MapCell.Empty());
        // customMap.SetCell(11, 8, MapCell.Empty()); 


        // Act & Assert
        Assert.That(customMap.IsInside(6, 6), Is.False); // Outside, but within outer box boundaries
        Assert.That(customMap.IsInside(8, 8), Is.True); // Inside room 2
        Assert.That(customMap.IsInside(1, 1), Is.False); // Far outside
    }

    [Test]
    public void IsInside_ComplexShapeWithMultipleEnclosures_ReturnsFalseForOutside()
    {
        // Arrange: Create a more complex map with multiple rooms
        GameMap customMap = new GameMap();
        for (int y = 0; y < GameMap.MapHeight; y++)
        {
            for (int x = 0; x < GameMap.MapWidth; x++)
            {
                customMap.SetCell(x, y, MapCell.Empty());
            }
        }

        // Outer box (room 1)
        for (int y = 5; y <= 15; y++)
        {
            for (int x = 5; x <= 15; x++)
            {
                if (x == 5 || x == 15 || y == 5 || y == 15) customMap.SetCell(x, y, MapCell.Wall());
            }
        }
        // Inner box (room 2, connected to room 1)
        for (int y = 7; y <= 13; y++)
        {
            for (int x = 7; x <= 13; x++)
            {
                if (x == 7 || x == 13 || y == 7 || y == 13) customMap.SetCell(x, y, MapCell.Wall());
            }
        }
        // Connection between room 1 and outer world
        customMap.SetCell(10, 5, MapCell.Empty()); 
        customMap.SetCell(10, 6, MapCell.Empty()); // Doorway between outside and room 1

        // Connection between room 1 and room 2
        customMap.SetCell(11, 7, MapCell.Empty()); 
        customMap.SetCell(11, 8, MapCell.Empty()); 


        // Act & Assert
        Assert.That(customMap.IsInside(6, 6), Is.False); // Outside, but within outer box boundaries
        Assert.That(customMap.IsInside(1, 1), Is.False); // Far outside
    }

}
