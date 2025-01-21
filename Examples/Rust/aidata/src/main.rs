use base64::{engine::general_purpose::STANDARD as base64_engine, Engine as _};
use chrono::Utc;
use rand::{Rng, SeedableRng};
use rand::rngs::StdRng;
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};
use std::collections::HashMap;
use std::fs;
use std::process::Command;

#[derive(Debug, Serialize, Deserialize)]
struct AIPrompt {
    prompt: String,
    metadata: HashMap<String, serde_json::Value>,
    context: Vec<HashMap<String, String>>,
    embeddings: Vec<f64>,
    security: HashMap<String, serde_json::Value>,
}

#[derive(Debug, Serialize, Deserialize)]
struct TrainingDataset {
    name: String,
    description: String,
    metadata: HashMap<String, serde_json::Value>,
    data: Vec<DataEntry>,
}

#[derive(Debug, Serialize, Deserialize)]
struct DataEntry {
    input: String,
    output: String,
    embeddings: Vec<f64>,
    metrics: HashMap<String, serde_json::Value>,
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

fn create_embeddings(text: &str, dim: usize) -> Vec<f64> {
    // Create deterministic embeddings based on text hash
    let mut hasher = Sha256::new();
    hasher.update(text.as_bytes());
    let hash = hasher.finalize();
    let seed = u64::from_le_bytes(hash[0..8].try_into().unwrap());
    
    let mut rng = StdRng::seed_from_u64(seed);
    let mut embeddings = vec![0.0; dim];
    
    // Generate random embeddings
    for i in 0..dim {
        embeddings[i] = rng.gen_range(-1.0..1.0);
    }
    
    // Normalize embeddings
    let norm: f64 = embeddings.iter().map(|x| x * x).sum::<f64>().sqrt();
    for i in 0..dim {
        embeddings[i] /= norm;
    }
    
    embeddings
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("Flexon AI Data Example (Rust)");
    println!("============================\n");

    // Create AI prompt
    let mut metadata = HashMap::new();
    metadata.insert(
        "model".to_string(),
        serde_json::Value::String("gpt-4".to_string()),
    );
    metadata.insert(
        "created_at".to_string(),
        serde_json::Value::String(Utc::now().to_rfc3339()),
    );
    metadata.insert(
        "version".to_string(),
        serde_json::Value::String("1.0".to_string()),
    );
    metadata.insert(
        "language".to_string(),
        serde_json::Value::String("en".to_string()),
    );
    metadata.insert(
        "domain".to_string(),
        serde_json::Value::String("programming".to_string()),
    );
    metadata.insert(
        "security_level".to_string(),
        serde_json::Value::String("medium".to_string()),
    );

    let context = vec![
        {
            let mut map = HashMap::new();
            map.insert("role".to_string(), "system".to_string());
            map.insert(
                "content".to_string(),
                "You are a helpful AI assistant specializing in programming.".to_string(),
            );
            map
        },
        {
            let mut map = HashMap::new();
            map.insert("role".to_string(), "user".to_string());
            map.insert(
                "content".to_string(),
                "How do I implement a binary search tree in Rust?".to_string(),
            );
            map
        },
    ];

    let prompt_text = "Explain binary search tree implementation with code examples.";
    let embeddings = create_embeddings(prompt_text, 384);

    let mut security = HashMap::new();
    let mut hasher = Sha256::new();
    hasher.update(format!("{}{}", prompt_text, Utc::now().to_rfc3339()).as_bytes());
    security.insert(
        "fingerprint".to_string(),
        serde_json::Value::String(base64_engine.encode(hasher.finalize())),
    );
    security.insert(
        "encrypted".to_string(),
        serde_json::Value::Bool(true),
    );
    security.insert(
        "access_level".to_string(),
        serde_json::Value::String("user".to_string()),
    );

    let audit_trail = vec![serde_json::json!({
        "timestamp": Utc::now().to_rfc3339(),
        "action": "created",
        "user": "system"
    })];
    security.insert(
        "audit_trail".to_string(),
        serde_json::Value::Array(audit_trail),
    );

    let prompt = AIPrompt {
        prompt: prompt_text.to_string(),
        metadata,
        context,
        embeddings,
        security,
    };

    // Create training dataset
    let training_pairs = vec![
        ("What is a binary search tree?", "A binary search tree is a data structure where each node has at most two children..."),
        ("How to insert into BST?", "To insert into a BST, compare the value with the root and recursively insert into left or right subtree..."),
        ("BST search algorithm?", "To search in a BST, start at root, compare value, and traverse left or right based on comparison..."),
    ];

    let mut dataset_metadata = HashMap::new();
    dataset_metadata.insert(
        "created_at".to_string(),
        serde_json::Value::String(Utc::now().to_rfc3339()),
    );
    dataset_metadata.insert(
        "version".to_string(),
        serde_json::Value::String("1.0".to_string()),
    );
    dataset_metadata.insert(
        "size".to_string(),
        serde_json::Value::Number(training_pairs.len().into()),
    );
    dataset_metadata.insert(
        "language".to_string(),
        serde_json::Value::String("en".to_string()),
    );
    dataset_metadata.insert(
        "domain".to_string(),
        serde_json::Value::String("programming".to_string()),
    );

    let mut data = Vec::new();
    for (input, output) in training_pairs {
        let mut metrics = HashMap::new();
        metrics.insert(
            "input_tokens".to_string(),
            serde_json::Value::Number(input.len().into()),
        );
        metrics.insert(
            "output_tokens".to_string(),
            serde_json::Value::Number(output.len().into()),
        );
        metrics.insert(
            "quality_score".to_string(),
            serde_json::Value::Number(serde_json::Number::from_f64(0.95).unwrap()),
        );
        metrics.insert(
            "diversity_score".to_string(),
            serde_json::Value::Number(serde_json::Number::from_f64(0.85).unwrap()),
        );

        data.push(DataEntry {
            input: input.to_string(),
            output: output.to_string(),
            embeddings: create_embeddings(input, 384),
            metrics,
        });
    }

    let training_data = TrainingDataset {
        name: "bst-tutor-v1".to_string(),
        description: "Binary Search Tree tutorial dataset".to_string(),
        metadata: dataset_metadata,
        data,
    };

    // Save prompt and training data
    let prompt_file = "prompt.json";
    let training_file = "training.json";
    let flexon_file = "ai_data.flexon";
    let schema_file = "../../../FlexonCLI/AI/Schemas/prompt_schema.json";
    let training_schema_file = "../../../FlexonCLI/AI/Schemas/training_schema.json";
    let training_flexon_file = "training.flexon";

    println!("1. Saving AI prompt...");
    let json_data = serde_json::to_string_pretty(&prompt)?;
    fs::write(prompt_file, json_data)?;

    println!("2. Saving training data...");
    let training_json = serde_json::to_string_pretty(&training_data)?;
    fs::write(training_file, training_json)?;

    // Validate and serialize prompt
    println!("3. Validating and serializing prompt...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o {} -s {} -e aikey ChaCha20",
        prompt_file, flexon_file, schema_file
    ))?;

    // Validate and serialize training data
    println!("4. Validating and serializing training data...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o {} -s {}",
        training_file, training_flexon_file, training_schema_file
    ))?;

    // Load and verify
    println!("5. Loading AI data...");
    fs::create_dir_all("ai_output")?;
    FlexonHelper::run_command(&format!(
        "deserialize -i {} -o ai_output/prompt_decoded.json -e aikey",
        flexon_file
    ))?;

    let loaded_data = fs::read_to_string("ai_output/prompt_decoded.json")?;
    let loaded_prompt: AIPrompt = serde_json::from_str(&loaded_data)?;

    println!("\nVerification:");
    println!("Prompt loaded: {}", !loaded_prompt.prompt.is_empty());
    println!("Metadata present: {}", !loaded_prompt.metadata.is_empty());
    println!("Context count: {}", loaded_prompt.context.len());
    println!("Embeddings size: {}", loaded_prompt.embeddings.len());

    // Test different encryption methods
    println!("\n6. Testing different encryption methods...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o ai_aes.flexon -e aikey AES256",
        prompt_file
    ))?;

    FlexonHelper::run_command(&format!(
        "serialize -i {} -o ai_tripledes.flexon -e aikey TripleDES",
        prompt_file
    ))?;

    println!("\nFile size comparison:");
    let prompt_size = fs::metadata(prompt_file)?.len();
    let chacha20_size = fs::metadata(flexon_file)?.len();
    let aes_size = fs::metadata("ai_aes.flexon")?.len();
    let tripledes_size = fs::metadata("ai_tripledes.flexon")?.len();

    println!("Original JSON: {} bytes", prompt_size);
    println!("ChaCha20: {} bytes", chacha20_size);
    println!("AES-256: {} bytes", aes_size);
    println!("TripleDES: {} bytes", tripledes_size);

    Ok(())
}
