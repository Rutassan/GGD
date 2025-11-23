using Raylib_cs;

public class RaylibInputService : IInputService
{
    public bool IsKeyPressed(KeyboardKey key)
    {
        return Raylib.IsKeyPressed(key);
    }
}
