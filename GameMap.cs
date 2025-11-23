using Raylib_cs;

public class GameMap
{
    private char[,] _map;
    private const int _mapWidth = 20;
    private const int _mapHeight = 10;

    public GameMap()
    {
        _map = new char[_mapHeight, _mapWidth];
        InitializeMap();
    }

    private void InitializeMap()
    {
        for (int y = 0; y < _mapHeight; y++)
        {
            for (int x = 0; x < _mapWidth; x++)
            {
                if (x == 0 || x == _mapWidth - 1 || y == 0 || y == _mapHeight - 1)
                {
                    _map[y, x] = '#'; // Walls
                }
                else
                {
                    _map[y, x] = '.'; // Empty space
                }
            }
        }
    }

    public virtual bool IsWall(int x, int y)
    {
        if (x < 0 || x >= _mapWidth || y < 0 || y >= _mapHeight)
        {
            return true; // Out of bounds is a wall
        }
        return _map[y, x] == '#';
    }

    public void Draw(int fontSize)
    {
        for (int y = 0; y < _mapHeight; y++)
        {
            for (int x = 0; x < _mapWidth; x++)
            {
                if (_map[y, x] == '#')
                {
                    // WORKAROUND: Using new Color() because Color.GRAY is not found.
                    Raylib.DrawText(_map[y, x].ToString(), x * fontSize, y * fontSize, fontSize, new Color(121, 121, 121, 255)); 
                }
                else
                {
                    // WORKAROUND: Using new Color() because Color.DARKGRAY is not found.
                    Raylib.DrawText(_map[y, x].ToString(), x * fontSize, y * fontSize, fontSize, new Color(80, 80, 80, 255));
                }
            }
        }
    }
}
