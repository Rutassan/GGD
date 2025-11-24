using Raylib_cs;

public class DoorTile
{
    public char OpenChar { get; }
    public char ClosedChar { get; }
    public Color DoorColor { get; }
    public bool IsOpen { get; private set; }

    public DoorTile(bool isOpen = false, char closedChar = '+', char openChar = '/', Color? doorColor = null)
    {
        IsOpen = isOpen;
        ClosedChar = closedChar;
        OpenChar = openChar;
        DoorColor = doorColor ?? new Color(200, 170, 120, 255);
    }

    public char CurrentDisplayChar => IsOpen ? OpenChar : ClosedChar;

    public Color CurrentColor => DoorColor;

    public void Toggle()
    {
        IsOpen = !IsOpen;
    }
}
