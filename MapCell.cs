using Raylib_cs;

public class MapCell
{
    public char DisplayChar { get; set; }
    public Color CellColor { get; set; }
    public bool IsWall { get; set; }
    public ResourceTile? Resource { get; set; } // Помечено как nullable

    public MapCell(char displayChar, Color cellColor, bool isWall, ResourceTile? resource = null) // Конструктор принимает nullable ResourceTile
    {
        DisplayChar = displayChar;
        CellColor = cellColor;
        IsWall = isWall;
        Resource = resource;
    }

    // Static factory methods for common cell types
    public static MapCell Empty() => new MapCell('.', new Color(80, 80, 80, 255), false); // DARKGRAY
    public static MapCell Wall() => new MapCell('#', new Color(121, 121, 121, 255), true); // GRAY
    public static MapCell ResourceCell(ResourceTile resource) => new MapCell(resource.DisplayChar, resource.TileColor, false, resource); // Переименовано в ResourceCell
}