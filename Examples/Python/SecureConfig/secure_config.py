import json
import subprocess
import os
from datetime import datetime
import secrets

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

class DatabaseConfig:
    def __init__(self):
        self.host = "db.example.com"
        self.port = 5432
        self.username = "admin"
        self.password = secrets.token_urlsafe(32)
        self.database = "production_db"
        self.ssl_mode = "verify-full"
        self.max_connections = 100
        self.timeout = 30

    def to_dict(self):
        return self.__dict__

class ApiConfig:
    def __init__(self):
        self.endpoint = "https://api.example.com/v1"
        self.api_key = secrets.token_urlsafe(48)
        self.timeout = 30000
        self.retry_attempts = 3
        self.headers = {
            "User-Agent": "MyApp/1.0",
            "X-Custom-Header": "custom-value"
        }
        self.rate_limit = {
            "requests_per_second": 10,
            "burst": 20
        }

    def to_dict(self):
        return self.__dict__

class AppConfig:
    def __init__(self):
        self.environment = "production"
        self.debug = False
        self.database = DatabaseConfig().to_dict()
        self.api = ApiConfig().to_dict()
        self.features = {
            "feature1": "enabled",
            "feature2": "disabled",
            "beta": "enabled"
        }
        self.logging = {
            "level": "INFO",
            "format": "%(asctime)s - %(name)s - %(levelname)s - %(message)s",
            "handlers": ["console", "file"]
        }
        self.security = {
            "jwt_secret": secrets.token_hex(32),
            "session_timeout": 3600,
            "allowed_origins": [
                "https://app.example.com",
                "https://admin.example.com"
            ],
            "cors_headers": [
                "Content-Type",
                "Authorization",
                "X-Custom-Header"
            ]
        }
        self.cache = {
            "type": "redis",
            "url": "redis://cache.example.com:6379",
            "ttl": 300
        }
        self.metrics = {
            "enabled": True,
            "provider": "prometheus",
            "endpoint": "/metrics",
            "collection_interval": 15
        }

    def to_dict(self):
        return {
            "environment": self.environment,
            "debug": self.debug,
            "database": self.database,
            "api": self.api,
            "features": self.features,
            "logging": self.logging,
            "security": self.security,
            "cache": self.cache,
            "metrics": self.metrics
        }

def create_schema():
    """Create JSON schema for configuration validation"""
    return {
        "type": "object",
        "required": ["environment", "database", "api", "security"],
        "properties": {
            "environment": {
                "type": "string",
                "enum": ["development", "staging", "production"]
            },
            "database": {
                "type": "object",
                "required": ["host", "port", "username", "password"]
            },
            "api": {
                "type": "object",
                "required": ["endpoint", "api_key"]
            },
            "security": {
                "type": "object",
                "required": ["jwt_secret", "allowed_origins"]
            }
        }
    }

def main():
    print("Flexon Secure Configuration Example (Python)")
    print("===========================================\n")

    # Create configuration
    config = AppConfig()
    config_file = "config.json"
    schema_file = "config_schema.json"
    flexon_file = "config.flexon"

    print("1. Creating configuration files...")
    with open(config_file, 'w') as f:
        json.dump(config.to_dict(), f, indent=2)
    
    with open(schema_file, 'w') as f:
        json.dump(create_schema(), f, indent=2)

    # Create secure configuration package
    print("2. Creating secure configuration package...")
    master_key = secrets.token_urlsafe(32)
    print(f"Generated master key: {master_key}")

    FlexonHelper.run_command(f"serialize -i {config_file} -o {flexon_file} -s {schema_file} -e {master_key} AES256")

    # Test configuration loading
    print("3. Loading secure configuration...")
    os.makedirs("config_output", exist_ok=True)
    FlexonHelper.run_command(f"deserialize -i {flexon_file} -o config_output/config_decoded.json -e {master_key}")

    with open("config_output/config_decoded.json", 'r') as f:
        loaded_config = json.load(f)

    print("\nVerification:")
    print(f"Environment: {loaded_config['environment']}")
    print(f"Database Host: {loaded_config['database']['host']}")
    print(f"API Endpoint: {loaded_config['api']['endpoint']}")
    print(f"Features count: {len(loaded_config['features'])}")
    print(f"Allowed origins: {len(loaded_config['security']['allowed_origins'])}")

    # Test different encryption algorithms
    print("\n4. Testing different encryption algorithms...")

    # ChaCha20
    print("\nTesting ChaCha20...")
    FlexonHelper.run_command(f"serialize -i {config_file} -o config_chacha20.flexon -e {master_key} ChaCha20")

    # TripleDES
    print("Testing TripleDES...")
    FlexonHelper.run_command(f"serialize -i {config_file} -o config_tripledes.flexon -e {master_key} TripleDES")

    # Compare file sizes
    print("\nFile size comparison:")
    print(f"Original JSON: {os.path.getsize(config_file)} bytes")
    print(f"AES-256: {os.path.getsize(flexon_file)} bytes")
    print(f"ChaCha20: {os.path.getsize('config_chacha20.flexon')} bytes")
    print(f"TripleDES: {os.path.getsize('config_tripledes.flexon')} bytes")

if __name__ == "__main__":
    main()
