using Raylib_cs;
using System;

public class AIPlayerController : IPlayerController
{
    private Random _random = new Random();
    private int _actionDelayCounter = 0; // Изменено название с _moveDelayCounter
    private const int _actionDelayMax = 20; // AI действует каждые 20 кадров

    public void Update(Player player, GameMap map)
    {
        _actionDelayCounter++;

        if (_actionDelayCounter >= _actionDelayMax)
        {
            _actionDelayCounter = 0;

            // Логика принятия решений ИИ
            // Приоритеты: добыть ресурс -> построить -> исследовать (двигаться)

            // 1. Попробовать добыть ресурс, если есть рядом
            MapCell currentCell = map.GetCell(player.X, player.Y);
            if (currentCell.Resource != null && currentCell.Resource.Health > 0)
            {
                player.Mine(map);
                // Console.WriteLine("AI is mining!"); // Для отладки
                return; // ИИ совершил действие
            }

            // 2. Попробовать построить, если есть ресурсы
            // Пока что строим только Stone
            if (player.PlayerInventory.HasItem("Stone", 1))
            {
                // ИИ может построить стену на своей текущей позиции, если она пуста
                if (!currentCell.IsWall && currentCell.Resource == null)
                {
                    player.Build(map, "Stone");
                    // Console.WriteLine("AI is building!"); // Для отладки
                    return; // ИИ совершил действие
                }
            }

            // 3. Исследовать (двигаться)
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