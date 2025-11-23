using System.Diagnostics.CodeAnalysis;
using Raylib_cs;

public class Player
{
    public int X { get; set; }
    public int Y { get; set; }
    public char Character { get; set; }
    public Color Color { get; set; }

    public Player(int x, int y, char character, Color color)
    {
        X = x;
        Y = y;
        Character = character;
        Color = color;
    }

    [ExcludeFromCodeCoverage]
    public void Draw(int fontSize)
    {
        Raylib.DrawText(Character.ToString(), X * fontSize, Y * fontSize, fontSize, Color);
    }

    public virtual void Move(int dx, int dy, GameMap map) // Добавлено 'virtual'
    {
        int newX = X + dx;
        int newY = Y + dy;

        if (!map.IsWall(newX, newY))
        {
            X = newX;
            Y = newY;
        }
    }
}