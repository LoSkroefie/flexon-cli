import json
import subprocess
import os
from datetime import datetime
from PIL import Image, ImageDraw, ImageFont
import numpy as np

class FlexonHelper:
    @staticmethod
    def run_command(args):
        try:
            result = subprocess.run(['flexon-cli'] + args.split(), 
                                  capture_output=True, text=True, check=True)
            return result.stdout
        except subprocess.CalledProcessError as e:
            print(f"Error running Flexon command: {e.stderr}")
            raise

class Vector2:
    def __init__(self, x, y):
        self.x = x
        self.y = y

    def to_dict(self):
        return {"x": self.x, "y": self.y}

class GameState:
    def __init__(self):
        self.version = "1.0.0"
        self.save_time = datetime.utcnow().isoformat()
        self.player = {
            "name": "DragonSlayer",
            "level": 35,
            "health": 100,
            "mana": 85,
            "position": Vector2(123.45, 678.90).to_dict(),
            "inventory": [
                {"id": "sword_123", "name": "Excalibur", "damage": 50},
                {"id": "shield_456", "name": "Dragon Shield", "defense": 30},
                {"id": "potion_789", "name": "Health Potion", "healing": 100}
            ]
        }
        self.world = {
            "name": "Mythical Realm",
            "current_zone": "Dragon's Lair",
            "discovered_locations": [
                "Starting Village",
                "Dark Forest",
                "Crystal Cave",
                "Dragon's Lair"
            ],
            "weather": "stormy",
            "time_of_day": "night"
        }
        self.quests = {
            "active": [
                {"id": "quest_123", "name": "Slay the Dragon", "progress": 0.75},
                {"id": "quest_456", "name": "Find the Sacred Sword", "progress": 0.33}
            ],
            "completed": [
                "Rescue the Villagers",
                "Defeat the Dark Knight",
                "Find the Ancient Map"
            ]
        }
        self.achievements = {
            "FirstBattle": True,
            "DragonSlayer": False,
            "LegendaryWeapon": True,
            "ExplorerSupreme": False
        }

    def to_dict(self):
        return {
            "version": self.version,
            "save_time": self.save_time,
            "player": self.player,
            "world": self.world,
            "quests": self.quests,
            "achievements": self.achievements
        }

def create_game_screenshot():
    # Create a simple game screenshot
    width, height = 800, 600
    image = Image.new('RGB', (width, height), color='navy')
    draw = ImageDraw.Draw(image)
    
    # Draw some game elements
    draw.rectangle([50, 50, 750, 550], outline='white')
    draw.text((400, 300), "Dragon's Lair", fill='white', anchor='mm')
    
    # Draw player position
    draw.ellipse([390, 290, 410, 310], fill='red')
    
    # Save the image
    image.save('screenshot.png')
    return 'screenshot.png'

def main():
    print("Flexon Game State Example (Python)")
    print("==================================\n")

    # Create game state
    game_state = GameState()
    state_file = "gamestate.json"
    save_file = "savegame.flexon"

    # Create screenshot
    print("1. Creating game screenshot...")
    screenshot_file = create_game_screenshot()

    # Save game state
    print("2. Saving game state...")
    with open(state_file, 'w') as f:
        json.dump(game_state.to_dict(), f, indent=2)

    # Create save package with state and screenshot
    print("3. Creating save package...")
    FlexonHelper.run_command(f"serialize -i {state_file} -i {screenshot_file} -o {save_file} -e savegamekey")

    # Load save package
    print("4. Loading save package...")
    os.makedirs("loaded_save", exist_ok=True)
    FlexonHelper.run_command(f"deserialize -i {save_file} -o loaded_save -e savegamekey")

    # Verify loaded state
    print("\nVerifying save data:")
    with open(os.path.join("loaded_save", "gamestate.json"), 'r') as f:
        loaded_state = json.load(f)

    print(f"Player name: {loaded_state['player']['name']}")
    print(f"Level: {loaded_state['player']['level']}")
    print(f"Current zone: {loaded_state['world']['current_zone']}")
    print(f"Active quests: {len(loaded_state['quests']['active'])}")
    print(f"Screenshot recovered: {os.path.exists(os.path.join('loaded_save', 'screenshot.png'))}")

    # Test different encryption algorithms
    print("\n5. Testing different encryption algorithms...")
    
    # ChaCha20
    print("\nTesting ChaCha20...")
    FlexonHelper.run_command(f"serialize -i {state_file} -o savegame_chacha20.flexon -e savegamekey ChaCha20")
    
    # TripleDES
    print("Testing TripleDES...")
    FlexonHelper.run_command(f"serialize -i {state_file} -o savegame_tripledes.flexon -e savegamekey TripleDES")

    # Compare file sizes
    print("\nFile size comparison:")
    print(f"Original JSON: {os.path.getsize(state_file)} bytes")
    print(f"AES-256: {os.path.getsize(save_file)} bytes")
    print(f"ChaCha20: {os.path.getsize('savegame_chacha20.flexon')} bytes")
    print(f"TripleDES: {os.path.getsize('savegame_tripledes.flexon')} bytes")

if __name__ == "__main__":
    main()
