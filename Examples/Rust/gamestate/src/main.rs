use chrono::Utc;
use image::{ImageBuffer, Rgb};
use imageproc::drawing::{draw_filled_circle_mut, draw_filled_rect_mut, draw_hollow_rect_mut};
use imageproc::rect::Rect;
use rusttype::{Font, Scale};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::fs;
use std::process::Command;

#[derive(Debug, Serialize, Deserialize)]
struct Vector2 {
    x: f64,
    y: f64,
}

#[derive(Debug, Serialize, Deserialize)]
struct PlayerState {
    name: String,
    level: i32,
    health: i32,
    mana: i32,
    position: Vector2,
    inventory: Vec<HashMap<String, serde_json::Value>>,
}

#[derive(Debug, Serialize, Deserialize)]
struct WorldState {
    name: String,
    current_zone: String,
    discovered_locations: Vec<String>,
    weather: String,
    time_of_day: String,
}

#[derive(Debug, Serialize, Deserialize)]
struct GameData {
    version: String,
    save_time: String,
    player: PlayerState,
    world: WorldState,
    quests: HashMap<String, serde_json::Value>,
    achievements: HashMap<String, bool>,
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

fn create_game_screenshot() -> Result<(), Box<dyn std::error::Error>> {
    let width = 800u32;
    let height = 600u32;
    let mut image = ImageBuffer::new(width, height);

    // Draw background (navy blue)
    draw_filled_rect_mut(
        &mut image,
        Rect::at(0, 0).of_size(width, height),
        Rgb([0, 0, 128]),
    );

    // Draw border (white)
    draw_hollow_rect_mut(
        &mut image,
        Rect::at(50, 50).of_size(700, 500),
        Rgb([255, 255, 255]),
    );

    // Draw player position (red dot)
    draw_filled_circle_mut(
        &mut image,
        ((width / 2) as i32, (height / 2 + 45) as i32),
        5,
        Rgb([255, 0, 0]),
    );

    // Save the image
    image.save("screenshot.png")?;
    Ok(())
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("Flexon Game State Example (Rust)");
    println!("===============================\n");

    // Create game state
    let game_state = GameData {
        version: "1.0.0".to_string(),
        save_time: Utc::now().to_rfc3339(),
        player: PlayerState {
            name: "DragonSlayer".to_string(),
            level: 35,
            health: 100,
            mana: 85,
            position: Vector2 {
                x: 123.45,
                y: 678.90,
            },
            inventory: vec![
                {
                    let mut item = HashMap::new();
                    item.insert("id".to_string(), serde_json::Value::String("sword_123".to_string()));
                    item.insert("name".to_string(), serde_json::Value::String("Excalibur".to_string()));
                    item.insert("damage".to_string(), serde_json::Value::Number(50.into()));
                    item
                },
                {
                    let mut item = HashMap::new();
                    item.insert("id".to_string(), serde_json::Value::String("shield_456".to_string()));
                    item.insert("name".to_string(), serde_json::Value::String("Dragon Shield".to_string()));
                    item.insert("defense".to_string(), serde_json::Value::Number(30.into()));
                    item
                },
                {
                    let mut item = HashMap::new();
                    item.insert("id".to_string(), serde_json::Value::String("potion_789".to_string()));
                    item.insert("name".to_string(), serde_json::Value::String("Health Potion".to_string()));
                    item.insert("healing".to_string(), serde_json::Value::Number(100.into()));
                    item
                },
            ],
        },
        world: WorldState {
            name: "Mythical Realm".to_string(),
            current_zone: "Dragon's Lair".to_string(),
            discovered_locations: vec![
                "Starting Village".to_string(),
                "Dark Forest".to_string(),
                "Crystal Cave".to_string(),
                "Dragon's Lair".to_string(),
            ],
            weather: "stormy".to_string(),
            time_of_day: "night".to_string(),
        },
        quests: {
            let mut quests = HashMap::new();
            let active_quests = vec![
                {
                    let mut quest = HashMap::new();
                    quest.insert("id".to_string(), serde_json::Value::String("quest_123".to_string()));
                    quest.insert("name".to_string(), serde_json::Value::String("Slay the Dragon".to_string()));
                    quest.insert("progress".to_string(), serde_json::Value::Number(0.75.into()));
                    quest
                },
                {
                    let mut quest = HashMap::new();
                    quest.insert("id".to_string(), serde_json::Value::String("quest_456".to_string()));
                    quest.insert("name".to_string(), serde_json::Value::String("Find the Sacred Sword".to_string()));
                    quest.insert("progress".to_string(), serde_json::Value::Number(0.33.into()));
                    quest
                },
            ];
            quests.insert(
                "active".to_string(),
                serde_json::Value::Array(active_quests.into_iter().map(|q| serde_json::Value::Object(serde_json::Map::from_iter(q))).collect()),
            );
            quests.insert(
                "completed".to_string(),
                serde_json::Value::Array(vec![
                    serde_json::Value::String("Rescue the Villagers".to_string()),
                    serde_json::Value::String("Defeat the Dark Knight".to_string()),
                    serde_json::Value::String("Find the Ancient Map".to_string()),
                ]),
            );
            quests
        },
        achievements: {
            let mut achievements = HashMap::new();
            achievements.insert("FirstBattle".to_string(), true);
            achievements.insert("DragonSlayer".to_string(), false);
            achievements.insert("LegendaryWeapon".to_string(), true);
            achievements.insert("ExplorerSupreme".to_string(), false);
            achievements
        },
    };

    let state_file = "gamestate.json";
    let save_file = "savegame.flexon";

    // Create screenshot
    println!("1. Creating game screenshot...");
    create_game_screenshot()?;

    // Save game state
    println!("2. Saving game state...");
    let json_data = serde_json::to_string_pretty(&game_state)?;
    fs::write(state_file, json_data)?;

    // Create save package with state and screenshot
    println!("3. Creating save package...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -i screenshot.png -o {} -e savegamekey",
        state_file, save_file
    ))?;

    // Load save package
    println!("4. Loading save package...");
    fs::create_dir_all("loaded_save")?;
    FlexonHelper::run_command(&format!(
        "deserialize -i {} -o loaded_save -e savegamekey",
        save_file
    ))?;

    // Verify loaded state
    println!("\nVerifying save data:");
    let loaded_data = fs::read_to_string("loaded_save/gamestate.json")?;
    let loaded_state: GameData = serde_json::from_str(&loaded_data)?;

    println!("Player name: {}", loaded_state.player.name);
    println!("Level: {}", loaded_state.player.level);
    println!("Current zone: {}", loaded_state.world.current_zone);
    if let serde_json::Value::Array(active_quests) = &loaded_state.quests["active"] {
        println!("Active quests: {}", active_quests.len());
    }
    println!(
        "Screenshot recovered: {}",
        fs::metadata("loaded_save/screenshot.png").is_ok()
    );

    // Test different encryption algorithms
    println!("\n5. Testing different encryption algorithms...");

    // ChaCha20
    println!("\nTesting ChaCha20...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o savegame_chacha20.flexon -e savegamekey ChaCha20",
        state_file
    ))?;

    // TripleDES
    println!("Testing TripleDES...");
    FlexonHelper::run_command(&format!(
        "serialize -i {} -o savegame_tripledes.flexon -e savegamekey TripleDES",
        state_file
    ))?;

    // Compare file sizes
    println!("\nFile size comparison:");
    println!("Original JSON: {} bytes", fs::metadata(state_file)?.len());
    println!("AES-256: {} bytes", fs::metadata(save_file)?.len());
    println!(
        "ChaCha20: {} bytes",
        fs::metadata("savegame_chacha20.flexon")?.len()
    );
    println!(
        "TripleDES: {} bytes",
        fs::metadata("savegame_tripledes.flexon")?.len()
    );

    Ok(())
}
