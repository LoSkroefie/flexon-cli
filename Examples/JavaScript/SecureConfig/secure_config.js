const fs = require('fs').promises;
const { execSync } = require('child_process');
const path = require('path');
const crypto = require('crypto');

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

class DatabaseConfig {
    constructor() {
        this.host = "db.example.com";
        this.port = 5432;
        this.username = "admin";
        this.password = crypto.randomBytes(32).toString('base64');
        this.database = "production_db";
        this.ssl_mode = "verify-full";
        this.max_connections = 100;
        this.timeout = 30;
    }
}

class ApiConfig {
    constructor() {
        this.endpoint = "https://api.example.com/v1";
        this.api_key = crypto.randomBytes(48).toString('base64');
        this.timeout = 30000;
        this.retry_attempts = 3;
        this.headers = {
            "User-Agent": "MyApp/1.0",
            "X-Custom-Header": "custom-value"
        };
        this.rate_limit = {
            requests_per_second: 10,
            burst: 20
        };
    }
}

class AppConfig {
    constructor() {
        this.environment = "production";
        this.debug = false;
        this.database = new DatabaseConfig();
        this.api = new ApiConfig();
        this.features = {
            feature1: "enabled",
            feature2: "disabled",
            beta: "enabled"
        };
        this.logging = {
            level: "INFO",
            format: "%(asctime)s - %(name)s - %(levelname)s - %(message)s",
            handlers: ["console", "file"]
        };
        this.security = {
            jwt_secret: crypto.randomBytes(32).toString('hex'),
            session_timeout: 3600,
            allowed_origins: [
                "https://app.example.com",
                "https://admin.example.com"
            ],
            cors_headers: [
                "Content-Type",
                "Authorization",
                "X-Custom-Header"
            ]
        };
        this.cache = {
            type: "redis",
            url: "redis://cache.example.com:6379",
            ttl: 300
        };
        this.metrics = {
            enabled: true,
            provider: "prometheus",
            endpoint: "/metrics",
            collection_interval: 15
        };
    }

    toJSON() {
        return {
            environment: this.environment,
            debug: this.debug,
            database: this.database,
            api: this.api,
            features: this.features,
            logging: this.logging,
            security: this.security,
            cache: this.cache,
            metrics: this.metrics
        };
    }
}

function createSchema() {
    return {
        type: "object",
        required: ["environment", "database", "api", "security"],
        properties: {
            environment: {
                type: "string",
                enum: ["development", "staging", "production"]
            },
            database: {
                type: "object",
                required: ["host", "port", "username", "password"]
            },
            api: {
                type: "object",
                required: ["endpoint", "api_key"]
            },
            security: {
                type: "object",
                required: ["jwt_secret", "allowed_origins"]
            }
        }
    };
}

async function main() {
    console.log("Flexon Secure Configuration Example (JavaScript)");
    console.log("=============================================\n");

    // Create configuration
    const config = new AppConfig();
    const configFile = "config.json";
    const schemaFile = "config_schema.json";
    const flexonFile = "config.flexon";

    console.log("1. Creating configuration files...");
    await fs.writeFile(configFile, JSON.stringify(config.toJSON(), null, 2));
    await fs.writeFile(schemaFile, JSON.stringify(createSchema(), null, 2));

    // Create secure configuration package
    console.log("2. Creating secure configuration package...");
    const masterKey = crypto.randomBytes(32).toString('base64');
    console.log(`Generated master key: ${masterKey}`);

    FlexonHelper.runCommand(`serialize -i ${configFile} -o ${flexonFile} -s ${schemaFile} -e ${masterKey} AES256`);

    // Test configuration loading
    console.log("3. Loading secure configuration...");
    await fs.mkdir("config_output", { recursive: true });
    FlexonHelper.runCommand(`deserialize -i ${flexonFile} -o config_output/config_decoded.json -e ${masterKey}`);

    const loadedConfig = JSON.parse(
        await fs.readFile("config_output/config_decoded.json", 'utf8')
    );

    console.log("\nVerification:");
    console.log(`Environment: ${loadedConfig.environment}`);
    console.log(`Database Host: ${loadedConfig.database.host}`);
    console.log(`API Endpoint: ${loadedConfig.api.endpoint}`);
    console.log(`Features count: ${Object.keys(loadedConfig.features).length}`);
    console.log(`Allowed origins: ${loadedConfig.security.allowed_origins.length}`);

    // Test different encryption algorithms
    console.log("\n4. Testing different encryption algorithms...");

    // ChaCha20
    console.log("\nTesting ChaCha20...");
    FlexonHelper.runCommand(`serialize -i ${configFile} -o config_chacha20.flexon -e ${masterKey} ChaCha20`);

    // TripleDES
    console.log("Testing TripleDES...");
    FlexonHelper.runCommand(`serialize -i ${configFile} -o config_tripledes.flexon -e ${masterKey} TripleDES`);

    const stats = await Promise.all([
        fs.stat(configFile),
        fs.stat(flexonFile),
        fs.stat('config_chacha20.flexon'),
        fs.stat('config_tripledes.flexon')
    ]);

    console.log("\nFile size comparison:");
    console.log(`Original JSON: ${stats[0].size} bytes`);
    console.log(`AES-256: ${stats[1].size} bytes`);
    console.log(`ChaCha20: ${stats[2].size} bytes`);
    console.log(`TripleDES: ${stats[3].size} bytes`);
}

main().catch(console.error);
