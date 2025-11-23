using System.Diagnostics.CodeAnalysis;
using Raylib_cs;

[ExcludeFromCodeCoverage]
public class RaylibInputService : IInputService
{
    public bool IsKeyPressed(KeyboardKey key)
    {
        return Raylib.IsKeyPressed(key);
    }
}
