using System;
using System.Text.Json;
using System.Security.Cryptography;
using System.Diagnostics;

class Program
{
    public class DatabaseConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ApiConfig
    {
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public int Timeout { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }

    public class AppConfig
    {
        public string Environment { get; set; }
        public DatabaseConfig Database { get; set; }
        public ApiConfig Api { get; set; }
        public Dictionary<string, string> Features { get; set; }
        public List<string> AllowedOrigins { get; set; }
    }

    static async Task Main()
    {
        Console.WriteLine("Flexon Secure Configuration Example");
        Console.WriteLine("=================================\n");

        // Create sample configuration
        var config = new AppConfig
        {
            Environment = "production",
            Database = new DatabaseConfig
            {
                Host = "db.example.com",
                Port = 5432,
                Username = "admin",
                Password = "super_secret_password"
            },
            Api = new ApiConfig
            {
                Endpoint = "https://api.example.com/v1",
                ApiKey = "sk_live_12345abcdef",
                Timeout = 30000,
                Headers = new Dictionary<string, string>
                {
                    ["User-Agent"] = "MyApp/1.0",
                    ["X-Custom-Header"] = "custom-value"
                }
            },
            Features = new Dictionary<string, string>
            {
                ["feature1"] = "enabled",
                ["feature2"] = "disabled",
                ["beta"] = "enabled"
            },
            AllowedOrigins = new List<string>
            {
                "https://app.example.com",
                "https://admin.example.com"
            }
        };

        // Create schema for validation
        var schema = new
        {
            type = "object",
            required = new[] { "Environment", "Database", "Api" },
            properties = new
            {
                Environment = new
                {
                    type = "string",
                    enum = new[] { "development", "staging", "production" }
                },
                Database = new
                {
                    type = "object",
                    required = new[] { "Host", "Port", "Username", "Password" }
                },
                Api = new
                {
                    type = "object",
                    required = new[] { "Endpoint", "ApiKey" }
                }
            }
        };

        // Save configuration and schema
        var configFile = "config.json";
        var schemaFile = "config_schema.json";
        var flexonFile = "config.flexon";

        Console.WriteLine("1. Creating configuration files...");
        await File.WriteAllTextAsync(configFile, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(schemaFile, JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true }));

        // Create secure configuration package
        Console.WriteLine("2. Creating secure configuration package...");
        var masterKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Console.WriteLine($"Generated master key: {masterKey}");

        RunFlexonCommand($"serialize -i {configFile} -o {flexonFile} -s {schemaFile} -e {masterKey} AES256");

        // Test configuration loading
        Console.WriteLine("3. Loading secure configuration...");
        Directory.CreateDirectory("config_output");
        RunFlexonCommand($"deserialize -i {flexonFile} -o config_output/config_decoded.json -e {masterKey}");

        var loadedConfig = JsonSerializer.Deserialize<AppConfig>(
            await File.ReadAllTextAsync("config_output/config_decoded.json"));

        Console.WriteLine("\nVerification:");
        Console.WriteLine($"Environment: {loadedConfig.Environment}");
        Console.WriteLine($"Database Host: {loadedConfig.Database.Host}");
        Console.WriteLine($"API Endpoint: {loadedConfig.Api.Endpoint}");
        Console.WriteLine($"Features count: {loadedConfig.Features.Count}");
        Console.WriteLine($"Allowed origins: {loadedConfig.AllowedOrigins.Count}");

        // Test different encryption algorithms
        Console.WriteLine("\n4. Testing different encryption algorithms...");

        // ChaCha20
        Console.WriteLine("\nTesting ChaCha20...");
        RunFlexonCommand($"serialize -i {configFile} -o config_chacha20.flexon -e {masterKey} ChaCha20");
        RunFlexonCommand($"deserialize -i config_chacha20.flexon -o config_output/config_chacha20.json -e {masterKey}");

        // TripleDES
        Console.WriteLine("\nTesting TripleDES...");
        RunFlexonCommand($"serialize -i {configFile} -o config_tripledes.flexon -e {masterKey} TripleDES");
        RunFlexonCommand($"deserialize -i config_tripledes.flexon -o config_output/config_tripledes.json -e {masterKey}");

        // Compare file sizes
        Console.WriteLine("\nFile size comparison:");
        Console.WriteLine($"Original JSON: {new FileInfo(configFile).Length} bytes");
        Console.WriteLine($"AES-256: {new FileInfo(flexonFile).Length} bytes");
        Console.WriteLine($"ChaCha20: {new FileInfo("config_chacha20.flexon").Length} bytes");
        Console.WriteLine($"TripleDES: {new FileInfo("config_tripledes.flexon").Length} bytes");
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
