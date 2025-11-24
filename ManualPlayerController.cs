using Raylib_cs;

public class ManualPlayerController : IPlayerController
{
    private readonly IInputService _inputService;
    private int _penaltyCounter;
    private const int PenaltyDelayFrames = 8;

    public ManualPlayerController(IInputService inputService)
    {
        _inputService = inputService;
    }

    public void Update(Player player, GameMap map, bool isDay, long gameTime)
    {
        if (_inputService.IsKeyPressed((KeyboardKey)69)) // KEY_E
        {
            player.Eat("Berry");
        }

        if (_inputService.IsKeyPressed((KeyboardKey)79)) // KEY_O
        {
            TryToggleNearbyDoor(player, map);
        }

        if (_inputService.IsKeyPressed((KeyboardKey)70)) // KEY_F
        {
            player.BuildDoor(map, player.X, player.Y);
        }

        bool isPenalized = player.IsFreezing || player.IsStarving;
        if (isPenalized)
        {
            _penaltyCounter++;
            if (_penaltyCounter < PenaltyDelayFrames)
            {
                return;
            }
        }
        else
        {
            _penaltyCounter = 0;
        }

        if (_inputService.IsKeyPressed((KeyboardKey)262))
        {
            player.Move(1, 0, map);
        }
        if (_inputService.IsKeyPressed((KeyboardKey)263))
        {
            player.Move(-1, 0, map);
        }
        if (_inputService.IsKeyPressed((KeyboardKey)265))
        {
            player.Move(0, -1, map);
        }
        if (_inputService.IsKeyPressed((KeyboardKey)264))
        {
            player.Move(0, 1, map);
        }

        if (_inputService.IsKeyPressed((KeyboardKey)77))
        {
            player.Mine(map);
        }

        if (_inputService.IsKeyPressed((KeyboardKey)66))
        {
            player.Build(map, "Stone", player.X, player.Y);
        }
    }

    private void TryToggleNearbyDoor(Player player, GameMap map)
    {
        foreach (var (dx, dy) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
        {
            int nx = player.X + dx;
            int ny = player.Y + dy;
            if (map.TryToggleDoor(nx, ny))
            {
                return;
            }
        }
    }
}
