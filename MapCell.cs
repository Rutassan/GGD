using Raylib_cs;

public class MapCell
{
    private readonly bool _baseIsWall;

    public char DisplayChar { get; set; }
    public Color CellColor { get; set; }
    public ResourceTile? Resource { get; set; }
    public DoorTile? Door { get; set; }

    public MapCell(char displayChar, Color cellColor, bool isWall, ResourceTile? resource = null, DoorTile? door = null)
    {
        DisplayChar = displayChar;
        CellColor = cellColor;
        _baseIsWall = isWall;
        Resource = resource;
        Door = door;
    }

    public bool IsWall => _baseIsWall || (Door != null && !Door.IsOpen);

    public bool HasDoor => Door != null;

    public char RenderChar => Door?.CurrentDisplayChar ?? DisplayChar;

    public Color RenderColor => Door?.CurrentColor ?? CellColor;

    public static MapCell Empty() => new MapCell('.', new Color(80, 80, 80, 255), false);

    public static MapCell Wall() => new MapCell('#', new Color(121, 121, 121, 255), true);

    public static MapCell ResourceCell(ResourceTile resource) => new MapCell(resource.DisplayChar, resource.TileColor, false, resource);

    public static MapCell DoorCell(DoorTile door) => new MapCell(door.CurrentDisplayChar, door.CurrentColor, false, null, door);
}
