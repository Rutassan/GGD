using System.Diagnostics.CodeAnalysis;
using Raylib_cs;

[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main()
    {
        const int screenWidth = 800;
        const int screenHeight = 450;
        const string windowTitle = "ASCII Game v0.01";
        const int charSize = 20;

        Raylib.InitWindow(screenWidth, screenHeight, windowTitle);
        Raylib.SetTargetFPS(60);

        // WORKAROUND: Using new Color() because Color.GREEN is not found.
        Player player = new Player(5, 5, '@', new Color(0, 255, 0, 255)); 
        GameMap gameMap = new GameMap();

        while (!Raylib.WindowShouldClose())
        {
            // Update
            // WORKAROUND: Explicitly casting int key codes because KeyboardKey.KEY_XXX is not found.
            if (Raylib.IsKeyPressed((KeyboardKey)262)) // KEY_RIGHT
            {
                player.Move(1, 0, gameMap);
            }
            if (Raylib.IsKeyPressed((KeyboardKey)263)) // KEY_LEFT
            {
                player.Move(-1, 0, gameMap);
            }
            if (Raylib.IsKeyPressed((KeyboardKey)265)) // KEY_UP
            {
                player.Move(0, -1, gameMap);
            }
            if (Raylib.IsKeyPressed((KeyboardKey)264)) // KEY_DOWN
            {
                player.Move(0, 1, gameMap);
            }

            // Draw
            Raylib.BeginDrawing();
            // WORKAROUND: Using new Color() because Color.BLACK is not found.
            Raylib.ClearBackground(new Color(0, 0, 0, 255));
            gameMap.Draw(charSize);
            player.Draw(charSize);
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
