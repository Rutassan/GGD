using System.Diagnostics.CodeAnalysis;
using Raylib_cs;

public class Player
{
    public int X { get; set; }
    public int Y { get; set; }
    public char Character { get; set; }
    public Color Color { get; set; }
    public Inventory PlayerInventory { get; private set; }

    public Player(int x, int y, char character, Color color)
    {
        X = x;
        Y = y;
        Character = character;
        Color = color;
        PlayerInventory = new Inventory();
    }

    [ExcludeFromCodeCoverage]
    public void Draw(int fontSize)
    {
        Raylib.DrawText(Character.ToString(), X * fontSize, Y * fontSize, fontSize, Color);
    }

    public virtual void Move(int dx, int dy, GameMap map)
    {
        int newX = X + dx;
        int newY = Y + dy;

        if (!map.IsWall(newX, newY))
        {
            X = newX;
            Y = newY;
        }
    }

    public void Mine(GameMap map)
    {
        MapCell currentCell = map.GetCell(X, Y);
        if (currentCell.Resource != null)
        {
            currentCell.Resource.Health--;
            if (currentCell.Resource.Health <= 0)
            {
                PlayerInventory.AddItem(currentCell.Resource.ResourceType, 1);
                map.SetCell(X, Y, MapCell.Empty()); // Заменяем добытый ресурс на пустую ячейку
            }
        }
    }

    public bool Build(GameMap map, string resourceType)
    {
        // Пока что строим только стены из "Stone"
        if (resourceType == "Stone" && PlayerInventory.HasItem("Stone", 1))
        {
            // Проверяем, что текущая ячейка пуста или не является стеной, чтобы можно было строить
            MapCell currentCell = map.GetCell(X, Y);
            if (!currentCell.IsWall && currentCell.Resource == null)
            {
                PlayerInventory.RemoveItem("Stone", 1);
                map.SetCell(X, Y, MapCell.Wall()); // Строим стену
                return true;
            }
        }
        return false;
    }
}