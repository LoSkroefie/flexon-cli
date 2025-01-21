const fs = require('fs').promises;
const { execSync } = require('child_process');
const { createCanvas } = require('canvas');
const path = require('path');

class FlexonHelper {
    static runCommand(args) {
        try {
            return execSync(`flexon-cli ${args}`, { encoding: 'utf8' });
        } catch (error) {
            console.error(`Error running Flexon command: ${error.stderr}`);
            throw error;
        }
    }
}

class Vector2 {
    constructor(x, y) {
        this.x = x;
        this.y = y;
    }
}

class GameState {
    constructor() {
        this.version = "1.0.0";
        this.save_time = new Date().toISOString();
        this.player = {
            name: "DragonSlayer",
            level: 35,
            health: 100,
            mana: 85,
            position: new Vector2(123.45, 678.90),
            inventory: [
                { id: "sword_123", name: "Excalibur", damage: 50 },
                { id: "shield_456", name: "Dragon Shield", defense: 30 },
                { id: "potion_789", name: "Health Potion", healing: 100 }
            ]
        };
        this.world = {
            name: "Mythical Realm",
            current_zone: "Dragon's Lair",
            discovered_locations: [
                "Starting Village",
                "Dark Forest",
                "Crystal Cave",
                "Dragon's Lair"
            ],
            weather: "stormy",
            time_of_day: "night"
        };
        this.quests = {
            active: [
                { id: "quest_123", name: "Slay the Dragon", progress: 0.75 },
                { id: "quest_456", name: "Find the Sacred Sword", progress: 0.33 }
            ],
            completed: [
                "Rescue the Villagers",
                "Defeat the Dark Knight",
                "Find the Ancient Map"
            ]
        };
        this.achievements = {
            FirstBattle: true,
            DragonSlayer: false,
            LegendaryWeapon: true,
            ExplorerSupreme: false
        };
    }

    toJSON() {
        return {
            version: this.version,
            save_time: this.save_time,
            player: this.player,
            world: this.world,
            quests: this.quests,
            achievements: this.achievements
        };
    }
}

async function createGameScreenshot() {
    const width = 800;
    const height = 600;
    const canvas = createCanvas(width, height);
    const ctx = canvas.getContext('2d');

    // Draw background
    ctx.fillStyle = 'navy';
    ctx.fillRect(0, 0, width, height);

    // Draw border
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 2;
    ctx.strokeRect(50, 50, width - 100, height - 100);

    // Draw text
    ctx.fillStyle = 'white';
    ctx.font = '30px Arial';
    ctx.textAlign = 'center';
    ctx.fillText("Dragon's Lair", width / 2, height / 2);

    // Draw player position
    ctx.beginPath();
    ctx.arc(width / 2, height / 2 + 50, 10, 0, Math.PI * 2);
    ctx.fillStyle = 'red';
    ctx.fill();

    // Save to file
    const buffer = canvas.toBuffer('image/png');
    await fs.writeFile('screenshot.png', buffer);
    return 'screenshot.png';
}

async function main() {
    console.log("Flexon Game State Example (JavaScript)");
    console.log("=====================================\n");

    // Create game state
    const gameState = new GameState();
    const stateFile = "gamestate.json";
    const saveFile = "savegame.flexon";

    // Create screenshot
    console.log("1. Creating game screenshot...");
    const screenshotFile = await createGameScreenshot();

    // Save game state
    console.log("2. Saving game state...");
    await fs.writeFile(stateFile, JSON.stringify(gameState.toJSON(), null, 2));

    // Create save package with state and screenshot
    console.log("3. Creating save package...");
    FlexonHelper.runCommand(`serialize -i ${stateFile} -i ${screenshotFile} -o ${saveFile} -e savegamekey`);

    // Load save package
    console.log("4. Loading save package...");
    await fs.mkdir("loaded_save", { recursive: true });
    FlexonHelper.runCommand(`deserialize -i ${saveFile} -o loaded_save -e savegamekey`);

    // Verify loaded state
    console.log("\nVerifying save data:");
    const loadedState = JSON.parse(
        await fs.readFile(path.join("loaded_save", "gamestate.json"), 'utf8')
    );

    console.log(`Player name: ${loadedState.player.name}`);
    console.log(`Level: ${loadedState.player.level}`);
    console.log(`Current zone: ${loadedState.world.current_zone}`);
    console.log(`Active quests: ${loadedState.quests.active.length}`);
    console.log(`Screenshot recovered: ${await fs.stat(path.join("loaded_save", "screenshot.png"))
        .then(() => true)
        .catch(() => false)}`);

    // Test different encryption algorithms
    console.log("\n5. Testing different encryption algorithms...");
    
    // ChaCha20
    console.log("\nTesting ChaCha20...");
    FlexonHelper.runCommand(`serialize -i ${stateFile} -o savegame_chacha20.flexon -e savegamekey ChaCha20`);
    
    // TripleDES
    console.log("Testing TripleDES...");
    FlexonHelper.runCommand(`serialize -i ${stateFile} -o savegame_tripledes.flexon -e savegamekey TripleDES`);

    // Compare file sizes
    const stats = await Promise.all([
        fs.stat(stateFile),
        fs.stat(saveFile),
        fs.stat('savegame_chacha20.flexon'),
        fs.stat('savegame_tripledes.flexon')
    ]);

    console.log("\nFile size comparison:");
    console.log(`Original JSON: ${stats[0].size} bytes`);
    console.log(`AES-256: ${stats[1].size} bytes`);
    console.log(`ChaCha20: ${stats[2].size} bytes`);
    console.log(`TripleDES: ${stats[3].size} bytes`);
}

main().catch(console.error);
