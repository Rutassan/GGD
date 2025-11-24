using System.Diagnostics.CodeAnalysis;
using Raylib_cs;
using System; // Для Math.Min
using System.Text; // Для StringBuilder, если понадобится, но пока не вижу необходимости.
using System.Net.Sockets;
using System.IO;

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

    // Константы для цикла дня и ночи (в кадрах)
    public const int DayDuration = 3600; // 60 секунд * 60 кадров/сек = 3600 кадров
    public const int NightDuration = 1800; // 30 секунд * 60 кадров/сек = 1800 кадров
    public const int TotalDayNightDuration = DayDuration + NightDuration;
    private static long gameTime = 0; // Глобальный счетчик игрового времени

    public static void Main()
    {
        if (!IsDisplayAvailable())
        {
            Console.Error.WriteLine("Графический дисплей недоступен (DISPLAY/WAYLAND_DISPLAY). Запустите в среде с X11/Wayland или настройте виртуальный дисплей.");
            return;
        }

        // Устанавливаем флаг для изменения размера окна (ConfigFlags.FLAG_WINDOW_RESIZABLE = 4)
        Raylib.SetConfigFlags((ConfigFlags)4); 
        Raylib.InitWindow(screenWidth, screenHeight, "ASCII Game v0.05 (Manual Controlled)"); 

        if (!Raylib.IsWindowReady())
        {
            Console.Error.WriteLine("Не удалось открыть окно Raylib. Проверьте, что доступен дисплей (DISPLAY/WAYLAND_DISPLAY) или запустите с графической сессией.");
            return;
        }

        Raylib.SetTargetFPS(60);

        Player player = new Player(5, 5, '@', new Color(0, 255, 0, 255)); 
        GameMap gameMap = new GameMap();

        IInputService inputService = new RaylibInputService();
        IPlayerController manualController = new ManualPlayerController(inputService);
        IPlayerController aiController = new AIPlayerController();
        IPlayerController currentController = manualController;

        while (!Raylib.WindowShouldClose())
        {
            gameTime++; // Увеличиваем глобальный счетчик времени

            // Определяем, день сейчас или ночь
            bool isDay = (gameTime % TotalDayNightDuration) < DayDuration;

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
                    Raylib.SetWindowTitle("ASCII Game v0.05 (AI Controlled)"); 
                }
                else
                {
                    currentController = manualController;
                    Raylib.SetWindowTitle("ASCII Game v0.05 (Manual Controlled)"); 
                }
            }

            currentController.Update(player, gameMap, isDay, gameTime);

            // Логика "холода"
            player.IsFreezing = !isDay && !gameMap.IsInside(player.X, player.Y);

            int targetCharWidth = screenWidth / GameMap.MapWidth;
            int targetCharHeight = screenHeight / GameMap.MapHeight;
            int charSize = Math.Max(1, Math.Min(targetCharWidth, targetCharHeight));

            // Ensure a minimum char size for readability
            if (charSize < 8) charSize = 8; // Arbitrary minimum

            int visibleMapWidth = Math.Max(1, Math.Min(GameMap.MapWidth, screenWidth / charSize));
            int visibleMapHeight = Math.Max(1, Math.Min(GameMap.MapHeight, screenHeight / charSize));

            // Камера следит за игроком
            int cameraX = player.X - visibleMapWidth / 2;
            int cameraY = player.Y - visibleMapHeight / 2;

            // Ограничиваем камеру пределами карты
            cameraX = Math.Clamp(cameraX, 0, Math.Max(0, GameMap.MapWidth - visibleMapWidth));
            cameraY = Math.Clamp(cameraY, 0, Math.Max(0, GameMap.MapHeight - visibleMapHeight));


            Color backgroundColor = isDay ? new Color(135, 206, 235, 255) : new Color(25, 25, 112, 255); // SKYBLUE или MIDNIGHTBLUE

            Raylib.BeginDrawing();
            Raylib.ClearBackground(backgroundColor);
            gameMap.Draw(charSize, cameraX, cameraY, visibleMapWidth, visibleMapHeight);
            player.Draw(charSize, cameraX, cameraY);

            // HUD для инвентаря
            int hudY = 10;
            Raylib.DrawText("Inventory:", screenWidth - 200, hudY, 20, new Color(255, 255, 255, 255)); // WHITE
            hudY += 25;
            foreach (var item in player.PlayerInventory.Resources)
            {
                Raylib.DrawText($"{item.Key}: {item.Value}", screenWidth - 200, hudY, 20, new Color(255, 255, 255, 255)); // WHITE
                hudY += 25;
            }

            // HUD для цикла дня/ночи
            string timeOfDayText = isDay ? "Day" : "Night";
            Raylib.DrawText($"Time: {timeOfDayText}", 10, 10, 20, new Color(255, 255, 255, 255)); // WHITE

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    private static bool IsDisplayAvailable()
    {
        // На Linux пытаемся проверить доступность сокета X11/Wayland, чтобы избежать падения raylib
        if (OperatingSystem.IsLinux())
        {
            string? display = Environment.GetEnvironmentVariable("DISPLAY");
            string? wayland = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
            if (string.IsNullOrEmpty(display) && string.IsNullOrEmpty(wayland))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(display) && display.StartsWith(":"))
            {
                string dispNumberPart = display.TrimStart(':').Split('.')[0];
                if (int.TryParse(dispNumberPart, out int dispNumber))
                {
                    string socketPath = $"/tmp/.X11-unix/X{dispNumber}";
                    if (!File.Exists(socketPath)) return false;
                    try
                    {
                        using Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                        socket.Connect(new UnixDomainSocketEndPoint(socketPath));
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }

        return true; // Windows/macOS или Wayland без явной проверки считаем доступными
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
