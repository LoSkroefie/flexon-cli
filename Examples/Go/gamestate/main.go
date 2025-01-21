package main

import (
	"encoding/json"
	"fmt"
	"github.com/fogleman/gg"
	"io/ioutil"
	"os"
	"os/exec"
	"path/filepath"
	"time"
)

type Vector2 struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
}

type PlayerState struct {
	Name      string                   `json:"name"`
	Level     int                      `json:"level"`
	Health    int                      `json:"health"`
	Mana      int                      `json:"mana"`
	Position  Vector2                  `json:"position"`
	Inventory []map[string]interface{} `json:"inventory"`
}

type WorldState struct {
	Name               string   `json:"name"`
	CurrentZone        string   `json:"current_zone"`
	DiscoveredLocations []string `json:"discovered_locations"`
	Weather            string   `json:"weather"`
	TimeOfDay          string   `json:"time_of_day"`
}

type GameData struct {
	Version      string                 `json:"version"`
	SaveTime     string                 `json:"save_time"`
	Player       PlayerState            `json:"player"`
	World        WorldState             `json:"world"`
	Quests       map[string]interface{} `json:"quests"`
	Achievements map[string]bool        `json:"achievements"`
}

type FlexonHelper struct{}

func (f *FlexonHelper) RunCommand(args string) error {
	cmd := exec.Command("cmd.exe", "/c", "flexon-cli "+args)
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	return cmd.Run()
}

func createGameScreenshot() error {
	const width = 800
	const height = 600

	dc := gg.NewContext(width, height)

	// Draw background
	dc.SetRGB(0, 0, 0.5) // Navy blue
	dc.Clear()

	// Draw border
	dc.SetRGB(1, 1, 1) // White
	dc.SetLineWidth(2)
	dc.DrawRectangle(50, 50, 700, 500)
	dc.Stroke()

	// Draw text
	dc.SetRGB(1, 1, 1)
	if err := dc.LoadFontFace("C:/Windows/Fonts/arial.ttf", 30); err != nil {
		return fmt.Errorf("error loading font: %v", err)
	}
	dc.DrawStringAnchored("Dragon's Lair", float64(width)/2, float64(height)/2, 0.5, 0.5)

	// Draw player position
	dc.SetRGB(1, 0, 0) // Red
	dc.DrawCircle(float64(width)/2, float64(height)/2+45, 5)
	dc.Fill()

	return dc.SavePNG("screenshot.png")
}

func main() {
	fmt.Println("Flexon Game State Example (Go)")
	fmt.Println("==============================\n")

	// Create game state
	gameState := GameData{
		Version:  "1.0.0",
		SaveTime: time.Now().Format(time.RFC3339),
		Player: PlayerState{
			Name:   "DragonSlayer",
			Level:  35,
			Health: 100,
			Mana:   85,
			Position: Vector2{
				X: 123.45,
				Y: 678.90,
			},
			Inventory: []map[string]interface{}{
				{
					"id":     "sword_123",
					"name":   "Excalibur",
					"damage": 50,
				},
				{
					"id":      "shield_456",
					"name":    "Dragon Shield",
					"defense": 30,
				},
				{
					"id":      "potion_789",
					"name":    "Health Potion",
					"healing": 100,
				},
			},
		},
		World: WorldState{
			Name:        "Mythical Realm",
			CurrentZone: "Dragon's Lair",
			DiscoveredLocations: []string{
				"Starting Village",
				"Dark Forest",
				"Crystal Cave",
				"Dragon's Lair",
			},
			Weather:   "stormy",
			TimeOfDay: "night",
		},
		Quests: map[string]interface{}{
			"active": []map[string]interface{}{
				{
					"id":       "quest_123",
					"name":     "Slay the Dragon",
					"progress": 0.75,
				},
				{
					"id":       "quest_456",
					"name":     "Find the Sacred Sword",
					"progress": 0.33,
				},
			},
			"completed": []string{
				"Rescue the Villagers",
				"Defeat the Dark Knight",
				"Find the Ancient Map",
			},
		},
		Achievements: map[string]bool{
			"FirstBattle":     true,
			"DragonSlayer":    false,
			"LegendaryWeapon": true,
			"ExplorerSupreme": false,
		},
	}

	stateFile := "gamestate.json"
	saveFile := "savegame.flexon"

	// Create screenshot
	fmt.Println("1. Creating game screenshot...")
	err := createGameScreenshot()
	if err != nil {
		fmt.Printf("Error creating screenshot: %v\n", err)
		return
	}

	// Save game state
	fmt.Println("2. Saving game state...")
	jsonData, err := json.MarshalIndent(gameState, "", "  ")
	if err != nil {
		fmt.Printf("Error marshaling JSON: %v\n", err)
		return
	}
	err = ioutil.WriteFile(stateFile, jsonData, 0644)
	if err != nil {
		fmt.Printf("Error writing JSON file: %v\n", err)
		return
	}

	flexon := &FlexonHelper{}

	// Create save package with state and screenshot
	fmt.Println("3. Creating save package...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -i screenshot.png -o %s -e savegamekey", stateFile, saveFile))
	if err != nil {
		fmt.Printf("Error creating save package: %v\n", err)
		return
	}

	// Load save package
	fmt.Println("4. Loading save package...")
	err = os.MkdirAll("loaded_save", 0755)
	if err != nil {
		fmt.Printf("Error creating directory: %v\n", err)
		return
	}
	err = flexon.RunCommand(fmt.Sprintf("deserialize -i %s -o loaded_save -e savegamekey", saveFile))
	if err != nil {
		fmt.Printf("Error loading save package: %v\n", err)
		return
	}

	// Verify loaded state
	fmt.Println("\nVerifying save data:")
	loadedData, err := ioutil.ReadFile(filepath.Join("loaded_save", "gamestate.json"))
	if err != nil {
		fmt.Printf("Error reading loaded state: %v\n", err)
		return
	}

	var loadedState GameData
	err = json.Unmarshal(loadedData, &loadedState)
	if err != nil {
		fmt.Printf("Error unmarshaling loaded state: %v\n", err)
		return
	}

	fmt.Printf("Player name: %s\n", loadedState.Player.Name)
	fmt.Printf("Level: %d\n", loadedState.Player.Level)
	fmt.Printf("Current zone: %s\n", loadedState.World.CurrentZone)
	fmt.Printf("Active quests: %d\n", len(loadedState.Quests["active"].([]interface{})))

	screenshotExists := false
	if _, err := os.Stat(filepath.Join("loaded_save", "screenshot.png")); err == nil {
		screenshotExists = true
	}
	fmt.Printf("Screenshot recovered: %v\n", screenshotExists)

	// Test different encryption algorithms
	fmt.Println("\n5. Testing different encryption algorithms...")

	// ChaCha20
	fmt.Println("\nTesting ChaCha20...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o savegame_chacha20.flexon -e savegamekey ChaCha20", stateFile))
	if err != nil {
		fmt.Printf("Error with ChaCha20: %v\n", err)
	}

	// TripleDES
	fmt.Println("Testing TripleDES...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o savegame_tripledes.flexon -e savegamekey TripleDES", stateFile))
	if err != nil {
		fmt.Printf("Error with TripleDES: %v\n", err)
	}

	// Compare file sizes
	fmt.Println("\nFile size comparison:")
	stateInfo, _ := os.Stat(stateFile)
	aesInfo, _ := os.Stat(saveFile)
	chacha20Info, _ := os.Stat("savegame_chacha20.flexon")
	tripledesInfo, _ := os.Stat("savegame_tripledes.flexon")

	fmt.Printf("Original JSON: %d bytes\n", stateInfo.Size())
	fmt.Printf("AES-256: %d bytes\n", aesInfo.Size())
	fmt.Printf("ChaCha20: %d bytes\n", chacha20Info.Size())
	fmt.Printf("TripleDES: %d bytes\n", tripledesInfo.Size())
}
