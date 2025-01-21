using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Benchmarking.Generator
{
    public class BenchmarkData
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Value { get; set; }
        public string Timestamp { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        public Metadata Metadata { get; set; } = new();
        public byte[] BinaryData { get; set; } = Array.Empty<byte>();
    }

    public class Metadata
    {
        public string Category { get; set; } = "";
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public double Score { get; set; }
    }

    public static class BenchmarkGenerator
    {
        private static readonly Random random = new(42); // Fixed seed for reproducibility
        private static readonly string[] categories = { "A", "B", "C", "D", "E" };
        private static readonly string[] tags = { "important", "urgent", "normal", "low", "critical" };

        public static void GenerateData(int count, string outputFile)
        {
            try
            {
                var data = GenerateDataList(count, includeBinary: false);
                var settings = new JsonSerializerSettings 
                { 
                    Formatting = Formatting.Indented,
                    DateFormatString = "yyyy-MM-ddTHH:mm:ssZ"
                };
                File.WriteAllText(outputFile, JsonConvert.SerializeObject(data, settings));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public static void GenerateDataWithBinary(int count, string outputFile)
        {
            try
            {
                var data = GenerateDataList(count, includeBinary: true);
                var settings = new JsonSerializerSettings 
                { 
                    Formatting = Formatting.Indented,
                    DateFormatString = "yyyy-MM-ddTHH:mm:ssZ"
                };
                File.WriteAllText(outputFile, JsonConvert.SerializeObject(data, settings));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static List<BenchmarkData> GenerateDataList(int count, bool includeBinary)
        {
            var data = new List<BenchmarkData>();

            for (int i = 0; i < count; i++)
            {
                var selectedTags = new List<string>();
                int numTags = random.Next(1, 4);
                for (int t = 0; t < numTags; t++)
                {
                    selectedTags.Add(tags[random.Next(tags.Length)]);
                }

                var item = new BenchmarkData
                {
                    Id = i + 1,
                    Name = $"Item{i + 1}",
                    Value = Math.Round(random.NextDouble() * 1000, 2),
                    Timestamp = DateTime.UtcNow.AddDays(-random.Next(365)).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Tags = selectedTags,
                    Metadata = new Metadata
                    {
                        Category = categories[random.Next(categories.Length)],
                        Priority = random.Next(1, 6),
                        IsActive = random.Next(2) == 1,
                        Score = Math.Round(random.NextDouble() * 100, 2)
                    }
                };

                if (includeBinary)
                {
                    // Generate 1KB of random binary data per item
                    var binaryData = new byte[1024];
                    random.NextBytes(binaryData);
                    item.BinaryData = binaryData;
                }

                data.Add(item);
            }

            return data;
        }
    }
}
