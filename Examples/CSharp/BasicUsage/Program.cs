using System;
using System.Text.Json;
using System.Diagnostics;

class Program
{
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Email { get; set; }
        public List<string> Hobbies { get; set; }
    }

    static async Task Main()
    {
        Console.WriteLine("Flexon Basic Usage Example");
        Console.WriteLine("=========================\n");

        // Create sample data
        var person = new Person
        {
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com",
            Hobbies = new List<string> { "Reading", "Gaming", "Hiking" }
        };

        // Serialize to JSON
        var jsonFile = "person.json";
        var flexonFile = "person.flexon";
        var encryptedFile = "person_encrypted.flexon";
        var decryptedFile = "person_decrypted.json";

        Console.WriteLine("1. Creating JSON file...");
        await File.WriteAllTextAsync(jsonFile, JsonSerializer.Serialize(person, new JsonSerializerOptions { WriteIndented = true }));

        // Basic serialization
        Console.WriteLine("2. Converting to Flexon format...");
        RunFlexonCommand($"serialize -i {jsonFile} -o {flexonFile}");

        // Encryption
        Console.WriteLine("3. Creating encrypted Flexon file...");
        RunFlexonCommand($"serialize -i {jsonFile} -o {encryptedFile} -e mysecretkey");

        // Decryption
        Console.WriteLine("4. Decrypting Flexon file...");
        RunFlexonCommand($"deserialize -i {encryptedFile} -o {decryptedFile} -e mysecretkey");

        // Verify results
        Console.WriteLine("\nResults:");
        Console.WriteLine($"Original JSON size: {new FileInfo(jsonFile).Length} bytes");
        Console.WriteLine($"Flexon size: {new FileInfo(flexonFile).Length} bytes");
        Console.WriteLine($"Encrypted size: {new FileInfo(encryptedFile).Length} bytes");

        // Compare contents
        var original = await File.ReadAllTextAsync(jsonFile);
        var decrypted = await File.ReadAllTextAsync(decryptedFile);
        var originalObj = JsonSerializer.Deserialize<Person>(original);
        var decryptedObj = JsonSerializer.Deserialize<Person>(decrypted);

        Console.WriteLine("\nData verification:");
        Console.WriteLine($"Name matches: {originalObj.Name == decryptedObj.Name}");
        Console.WriteLine($"Age matches: {originalObj.Age == decryptedObj.Age}");
        Console.WriteLine($"Email matches: {originalObj.Email == decryptedObj.Email}");
        Console.WriteLine($"Hobbies match: {originalObj.Hobbies.SequenceEqual(decryptedObj.Hobbies)}");
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
