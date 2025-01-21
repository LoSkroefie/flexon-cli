use chrono::Utc;
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::fs;
use std::process::Command;

#[derive(Debug, Serialize, Deserialize)]
struct User {
    name: String,
    age: i32,
    email: String,
    interests: Vec<String>,
    metadata: HashMap<String, serde_json::Value>,
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

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("Flexon Basic Usage Example (Rust)");
    println!("================================\n");

    // Create sample data
    let mut metadata = HashMap::new();
    metadata.insert(
        "created_at".to_string(),
        serde_json::Value::String(Utc::now().to_rfc3339()),
    );
    metadata.insert(
        "last_login".to_string(),
        serde_json::Value::String(Utc::now().to_rfc3339()),
    );

    let mut preferences = HashMap::new();
    preferences.insert("theme".to_string(), serde_json::Value::String("dark".to_string()));
    preferences.insert("notifications".to_string(), serde_json::Value::Bool(true));
    metadata.insert(
        "preferences".to_string(),
        serde_json::Value::Object(serde_json::Map::from_iter(preferences)),
    );

    let user = User {
        name: "Alice Smith".to_string(),
        age: 28,
        email: "alice@example.com".to_string(),
        interests: vec![
            "programming".to_string(),
            "AI".to_string(),
            "data science".to_string(),
        ],
        metadata,
    };

    // Save to JSON
    let json_file = "user.json";
    let flexon_file = "user.flexon";
    let encrypted_file = "user_encrypted.flexon";
    let decrypted_file = "user_decrypted.json";

    println!("1. Creating JSON file...");
    let json_data = serde_json::to_string_pretty(&user)?;
    fs::write(json_file, json_data)?;

    // Basic serialization
    println!("2. Converting to Flexon format...");
    FlexonHelper::run_command(&format!("serialize -i {} -o {}", json_file, flexon_file))?;

    // Encryption
    println!("3. Creating encrypted Flexon file...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o {} -e mysecretkey",
        json_file, encrypted_file
    ))?;

    // Decryption
    println!("4. Decrypting Flexon file...");
    FlexonHelper::run_command(&format!(
        "deserialize -i {} -o {} -e mysecretkey",
        encrypted_file, decrypted_file
    ))?;

    // Verify results
    println!("\nResults:");
    let json_size = fs::metadata(json_file)?.len();
    let flexon_size = fs::metadata(flexon_file)?.len();
    let encrypted_size = fs::metadata(encrypted_file)?.len();

    println!("Original JSON size: {} bytes", json_size);
    println!("Flexon size: {} bytes", flexon_size);
    println!("Encrypted size: {} bytes", encrypted_size);

    // Compare contents
    let original_data = fs::read_to_string(json_file)?;
    let decrypted_data = fs::read_to_string(decrypted_file)?;

    let original_user: User = serde_json::from_str(&original_data)?;
    let decrypted_user: User = serde_json::from_str(&decrypted_data)?;

    println!("\nData verification:");
    println!("Name matches: {}", original_user.name == decrypted_user.name);
    println!("Age matches: {}", original_user.age == decrypted_user.age);
    println!("Email matches: {}", original_user.email == decrypted_user.email);
    println!(
        "Interests match: {}",
        original_user.interests == decrypted_user.interests
    );

    // Test different encryption algorithms
    println!("\n5. Testing different encryption algorithms...");

    // ChaCha20
    println!("\nTesting ChaCha20...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o user_chacha20.flexon -e mysecretkey ChaCha20",
        json_file
    ))?;

    // TripleDES
    println!("Testing TripleDES...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o user_tripledes.flexon -e mysecretkey TripleDES",
        json_file
    ))?;

    // Compare file sizes
    println!("\nFile size comparison:");
    println!("Original JSON: {} bytes", json_size);
    println!(
        "AES-256: {} bytes",
        fs::metadata(encrypted_file)?.len()
    );
    println!(
        "ChaCha20: {} bytes",
        fs::metadata("user_chacha20.flexon")?.len()
    );
    println!(
        "TripleDES: {} bytes",
        fs::metadata("user_tripledes.flexon")?.len()
    );

    Ok(())
}
