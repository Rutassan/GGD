using System.Diagnostics.CodeAnalysis;
using Raylib_cs;

public class GameMap
{
    private string[] _layout;

    // Using new Color() as a workaround
    private readonly Color _wallColor = new Color(128, 128, 128, 255); // Gray
    private readonly Color _floorColor = new Color(50, 50, 50, 255);   // Dark Gray

    public GameMap()
    {
        _layout = new string[]
        {
            "####################",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "####################"
        };
    }

    [ExcludeFromCodeCoverage]
    public void Draw(int charSize)
    {
        for (int y = 0; y < _layout.Length; y++)
        {
            for (int x = 0; x < _layout[y].Length; x++)
            {
                char tile = _layout[y][x];
                Color color = _floorColor;
                if (tile == '#')
                {
                    color = _wallColor;
                }
                
                Raylib.DrawText(tile.ToString(), x * charSize, y * charSize, charSize, color);
            }
        }
    }

    public bool IsWall(int x, int y)
    {
        if (x < 0 || x >= _layout[0].Length || y < 0 || y >= _layout.Length)
        {
            return true; // Outside map boundaries is considered a wall
        }
        return _layout[y][x] == '#';
    }
}
