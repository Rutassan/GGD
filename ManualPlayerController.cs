using Raylib_cs;

public class ManualPlayerController : IPlayerController
{
    private readonly IInputService _inputService;

    public ManualPlayerController(IInputService inputService)
    {
        _inputService = inputService;
    }

    public void Update(Player player, GameMap map)
    {
        if (_inputService.IsKeyPressed((KeyboardKey)262)) // KEY_RIGHT
        {
            player.Move(1, 0, map);
        }
        if (_inputService.IsKeyPressed((KeyboardKey)263)) // KEY_LEFT
        {
            player.Move(-1, 0, map);
        }
        if (_inputService.IsKeyPressed((KeyboardKey)265)) // KEY_UP
        {
            player.Move(0, -1, map);
        }
        if (_inputService.IsKeyPressed((KeyboardKey)264)) // KEY_DOWN
        {
            player.Move(0, 1, map);
        }
    }
}