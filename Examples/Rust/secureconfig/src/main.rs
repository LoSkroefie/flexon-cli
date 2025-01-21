use base64::{engine::general_purpose::STANDARD as base64_engine, Engine as _};
use chrono::Utc;
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};
use std::collections::HashMap;
use std::fs;
use std::process::Command;

#[derive(Debug, Serialize, Deserialize)]
struct DatabaseConfig {
    host: String,
    port: i32,
    database: String,
    username: String,
    password: String,
    options: HashMap<String, serde_json::Value>,
}

#[derive(Debug, Serialize, Deserialize)]
struct APIConfig {
    endpoint: String,
    api_key: String,
    timeout: i32,
    headers: HashMap<String, String>,
    allowed_methods: Vec<String>,
}

#[derive(Debug, Serialize, Deserialize)]
struct CacheConfig {
    provider: String,
    ttl: i32,
    settings: HashMap<String, serde_json::Value>,
}

#[derive(Debug, Serialize, Deserialize)]
struct SecurityConfig {
    authentication: HashMap<String, serde_json::Value>,
    authorization: HashMap<String, serde_json::Value>,
    encryption: HashMap<String, serde_json::Value>,
}

#[derive(Debug, Serialize, Deserialize)]
struct AppConfig {
    version: String,
    environment: String,
    database: DatabaseConfig,
    api: APIConfig,
    cache: CacheConfig,
    security: SecurityConfig,
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
    println!("Flexon Secure Config Example (Rust)");
    println!("=================================\n");

    // Create configuration
    let mut db_options = HashMap::new();
    db_options.insert(
        "maxConnections".to_string(),
        serde_json::Value::Number(100.into()),
    );
    db_options.insert(
        "timeout".to_string(),
        serde_json::Value::Number(30.into()),
    );
    db_options.insert("ssl".to_string(), serde_json::Value::Bool(true));

    let mut api_headers = HashMap::new();
    api_headers.insert("User-Agent".to_string(), "MyApp/1.0".to_string());
    api_headers.insert("Accept".to_string(), "application/json".to_string());

    let mut cache_settings = HashMap::new();
    cache_settings.insert(
        "host".to_string(),
        serde_json::Value::String("localhost".to_string()),
    );
    cache_settings.insert(
        "port".to_string(),
        serde_json::Value::Number(6379.into()),
    );
    cache_settings.insert(
        "maxMemory".to_string(),
        serde_json::Value::String("2gb".to_string()),
    );

    let mut auth_config = HashMap::new();
    auth_config.insert(
        "provider".to_string(),
        serde_json::Value::String("oauth2".to_string()),
    );
    auth_config.insert(
        "clientId".to_string(),
        serde_json::Value::String("myapp_client".to_string()),
    );
    auth_config.insert(
        "clientSecret".to_string(),
        serde_json::Value::String("client_secret_xyz".to_string()),
    );
    auth_config.insert(
        "tokenUrl".to_string(),
        serde_json::Value::String("https://auth.example.com/token".to_string()),
    );

    let mut authz_config = HashMap::new();
    authz_config.insert(
        "type".to_string(),
        serde_json::Value::String("rbac".to_string()),
    );
    authz_config.insert(
        "defaultRole".to_string(),
        serde_json::Value::String("user".to_string()),
    );
    authz_config.insert(
        "adminRoles".to_string(),
        serde_json::Value::Array(vec![
            serde_json::Value::String("admin".to_string()),
            serde_json::Value::String("superadmin".to_string()),
        ]),
    );

    let mut encryption_config = HashMap::new();
    encryption_config.insert(
        "algorithm".to_string(),
        serde_json::Value::String("AES256".to_string()),
    );
    encryption_config.insert(
        "keySize".to_string(),
        serde_json::Value::Number(256.into()),
    );
    encryption_config.insert(
        "provider".to_string(),
        serde_json::Value::String("default".to_string()),
    );

    let config = AppConfig {
        version: "1.0.0".to_string(),
        environment: "production".to_string(),
        database: DatabaseConfig {
            host: "localhost".to_string(),
            port: 5432,
            database: "myapp".to_string(),
            username: "admin".to_string(),
            password: "supersecret123".to_string(),
            options: db_options,
        },
        api: APIConfig {
            endpoint: "https://api.example.com/v1".to_string(),
            api_key: "sk_live_abcdef123456789".to_string(),
            timeout: 60,
            headers: api_headers,
            allowed_methods: vec![
                "GET".to_string(),
                "POST".to_string(),
                "PUT".to_string(),
                "DELETE".to_string(),
            ],
        },
        cache: CacheConfig {
            provider: "redis".to_string(),
            ttl: 3600,
            settings: cache_settings,
        },
        security: SecurityConfig {
            authentication: auth_config,
            authorization: authz_config,
            encryption: encryption_config,
        },
        metadata: {
            let mut metadata = HashMap::new();
            metadata.insert(
                "created_at".to_string(),
                serde_json::Value::String(Utc::now().to_rfc3339()),
            );
            metadata.insert(
                "updated_at".to_string(),
                serde_json::Value::String(Utc::now().to_rfc3339()),
            );
            metadata.insert(
                "created_by".to_string(),
                serde_json::Value::String("system".to_string()),
            );

            let config_string = format!(
                "{}{}{}",
                "1.0.0", "production", Utc::now().to_rfc3339()
            );
            let mut hasher = Sha256::new();
            hasher.update(config_string.as_bytes());
            metadata.insert(
                "fingerprint".to_string(),
                serde_json::Value::String(base64_engine.encode(hasher.finalize())),
            );
            metadata
        },
    };

    let config_file = "config.json";
    let encrypted_file = "config.flexon";
    let schema_file = "../../../FlexonCLI/Config/Schemas/config_schema.json";

    // Save configuration
    println!("1. Saving configuration...");
    let json_data = serde_json::to_string_pretty(&config)?;
    fs::write(config_file, json_data)?;

    // Encrypt with different algorithms
    println!("2. Testing encryption algorithms...");

    // AES-256
    println!("\nUsing AES-256...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o config_aes.flexon -e configkey AES256",
        config_file
    ))?;

    // ChaCha20
    println!("\nUsing ChaCha20...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o config_chacha20.flexon -e configkey ChaCha20",
        config_file
    ))?;

    // TripleDES
    println!("\nUsing TripleDES...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o config_tripledes.flexon -e configkey TripleDES",
        config_file
    ))?;

    // Validate and encrypt with schema
    println!("\n3. Validating and encrypting with schema...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o {} -s {} -e configkey",
        config_file, encrypted_file, schema_file
    ))?;

    // Load and verify
    println!("4. Loading configuration...");
    FlexonHelper::run_command(&format!(
        "deserialize -i {} -o config_decoded.json -e configkey",
        encrypted_file
    ))?;

    let loaded_data = fs::read_to_string("config_decoded.json")?;
    let loaded_config: AppConfig = serde_json::from_str(&loaded_data)?;

    println!("\nVerification:");
    println!("Version: {}", loaded_config.version);
    println!("Environment: {}", loaded_config.environment);
    println!("Database host: {}", loaded_config.database.host);
    println!("API endpoint: {}", loaded_config.api.endpoint);
    println!("Cache provider: {}", loaded_config.cache.provider);
    println!(
        "Auth provider: {}",
        loaded_config.security.authentication["provider"]
    );

    // Compare file sizes
    println!("\nFile size comparison:");
    let config_size = fs::metadata(config_file)?.len();
    let aes_size = fs::metadata("config_aes.flexon")?.len();
    let chacha20_size = fs::metadata("config_chacha20.flexon")?.len();
    let tripledes_size = fs::metadata("config_tripledes.flexon")?.len();

    println!("Original JSON: {} bytes", config_size);
    println!("AES-256: {} bytes", aes_size);
    println!("ChaCha20: {} bytes", chacha20_size);
    println!("TripleDES: {} bytes", tripledes_size);

    Ok(())
}
