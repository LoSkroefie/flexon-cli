package com.flexon.examples.config;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.time.Instant;
import java.util.*;

public class SecureConfig {
    private static final ObjectMapper mapper = new ObjectMapper()
            .enable(SerializationFeature.INDENT_OUTPUT);

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

    static class DatabaseConfig {
        public String host;
        public int port;
        public String database;
        public String username;
        public String password;
        public Map<String, Object> options;

        public DatabaseConfig() {
            this.host = "localhost";
            this.port = 5432;
            this.database = "myapp";
            this.username = "admin";
            this.password = "supersecret123";
            this.options = new HashMap<>();
            this.options.put("maxConnections", 100);
            this.options.put("timeout", 30);
            this.options.put("ssl", true);
        }
    }

    static class APIConfig {
        public String endpoint;
        public String apiKey;
        public int timeout;
        public Map<String, String> headers;
        public List<String> allowedMethods;

        public APIConfig() {
            this.endpoint = "https://api.example.com/v1";
            this.apiKey = "sk_live_abcdef123456789";
            this.timeout = 60;
            this.headers = new HashMap<>();
            this.headers.put("User-Agent", "MyApp/1.0");
            this.headers.put("Accept", "application/json");
            this.allowedMethods = Arrays.asList("GET", "POST", "PUT", "DELETE");
        }
    }

    static class CacheConfig {
        public String provider;
        public int ttl;
        public Map<String, Object> settings;

        public CacheConfig() {
            this.provider = "redis";
            this.ttl = 3600;
            this.settings = new HashMap<>();
            this.settings.put("host", "localhost");
            this.settings.put("port", 6379);
            this.settings.put("maxMemory", "2gb");
        }
    }

    static class SecurityConfig {
        public Map<String, Object> authentication;
        public Map<String, Object> authorization;
        public Map<String, Object> encryption;

        public SecurityConfig() {
            this.authentication = new HashMap<>();
            this.authentication.put("provider", "oauth2");
            this.authentication.put("clientId", "myapp_client");
            this.authentication.put("clientSecret", "client_secret_xyz");
            this.authentication.put("tokenUrl", "https://auth.example.com/token");

            this.authorization = new HashMap<>();
            this.authorization.put("type", "rbac");
            this.authorization.put("defaultRole", "user");
            List<String> adminRoles = Arrays.asList("admin", "superadmin");
            this.authorization.put("adminRoles", adminRoles);

            this.encryption = new HashMap<>();
            this.encryption.put("algorithm", "AES256");
            this.encryption.put("keySize", 256);
            this.encryption.put("provider", "default");
        }
    }

    static class AppConfig {
        public String version;
        public String environment;
        public DatabaseConfig database;
        public APIConfig api;
        public CacheConfig cache;
        public SecurityConfig security;
        public Map<String, Object> metadata;

        public AppConfig() {
            this.version = "1.0.0";
            this.environment = "production";
            this.database = new DatabaseConfig();
            this.api = new APIConfig();
            this.cache = new CacheConfig();
            this.security = new SecurityConfig();
            
            this.metadata = new HashMap<>();
            this.metadata.put("created_at", Instant.now().toString());
            this.metadata.put("updated_at", Instant.now().toString());
            this.metadata.put("created_by", "system");
            
            try {
                MessageDigest digest = MessageDigest.getInstance("SHA-256");
                String configString = this.version + this.environment + Instant.now().toString();
                byte[] hash = digest.digest(configString.getBytes());
                this.metadata.put("fingerprint", Base64.getEncoder().encodeToString(hash));
            } catch (NoSuchAlgorithmException e) {
                throw new RuntimeException(e);
            }
        }
    }

    public static void main(String[] args) {
        try {
            System.out.println("Flexon Secure Config Example (Java)");
            System.out.println("==================================\n");

            // Create configuration
            AppConfig config = new AppConfig();
            String configFile = "config.json";
            String encryptedFile = "config.flexon";
            String schemaFile = "../../../FlexonCLI/Config/Schemas/config_schema.json";

            // Save configuration
            System.out.println("1. Saving configuration...");
            mapper.writeValue(new File(configFile), config);

            // Encrypt with different algorithms
            System.out.println("2. Testing encryption algorithms...");
            
            // AES-256
            System.out.println("\nUsing AES-256...");
            FlexonHelper.runCommand("serialize -i " + configFile + " -o config_aes.flexon -e configkey AES256");
            
            // ChaCha20
            System.out.println("\nUsing ChaCha20...");
            FlexonHelper.runCommand("serialize -i " + configFile + " -o config_chacha20.flexon -e configkey ChaCha20");
            
            // TripleDES
            System.out.println("\nUsing TripleDES...");
            FlexonHelper.runCommand("serialize -i " + configFile + " -o config_tripledes.flexon -e configkey TripleDES");

            // Validate and encrypt with schema
            System.out.println("\n3. Validating and encrypting with schema...");
            FlexonHelper.runCommand("serialize -i " + configFile + " -o " + encryptedFile + " -s " + schemaFile + " -e configkey");

            // Load and verify
            System.out.println("4. Loading configuration...");
            FlexonHelper.runCommand("deserialize -i " + encryptedFile + " -o config_decoded.json -e configkey");

            AppConfig loadedConfig = mapper.readValue(
                new File("config_decoded.json"), AppConfig.class
            );

            System.out.println("\nVerification:");
            System.out.println("Version: " + loadedConfig.version);
            System.out.println("Environment: " + loadedConfig.environment);
            System.out.println("Database host: " + loadedConfig.database.host);
            System.out.println("API endpoint: " + loadedConfig.api.endpoint);
            System.out.println("Cache provider: " + loadedConfig.cache.provider);
            System.out.println("Auth provider: " + loadedConfig.security.authentication.get("provider"));

            // Compare file sizes
            System.out.println("\nFile size comparison:");
            System.out.println("Original JSON: " + Files.size(Path.of(configFile)) + " bytes");
            System.out.println("AES-256: " + Files.size(Path.of("config_aes.flexon")) + " bytes");
            System.out.println("ChaCha20: " + Files.size(Path.of("config_chacha20.flexon")) + " bytes");
            System.out.println("TripleDES: " + Files.size(Path.of("config_tripledes.flexon")) + " bytes");

        } catch (IOException e) {
            System.err.println("Error: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
