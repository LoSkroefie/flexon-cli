package main

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"os"
	"os/exec"
	"path/filepath"
	"time"
)

type User struct {
	Name      string                 `json:"name"`
	Age       int                    `json:"age"`
	Email     string                 `json:"email"`
	Interests []string               `json:"interests"`
	Metadata  map[string]interface{} `json:"metadata"`
}

type FlexonHelper struct{}

func (f *FlexonHelper) RunCommand(args string) error {
	cmd := exec.Command("cmd.exe", "/c", "flexon-cli "+args)
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	return cmd.Run()
}

func main() {
	fmt.Println("Flexon Basic Usage Example (Go)")
	fmt.Println("===============================\n")

	// Create sample data
	user := User{
		Name:      "Alice Smith",
		Age:       28,
		Email:     "alice@example.com",
		Interests: []string{"programming", "AI", "data science"},
		Metadata: map[string]interface{}{
			"created_at":  time.Now().Format(time.RFC3339),
			"last_login":  time.Now().Format(time.RFC3339),
			"preferences": map[string]interface{}{
				"theme":         "dark",
				"notifications": true,
			},
		},
	}

	// Save to JSON
	jsonFile := "user.json"
	flexonFile := "user.flexon"
	encryptedFile := "user_encrypted.flexon"
	decryptedFile := "user_decrypted.json"

	fmt.Println("1. Creating JSON file...")
	jsonData, err := json.MarshalIndent(user, "", "  ")
	if err != nil {
		fmt.Printf("Error marshaling JSON: %v\n", err)
		return
	}
	err = ioutil.WriteFile(jsonFile, jsonData, 0644)
	if err != nil {
		fmt.Printf("Error writing JSON file: %v\n", err)
		return
	}

	flexon := &FlexonHelper{}

	// Basic serialization
	fmt.Println("2. Converting to Flexon format...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o %s", jsonFile, flexonFile))
	if err != nil {
		fmt.Printf("Error serializing to Flexon: %v\n", err)
		return
	}

	// Encryption
	fmt.Println("3. Creating encrypted Flexon file...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o %s -e mysecretkey", jsonFile, encryptedFile))
	if err != nil {
		fmt.Printf("Error creating encrypted file: %v\n", err)
		return
	}

	// Decryption
	fmt.Println("4. Decrypting Flexon file...")
	err = flexon.RunCommand(fmt.Sprintf("deserialize -i %s -o %s -e mysecretkey", encryptedFile, decryptedFile))
	if err != nil {
		fmt.Printf("Error decrypting file: %v\n", err)
		return
	}

	// Verify results
	fmt.Println("\nResults:")
	jsonInfo, _ := os.Stat(jsonFile)
	flexonInfo, _ := os.Stat(flexonFile)
	encryptedInfo, _ := os.Stat(encryptedFile)

	fmt.Printf("Original JSON size: %d bytes\n", jsonInfo.Size())
	fmt.Printf("Flexon size: %d bytes\n", flexonInfo.Size())
	fmt.Printf("Encrypted size: %d bytes\n", encryptedInfo.Size())

	// Compare contents
	var originalUser, decryptedUser User
	originalData, _ := ioutil.ReadFile(jsonFile)
	decryptedData, _ := ioutil.ReadFile(decryptedFile)

	json.Unmarshal(originalData, &originalUser)
	json.Unmarshal(decryptedData, &decryptedUser)

	fmt.Println("\nData verification:")
	fmt.Printf("Name matches: %v\n", originalUser.Name == decryptedUser.Name)
	fmt.Printf("Age matches: %v\n", originalUser.Age == decryptedUser.Age)
	fmt.Printf("Email matches: %v\n", originalUser.Email == decryptedUser.Email)

	// Test different encryption algorithms
	fmt.Println("\n5. Testing different encryption algorithms...")

	// ChaCha20
	fmt.Println("\nTesting ChaCha20...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o user_chacha20.flexon -e mysecretkey ChaCha20", jsonFile))
	if err != nil {
		fmt.Printf("Error with ChaCha20: %v\n", err)
	}

	// TripleDES
	fmt.Println("Testing TripleDES...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o user_tripledes.flexon -e mysecretkey TripleDES", jsonFile))
	if err != nil {
		fmt.Printf("Error with TripleDES: %v\n", err)
	}

	// Compare file sizes
	fmt.Println("\nFile size comparison:")
	fmt.Printf("Original JSON: %d bytes\n", jsonInfo.Size())
	aesInfo, _ := os.Stat(encryptedFile)
	chacha20Info, _ := os.Stat("user_chacha20.flexon")
	tripledesInfo, _ := os.Stat("user_tripledes.flexon")

	fmt.Printf("AES-256: %d bytes\n", aesInfo.Size())
	fmt.Printf("ChaCha20: %d bytes\n", chacha20Info.Size())
	fmt.Printf("TripleDES: %d bytes\n", tripledesInfo.Size())
}
