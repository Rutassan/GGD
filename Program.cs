using System.Diagnostics.CodeAnalysis;
using Raylib_cs;

[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main()
    {
        const int screenWidth = 800;
        const int screenHeight = 450;
        const string windowTitle = "ASCII Game v0.02"; // Обновляем версию
        const int charSize = 20;

        Raylib.InitWindow(screenWidth, screenHeight, windowTitle);
        Raylib.SetTargetFPS(60);

        Player player = new Player(5, 5, '@', new Color(0, 255, 0, 255)); 
        GameMap gameMap = new GameMap();

        IInputService inputService = new RaylibInputService(); // Создаем сервис ввода
        IPlayerController manualController = new ManualPlayerController(inputService); // Передаем сервис ввода
        IPlayerController aiController = new AIPlayerController();
        IPlayerController currentController = manualController; // По умолчанию - ручное управление

        while (!Raylib.WindowShouldClose())
        {
            // Переключение контроллеров
            if (Raylib.IsKeyPressed((KeyboardKey)65)) // Клавиша 'A' (65) для переключения
            {
                if (currentController == manualController)
                {
                    currentController = aiController;
                    Raylib.SetWindowTitle("ASCII Game v0.02 (AI Controlled)");
                }
                else
                {
                    currentController = manualController;
                    Raylib.SetWindowTitle("ASCII Game v0.02 (Manual Controlled)");
                }
            }

            // Обновление игрока через текущий контроллер
            currentController.Update(player, gameMap);

            // Draw
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0, 0, 0, 255));
            gameMap.Draw(charSize);
            player.Draw(charSize);
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}