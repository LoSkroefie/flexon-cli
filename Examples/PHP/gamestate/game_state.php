<?php

declare(strict_types=1);

require 'vendor/autoload.php';

use Intervention\Image\ImageManagerStatic as Image;

class FlexonHelper {
    public static function runCommand(string $args): void {
        $command = "flexon-cli $args";
        $result = shell_exec($command);
        if ($result === null) {
            throw new RuntimeException("Flexon command failed: $command");
        }
    }
}

class Vector2 implements JsonSerializable {
    public function __construct(
        private float $x,
        private float $y
    ) {}

    public function jsonSerialize(): array {
        return [
            'x' => $this->x,
            'y' => $this->y
        ];
    }
}

class PlayerState implements JsonSerializable {
    public function __construct(
        private string $name,
        private int $level,
        private float $health,
        private float $mana,
        private Vector2 $position,
        private array $inventory
    ) {}

    public function jsonSerialize(): array {
        return [
            'name' => $this->name,
            'level' => $this->level,
            'health' => $this->health,
            'mana' => $this->mana,
            'position' => $this->position,
            'inventory' => $this->inventory
        ];
    }
}

class WorldState implements JsonSerializable {
    public function __construct(
        private string $name,
        private string $currentZone,
        private array $discoveredLocations,
        private string $weather,
        private string $timeOfDay
    ) {}

    public function jsonSerialize(): array {
        return [
            'name' => $this->name,
            'current_zone' => $this->currentZone,
            'discovered_locations' => $this->discoveredLocations,
            'weather' => $this->weather,
            'time_of_day' => $this->timeOfDay
        ];
    }
}

class GameData implements JsonSerializable {
    public function __construct(
        private string $version,
        private string $saveTime,
        private PlayerState $player,
        private WorldState $world,
        private array $quests,
        private array $achievements
    ) {}

    public function jsonSerialize(): array {
        return [
            'version' => $this->version,
            'save_time' => $this->saveTime,
            'player' => $this->player,
            'world' => $this->world,
            'quests' => $this->quests,
            'achievements' => $this->achievements
        ];
    }
}

function createGameScreenshot(): void {
    $canvas = Image::canvas(800, 600, '#000080');
    
    // Draw border
    $canvas->rectangle(50, 50, 750, 550, function($draw) {
        $draw->border(2, '#ffffff');
    });
    
    // Draw player position
    $canvas->circle(10, 400, 345, function($draw) {
        $draw->background('#ff0000');
    });
    
    $canvas->save('screenshot.png');
}

echo "Flexon Game State Example (PHP)\n";
echo "============================\n\n";

// Create game state
$gameState = new GameData(
    '1.0.0',
    date(DATE_ISO8601),
    new PlayerState(
        'DragonSlayer',
        35,
        100.0,
        85.0,
        new Vector2(123.45, 678.90),
        [
            [
                'id' => 'sword_123',
                'name' => 'Excalibur',
                'damage' => 50
            ],
            [
                'id' => 'shield_456',
                'name' => 'Dragon Shield',
                'defense' => 30
            ],
            [
                'id' => 'potion_789',
                'name' => 'Health Potion',
                'healing' => 100
            ]
        ]
    ),
    new WorldState(
        'Mythical Realm',
        'Dragon\'s Lair',
        ['Starting Village', 'Dark Forest', 'Crystal Cave', 'Dragon\'s Lair'],
        'stormy',
        'night'
    ),
    [
        'active' => [
            [
                'id' => 'quest_123',
                'name' => 'Slay the Dragon',
                'progress' => 0.75
            ],
            [
                'id' => 'quest_456',
                'name' => 'Find the Sacred Sword',
                'progress' => 0.33
            ]
        ],
        'completed' => [
            'Rescue the Villagers',
            'Defeat the Dark Knight',
            'Find the Ancient Map'
        ]
    ],
    [
        'FirstBattle' => true,
        'DragonSlayer' => false,
        'LegendaryWeapon' => true,
        'ExplorerSupreme' => false
    ]
);

$stateFile = 'gamestate.json';
$saveFile = 'savegame.flexon';

// Create screenshot
echo "1. Creating game screenshot...\n";
createGameScreenshot();

// Save game state
echo "2. Saving game state...\n";
file_put_contents($stateFile, json_encode($gameState, JSON_PRETTY_PRINT));

// Create save package with state and screenshot
echo "3. Creating save package...\n";
FlexonHelper::runCommand("serialize -i $stateFile -i screenshot.png -o $saveFile -e savegamekey");

// Load save package
echo "4. Loading save package...\n";
if (!is_dir('loaded_save')) {
    mkdir('loaded_save');
}
FlexonHelper::runCommand("deserialize -i $saveFile -o loaded_save -e savegamekey");

// Verify loaded state
echo "\nVerifying save data:\n";
$loadedData = json_decode(file_get_contents('loaded_save/gamestate.json'), true);

echo "Player name: {$loadedData['player']['name']}\n";
echo "Level: {$loadedData['player']['level']}\n";
echo "Current zone: {$loadedData['world']['current_zone']}\n";
echo "Active quests: " . count($loadedData['quests']['active']) . "\n";
echo "Screenshot recovered: " . (file_exists('loaded_save/screenshot.png') ? 'Yes' : 'No') . "\n";

// Test different encryption algorithms
echo "\n5. Testing different encryption algorithms...\n";

// ChaCha20
echo "\nTesting ChaCha20...\n";
FlexonHelper::runCommand("serialize -i $stateFile -o savegame_chacha20.flexon -e savegamekey ChaCha20");

// TripleDES
echo "Testing TripleDES...\n";
FlexonHelper::runCommand("serialize -i $stateFile -o savegame_tripledes.flexon -e savegamekey TripleDES");

// Compare file sizes
echo "\nFile size comparison:\n";
echo "Original JSON: " . filesize($stateFile) . " bytes\n";
echo "AES-256: " . filesize($saveFile) . " bytes\n";
echo "ChaCha20: " . filesize('savegame_chacha20.flexon') . " bytes\n";
echo "TripleDES: " . filesize('savegame_tripledes.flexon') . " bytes\n";
