use chrono::Utc;
use rand::Rng;
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::fs;
use std::process::Command;
use std::time::{Duration, Instant};

#[derive(Debug, Serialize, Deserialize)]
struct TestData {
    id: String,
    name: String,
    description: String,
    metadata: HashMap<String, serde_json::Value>,
    records: Vec<HashMap<String, serde_json::Value>>,
}

struct FlexonHelper;

impl FlexonHelper {
    fn run_command(args: &str) -> Result<(), Box<dyn std::error::Error>> {
        let status = Command::new("cmd.exe")
            .args(&["/c", &format!("flexon-cli {}", args)])
            .status()?;

        if !status.success() {
            return Err("Flexon command failed".into());
        }
        Ok(())
    }
}

fn create_test_data() -> TestData {
    let mut rng = rand::thread_rng();
    let possible_tags = vec!["web", "database", "cache", "compute", "storage"];

    let mut metadata = HashMap::new();
    metadata.insert(
        "created_at".to_string(),
        serde_json::Value::String(Utc::now().to_rfc3339()),
    );
    metadata.insert(
        "version".to_string(),
        serde_json::Value::String("1.0".to_string()),
    );
    metadata.insert(
        "type".to_string(),
        serde_json::Value::String("benchmark".to_string()),
    );

    let mut records = Vec::new();
    for i in 0..1000 {
        let mut record = HashMap::new();
        record.insert(
            "id".to_string(),
            serde_json::Value::String(format!("record-{}", i)),
        );
        record.insert(
            "timestamp".to_string(),
            serde_json::Value::String(Utc::now().to_rfc3339()),
        );
        record.insert(
            "value".to_string(),
            serde_json::Value::Number(serde_json::Number::from_f64(rng.gen_range(0.0..1000.0)).unwrap()),
        );

        let mut metrics = HashMap::new();
        metrics.insert(
            "cpu".to_string(),
            serde_json::Value::Number(serde_json::Number::from_f64(rng.gen_range(0.0..100.0)).unwrap()),
        );
        metrics.insert(
            "memory".to_string(),
            serde_json::Value::Number(serde_json::Number::from_f64(rng.gen_range(0.0..16384.0)).unwrap()),
        );
        metrics.insert(
            "disk".to_string(),
            serde_json::Value::Number(serde_json::Number::from_f64(rng.gen_range(0.0..1024.0)).unwrap()),
        );
        record.insert(
            "metrics".to_string(),
            serde_json::Value::Object(serde_json::Map::from_iter(metrics)),
        );

        let tags: Vec<String> = possible_tags
            .iter()
            .filter(|_| rng.gen_bool(0.5))
            .map(|&s| s.to_string())
            .collect();
        record.insert(
            "tags".to_string(),
            serde_json::Value::Array(tags.into_iter().map(serde_json::Value::String).collect()),
        );

        records.push(record);
    }

    TestData {
        id: format!("test-{}", Utc::now().timestamp_nanos()),
        name: "Benchmark Dataset".to_string(),
        description: "Large dataset for benchmarking Flexon performance".to_string(),
        metadata,
        records,
    }
}

fn benchmark_json_serialization(data: &TestData, iterations: usize) -> Duration {
    let start = Instant::now();
    for _ in 0..iterations {
        serde_json::to_string(data).unwrap();
    }
    start.elapsed() / iterations as u32
}

fn benchmark_json_deserialization(json_data: &str, iterations: usize) -> Duration {
    let start = Instant::now();
    for _ in 0..iterations {
        let _: TestData = serde_json::from_str(json_data).unwrap();
    }
    start.elapsed() / iterations as u32
}

fn benchmark_flexon_operations(
    json_file: &str,
    iterations: usize,
) -> Result<(Duration, Duration), Box<dyn std::error::Error>> {
    // Benchmark serialization
    let mut total_serialization = Duration::new(0, 0);
    for _ in 0..iterations {
        let start = Instant::now();
        FlexonHelper::run_command(&format!(
            "serialize -i {} -o benchmark_test.flexon",
            json_file
        ))?;
        total_serialization += start.elapsed();
    }

    // Benchmark deserialization
    let mut total_deserialization = Duration::new(0, 0);
    for _ in 0..iterations {
        let start = Instant::now();
        FlexonHelper::run_command(&format!(
            "deserialize -i benchmark_test.flexon -o benchmark_test.json"
        ))?;
        total_deserialization += start.elapsed();
    }

    Ok((
        total_serialization / iterations as u32,
        total_deserialization / iterations as u32,
    ))
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("Flexon Benchmarking Example (Rust)");
    println!("================================\n");

    // Create test data
    let data = create_test_data();
    let json_file = "benchmark_data.json";
    let flexon_file = "benchmark_data.flexon";
    let encrypted_file = "benchmark_encrypted.flexon";

    println!("1. Creating test data...");
    let json_data = serde_json::to_string_pretty(&data)?;
    fs::write(json_file, &json_data)?;

    // Basic serialization
    println!("2. Testing basic serialization...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o {}",
        json_file, flexon_file
    ))?;

    // Encrypted serialization
    println!("3. Testing encrypted serialization...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o {} -e benchmarkkey",
        json_file, encrypted_file
    ))?;

    // Test different encryption algorithms
    println!("\n4. Testing different encryption algorithms...");

    // AES-256
    println!("Testing AES-256...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o benchmark_aes.flexon -e benchmarkkey AES256",
        json_file
    ))?;

    // ChaCha20
    println!("Testing ChaCha20...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o benchmark_chacha20.flexon -e benchmarkkey ChaCha20",
        json_file
    ))?;

    // TripleDES
    println!("Testing TripleDES...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o benchmark_tripledes.flexon -e benchmarkkey TripleDES",
        json_file
    ))?;

    // Run benchmarks
    println!("\n5. Running benchmarks...");
    let iterations = 10;

    println!("\nJSON Operations:");
    let json_ser_time = benchmark_json_serialization(&data, iterations);
    println!("Average JSON serialization time: {:?}", json_ser_time);

    let json_deser_time = benchmark_json_deserialization(&json_data, iterations);
    println!("Average JSON deserialization time: {:?}", json_deser_time);

    println!("\nFlexon Operations:");
    let (flexon_ser_time, flexon_deser_time) =
        benchmark_flexon_operations(json_file, iterations)?;
    println!("Average Flexon serialization time: {:?}", flexon_ser_time);
    println!("Average Flexon deserialization time: {:?}", flexon_deser_time);

    // Compare file sizes
    println!("\nFile size comparison:");
    let json_size = fs::metadata(json_file)?.len();
    let flexon_size = fs::metadata(flexon_file)?.len();
    let aes_size = fs::metadata("benchmark_aes.flexon")?.len();
    let chacha20_size = fs::metadata("benchmark_chacha20.flexon")?.len();
    let tripledes_size = fs::metadata("benchmark_tripledes.flexon")?.len();

    println!("Original JSON: {} bytes", json_size);
    println!("Flexon: {} bytes", flexon_size);
    println!("AES-256: {} bytes", aes_size);
    println!("ChaCha20: {} bytes", chacha20_size);
    println!("TripleDES: {} bytes", tripledes_size);

    Ok(())
}
