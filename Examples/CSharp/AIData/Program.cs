using System;
using System.Text.Json;
using System.Diagnostics;
using FlexonCLI.AI.Utils;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Flexon AI Data Example");
        Console.WriteLine("=====================\n");

        // Create AI prompt with context
        var context = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                ["role"] = "system",
                ["content"] = "You are a helpful AI assistant specializing in programming."
            },
            new Dictionary<string, string>
            {
                ["role"] = "user",
                ["content"] = "How do I implement a binary search tree in C#?"
            }
        };

        // Generate sample embeddings
        var embeddings = Enumerable.Range(0, 10).Select(i => Math.Sin(i * 0.1)).ToArray();

        // Create AI prompt using utilities
        var prompt = AIUtils.CreateAIPrompt(
            "Explain binary search tree implementation with code examples.",
            "gpt-4",
            context,
            embeddings);

        // Save prompt to JSON
        var promptFile = "prompt.json";
        var flexonFile = "ai_data.flexon";
        var schemaFile = "../../../FlexonCLI/AI/Schemas/prompt_schema.json";

        Console.WriteLine("1. Saving AI prompt...");
        await File.WriteAllTextAsync(promptFile, JsonSerializer.Serialize(prompt, new JsonSerializerOptions { WriteIndented = true }));

        // Create training data
        var trainingPairs = new List<(string input, string output)>
        {
            ("What is a binary search tree?", "A binary search tree is a data structure where each node has at most two children..."),
            ("How to insert into BST?", "To insert into a BST, compare the value with the root and recursively insert into left or right subtree..."),
            ("BST search algorithm?", "To search in a BST, start at root, compare value, and traverse left or right based on comparison...")
        };

        var trainingData = AIUtils.CreateTrainingDataset(
            trainingPairs,
            "bst-tutor-v1",
            "Binary Search Tree tutorial dataset");

        var trainingFile = "training.json";
        var trainingSchemaFile = "../../../FlexonCLI/AI/Schemas/training_schema.json";
        var trainingFlexonFile = "training.flexon";

        Console.WriteLine("2. Saving training data...");
        await File.WriteAllTextAsync(trainingFile, JsonSerializer.Serialize(trainingData, new JsonSerializerOptions { WriteIndented = true }));

        // Validate and serialize prompt
        Console.WriteLine("3. Validating and serializing prompt...");
        RunFlexonCommand($"serialize -i {promptFile} -o {flexonFile} -s {schemaFile} -e aikey ChaCha20");

        // Validate and serialize training data
        Console.WriteLine("4. Validating and serializing training data...");
        RunFlexonCommand($"serialize -i {trainingFile} -o {trainingFlexonFile} -s {trainingSchemaFile}");

        // Load and verify
        Console.WriteLine("5. Loading AI data...");
        Directory.CreateDirectory("ai_output");
        RunFlexonCommand($"deserialize -i {flexonFile} -o ai_output/prompt_decoded.json -e aikey");

        var loadedPrompt = JsonSerializer.Deserialize<Dictionary<string, object>>(
            await File.ReadAllTextAsync("ai_output/prompt_decoded.json"));

        Console.WriteLine("\nVerification:");
        Console.WriteLine($"Prompt loaded: {loadedPrompt.ContainsKey("prompt")}");
        Console.WriteLine($"Metadata present: {loadedPrompt.ContainsKey("metadata")}");
        Console.WriteLine($"Context count: {(loadedPrompt["context"] as JsonElement).EnumerateArray().Count()}");
        Console.WriteLine($"Embeddings size: {(loadedPrompt["embeddings"] as JsonElement).EnumerateArray().Count()}");
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
