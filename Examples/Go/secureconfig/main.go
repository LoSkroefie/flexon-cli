package main

import (
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"os"
	"os/exec"
	"time"
)

type DatabaseConfig struct {
	Host     string                 `json:"host"`
	Port     int                    `json:"port"`
	Database string                 `json:"database"`
	Username string                 `json:"username"`
	Password string                 `json:"password"`
	Options  map[string]interface{} `json:"options"`
}

type APIConfig struct {
	Endpoint      string            `json:"endpoint"`
	APIKey        string            `json:"api_key"`
	Timeout       int               `json:"timeout"`
	Headers       map[string]string `json:"headers"`
	AllowedMethods []string         `json:"allowed_methods"`
}

type CacheConfig struct {
	Provider string                 `json:"provider"`
	TTL      int                    `json:"ttl"`
	Settings map[string]interface{} `json:"settings"`
}

type SecurityConfig struct {
	Authentication map[string]interface{} `json:"authentication"`
	Authorization  map[string]interface{} `json:"authorization"`
	Encryption     map[string]interface{} `json:"encryption"`
}

type AppConfig struct {
	Version     string                 `json:"version"`
	Environment string                 `json:"environment"`
	Database    DatabaseConfig         `json:"database"`
	API         APIConfig              `json:"api"`
	Cache       CacheConfig            `json:"cache"`
	Security    SecurityConfig         `json:"security"`
	Metadata    map[string]interface{} `json:"metadata"`
}

type FlexonHelper struct{}

func (f *FlexonHelper) RunCommand(args string) error {
	cmd := exec.Command("cmd.exe", "/c", "flexon-cli "+args)
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	return cmd.Run()
}

func main() {
	fmt.Println("Flexon Secure Config Example (Go)")
	fmt.Println("=================================\n")

	// Create configuration
	config := AppConfig{
		Version:     "1.0.0",
		Environment: "production",
		Database: DatabaseConfig{
			Host:     "localhost",
			Port:     5432,
			Database: "myapp",
			Username: "admin",
			Password: "supersecret123",
			Options: map[string]interface{}{
				"maxConnections": 100,
				"timeout":        30,
				"ssl":           true,
			},
		},
		API: APIConfig{
			Endpoint: "https://api.example.com/v1",
			APIKey:   "sk_live_abcdef123456789",
			Timeout:  60,
			Headers: map[string]string{
				"User-Agent": "MyApp/1.0",
				"Accept":     "application/json",
			},
			AllowedMethods: []string{"GET", "POST", "PUT", "DELETE"},
		},
		Cache: CacheConfig{
			Provider: "redis",
			TTL:      3600,
			Settings: map[string]interface{}{
				"host":      "localhost",
				"port":      6379,
				"maxMemory": "2gb",
			},
		},
		Security: SecurityConfig{
			Authentication: map[string]interface{}{
				"provider":     "oauth2",
				"clientId":     "myapp_client",
				"clientSecret": "client_secret_xyz",
				"tokenUrl":     "https://auth.example.com/token",
			},
			Authorization: map[string]interface{}{
				"type":        "rbac",
				"defaultRole": "user",
				"adminRoles":  []string{"admin", "superadmin"},
			},
			Encryption: map[string]interface{}{
				"algorithm": "AES256",
				"keySize":   256,
				"provider":  "default",
			},
		},
	}

	// Add metadata
	config.Metadata = make(map[string]interface{})
	config.Metadata["created_at"] = time.Now().Format(time.RFC3339)
	config.Metadata["updated_at"] = time.Now().Format(time.RFC3339)
	config.Metadata["created_by"] = "system"

	// Add fingerprint
	configString := fmt.Sprintf("%s%s%s", config.Version, config.Environment, time.Now().Format(time.RFC3339))
	hash := sha256.Sum256([]byte(configString))
	config.Metadata["fingerprint"] = base64.StdEncoding.EncodeToString(hash[:])

	configFile := "config.json"
	encryptedFile := "config.flexon"
	schemaFile := "../../../FlexonCLI/Config/Schemas/config_schema.json"

	// Save configuration
	fmt.Println("1. Saving configuration...")
	jsonData, err := json.MarshalIndent(config, "", "  ")
	if err != nil {
		fmt.Printf("Error marshaling JSON: %v\n", err)
		return
	}
	err = ioutil.WriteFile(configFile, jsonData, 0644)
	if err != nil {
		fmt.Printf("Error writing JSON file: %v\n", err)
		return
	}

	flexon := &FlexonHelper{}

	// Encrypt with different algorithms
	fmt.Println("2. Testing encryption algorithms...")

	// AES-256
	fmt.Println("\nUsing AES-256...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o config_aes.flexon -e configkey AES256", configFile))
	if err != nil {
		fmt.Printf("Error with AES-256: %v\n", err)
	}

	// ChaCha20
	fmt.Println("\nUsing ChaCha20...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o config_chacha20.flexon -e configkey ChaCha20", configFile))
	if err != nil {
		fmt.Printf("Error with ChaCha20: %v\n", err)
	}

	// TripleDES
	fmt.Println("\nUsing TripleDES...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o config_tripledes.flexon -e configkey TripleDES", configFile))
	if err != nil {
		fmt.Printf("Error with TripleDES: %v\n", err)
	}

	// Validate and encrypt with schema
	fmt.Println("\n3. Validating and encrypting with schema...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o %s -s %s -e configkey", configFile, encryptedFile, schemaFile))
	if err != nil {
		fmt.Printf("Error validating and encrypting: %v\n", err)
		return
	}

	// Load and verify
	fmt.Println("4. Loading configuration...")
	err = flexon.RunCommand(fmt.Sprintf("deserialize -i %s -o config_decoded.json -e configkey", encryptedFile))
	if err != nil {
		fmt.Printf("Error loading configuration: %v\n", err)
		return
	}

	loadedData, err := ioutil.ReadFile("config_decoded.json")
	if err != nil {
		fmt.Printf("Error reading loaded data: %v\n", err)
		return
	}

	var loadedConfig AppConfig
	err = json.Unmarshal(loadedData, &loadedConfig)
	if err != nil {
		fmt.Printf("Error unmarshaling loaded data: %v\n", err)
		return
	}

	fmt.Println("\nVerification:")
	fmt.Printf("Version: %s\n", loadedConfig.Version)
	fmt.Printf("Environment: %s\n", loadedConfig.Environment)
	fmt.Printf("Database host: %s\n", loadedConfig.Database.Host)
	fmt.Printf("API endpoint: %s\n", loadedConfig.API.Endpoint)
	fmt.Printf("Cache provider: %s\n", loadedConfig.Cache.Provider)
	fmt.Printf("Auth provider: %s\n", loadedConfig.Security.Authentication["provider"])

	// Compare file sizes
	fmt.Println("\nFile size comparison:")
	configInfo, _ := os.Stat(configFile)
	aesInfo, _ := os.Stat("config_aes.flexon")
	chacha20Info, _ := os.Stat("config_chacha20.flexon")
	tripledesInfo, _ := os.Stat("config_tripledes.flexon")

	fmt.Printf("Original JSON: %d bytes\n", configInfo.Size())
	fmt.Printf("AES-256: %d bytes\n", aesInfo.Size())
	fmt.Printf("ChaCha20: %d bytes\n", chacha20Info.Size())
	fmt.Printf("TripleDES: %d bytes\n", tripledesInfo.Size())
}
