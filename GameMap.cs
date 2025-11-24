using System.Diagnostics.CodeAnalysis;
using Raylib_cs;
using System;
using System.Collections.Generic;

[ExcludeFromCodeCoverage]
public class GameMap
{
    private MapCell[,] _mapCells;
    public const int MapWidth = 160;
    public const int MapHeight = 60;
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
                if (IsBoundary(x, y))
                {
                    _mapCells[y, x] = MapCell.Wall();
                    continue;
                }

                int roll = _random.Next(100);
                if (roll < 8)
                {
                    _mapCells[y, x] = MapCell.Wall();
                }
                else if (roll < 16)
                {
                    _mapCells[y, x] = MapCell.ResourceCell(new ResourceTile('$', new Color(150, 150, 150, 255), "Stone", 3));
                }
                else if (roll < 23)
                {
                    _mapCells[y, x] = MapCell.ResourceCell(new ResourceTile('*', new Color(220, 50, 50, 255), "Berry", 2));
                }
                else
                {
                    _mapCells[y, x] = MapCell.Empty();
                }
            }
        }
    }

    public virtual bool IsWall(int x, int y)
    {
        if (!IsWithinBounds(x, y))
        {
            return true;
        }
        return _mapCells[y, x].IsWall;
    }

    public virtual MapCell GetCell(int x, int y)
    {
        if (!IsWithinBounds(x, y))
        {
            return MapCell.Empty();
        }
        return _mapCells[y, x];
    }

    public virtual void SetCell(int x, int y, MapCell newCell)
    {
        if (IsWithinBounds(x, y))
        {
            _mapCells[y, x] = newCell;
        }
    }

    public virtual bool PlaceDoor(int x, int y, bool initiallyOpen = false)
    {
        if (!IsWithinBounds(x, y))
        {
            return false;
        }
        MapCell target = _mapCells[y, x];
        if (target.IsWall || target.Resource != null || target.HasDoor)
        {
            return false;
        }

        DoorTile door = new DoorTile(initiallyOpen);
        _mapCells[y, x] = MapCell.DoorCell(door);
        return true;
    }

    public virtual bool TryToggleDoor(int x, int y)
    {
        if (!IsWithinBounds(x, y))
        {
            return false;
        }
        DoorTile? door = _mapCells[y, x].Door;
        if (door == null)
        {
            return false;
        }

        door.Toggle();
        return true;
    }

    public virtual bool HasDoorAt(int x, int y) => IsWithinBounds(x, y) && _mapCells[y, x].HasDoor;

    public ShelterInfo AnalyzeShelter(int startX, int startY)
    {
        var info = new ShelterInfo();

        if (!IsWithinBounds(startX, startY) || _mapCells[startY, startX].IsWall)
        {
            info.Classification = "На улице";
            return info;
        }

        bool[,] visited = new bool[MapHeight, MapWidth];
        Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
        queue.Enqueue((startX, startY));
        visited[startY, startX] = true;

        bool touchesOutside = false;
        int area = 0;
        HashSet<(int x, int y)> doorPositions = new HashSet<(int x, int y)>();

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            area++;

            MapCell cell = _mapCells[y, x];
            if (cell.HasDoor)
            {
                doorPositions.Add((x, y));
            }

            if (x == 0 || x == MapWidth - 1 || y == 0 || y == MapHeight - 1)
            {
                touchesOutside = true;
            }

            foreach ((int dx, int dy) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
            {
                int nx = x + dx;
                int ny = y + dy;
                if (!IsWithinBounds(nx, ny) || visited[ny, nx])
                {
                    continue;
                }

                MapCell nextCell = _mapCells[ny, nx];
                if (nextCell.IsWall)
                {
                    if (nextCell.HasDoor)
                    {
                        doorPositions.Add((nx, ny));
                    }
                    continue;
                }

                visited[ny, nx] = true;
                queue.Enqueue((nx, ny));
            }
        }

        bool isInside = !touchesOutside;
        info.IsInside = isInside;
        info.Area = isInside ? area : 0;
        info.DoorCount = doorPositions.Count;
        info.HasDoor = doorPositions.Count > 0;
        info.Classification = ClassifyShelter(isInside, info.Area);

        return info;
    }

    public bool IsInside(int x, int y)
    {
        return AnalyzeShelter(x, y).IsInside;
    }

    private string ClassifyShelter(bool isInside, int area)
    {
        if (!isInside || area <= 0)
        {
            return "На улице";
        }

        if (area < 12)
        {
            return "Укрытие";
        }
        if (area < 40)
        {
            return "Маленький дом";
        }
        return "Большой дом";
    }

    private bool IsBoundary(int x, int y)
    {
        return x == 0 || y == 0 || x == MapWidth - 1 || y == MapHeight - 1;
    }

    private bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < MapWidth && y >= 0 && y < MapHeight;
    }

    [ExcludeFromCodeCoverage]
    public void Draw(int fontSize, int cameraX, int cameraY, int visibleMapWidth, int visibleMapHeight)
    {
        for (int y = cameraY; y < cameraY + visibleMapHeight; y++)
        {
            for (int x = cameraX; x < cameraX + visibleMapWidth; x++)
            {
                if (!IsWithinBounds(x, y)) continue;

                MapCell cell = _mapCells[y, x];
                Raylib.DrawText(cell.RenderChar.ToString(), (x - cameraX) * fontSize, (y - cameraY) * fontSize, fontSize, cell.RenderColor);
            }
        }
    }
}

public class ShelterInfo
{
    public bool IsInside { get; set; }
    public int Area { get; set; }
    public bool HasDoor { get; set; }
    public int DoorCount { get; set; }
    public string Classification { get; set; } = "На улице";
}
