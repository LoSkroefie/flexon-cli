using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Benchmarking.Generator;

namespace Benchmarking
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

    [MemoryDiagnoser]
    public class FlexonBenchmarks
    {
        private readonly string jsonFile;
        private readonly string jsonBinaryFile;
        private readonly string flexonFile;
        private readonly string flexonBinaryFile;
        private readonly string encryptedFile;
        private readonly string key = "benchmark_key";
        private List<BenchmarkData> data;
        private List<BenchmarkData> binaryData;

        public FlexonBenchmarks()
        {
            var size = 1000; // Default size
            jsonFile = $"data_{size}.json";
            jsonBinaryFile = $"data_{size}_binary.json";
            flexonFile = $"data_{size}.flexon";
            flexonBinaryFile = $"data_{size}_binary.flexon";
            encryptedFile = $"data_{size}_encrypted.flexon";
        }

        [GlobalSetup]
        public void Setup()
        {
            // Generate text-only data
            BenchmarkGenerator.GenerateData(1000, jsonFile);
            data = JsonSerializer.Deserialize<List<BenchmarkData>>(File.ReadAllText(jsonFile));

            // Generate data with binary content
            BenchmarkGenerator.GenerateDataWithBinary(1000, jsonBinaryFile);
            binaryData = JsonSerializer.Deserialize<List<BenchmarkData>>(File.ReadAllText(jsonBinaryFile));
        }

        [Benchmark(Baseline = true)]
        public void JsonSerialize()
        {
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText("bench_json.json", json);
        }

        [Benchmark]
        public void JsonSerializeBinary()
        {
            var json = JsonSerializer.Serialize(binaryData);
            File.WriteAllText("bench_json_binary.json", json);
        }

        [Benchmark]
        public void FlexonSerialize()
        {
            RunFlexonCommand($"serialize -i {jsonFile} -o {flexonFile}");
        }

        [Benchmark]
        public void FlexonSerializeBinary()
        {
            RunFlexonCommand($"serialize -i {jsonBinaryFile} -o {flexonBinaryFile}");
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
            var obj = JsonSerializer.Deserialize<List<BenchmarkData>>(json);
        }

        [Benchmark]
        public void JsonDeserializeBinary()
        {
            var json = File.ReadAllText(jsonBinaryFile);
            var obj = JsonSerializer.Deserialize<List<BenchmarkData>>(json);
        }

        [Benchmark]
        public void FlexonDeserialize()
        {
            RunFlexonCommand($"deserialize -i {flexonFile} -o bench_decoded.json");
        }

        [Benchmark]
        public void FlexonDeserializeBinary()
        {
            RunFlexonCommand($"deserialize -i {flexonBinaryFile} -o bench_decoded_binary.json");
        }

        [Benchmark]
        public void FlexonDeserializeEncrypted()
        {
            RunFlexonCommand($"deserialize -i {encryptedFile} -o bench_decrypted.json -e {key}");
        }

        private void RunFlexonCommand(string args)
        {
            var targetPath = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "FlexonCLI.dll"
            ));
            var targetConfigPath = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "FlexonCLI.runtimeconfig.json"
            ));

            // Look for FlexonCLI files in the Release directory
            var sourcePath = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..", "FlexonCLI", "bin", "Release", "net8.0", "FlexonCLI.dll"
            ));
            var sourceConfigPath = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..", "FlexonCLI", "bin", "Release", "net8.0", "FlexonCLI.runtimeconfig.json"
            ));

            // Copy FlexonCLI files from the source to the target directory if they don't exist
            if (!File.Exists(targetPath) || !File.Exists(targetConfigPath))
            {
                if (!File.Exists(sourcePath) || !File.Exists(sourceConfigPath))
                {
                    Console.WriteLine($"FlexonCLI files not found at source path: {sourcePath}");
                    // Try alternative path
                    sourcePath = Path.GetFullPath(Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "..", "FlexonCLI", "bin", "Release", "net8.0", "FlexonCLI.dll"
                    ));
                    sourceConfigPath = Path.GetFullPath(Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "..", "FlexonCLI", "bin", "Release", "net8.0", "FlexonCLI.runtimeconfig.json"
                    ));
                    if (!File.Exists(sourcePath) || !File.Exists(sourceConfigPath))
                    {
                        throw new FileNotFoundException($"FlexonCLI files not found at {sourcePath}");
                    }
                }

                try
                {
                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    File.Copy(sourcePath, targetPath, true);
                    File.Copy(sourceConfigPath, targetConfigPath, true);
                    Console.WriteLine($"Copied FlexonCLI files from {Path.GetDirectoryName(sourcePath)} to {targetDir}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying FlexonCLI files: {ex.Message}");
                    throw;
                }
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"{targetPath} {args}",
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
                var error = process.StandardError.ReadToEnd();
                throw new Exception($"Flexon command failed: {error}");
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length >= 2)
            {
                // Generate test data mode
                int count = int.Parse(args[0]);
                string outputFile = args[1];
                BenchmarkGenerator.GenerateData(count, outputFile);
            }
            else
            {
                // Process benchmark results
                var processor = new BenchmarkResultProcessor("results");
                var results = processor.ProcessResults();
                
                Console.WriteLine($"Processed {results.Count} benchmark results");
                Console.WriteLine($"Results saved to results/benchmark_results.json");
                Console.WriteLine($"HTML report generated at results/benchmark_report.html");
                
                // Run new benchmarks if requested
                if (args.Length == 0 || args[0] == "--run")
                {
                    var summary = BenchmarkRunner.Run<FlexonBenchmarks>();
                }
            }
        }
    }
}
