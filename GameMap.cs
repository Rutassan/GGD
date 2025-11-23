using Raylib_cs;
using System; // Для Random

public class GameMap
{
    private MapCell[,] _mapCells; 
    public const int MapWidth = 20; 
    public const int MapHeight = 10; // Исправлено

    private Random _random = new Random(); 

    public GameMap()
    {
        _mapCells = new MapCell[MapHeight, MapWidth];
        GenerateMap(); 
    }

    public void GenerateMap()
    {
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                if (x == 0 || x == MapWidth - 1 || y == 0 || y == MapHeight - 1)
                {
                    _mapCells[y, x] = MapCell.Wall();
                }
                else
                {
                    int rand = _random.Next(100);
                    if (rand < 10) 
                    {
                        _mapCells[y, x] = MapCell.Wall();
                    }
                    else if (rand < 15) 
                    {
                        _mapCells[y, x] = MapCell.ResourceCell(new ResourceTile('$', new Color(150, 150, 150, 255), "Stone", 3)); 
                    }
                    else
                    {
                        _mapCells[y, x] = MapCell.Empty();
                    }
                }
            }
        }
    }

    public virtual bool IsWall(int x, int y)
    {
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
        {
            return true; 
        }
        return _mapCells[y, x].IsWall; 
    }

    public virtual MapCell GetCell(int x, int y)
    {
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
        {
            return MapCell.Empty();
        }
        return _mapCells[y, x];
    }

    public virtual void SetCell(int x, int y, MapCell newCell)
    {
        if (x >= 0 && x < MapWidth && y >= 0 && y < MapHeight)
        {
            _mapCells[y, x] = newCell;
        }
    }


    public void Draw(int fontSize)
    {
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                MapCell cell = _mapCells[y, x];
                Raylib.DrawText(cell.DisplayChar.ToString(), x * fontSize, y * fontSize, fontSize, cell.CellColor);
            }
        }
    }
}