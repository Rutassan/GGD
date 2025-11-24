using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using Raylib_cs;

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

    public const int DayDuration = 3600;
    public const int NightDuration = 1800;
    public const int TotalDayNightDuration = DayDuration + NightDuration;
    private static long gameTime = 0;

    private static float cameraX = 0f;
    private static float cameraY = 0f;
    private static float cameraZoom = 1.0f;
    private const float CameraZoomStep = 0.15f;
    private const float CameraZoomMin = 0.6f;
    private const float CameraZoomMax = 2.6f;
    private const float CameraPanSpeed = 18f;
    private const int CameraEdgeMargin = 40;
    private const int PanelTopHeight = 80;
    private const int PanelBottomHeight = 150;
    private const int MinTileSize = 8;
    private const int MaxTileSize = 48;
    private const string UiFontRelativePath = "resources/fonts/NotoSansMono-Regular.ttf";
    private static readonly int[] UiCodepoints = BuildUiCodepoints();

    public static void Main()
    {
        if (!IsDisplayAvailable())
        {
            Console.Error.WriteLine("Графический дисплей недоступен (DISPLAY/WAYLAND_DISPLAY). Запустите в среде с X11/Wayland или настройте виртуальный дисплей.");
            return;
        }

        Raylib.SetConfigFlags((ConfigFlags)4);
        Raylib.InitWindow(screenWidth, screenHeight, "ASCII Game v0.07");

        if (!Raylib.IsWindowReady())
        {
            Console.Error.WriteLine("Не удалось открыть окно Raylib. Проверьте, что доступен дисплей (DISPLAY/WAYLAND_DISPLAY) или запустите с графической сессией.");
            return;
        }

        Raylib.SetTargetFPS(60);
        Font uiFont = LoadUIFont();

        Player player = new Player(5, 5, '@', new Color(0, 255, 0, 255));
        GameMap gameMap = new GameMap();

        int initialCharSize = ComputeTileSize(screenWidth, screenHeight, cameraZoom);
        int initialVisibleWidth = Math.Max(1, screenWidth / initialCharSize);
        int initialVisibleHeight = Math.Max(1, screenHeight / initialCharSize);
        cameraX = Math.Clamp(player.X - initialVisibleWidth / 2f, 0, Math.Max(0, GameMap.MapWidth - initialVisibleWidth));
        cameraY = Math.Clamp(player.Y - initialVisibleHeight / 2f, 0, Math.Max(0, GameMap.MapHeight - initialVisibleHeight));

        IInputService inputService = new RaylibInputService();
        IPlayerController manualController = new ManualPlayerController(inputService);
        IPlayerController aiController = new AIPlayerController();
        IPlayerController currentController = manualController;

        const string manualTitle = "ASCII Game v0.07 (Ручное управление)";
        const string aiTitle = "ASCII Game v0.07 (ИИ управляет)";
        Raylib.SetWindowTitle(manualTitle);

        while (!Raylib.WindowShouldClose())
        {
            gameTime++;
            player.UpdateHunger();

            bool isDay = (gameTime % TotalDayNightDuration) < DayDuration;
            bool isInside = gameMap.IsInside(player.X, player.Y);

            if (Raylib.IsWindowResized())
            {
                screenWidth = Raylib.GetScreenWidth();
                screenHeight = Raylib.GetScreenHeight();
            }

            if (Raylib.IsKeyPressed((KeyboardKey)300))
            {
                ToggleFullscreenMode();
            }

            if (Raylib.IsKeyPressed((KeyboardKey)65))
            {
                if (currentController == manualController)
                {
                    currentController = aiController;
                    Raylib.SetWindowTitle(aiTitle);
                }
                else
                {
                    currentController = manualController;
                    Raylib.SetWindowTitle(manualTitle);
                }
            }

            player.IsFreezing = !isDay && !isInside;
            currentController.Update(player, gameMap, isDay, gameTime);

            float deltaTime = Raylib.GetFrameTime();
            int charSize = ComputeTileSize(screenWidth, screenHeight, cameraZoom);
            int oldCharSize = charSize;
            float wheelDelta = Raylib.GetMouseWheelMove();
            if (Math.Abs(wheelDelta) > float.Epsilon)
            {
                float mouseX = Raylib.GetMouseX();
                float mouseY = Raylib.GetMouseY();
                float worldMouseX = cameraX + mouseX / oldCharSize;
                float worldMouseY = cameraY + mouseY / oldCharSize;
                cameraZoom = Math.Clamp(cameraZoom + wheelDelta * CameraZoomStep, CameraZoomMin, CameraZoomMax);
                charSize = ComputeTileSize(screenWidth, screenHeight, cameraZoom);
                cameraX = worldMouseX - mouseX / charSize;
                cameraY = worldMouseY - mouseY / charSize;
            }

            UpdateCameraPan(deltaTime);

            int visibleMapWidth = Math.Max(1, screenWidth / charSize);
            int visibleMapHeight = Math.Max(1, screenHeight / charSize);
            ClampCamera(visibleMapWidth, visibleMapHeight);

            int cameraTileX = (int)Math.Floor(cameraX);
            int cameraTileY = (int)Math.Floor(cameraY);

            Color backgroundColor = isDay ? new Color(135, 206, 235, 255) : new Color(25, 25, 112, 255);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(backgroundColor);
            gameMap.Draw(charSize, cameraTileX, cameraTileY, visibleMapWidth, visibleMapHeight);
            player.Draw(charSize, cameraTileX, cameraTileY);

            long dayNumber = (gameTime / TotalDayNightDuration) + 1;
            Color hungerColor = player.IsStarving ? new Color(255, 100, 100, 255)
                : player.IsHungry ? new Color(255, 235, 59, 255)
                : new Color(255, 255, 255, 255);

            ShelterInfo shelterInfo = gameMap.AnalyzeShelter(player.X, player.Y);
            DrawTopPanel(uiFont, screenWidth, player, isDay, dayNumber, hungerColor, shelterInfo);
            DrawBottomPanel(uiFont, screenWidth, screenHeight, player);

            Raylib.EndDrawing();
        }

        Raylib.UnloadFont(uiFont);
        Raylib.CloseWindow();
    }

    private static int ComputeTileSize(int width, int height, float zoom)
    {
        int baseSize = Math.Max(MinTileSize, Math.Min(width / 32, height / 18));
        int scaledSize = (int)Math.Round(baseSize * zoom);
        return Math.Clamp(scaledSize, MinTileSize, MaxTileSize);
    }

    private static void ClampCamera(int visibleWidth, int visibleHeight)
    {
        float maxX = Math.Max(0, GameMap.MapWidth - visibleWidth);
        float maxY = Math.Max(0, GameMap.MapHeight - visibleHeight);
        cameraX = Math.Clamp(cameraX, 0, maxX);
        cameraY = Math.Clamp(cameraY, 0, maxY);
    }

    private static void UpdateCameraPan(float deltaTime)
    {
        float dx = 0f;
        float dy = 0f;
        int mouseX = Raylib.GetMouseX();
        int mouseY = Raylib.GetMouseY();

        if (mouseX < CameraEdgeMargin)
        {
            dx = -CameraPanSpeed * deltaTime;
        }
        else if (mouseX > screenWidth - CameraEdgeMargin)
        {
            dx = CameraPanSpeed * deltaTime;
        }

        if (mouseY < CameraEdgeMargin)
        {
            dy = -CameraPanSpeed * deltaTime;
        }
        else if (mouseY > screenHeight - CameraEdgeMargin)
        {
            dy = CameraPanSpeed * deltaTime;
        }

        cameraX += dx;
        cameraY += dy;
    }

    private static void DrawTopPanel(Font uiFont, int width, Player player, bool isDay, long dayNumber, Color hungerColor, ShelterInfo shelterInfo)
    {
        Raylib.DrawRectangle(0, 0, width, PanelTopHeight, new Color(10, 10, 20, 220));
        
        int leftX = 14;
        int rightX = width / 2 + 10;
        int line1Y = 10;
        int line2Y = 35;
        int line3Y = 58;
        
        // Левая колонка
        string timeLabel = isDay ? "День" : "Ночь";
        Raylib.DrawTextEx(uiFont, $"День {dayNumber} | {timeLabel}", new Vector2(leftX, line1Y), 20, 0, new Color(255, 255, 255, 255));
        
        string hungerText = $"Сытость: {player.Hunger}/{Player.MaxHunger}";
        Raylib.DrawTextEx(uiFont, hungerText, new Vector2(leftX, line2Y), 18, 0, hungerColor);
        
        string status = player.IsFreezing ? "⚠ Замерзает" : player.IsStarving ? "⚠ Голод" : "✓ Стабильно";
        Color statusColor = player.IsFreezing || player.IsStarving ? new Color(255, 100, 100, 255) : new Color(100, 255, 100, 255);
        Raylib.DrawTextEx(uiFont, status, new Vector2(leftX, line3Y), 16, 0, statusColor);
        
        // Правая колонка - укрытие
        string shelterLine = shelterInfo.IsInside ? $"Укрытие: {shelterInfo.Classification}" : "Укрытие: больш. дом";
        Raylib.DrawTextEx(uiFont, shelterLine, new Vector2(rightX, line1Y), 18, 0, new Color(190, 210, 255, 255));
        
        if (shelterInfo.IsInside)
        {
            string areaText = $"(площадь: {shelterInfo.Area})";
            Raylib.DrawTextEx(uiFont, areaText, new Vector2(rightX, line2Y), 16, 0, new Color(180, 200, 245, 255));
            
            string doors = shelterInfo.HasDoor ? $"Двери: {shelterInfo.DoorCount}" : "Дверей нет";
            Raylib.DrawTextEx(uiFont, doors, new Vector2(rightX, line3Y), 16, 0, new Color(200, 200, 255, 255));
        }
        else
        {
            Raylib.DrawTextEx(uiFont, "На улице", new Vector2(rightX, line2Y), 16, 0, new Color(200, 180, 180, 255));
        }
    }

    private static void DrawBottomPanel(Font uiFont, int width, int height, Player player)
    {
        int y = Math.Max(0, height - PanelBottomHeight);
        Raylib.DrawRectangle(0, y, width, PanelBottomHeight, new Color(8, 8, 18, 230));

        const int padding = 14;
        int inventoryWidth = (int)(width * 0.35f);
        int dividerX = inventoryWidth;

        // --- Inventory Column ---
        int invX = padding;
        int invY = y + padding;
        Raylib.DrawTextEx(uiFont, "Инвентарь:", new Vector2(invX, invY), 19, 0, new Color(240, 240, 255, 255));
        invY += 24;

        if (player.PlayerInventory.Resources.Count == 0)
        {
            Raylib.DrawTextEx(uiFont, "Пусто", new Vector2(invX, invY), 16, 0, new Color(180, 180, 180, 255));
        }
        else
        {
            foreach (var item in player.PlayerInventory.Resources)
            {
                if (invY > y + PanelBottomHeight - 20) break;
                Raylib.DrawTextEx(uiFont, $"{item.Key}: {item.Value}", new Vector2(invX, invY), 16, 0, new Color(220, 220, 220, 255));
                invY += 22;
            }
        }

        // --- Divider ---
        Raylib.DrawLine(dividerX, y + 6, dividerX, y + PanelBottomHeight - 6, new Color(50, 60, 80, 255));

        // --- Commands Column ---
        int commandsX = dividerX + padding + 4;
        int commandsY = y + padding;
        Raylib.DrawTextEx(uiFont, "Команды:", new Vector2(commandsX, commandsY), 19, 0, new Color(235, 235, 255, 255));
        commandsY += 24;

        string[] commandsLeft =
        {
            "WASD / стрелки - движение",
            "E - съесть ягоды",
            "M - добыть ресурс"
        };

        string[] commandsRight =
        {
            "B - построить стену (Stone)",
            "F - поставить дверь",
            "O - открыть/закрыть дверь"
        };

        int cmdLeftX = commandsX;
        int cmdRightX = commandsX + (int)((width - dividerX) * 0.5f);
        int cmdY = commandsY;

        for (int i = 0; i < Math.Max(commandsLeft.Length, commandsRight.Length); i++)
        {
            if (i < commandsLeft.Length)
            {
                Raylib.DrawTextEx(uiFont, commandsLeft[i], new Vector2(cmdLeftX, cmdY), 14, 0, new Color(200, 220, 255, 255));
            }
            if (i < commandsRight.Length)
            {
                Raylib.DrawTextEx(uiFont, commandsRight[i], new Vector2(cmdRightX, cmdY), 14, 0, new Color(200, 220, 255, 255));
            }
            cmdY += 18;
        }

        // Camera hints at bottom right
        string[] cameraHints =
        {
            "Камера: края окна | Зум: колесо мыши"
        };
        
        int hintY = y + PanelBottomHeight - 24;
        foreach (string hint in cameraHints)
        {
            Vector2 hintSize = Raylib.MeasureTextEx(uiFont, hint, 13, 0);
            float hintX = width - hintSize.X - padding - 4;
            Raylib.DrawTextEx(uiFont, hint, new Vector2(hintX, hintY), 13, 0, new Color(160, 190, 235, 220));
            hintY += 16;
        }
    }

    private static Font LoadUIFont()
    {
        string? fontPath = ResolveFontPath();
        if (fontPath != null)
        {
            Font font = Raylib.LoadFontEx(fontPath, 28, UiCodepoints, UiCodepoints.Length);
            Raylib.SetTextureFilter(font.Texture, TextureFilter.Bilinear);
            return font;
        }

        return Raylib.GetFontDefault();
    }

    private static string? ResolveFontPath()
    {
        string primaryPath = Path.Combine(AppContext.BaseDirectory, UiFontRelativePath);
        if (File.Exists(primaryPath))
        {
            return primaryPath;
        }

        string secondaryPath = Path.Combine(Directory.GetCurrentDirectory(), UiFontRelativePath);
        if (File.Exists(secondaryPath))
        {
            return secondaryPath;
        }

        return null;
    }

    private static int[] BuildUiCodepoints()
    {
        List<int> codepoints = new List<int>(256);
        for (int c = 32; c <= 126; c++)
        {
            codepoints.Add(c);
        }

        for (int c = 0x0410; c <= 0x044F; c++)
        {
            codepoints.Add(c);
        }

        codepoints.Add(0x0401);
        codepoints.Add(0x0451);
        return codepoints.ToArray();
    }

    private static bool IsDisplayAvailable()
    {
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

        return true;
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
