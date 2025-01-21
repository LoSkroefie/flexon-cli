using System;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace FlexonCLI.AI.Utils
{
    public class AIUtils
    {
        public static string GeneratePromptFingerprint(string prompt, string model)
        {
            using var sha256 = SHA256.Create();
            var input = $"{prompt}:{model}:{DateTime.UtcNow:yyyy-MM-dd}";
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hash);
        }

        public static int EstimateTokenCount(string text)
        {
            // Simple estimation: ~4 characters per token
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        public static Dictionary<string, object> CreatePromptMetadata(
            string model,
            double temperature = 0.7,
            int maxTokens = 2048,
            double topP = 1.0,
            double frequencyPenalty = 0.0,
            double presencePenalty = 0.0)
        {
            return new Dictionary<string, object>
            {
                ["model"] = model,
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["version"] = "1.0.0",
                ["temperature"] = temperature,
                ["maxTokens"] = maxTokens,
                ["topP"] = topP,
                ["frequencyPenalty"] = frequencyPenalty,
                ["presencePenalty"] = presencePenalty
            };
        }

        public static Dictionary<string, object> CreateAIPrompt(
            string prompt,
            string model,
            List<Dictionary<string, string>> context = null,
            double[] embeddings = null)
        {
            var data = new Dictionary<string, object>
            {
                ["prompt"] = prompt,
                ["metadata"] = CreatePromptMetadata(model)
            };

            if (context != null)
            {
                data["context"] = context;
            }

            if (embeddings != null)
            {
                data["embeddings"] = embeddings;
            }

            var fingerprint = GeneratePromptFingerprint(prompt, model);
            data["security"] = new Dictionary<string, object>
            {
                ["fingerprint"] = fingerprint,
                ["auditTrail"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["timestamp"] = DateTime.UtcNow.ToString("o"),
                        ["action"] = "created",
                        ["actor"] = Environment.UserName
                    }
                }
            };

            return data;
        }

        public static Dictionary<string, object> CreateTrainingDataset(
            List<(string input, string output)> pairs,
            string model,
            string description = null)
        {
            var dataset = pairs.Select(pair => new Dictionary<string, object>
            {
                ["input"] = pair.input,
                ["output"] = pair.output,
                ["metadata"] = new Dictionary<string, object>
                {
                    ["timestamp"] = DateTime.UtcNow.ToString("o"),
                    ["source"] = "user_generated",
                    ["tags"] = new string[] { "training" },
                    ["quality"] = 1.0
                }
            }).ToList();

            return new Dictionary<string, object>
            {
                ["dataset"] = dataset,
                ["metadata"] = new Dictionary<string, object>
                {
                    ["model"] = model,
                    ["version"] = "1.0.0",
                    ["created"] = DateTime.UtcNow.ToString("o"),
                    ["description"] = description ?? "Training dataset",
                    ["parameters"] = new Dictionary<string, object>
                    {
                        ["embeddingModel"] = "default",
                        ["tokenizer"] = "default",
                        ["maxTokens"] = 2048
                    }
                }
            };
        }

        public static bool ValidateAIData(string jsonData, string schemaPath)
        {
            try
            {
                var data = JsonSerializer.Deserialize<JsonElement>(jsonData);
                var schema = JsonSerializer.Deserialize<JsonElement>(
                    System.IO.File.ReadAllText(schemaPath));
                
                // Basic validation (in practice, use a proper JSON Schema validator)
                return data.ValueKind == JsonValueKind.Object;
            }
            catch
            {
                return false;
            }
        }
    }
}
