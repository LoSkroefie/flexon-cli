using System;
using System.Text.Json;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

class Program
{
    public class Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class PlayerState
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public int Health { get; set; }
        public Vector2 Position { get; set; }
        public List<string> Inventory { get; set; }
        public Dictionary<string, int> Stats { get; set; }
    }

    public class GameState
    {
        public string GameVersion { get; set; }
        public DateTime SaveTime { get; set; }
        public PlayerState Player { get; set; }
        public List<string> CompletedQuests { get; set; }
        public Dictionary<string, bool> Achievements { get; set; }
    }

    static async Task Main()
    {
        Console.WriteLine("Flexon Game State Example");
        Console.WriteLine("========================\n");

        // Create sample game state
        var gameState = new GameState
        {
            GameVersion = "1.0.0",
            SaveTime = DateTime.UtcNow,
            Player = new PlayerState
            {
                Name = "Hero123",
                Level = 42,
                Health = 100,
                Position = new Vector2 { X = 123.45f, Y = 678.90f },
                Inventory = new List<string> { "Sword", "Shield", "Health Potion" },
                Stats = new Dictionary<string, int>
                {
                    ["Strength"] = 18,
                    ["Dexterity"] = 15,
                    ["Intelligence"] = 12
                }
            },
            CompletedQuests = new List<string>
            {
                "Tutorial",
                "Dragon Slayer",
                "Lost Artifact"
            },
            Achievements = new Dictionary<string, bool>
            {
                ["FirstBattle"] = true,
                ["DragonSlayer"] = true,
                ["MasterCollector"] = false
            }
        };

        // Create a screenshot
        var screenshot = CreateGameScreenshot();
        await File.WriteAllBytesAsync("screenshot.png", screenshot);

        // Save game state to JSON
        var stateFile = "gamestate.json";
        var saveFile = "savegame.flexon";
        
        Console.WriteLine("1. Saving game state...");
        await File.WriteAllTextAsync(stateFile, JsonSerializer.Serialize(gameState, new JsonSerializerOptions { WriteIndented = true }));

        // Create save package with state and screenshot
        Console.WriteLine("2. Creating save package...");
        RunFlexonCommand($"serialize -i {stateFile} -i screenshot.png -o {saveFile} -e savegamekey");

        // Load save package
        Console.WriteLine("3. Loading save package...");
        Directory.CreateDirectory("loaded_save");
        RunFlexonCommand($"deserialize -i {saveFile} -o loaded_save -e savegamekey");

        // Verify loaded state
        Console.WriteLine("\nVerifying save data:");
        var loadedJson = await File.ReadAllTextAsync(Path.Combine("loaded_save", "gamestate.json"));
        var loadedState = JsonSerializer.Deserialize<GameState>(loadedJson);

        Console.WriteLine($"Player name: {loadedState.Player.Name}");
        Console.WriteLine($"Level: {loadedState.Player.Level}");
        Console.WriteLine($"Position: ({loadedState.Player.Position.X}, {loadedState.Player.Position.Y})");
        Console.WriteLine($"Completed quests: {loadedState.CompletedQuests.Count}");
        Console.WriteLine($"Screenshot recovered: {File.Exists(Path.Combine("loaded_save", "screenshot.png"))}");
    }

    static byte[] CreateGameScreenshot()
    {
        // Create a simple image for demonstration
        using var bitmap = new Bitmap(800, 600);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Fill background
        graphics.Clear(Color.DarkBlue);
        
        // Draw some game elements
        using var brush = new SolidBrush(Color.White);
        using var font = new Font(FontFamily.GenericSansSerif, 20);
        graphics.DrawString("Game Screenshot", font, brush, new PointF(300, 250));

        // Convert to PNG
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    static void RunFlexonCommand(string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "flexon-cli",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Flexon command failed: {process.StandardError.ReadToEnd()}");
        }
    }
}
