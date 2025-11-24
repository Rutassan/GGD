using System.Diagnostics.CodeAnalysis;
using Raylib_cs;
using System; // Для Random
using System.Collections.Generic; // Для Queue

public class GameMap
{
    private MapCell[,] _mapCells; 
    public const int MapWidth = 80;
    public const int MapHeight = 30; // Исправлено

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
    
    public bool IsInside(int startX, int startY)
    {
        // Если стартовая точка - стена или находится за пределами карты, она не может быть "внутри" открытого пространства
        if (startX < 0 || startX >= MapWidth || startY < 0 || startY >= MapHeight || IsWall(startX, startY))
        {
            return false;
        }

        bool[,] visited = new bool[MapHeight, MapWidth];
        Queue<(int x, int y)> queue = new Queue<(int x, int y)>();

        // Добавляем все граничные клетки, которые не являются стенами, в очередь
        // Эти клетки считаются "снаружи"
        for (int y = 0; y < MapHeight; y++)
        {
            if (!IsWall(0, y) && !visited[y, 0])
            {
                queue.Enqueue((0, y));
                visited[y, 0] = true;
            }
            if (!IsWall(MapWidth - 1, y) && !visited[y, MapWidth - 1])
            {
                queue.Enqueue((MapWidth - 1, y));
                visited[y, MapWidth - 1] = true;
            }
        }
        for (int x = 1; x < MapWidth - 1; x++) // Избегаем повторной обработки углов
        {
            if (!IsWall(x, 0) && !visited[0, x])
            {
                queue.Enqueue((x, 0));
                visited[0, x] = true;
            }
            if (!IsWall(x, MapHeight - 1) && !visited[MapHeight - 1, x])
            {
                queue.Enqueue((x, MapHeight - 1));
                visited[MapHeight - 1, x] = true;
            }
        }

        // Выполняем BFS
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (nx >= 0 && nx < MapWidth && ny >= 0 && ny < MapHeight && !visited[ny, nx] && !IsWall(nx, ny))
                {
                    visited[ny, nx] = true;
                    queue.Enqueue((nx, ny));
                }
            }
        }

        // Если стартовая точка не была посещена, значит она "внутри"
        return !visited[startY, startX];
    }


    [ExcludeFromCodeCoverage] // Графический вывод не тестируем
    public void Draw(int fontSize, int cameraX, int cameraY, int visibleMapWidth, int visibleMapHeight)
    {
        for (int y = cameraY; y < cameraY + visibleMapHeight; y++)
        {
            for (int x = cameraX; x < cameraX + visibleMapWidth; x++)
            {
                if (x >= 0 && x < MapWidth && y >= 0 && y < MapHeight) // Проверка границ карты
                {
                    MapCell cell = _mapCells[y, x];
                    // Отрисовываем тайл, смещенный относительно позиции камеры
                    Raylib.DrawText(cell.DisplayChar.ToString(), (x - cameraX) * fontSize, (y - cameraY) * fontSize, fontSize, cell.CellColor);
                }
            }
        }
    }
}
