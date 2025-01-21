<?php

declare(strict_types=1);

class FlexonHelper {
    public static function runCommand(string $args): void {
        $command = "flexon-cli $args";
        $result = shell_exec($command);
        if ($result === null) {
            throw new RuntimeException("Flexon command failed: $command");
        }
    }
}

class User implements JsonSerializable {
    public function __construct(
        private string $name,
        private int $age,
        private string $email,
        private array $interests,
        private array $metadata
    ) {}

    public function jsonSerialize(): array {
        return [
            'name' => $this->name,
            'age' => $this->age,
            'email' => $this->email,
            'interests' => $this->interests,
            'metadata' => $this->metadata
        ];
    }
}

echo "Flexon Basic Usage Example (PHP)\n";
echo "==============================\n\n";

// Create sample data
$metadata = [
    'created_at' => date(DATE_ISO8601),
    'last_login' => date(DATE_ISO8601),
    'preferences' => [
        'theme' => 'dark',
        'notifications' => true
    ]
];

$user = new User(
    'Alice Smith',
    28,
    'alice@example.com',
    ['programming', 'AI', 'data science'],
    $metadata
);

// Save to JSON
$jsonFile = 'user.json';
$flexonFile = 'user.flexon';
$encryptedFile = 'user_encrypted.flexon';
$decryptedFile = 'user_decrypted.json';

echo "1. Creating JSON file...\n";
file_put_contents($jsonFile, json_encode($user, JSON_PRETTY_PRINT));

// Basic serialization
echo "2. Converting to Flexon format...\n";
FlexonHelper::runCommand("serialize -i $jsonFile -o $flexonFile");

// Encryption
echo "3. Creating encrypted Flexon file...\n";
FlexonHelper::runCommand("serialize -i $jsonFile -o $encryptedFile -e mysecretkey");

// Decryption
echo "4. Decrypting Flexon file...\n";
FlexonHelper::runCommand("deserialize -i $encryptedFile -o $decryptedFile -e mysecretkey");

// Verify results
echo "\nResults:\n";
$jsonSize = filesize($jsonFile);
$flexonSize = filesize($flexonFile);
$encryptedSize = filesize($encryptedFile);

echo "Original JSON size: $jsonSize bytes\n";
echo "Flexon size: $flexonSize bytes\n";
echo "Encrypted size: $encryptedSize bytes\n";

// Compare contents
$originalData = json_decode(file_get_contents($jsonFile), true);
$decryptedData = json_decode(file_get_contents($decryptedFile), true);

echo "\nData verification:\n";
echo "Name matches: " . ($originalData['name'] === $decryptedData['name'] ? 'Yes' : 'No') . "\n";
echo "Age matches: " . ($originalData['age'] === $decryptedData['age'] ? 'Yes' : 'No') . "\n";
echo "Email matches: " . ($originalData['email'] === $decryptedData['email'] ? 'Yes' : 'No') . "\n";
echo "Interests match: " . (json_encode($originalData['interests']) === json_encode($decryptedData['interests']) ? 'Yes' : 'No') . "\n";

// Test different encryption algorithms
echo "\n5. Testing different encryption algorithms...\n";

// ChaCha20
echo "\nTesting ChaCha20...\n";
FlexonHelper::runCommand("serialize -i $jsonFile -o user_chacha20.flexon -e mysecretkey ChaCha20");

// TripleDES
echo "Testing TripleDES...\n";
FlexonHelper::runCommand("serialize -i $jsonFile -o user_tripledes.flexon -e mysecretkey TripleDES");

// Compare file sizes
echo "\nFile size comparison:\n";
echo "Original JSON: $jsonSize bytes\n";
echo "AES-256: $encryptedSize bytes\n";
echo "ChaCha20: " . filesize('user_chacha20.flexon') . " bytes\n";
echo "TripleDES: " . filesize('user_tripledes.flexon') . " bytes\n";
