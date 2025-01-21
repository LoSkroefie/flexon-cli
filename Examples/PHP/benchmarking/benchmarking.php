<?php

declare(strict_types=1);

class FlexonHelper {
    public static function runCommand(string $args): void {
        $command = "flexon-cli $args";
        $result = shell_exec($command);
        if ($result === null) {
            throw new RuntimeException("Flexon command failed: $command");
        }
    }
}

function createTestData(): array {
    $possibleTags = ['web', 'database', 'cache', 'compute', 'storage'];
    $records = [];

    for ($i = 0; $i < 1000; $i++) {
        $records[] = [
            'id' => "record-$i",
            'timestamp' => date(DATE_ISO8601),
            'value' => mt_rand() / mt_getrandmax() * 1000,
            'metrics' => [
                'cpu' => mt_rand() / mt_getrandmax() * 100,
                'memory' => mt_rand() / mt_getrandmax() * 16384,
                'disk' => mt_rand() / mt_getrandmax() * 1024
            ],
            'tags' => array_filter($possibleTags, fn() => mt_rand() / mt_getrandmax() > 0.5)
        ];
    }

    return [
        'id' => "test-" . time(),
        'name' => 'Benchmark Dataset',
        'description' => 'Large dataset for benchmarking Flexon performance',
        'metadata' => [
            'created_at' => date(DATE_ISO8601),
            'version' => '1.0',
            'type' => 'benchmark'
        ],
        'records' => $records
    ];
}

function benchmarkJsonOperations(array $data, int $iterations): array {
    $jsonSerTime = microtime(true);
    for ($i = 0; $i < $iterations; $i++) {
        json_encode($data);
    }
    $jsonSerTime = (microtime(true) - $jsonSerTime) / $iterations;

    $jsonStr = json_encode($data);
    $jsonDeserTime = microtime(true);
    for ($i = 0; $i < $iterations; $i++) {
        json_decode($jsonStr, true);
    }
    $jsonDeserTime = (microtime(true) - $jsonDeserTime) / $iterations;

    return [$jsonSerTime, $jsonDeserTime];
}

function benchmarkFlexonOperations(string $jsonFile, int $iterations): array {
    $serTime = microtime(true);
    for ($i = 0; $i < $iterations; $i++) {
        FlexonHelper::runCommand("serialize -i $jsonFile -o benchmark_test.flexon");
    }
    $serTime = (microtime(true) - $serTime) / $iterations;

    $deserTime = microtime(true);
    for ($i = 0; $i < $iterations; $i++) {
        FlexonHelper::runCommand("deserialize -i benchmark_test.flexon -o benchmark_test.json");
    }
    $deserTime = (microtime(true) - $deserTime) / $iterations;

    return [$serTime, $deserTime];
}

echo "Flexon Benchmarking Example (PHP)\n";
echo "==============================\n\n";

// Create test data
$data = createTestData();
$jsonFile = 'benchmark_data.json';
$flexonFile = 'benchmark_data.flexon';
$encryptedFile = 'benchmark_encrypted.flexon';

echo "1. Creating test data...\n";
file_put_contents($jsonFile, json_encode($data, JSON_PRETTY_PRINT));

// Basic serialization
echo "2. Testing basic serialization...\n";
FlexonHelper::runCommand("serialize -i $jsonFile -o $flexonFile");

// Encrypted serialization
echo "3. Testing encrypted serialization...\n";
FlexonHelper::runCommand("serialize -i $jsonFile -o $encryptedFile -e benchmarkkey");

// Test different encryption algorithms
echo "\n4. Testing different encryption algorithms...\n";

// AES-256
echo "Testing AES-256...\n";
FlexonHelper::runCommand("serialize -i $jsonFile -o benchmark_aes.flexon -e benchmarkkey AES256");

// ChaCha20
echo "Testing ChaCha20...\n";
FlexonHelper::runCommand("serialize -i $jsonFile -o benchmark_chacha20.flexon -e benchmarkkey ChaCha20");

// TripleDES
echo "Testing TripleDES...\n";
FlexonHelper::runCommand("serialize -i $jsonFile -o benchmark_tripledes.flexon -e benchmarkkey TripleDES");

// Run benchmarks
echo "\n5. Running benchmarks...\n";
$iterations = 10;

echo "\nJSON Operations:\n";
[$jsonSerTime, $jsonDeserTime] = benchmarkJsonOperations($data, $iterations);
echo "Average JSON serialization time: $jsonSerTime seconds\n";
echo "Average JSON deserialization time: $jsonDeserTime seconds\n";

echo "\nFlexon Operations:\n";
[$flexonSerTime, $flexonDeserTime] = benchmarkFlexonOperations($jsonFile, $iterations);
echo "Average Flexon serialization time: $flexonSerTime seconds\n";
echo "Average Flexon deserialization time: $flexonDeserTime seconds\n";

// Compare file sizes
echo "\nFile size comparison:\n";
echo "Original JSON: " . filesize($jsonFile) . " bytes\n";
echo "Flexon: " . filesize($flexonFile) . " bytes\n";
echo "AES-256: " . filesize('benchmark_aes.flexon') . " bytes\n";
echo "ChaCha20: " . filesize('benchmark_chacha20.flexon') . " bytes\n";
echo "TripleDES: " . filesize('benchmark_tripledes.flexon') . " bytes\n";

// Additional benchmarks
echo "\n6. Running detailed benchmarks...\n";

echo "\nDetailed JSON Operations:\n";
$start = microtime(true);
for ($i = 0; $i < 100; $i++) {
    json_encode($data);
}
$end = microtime(true);
echo "JSON serialize (100x): " . ($end - $start) . " seconds\n";

$jsonStr = json_encode($data);
$start = microtime(true);
for ($i = 0; $i < 100; $i++) {
    json_decode($jsonStr, true);
}
$end = microtime(true);
echo "JSON deserialize (100x): " . ($end - $start) . " seconds\n";

echo "\nDetailed Flexon Operations:\n";
$start = microtime(true);
for ($i = 0; $i < 10; $i++) {
    FlexonHelper::runCommand("serialize -i $jsonFile -o benchmark_test.flexon");
}
$end = microtime(true);
echo "Flexon serialize (10x): " . ($end - $start) . " seconds\n";

$start = microtime(true);
for ($i = 0; $i < 10; $i++) {
    FlexonHelper::runCommand("deserialize -i benchmark_test.flexon -o benchmark_test.json");
}
$end = microtime(true);
echo "Flexon deserialize (10x): " . ($end - $start) . " seconds\n";
