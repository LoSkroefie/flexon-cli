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

class DatabaseConfig implements JsonSerializable {
    public function __construct(
        private string $host,
        private int $port,
        private string $database,
        private string $username,
        private string $password,
        private array $options
    ) {}

    public function jsonSerialize(): array {
        return [
            'host' => $this->host,
            'port' => $this->port,
            'database' => $this->database,
            'username' => $this->username,
            'password' => $this->password,
            'options' => $this->options
        ];
    }
}

class APIConfig implements JsonSerializable {
    public function __construct(
        private string $endpoint,
        private string $apiKey,
        private int $timeout,
        private array $headers,
        private array $allowedMethods
    ) {}

    public function jsonSerialize(): array {
        return [
            'endpoint' => $this->endpoint,
            'api_key' => $this->apiKey,
            'timeout' => $this->timeout,
            'headers' => $this->headers,
            'allowed_methods' => $this->allowedMethods
        ];
    }
}

class CacheConfig implements JsonSerializable {
    public function __construct(
        private string $provider,
        private int $ttl,
        private array $settings
    ) {}

    public function jsonSerialize(): array {
        return [
            'provider' => $this->provider,
            'ttl' => $this->ttl,
            'settings' => $this->settings
        ];
    }
}

class SecurityConfig implements JsonSerializable {
    public function __construct(
        private array $authentication,
        private array $authorization,
        private array $encryption
    ) {}

    public function jsonSerialize(): array {
        return [
            'authentication' => $this->authentication,
            'authorization' => $this->authorization,
            'encryption' => $this->encryption
        ];
    }
}

class AppConfig implements JsonSerializable {
    public function __construct(
        private string $version,
        private string $environment,
        private DatabaseConfig $database,
        private APIConfig $api,
        private CacheConfig $cache,
        private SecurityConfig $security,
        private array $metadata
    ) {}

    public function jsonSerialize(): array {
        return [
            'version' => $this->version,
            'environment' => $this->environment,
            'database' => $this->database,
            'api' => $this->api,
            'cache' => $this->cache,
            'security' => $this->security,
            'metadata' => $this->metadata
        ];
    }
}

echo "Flexon Secure Config Example (PHP)\n";
echo "==============================\n\n";

// Create configuration
$config = new AppConfig(
    '1.0.0',
    'production',
    new DatabaseConfig(
        'localhost',
        5432,
        'myapp',
        'admin',
        'supersecret123',
        [
            'maxConnections' => 100,
            'timeout' => 30,
            'ssl' => true
        ]
    ),
    new APIConfig(
        'https://api.example.com/v1',
        'sk_live_abcdef123456789',
        60,
        [
            'User-Agent' => 'MyApp/1.0',
            'Accept' => 'application/json'
        ],
        ['GET', 'POST', 'PUT', 'DELETE']
    ),
    new CacheConfig(
        'redis',
        3600,
        [
            'host' => 'localhost',
            'port' => 6379,
            'maxMemory' => '2gb'
        ]
    ),
    new SecurityConfig(
        [
            'provider' => 'oauth2',
            'clientId' => 'myapp_client',
            'clientSecret' => 'client_secret_xyz',
            'tokenUrl' => 'https://auth.example.com/token'
        ],
        [
            'type' => 'rbac',
            'defaultRole' => 'user',
            'adminRoles' => ['admin', 'superadmin']
        ],
        [
            'algorithm' => 'AES256',
            'keySize' => 256,
            'provider' => 'default'
        ]
    ),
    [
        'created_at' => date(DATE_ISO8601),
        'updated_at' => date(DATE_ISO8601),
        'created_by' => 'system',
        'fingerprint' => base64_encode(
            hash('sha256', "1.0.0production" . date(DATE_ISO8601), true)
        )
    ]
);

$configFile = 'config.json';
$encryptedFile = 'config.flexon';
$schemaFile = '../../../FlexonCLI/Config/Schemas/config_schema.json';

// Save configuration
echo "1. Saving configuration...\n";
file_put_contents($configFile, json_encode($config, JSON_PRETTY_PRINT));

// Encrypt with different algorithms
echo "2. Testing encryption algorithms...\n";

// AES-256
echo "\nUsing AES-256...\n";
FlexonHelper::runCommand("serialize -i $configFile -o config_aes.flexon -e configkey AES256");

// ChaCha20
echo "\nUsing ChaCha20...\n";
FlexonHelper::runCommand("serialize -i $configFile -o config_chacha20.flexon -e configkey ChaCha20");

// TripleDES
echo "\nUsing TripleDES...\n";
FlexonHelper::runCommand("serialize -i $configFile -o config_tripledes.flexon -e configkey TripleDES");

// Validate and encrypt with schema
echo "\n3. Validating and encrypting with schema...\n";
FlexonHelper::runCommand("serialize -i $configFile -o $encryptedFile -s $schemaFile -e configkey");

// Load and verify
echo "4. Loading configuration...\n";
FlexonHelper::runCommand("deserialize -i $encryptedFile -o config_decoded.json -e configkey");

$loadedData = json_decode(file_get_contents('config_decoded.json'), true);

echo "\nVerification:\n";
echo "Version: {$loadedData['version']}\n";
echo "Environment: {$loadedData['environment']}\n";
echo "Database host: {$loadedData['database']['host']}\n";
echo "API endpoint: {$loadedData['api']['endpoint']}\n";
echo "Cache provider: {$loadedData['cache']['provider']}\n";
echo "Auth provider: {$loadedData['security']['authentication']['provider']}\n";

// Compare file sizes
echo "\nFile size comparison:\n";
echo "Original JSON: " . filesize($configFile) . " bytes\n";
echo "AES-256: " . filesize('config_aes.flexon') . " bytes\n";
echo "ChaCha20: " . filesize('config_chacha20.flexon') . " bytes\n";
echo "TripleDES: " . filesize('config_tripledes.flexon') . " bytes\n";
