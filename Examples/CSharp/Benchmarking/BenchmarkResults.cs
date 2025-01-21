using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Benchmarking
{
    public class BenchmarkResult
    {
        public int RecordCount { get; set; }
        public string CompressionMethod { get; set; }
        public bool Encrypted { get; set; }
        public double SerializationTimeMs { get; set; }
        public double DeserializationTimeMs { get; set; }
        public long CompressedSizeBytes { get; set; }
        public double CompressionRatio { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class BenchmarkResultProcessor
    {
        private readonly string resultsDir;

        public BenchmarkResultProcessor(string resultsDirectory)
        {
            resultsDir = resultsDirectory;
        }

        public List<BenchmarkResult> ProcessResults()
        {
            var results = new List<BenchmarkResult>();
            var files = Directory.GetFiles(resultsDir, "data_*.flexon");

            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                var parts = filename.Split('_');
                
                if (parts.Length < 3) continue;

                var recordCount = int.Parse(parts[1]);
                var compression = parts[2].Replace(".flexon", "");
                var encrypted = filename.Contains("encrypted");

                var jsonFile = Path.Combine(resultsDir, $"data_{recordCount}.json");
                var originalSize = new FileInfo(jsonFile).Length;
                var compressedSize = new FileInfo(file).Length;

                results.Add(new BenchmarkResult
                {
                    RecordCount = recordCount,
                    CompressionMethod = compression,
                    Encrypted = encrypted,
                    CompressedSizeBytes = compressedSize,
                    CompressionRatio = (double)compressedSize / originalSize,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Save results as JSON
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(
                Path.Combine(resultsDir, "benchmark_results.json"), 
                JsonSerializer.Serialize(results, jsonOptions)
            );

            // Generate HTML report
            GenerateHtmlReport(results);

            return results;
        }

        private void GenerateHtmlReport(List<BenchmarkResult> results)
        {
            var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Flexon Benchmark Results</title>
    <script src='https://cdn.plot.ly/plotly-latest.min.js'></script>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .chart { width: 100%; height: 500px; margin: 20px 0; }
        table { border-collapse: collapse; width: 100%; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f5f5f5; }
        tr:nth-child(even) { background-color: #f9f9f9; }
    </style>
</head>
<body>
    <h1>Flexon Benchmark Results</h1>
    <p>Last updated: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") + @"</p>
    
    <div id='compressionRatioChart' class='chart'></div>
    <div id='fileSizeChart' class='chart'></div>
    
    <h2>Detailed Results</h2>
    <table>
        <tr>
            <th>Records</th>
            <th>Compression</th>
            <th>Encrypted</th>
            <th>Size (KB)</th>
            <th>Compression Ratio</th>
        </tr>";

            foreach (var result in results.OrderBy(r => r.RecordCount).ThenBy(r => r.CompressionMethod))
            {
                html += $@"
        <tr>
            <td>{result.RecordCount:N0}</td>
            <td>{result.CompressionMethod}</td>
            <td>{result.Encrypted}</td>
            <td>{result.CompressedSizeBytes / 1024.0:N2}</td>
            <td>{result.CompressionRatio:P2}</td>
        </tr>";
            }

            html += @"
    </table>

    <script>
        const results = " + JsonSerializer.Serialize(results) + @";
        
        // Compression Ratio Chart
        const ratioData = [];
        const methods = [...new Set(results.map(r => r.compressionMethod))];
        const recordCounts = [...new Set(results.map(r => r.recordCount))];
        
        methods.forEach(method => {
            ratioData.push({
                x: recordCounts,
                y: results
                    .filter(r => r.compressionMethod === method && !r.encrypted)
                    .map(r => r.compressionRatio),
                name: method,
                type: 'scatter',
                mode: 'lines+markers'
            });
        });

        Plotly.newPlot('compressionRatioChart', ratioData, {
            title: 'Compression Ratio by Method and Record Count',
            xaxis: { title: 'Number of Records', type: 'log' },
            yaxis: { title: 'Compression Ratio (smaller is better)' }
        });

        // File Size Chart
        const sizeData = [];
        
        methods.forEach(method => {
            sizeData.push({
                x: recordCounts,
                y: results
                    .filter(r => r.compressionMethod === method && !r.encrypted)
                    .map(r => r.compressedSizeBytes / 1024), // Convert to KB
                name: method,
                type: 'scatter',
                mode: 'lines+markers'
            });
        });

        Plotly.newPlot('fileSizeChart', sizeData, {
            title: 'File Size by Method and Record Count',
            xaxis: { title: 'Number of Records', type: 'log' },
            yaxis: { title: 'File Size (KB)', type: 'log' }
        });
    </script>
</body>
</html>";

            File.WriteAllText(Path.Combine(resultsDir, "benchmark_report.html"), html);
        }
    }
}
