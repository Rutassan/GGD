using Raylib_cs;

public class ResourceTile
{
    public char DisplayChar { get; set; }
    public Color TileColor { get; set; }
    public string ResourceType { get; set; }
    public int Health { get; set; }

    public ResourceTile(char displayChar, Color tileColor, string resourceType, int health)
    {
        DisplayChar = displayChar;
        TileColor = tileColor;
        ResourceType = resourceType;
        Health = health;
    }
}
