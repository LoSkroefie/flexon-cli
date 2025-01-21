use criterion::{criterion_group, criterion_main, Criterion};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::process::Command;

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

fn bench_json_serialization(c: &mut Criterion) {
    let data = TestData {
        id: "test-1".to_string(),
        name: "Benchmark Test".to_string(),
        description: "Test data for benchmarking".to_string(),
        metadata: {
            let mut map = HashMap::new();
            map.insert("version".to_string(), serde_json::Value::String("1.0".to_string()));
            map
        },
        records: vec![
            {
                let mut record = HashMap::new();
                record.insert("key".to_string(), serde_json::Value::String("value".to_string()));
                record
            };
            100
        ],
    };

    c.bench_function("json_serialize", |b| {
        b.iter(|| serde_json::to_string(&data))
    });
}

fn bench_json_deserialization(c: &mut Criterion) {
    let data = TestData {
        id: "test-1".to_string(),
        name: "Benchmark Test".to_string(),
        description: "Test data for benchmarking".to_string(),
        metadata: {
            let mut map = HashMap::new();
            map.insert("version".to_string(), serde_json::Value::String("1.0".to_string()));
            map
        },
        records: vec![
            {
                let mut record = HashMap::new();
                record.insert("key".to_string(), serde_json::Value::String("value".to_string()));
                record
            };
            100
        ],
    };

    let json_str = serde_json::to_string(&data).unwrap();

    c.bench_function("json_deserialize", |b| {
        b.iter(|| {
            let _: TestData = serde_json::from_str(&json_str).unwrap();
        })
    });
}

fn bench_flexon_serialization(c: &mut Criterion) {
    let data = TestData {
        id: "test-1".to_string(),
        name: "Benchmark Test".to_string(),
        description: "Test data for benchmarking".to_string(),
        metadata: {
            let mut map = HashMap::new();
            map.insert("version".to_string(), serde_json::Value::String("1.0".to_string()));
            map
        },
        records: vec![
            {
                let mut record = HashMap::new();
                record.insert("key".to_string(), serde_json::Value::String("value".to_string()));
                record
            };
            100
        ],
    };

    let json_str = serde_json::to_string(&data).unwrap();
    std::fs::write("bench_test.json", &json_str).unwrap();

    c.bench_function("flexon_serialize", |b| {
        b.iter(|| {
            FlexonHelper::run_command("serialize -i bench_test.json -o bench_test.flexon").unwrap();
        })
    });
}

fn bench_flexon_deserialization(c: &mut Criterion) {
    c.bench_function("flexon_deserialize", |b| {
        b.iter(|| {
            FlexonHelper::run_command("deserialize -i bench_test.flexon -o bench_test_out.json").unwrap();
        })
    });
}

criterion_group!(
    benches,
    bench_json_serialization,
    bench_json_deserialization,
    bench_flexon_serialization,
    bench_flexon_deserialization
);
criterion_main!(benches);
