using System.Diagnostics.CodeAnalysis;
using Raylib_cs;
using System; // Для Math.Min
using System.Text; // Для StringBuilder, если понадобится, но пока не вижу необходимости.

[ExcludeFromCodeCoverage]
public class Program
{
    private const int InitialScreenWidth = 800;
    private const int InitialScreenHeight = 450;
    private static int screenWidth = InitialScreenWidth;
    private static int screenHeight = InitialScreenHeight;
    private static bool isFullscreen = false;
    private static int previousScreenWidth = InitialScreenWidth;
    private static int previousScreenHeight = InitialScreenHeight;

    public static void Main()
    {
        // Устанавливаем флаг для изменения размера окна (ConfigFlags.FLAG_WINDOW_RESIZABLE = 4)
        Raylib.SetConfigFlags((ConfigFlags)4); 
        Raylib.InitWindow(screenWidth, screenHeight, "ASCII Game v0.03 (Manual Controlled)"); 
        Raylib.SetTargetFPS(60);

        Player player = new Player(5, 5, '@', new Color(0, 255, 0, 255)); 
        GameMap gameMap = new GameMap();

        IInputService inputService = new RaylibInputService();
        IPlayerController manualController = new ManualPlayerController(inputService);
        IPlayerController aiController = new AIPlayerController();
        IPlayerController currentController = manualController;

        while (!Raylib.WindowShouldClose())
        {
            // Обновляем размеры экрана, если окно было изменено
            if (Raylib.IsWindowResized())
            {
                screenWidth = Raylib.GetScreenWidth();
                screenHeight = Raylib.GetScreenHeight();
            }

            // Переключение полноэкранного режима по нажатию F11 (KeyboardKey.KEY_F11 = 300)
            if (Raylib.IsKeyPressed((KeyboardKey)300)) 
            {
                ToggleFullscreenMode();
            }

            // Переключение контроллеров по нажатию 'A' (65)
            if (Raylib.IsKeyPressed((KeyboardKey)65)) 
            {
                if (currentController == manualController)
                {
                    currentController = aiController;
                    Raylib.SetWindowTitle("ASCII Game v0.03 (AI Controlled)"); 
                }
                else
                {
                    currentController = manualController;
                    Raylib.SetWindowTitle("ASCII Game v0.03 (Manual Controlled)"); 
                }
            }

            currentController.Update(player, gameMap);

            int charSizeHeight = screenHeight / GameMap.MapHeight;
            int charSizeWidth = screenWidth / GameMap.MapWidth;
            int charSize = Math.Min(charSizeHeight, charSizeWidth);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0, 0, 0, 255));
            gameMap.Draw(charSize);
            player.Draw(charSize);

            // HUD для инвентаря
            int hudY = 10;
            Raylib.DrawText("Inventory:", screenWidth - 200, hudY, 20, new Color(255, 255, 255, 255)); // WHITE
            hudY += 25;
            foreach (var item in player.PlayerInventory.Resources)
            {
                Raylib.DrawText($"{item.Key}: {item.Value}", screenWidth - 200, hudY, 20, new Color(255, 255, 255, 255)); // WHITE
                hudY += 25;
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    private static void ToggleFullscreenMode()
    {
        if (!isFullscreen)
        {
            previousScreenWidth = Raylib.GetScreenWidth();
            previousScreenHeight = Raylib.GetScreenHeight();

            int monitor = Raylib.GetCurrentMonitor();
            int monitorWidth = Raylib.GetMonitorWidth(monitor);
            int monitorHeight = Raylib.GetMonitorHeight(monitor);

            Raylib.SetWindowSize(monitorWidth, monitorHeight);
            Raylib.ToggleFullscreen();
            isFullscreen = true;
        }
        else
        {
            Raylib.ToggleFullscreen();
            Raylib.SetWindowSize(previousScreenWidth, previousScreenHeight);
            isFullscreen = false;
        }
    }
}
