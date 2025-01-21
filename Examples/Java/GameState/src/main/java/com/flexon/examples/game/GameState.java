package com.flexon.examples.game;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import javafx.application.Platform;
import javafx.embed.swing.SwingFXUtils;
import javafx.scene.canvas.Canvas;
import javafx.scene.canvas.GraphicsContext;
import javafx.scene.image.WritableImage;
import javafx.scene.paint.Color;
import javafx.scene.text.Font;
import javafx.scene.text.TextAlignment;

import javax.imageio.ImageIO;
import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Instant;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class GameState {
    private static final ObjectMapper mapper = new ObjectMapper()
            .enable(SerializationFeature.INDENT_OUTPUT);

    static class Vector2 {
        public double x;
        public double y;

        public Vector2() {}

        public Vector2(double x, double y) {
            this.x = x;
            this.y = y;
        }
    }

    static class PlayerState {
        public String name;
        public int level;
        public int health;
        public int mana;
        public Vector2 position;
        public List<Map<String, Object>> inventory;

        public PlayerState() {
            this.name = "DragonSlayer";
            this.level = 35;
            this.health = 100;
            this.mana = 85;
            this.position = new Vector2(123.45, 678.90);
            this.inventory = new ArrayList<>();
            
            Map<String, Object> sword = new HashMap<>();
            sword.put("id", "sword_123");
            sword.put("name", "Excalibur");
            sword.put("damage", 50);
            
            Map<String, Object> shield = new HashMap<>();
            shield.put("id", "shield_456");
            shield.put("name", "Dragon Shield");
            shield.put("defense", 30);
            
            Map<String, Object> potion = new HashMap<>();
            potion.put("id", "potion_789");
            potion.put("name", "Health Potion");
            potion.put("healing", 100);
            
            this.inventory.add(sword);
            this.inventory.add(shield);
            this.inventory.add(potion);
        }
    }

    static class WorldState {
        public String name;
        public String currentZone;
        public List<String> discoveredLocations;
        public String weather;
        public String timeOfDay;

        public WorldState() {
            this.name = "Mythical Realm";
            this.currentZone = "Dragon's Lair";
            this.discoveredLocations = List.of(
                "Starting Village",
                "Dark Forest",
                "Crystal Cave",
                "Dragon's Lair"
            );
            this.weather = "stormy";
            this.timeOfDay = "night";
        }
    }

    static class GameData {
        public String version;
        public String saveTime;
        public PlayerState player;
        public WorldState world;
        public Map<String, List<Object>> quests;
        public Map<String, Boolean> achievements;

        public GameData() {
            this.version = "1.0.0";
            this.saveTime = Instant.now().toString();
            this.player = new PlayerState();
            this.world = new WorldState();
            
            this.quests = new HashMap<>();
            List<Object> activeQuests = new ArrayList<>();
            Map<String, Object> quest1 = new HashMap<>();
            quest1.put("id", "quest_123");
            quest1.put("name", "Slay the Dragon");
            quest1.put("progress", 0.75);
            activeQuests.add(quest1);
            
            Map<String, Object> quest2 = new HashMap<>();
            quest2.put("id", "quest_456");
            quest2.put("name", "Find the Sacred Sword");
            quest2.put("progress", 0.33);
            activeQuests.add(quest2);
            
            this.quests.put("active", activeQuests);
            this.quests.put("completed", List.of(
                "Rescue the Villagers",
                "Defeat the Dark Knight",
                "Find the Ancient Map"
            ));
            
            this.achievements = new HashMap<>();
            this.achievements.put("FirstBattle", true);
            this.achievements.put("DragonSlayer", false);
            this.achievements.put("LegendaryWeapon", true);
            this.achievements.put("ExplorerSupreme", false);
        }
    }

    static class FlexonHelper {
        public static void runCommand(String args) {
            try {
                ProcessBuilder pb = new ProcessBuilder();
                pb.command("cmd.exe", "/c", "flexon-cli " + args);
                pb.inheritIO();
                Process process = pb.start();
                int exitCode = process.waitFor();
                if (exitCode != 0) {
                    throw new RuntimeException("Flexon command failed with exit code: " + exitCode);
                }
            } catch (Exception e) {
                throw new RuntimeException("Error running Flexon command: " + e.getMessage(), e);
            }
        }
    }

    private static void createGameScreenshot() throws IOException {
        Platform.startup(() -> {});
        
        Canvas canvas = new Canvas(800, 600);
        GraphicsContext gc = canvas.getGraphicsContext2D();

        // Draw background
        gc.setFill(Color.NAVY);
        gc.fillRect(0, 0, 800, 600);

        // Draw border
        gc.setStroke(Color.WHITE);
        gc.setLineWidth(2);
        gc.strokeRect(50, 50, 700, 500);

        // Draw text
        gc.setFill(Color.WHITE);
        gc.setFont(Font.font(30));
        gc.setTextAlign(TextAlignment.CENTER);
        gc.fillText("Dragon's Lair", 400, 300);

        // Draw player position
        gc.setFill(Color.RED);
        gc.fillOval(395, 345, 10, 10);

        // Save to file
        WritableImage writableImage = new WritableImage(800, 600);
        canvas.snapshot(null, writableImage);
        File file = new File("screenshot.png");
        ImageIO.write(SwingFXUtils.fromFXImage(writableImage, null), "png", file);
    }

    public static void main(String[] args) {
        try {
            System.out.println("Flexon Game State Example (Java)");
            System.out.println("================================\n");

            // Create game state
            GameData gameState = new GameData();
            String stateFile = "gamestate.json";
            String saveFile = "savegame.flexon";

            // Create screenshot
            System.out.println("1. Creating game screenshot...");
            createGameScreenshot();

            // Save game state
            System.out.println("2. Saving game state...");
            mapper.writeValue(new File(stateFile), gameState);

            // Create save package with state and screenshot
            System.out.println("3. Creating save package...");
            FlexonHelper.runCommand("serialize -i " + stateFile + " -i screenshot.png -o " + saveFile + " -e savegamekey");

            // Load save package
            System.out.println("4. Loading save package...");
            Files.createDirectories(Path.of("loaded_save"));
            FlexonHelper.runCommand("deserialize -i " + saveFile + " -o loaded_save -e savegamekey");

            // Verify loaded state
            System.out.println("\nVerifying save data:");
            GameData loadedState = mapper.readValue(
                new File("loaded_save/gamestate.json"), GameData.class
            );

            System.out.println("Player name: " + loadedState.player.name);
            System.out.println("Level: " + loadedState.player.level);
            System.out.println("Current zone: " + loadedState.world.currentZone);
            System.out.println("Active quests: " + loadedState.quests.get("active").size());
            System.out.println("Screenshot recovered: " + Files.exists(Path.of("loaded_save/screenshot.png")));

            // Test different encryption algorithms
            System.out.println("\n5. Testing different encryption algorithms...");
            
            // ChaCha20
            System.out.println("\nTesting ChaCha20...");
            FlexonHelper.runCommand("serialize -i " + stateFile + " -o savegame_chacha20.flexon -e savegamekey ChaCha20");
            
            // TripleDES
            System.out.println("Testing TripleDES...");
            FlexonHelper.runCommand("serialize -i " + stateFile + " -o savegame_tripledes.flexon -e savegamekey TripleDES");

            // Compare file sizes
            System.out.println("\nFile size comparison:");
            System.out.println("Original JSON: " + Files.size(Path.of(stateFile)) + " bytes");
            System.out.println("AES-256: " + Files.size(Path.of(saveFile)) + " bytes");
            System.out.println("ChaCha20: " + Files.size(Path.of("savegame_chacha20.flexon")) + " bytes");
            System.out.println("TripleDES: " + Files.size(Path.of("savegame_tripledes.flexon")) + " bytes");

        } catch (Exception e) {
            System.err.println("Error: " + e.getMessage());
            e.printStackTrace();
        } finally {
            Platform.exit();
        }
    }
}
