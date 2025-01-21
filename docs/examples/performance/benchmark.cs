using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FlexonCLI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexonExamples.Performance
{
    public class BenchmarkExample
    {
        public class TestData
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public DateTime Timestamp { get; set; }
            public Dictionary<string, object> Properties { get; set; }
            public List<double> Values { get; set; }
        }

        private TestData _testData;
        private byte[] _flexonData;
        private string _jsonData;
        private FlexonSerializer _serializer;
        private JsonSerializerOptions _jsonOptions;

        [GlobalSetup]
        public void Setup()
        {
            _testData = new TestData
            {
                Id = Guid.NewGuid(),
                Name = "Benchmark Test",
                Timestamp = DateTime.Now,
                Properties = new Dictionary<string, object>
                {
                    ["type"] = "benchmark",
                    ["version"] = 1.0,
                    ["enabled"] = true
                },
                Values = new List<double>
                {
                    1.1, 2.2, 3.3, 4.4, 5.5
                }
            };

            _serializer = new FlexonSerializer(new FlexonOptions
            {
                EnableCompression = true,
                UsePooledBuffers = true
            });

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false
            };

            // Pre-serialize for deserialization benchmarks
            _flexonData = _serializer.Serialize(_testData);
            _jsonData = JsonSerializer.Serialize(_testData, _jsonOptions);
        }

        [Benchmark]
        public byte[] FlexonSerialize()
        {
            return _serializer.Serialize(_testData);
        }

        [Benchmark]
        public string JsonSerialize()
        {
            return JsonSerializer.Serialize(_testData, _jsonOptions);
        }

        [Benchmark]
        public TestData FlexonDeserialize()
        {
            return _serializer.Deserialize<TestData>(_flexonData);
        }

        [Benchmark]
        public TestData JsonDeserialize()
        {
            return JsonSerializer.Deserialize<TestData>(_jsonData, _jsonOptions);
        }

        [Benchmark]
        public async Task FlexonStreamingSerialize()
        {
            using var stream = new MemoryStream();
            await _serializer.SerializeAsync(_testData, stream);
        }

        [Benchmark]
        public async Task JsonStreamingSerialize()
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, _testData, _jsonOptions);
        }
    }

    [MemoryDiagnoser]
    public class CompressionBenchmark
    {
        private TestData _testData;
        private FlexonSerializer _noCompressionSerializer;
        private FlexonSerializer _fastCompressionSerializer;
        private FlexonSerializer _optimalCompressionSerializer;

        [GlobalSetup]
        public void Setup()
        {
            _testData = GenerateLargeTestData();

            _noCompressionSerializer = new FlexonSerializer(new FlexonOptions
            {
                EnableCompression = false
            });

            _fastCompressionSerializer = new FlexonSerializer(new FlexonOptions
            {
                EnableCompression = true,
                CompressionLevel = System.IO.Compression.CompressionLevel.Fastest
            });

            _optimalCompressionSerializer = new FlexonSerializer(new FlexonOptions
            {
                EnableCompression = true,
                CompressionLevel = System.IO.Compression.CompressionLevel.Optimal
            });
        }

        private TestData GenerateLargeTestData()
        {
            var data = new TestData
            {
                Id = Guid.NewGuid(),
                Name = "Large Test Data",
                Timestamp = DateTime.Now,
                Properties = new Dictionary<string, object>(),
                Values = new List<double>()
            };

            // Add lots of properties
            for (int i = 0; i < 1000; i++)
            {
                data.Properties[$"prop_{i}"] = $"value_{i}";
                data.Values.Add(Math.Sin(i * 0.1));
            }

            return data;
        }

        [Benchmark(Baseline = true)]
        public byte[] NoCompression()
        {
            return _noCompressionSerializer.Serialize(_testData);
        }

        [Benchmark]
        public byte[] FastCompression()
        {
            return _fastCompressionSerializer.Serialize(_testData);
        }

        [Benchmark]
        public byte[] OptimalCompression()
        {
            return _optimalCompressionSerializer.Serialize(_testData);
        }
    }

    [MemoryDiagnoser]
    public class BufferPoolingBenchmark
    {
        private TestData _testData;
        private FlexonSerializer _pooledSerializer;
        private FlexonSerializer _nonPooledSerializer;

        [GlobalSetup]
        public void Setup()
        {
            _testData = GenerateLargeTestData();

            _pooledSerializer = new FlexonSerializer(new FlexonOptions
            {
                UsePooledBuffers = true,
                BufferSize = 8192
            });

            _nonPooledSerializer = new FlexonSerializer(new FlexonOptions
            {
                UsePooledBuffers = false
            });
        }

        private TestData GenerateLargeTestData()
        {
            // Similar to CompressionBenchmark.GenerateLargeTestData
            var data = new TestData
            {
                Id = Guid.NewGuid(),
                Name = "Buffer Pool Test",
                Timestamp = DateTime.Now,
                Properties = new Dictionary<string, object>(),
                Values = new List<double>()
            };

            for (int i = 0; i < 1000; i++)
            {
                data.Properties[$"prop_{i}"] = $"value_{i}";
                data.Values.Add(Math.Cos(i * 0.1));
            }

            return data;
        }

        [Benchmark(Baseline = true)]
        public void NonPooledOperations()
        {
            for (int i = 0; i < 100; i++)
            {
                var binary = _nonPooledSerializer.Serialize(_testData);
                var restored = _nonPooledSerializer.Deserialize<TestData>(binary);
            }
        }

        [Benchmark]
        public void PooledOperations()
        {
            for (int i = 0; i < 100; i++)
            {
                var binary = _pooledSerializer.Serialize(_testData);
                var restored = _pooledSerializer.Deserialize<TestData>(binary);
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Running FLEXON Performance Benchmarks");
            
            Console.WriteLine("\nBasic Serialization Benchmarks:");
            BenchmarkRunner.Run<BenchmarkExample>();

            Console.WriteLine("\nCompression Benchmarks:");
            BenchmarkRunner.Run<CompressionBenchmark>();

            Console.WriteLine("\nBuffer Pooling Benchmarks:");
            BenchmarkRunner.Run<BufferPoolingBenchmark>();
        }
    }
}
