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

function createEmbeddings(string $text, int $dim): array {
    // Create deterministic embeddings based on text hash
    $hash = hash('sha256', $text, true);
    $seed = array_reduce(
        array_slice(unpack('C*', $hash), 0, 8),
        fn($acc, $byte) => $acc + ($byte << (8 * $acc)),
        0
    );
    
    // Use seed to generate deterministic random numbers
    mt_srand($seed);
    $embeddings = array_map(
        fn() => (mt_rand() / mt_getrandmax()) * 2 - 1,
        range(1, $dim)
    );
    
    // Normalize embeddings
    $norm = sqrt(array_sum(array_map(fn($x) => $x * $x, $embeddings)));
    return array_map(fn($x) => $x / $norm, $embeddings);
}

echo "Flexon AI Data Example (PHP)\n";
echo "=========================\n\n";

// Create AI prompt
$metadata = [
    'model' => 'gpt-4',
    'created_at' => date(DATE_ISO8601),
    'version' => '1.0',
    'language' => 'en',
    'domain' => 'programming',
    'security_level' => 'medium'
];

$context = [
    [
        'role' => 'system',
        'content' => 'You are a helpful AI assistant specializing in programming.'
    ],
    [
        'role' => 'user',
        'content' => 'How do I implement a binary search tree in PHP?'
    ]
];

$promptText = 'Explain binary search tree implementation with code examples.';
$embeddings = createEmbeddings($promptText, 384);

$security = [
    'fingerprint' => base64_encode(hash('sha256', $promptText . date(DATE_ISO8601), true)),
    'encrypted' => true,
    'access_level' => 'user',
    'audit_trail' => [
        [
            'timestamp' => date(DATE_ISO8601),
            'action' => 'created',
            'user' => 'system'
        ]
    ]
];

$prompt = [
    'prompt' => $promptText,
    'metadata' => $metadata,
    'context' => $context,
    'embeddings' => $embeddings,
    'security' => $security
];

// Create training dataset
$trainingPairs = [
    [
        'input' => 'What is a binary search tree?',
        'output' => 'A binary search tree is a data structure where each node has at most two children...'
    ],
    [
        'input' => 'How to insert into BST?',
        'output' => 'To insert into a BST, compare the value with the root and recursively insert into left or right subtree...'
    ],
    [
        'input' => 'BST search algorithm?',
        'output' => 'To search in a BST, start at root, compare value, and traverse left or right based on comparison...'
    ]
];

$datasetMetadata = [
    'created_at' => date(DATE_ISO8601),
    'version' => '1.0',
    'size' => count($trainingPairs),
    'language' => 'en',
    'domain' => 'programming'
];

$data = array_map(function($pair) {
    return [
        'input' => $pair['input'],
        'output' => $pair['output'],
        'embeddings' => createEmbeddings($pair['input'], 384),
        'metrics' => [
            'input_tokens' => strlen($pair['input']),
            'output_tokens' => strlen($pair['output']),
            'quality_score' => 0.95,
            'diversity_score' => 0.85
        ]
    ];
}, $trainingPairs);

$trainingData = [
    'name' => 'bst-tutor-v1',
    'description' => 'Binary Search Tree tutorial dataset',
    'metadata' => $datasetMetadata,
    'data' => $data
];

// Save prompt and training data
$promptFile = 'prompt.json';
$trainingFile = 'training.json';
$flexonFile = 'ai_data.flexon';
$schemaFile = '../../../FlexonCLI/AI/Schemas/prompt_schema.json';
$trainingSchemaFile = '../../../FlexonCLI/AI/Schemas/training_schema.json';
$trainingFlexonFile = 'training.flexon';

echo "1. Saving AI prompt...\n";
file_put_contents($promptFile, json_encode($prompt, JSON_PRETTY_PRINT));

echo "2. Saving training data...\n";
file_put_contents($trainingFile, json_encode($trainingData, JSON_PRETTY_PRINT));

// Validate and serialize prompt
echo "3. Validating and serializing prompt...\n";
FlexonHelper::runCommand("serialize -i $promptFile -o $flexonFile -s $schemaFile -e aikey ChaCha20");

// Validate and serialize training data
echo "4. Validating and serializing training data...\n";
FlexonHelper::runCommand("serialize -i $trainingFile -o $trainingFlexonFile -s $trainingSchemaFile");

// Load and verify
echo "5. Loading AI data...\n";
if (!is_dir('ai_output')) {
    mkdir('ai_output');
}
FlexonHelper::runCommand("deserialize -i $flexonFile -o ai_output/prompt_decoded.json -e aikey");

$loadedData = json_decode(file_get_contents('ai_output/prompt_decoded.json'), true);

echo "\nVerification:\n";
echo "Prompt loaded: " . (!empty($loadedData['prompt']) ? 'Yes' : 'No') . "\n";
echo "Metadata present: " . (!empty($loadedData['metadata']) ? 'Yes' : 'No') . "\n";
echo "Context count: " . count($loadedData['context']) . "\n";
echo "Embeddings size: " . count($loadedData['embeddings']) . "\n";

// Test different encryption methods
echo "\n6. Testing different encryption methods...\n";

// AES-256
echo "\nTesting AES-256...\n";
FlexonHelper::runCommand("serialize -i $promptFile -o ai_aes.flexon -e aikey AES256");

// TripleDES
echo "Testing TripleDES...\n";
FlexonHelper::runCommand("serialize -i $promptFile -o ai_tripledes.flexon -e aikey TripleDES");

echo "\nFile size comparison:\n";
echo "Original JSON: " . filesize($promptFile) . " bytes\n";
echo "ChaCha20: " . filesize($flexonFile) . " bytes\n";
echo "AES-256: " . filesize('ai_aes.flexon') . " bytes\n";
echo "TripleDES: " . filesize('ai_tripledes.flexon') . " bytes\n";
