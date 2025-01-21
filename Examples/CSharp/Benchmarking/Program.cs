using System;
using System.Text.Json;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmarking
{
    public class WeatherData
    {
        public string Location { get; set; }
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public double WindSpeed { get; set; }
        public string WindDirection { get; set; }
        public List<string> Conditions { get; set; }
        public Dictionary<string, double> Measurements { get; set; }
    }

    [MemoryDiagnoser]
    public class FlexonBenchmarks
    {
        private readonly string jsonFile = "weather.json";
        private readonly string flexonFile = "weather.flexon";
        private readonly string encryptedFile = "weather_encrypted.flexon";
        private readonly string key = "benchmark_key";
        private WeatherData data;

        [GlobalSetup]
        public void Setup()
        {
            // Create large dataset
            var random = new Random(42);
            data = new WeatherData
            {
                Location = "New York",
                Timestamp = DateTime.UtcNow,
                Temperature = random.NextDouble() * 40,
                Humidity = random.NextDouble() * 100,
                Pressure = random.NextDouble() * 1000 + 900,
                WindSpeed = random.NextDouble() * 100,
                WindDirection = "NE",
                Conditions = new List<string> { "Cloudy", "Rain", "Wind" },
                Measurements = Enumerable.Range(0, 1000).ToDictionary(
                    i => $"sensor_{i}",
                    i => random.NextDouble() * 1000
                )
            };

            // Save to JSON
            File.WriteAllText(jsonFile, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }

        [Benchmark(Baseline = true)]
        public void JsonSerialize()
        {
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText("bench_json.json", json);
        }

        [Benchmark]
        public void FlexonSerialize()
        {
            RunFlexonCommand($"serialize -i {jsonFile} -o {flexonFile}");
        }

        [Benchmark]
        public void FlexonSerializeEncrypted()
        {
            RunFlexonCommand($"serialize -i {jsonFile} -o {encryptedFile} -e {key} AES256");
        }

        [Benchmark]
        public void JsonDeserialize()
        {
            var json = File.ReadAllText(jsonFile);
            var obj = JsonSerializer.Deserialize<WeatherData>(json);
        }

        [Benchmark]
        public void FlexonDeserialize()
        {
            RunFlexonCommand($"deserialize -i {flexonFile} -o bench_decoded.json");
        }

        [Benchmark]
        public void FlexonDeserializeEncrypted()
        {
            RunFlexonCommand($"deserialize -i {encryptedFile} -o bench_decrypted.json -e {key}");
        }

        private void RunFlexonCommand(string args)
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

    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Flexon Benchmarking Example");
            Console.WriteLine("==========================\n");

            Console.WriteLine("Running benchmarks...");
            var summary = BenchmarkRunner.Run<FlexonBenchmarks>();

            Console.WriteLine("\nBenchmark complete. Check BenchmarkDotNet-Reports directory for detailed results.");
        }
    }
}
