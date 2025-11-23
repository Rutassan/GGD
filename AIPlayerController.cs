using Raylib_cs;
using System;

public class AIPlayerController : IPlayerController
{
    private Random _random = new Random();
    private int _moveDelayCounter = 0;
    private const int _moveDelayMax = 20; // AI moves every 20 frames

    public void Update(Player player, GameMap map)
    {
        _moveDelayCounter++;

        if (_moveDelayCounter >= _moveDelayMax)
        {
            _moveDelayCounter = 0;

            int dx = 0;
            int dy = 0;

            int direction = _random.Next(4); // 0: Up, 1: Down, 2: Left, 3: Right

            switch (direction)
            {
                case 0: // Up
                    dy = -1;
                    break;
                case 1: // Down
                    dy = 1;
                    break;
                case 2: // Left
                    dx = -1;
                    break;
                case 3: // Right
                    dx = 1;
                    break;
            }
            
            player.Move(dx, dy, map);
        }
    }
}
