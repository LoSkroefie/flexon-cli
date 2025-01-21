package main

import (
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"math"
	"math/rand"
	"os"
	"os/exec"
	"time"
)

type FlexonHelper struct{}

func (f *FlexonHelper) RunCommand(args string) error {
	cmd := exec.Command("cmd.exe", "/c", "flexon-cli "+args)
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	return cmd.Run()
}

func createEmbeddings(text string, dim int) []float64 {
	// Create deterministic but seemingly random embeddings based on text hash
	hash := sha256.Sum256([]byte(text))
	seed := int64(hash[0])<<56 | int64(hash[1])<<48 | int64(hash[2])<<40 | int64(hash[3])<<32 |
		int64(hash[4])<<24 | int64(hash[5])<<16 | int64(hash[6])<<8 | int64(hash[7])

	r := rand.New(rand.NewSource(seed))
	embeddings := make([]float64, dim)
	for i := 0; i < dim; i++ {
		embeddings[i] = r.NormFloat64()
	}

	// Normalize embeddings
	var sum float64
	for _, v := range embeddings {
		sum += v * v
	}
	norm := math.Sqrt(sum)
	for i := range embeddings {
		embeddings[i] /= norm
	}

	return embeddings
}

func createAIPrompt(promptText, model string, context []map[string]string, embeddings []float64) map[string]interface{} {
	prompt := make(map[string]interface{})
	prompt["prompt"] = promptText

	metadata := map[string]interface{}{
		"model":          model,
		"created_at":     time.Now().Format(time.RFC3339),
		"version":        "1.0",
		"language":       "en",
		"domain":         "programming",
		"security_level": "medium",
	}
	prompt["metadata"] = metadata

	prompt["context"] = context
	prompt["embeddings"] = embeddings

	// Security information
	security := make(map[string]interface{})
	hash := sha256.Sum256([]byte(promptText + time.Now().Format(time.RFC3339)))
	security["fingerprint"] = base64.StdEncoding.EncodeToString(hash[:])
	security["encrypted"] = true
	security["access_level"] = "user"

	auditTrail := []map[string]string{
		{
			"timestamp": time.Now().Format(time.RFC3339),
			"action":    "created",
			"user":      "system",
		},
	}
	security["audit_trail"] = auditTrail

	prompt["security"] = security

	return prompt
}

func createTrainingDataset(pairs []map[string]string, name, description string) map[string]interface{} {
	dataset := make(map[string]interface{})
	dataset["name"] = name
	dataset["description"] = description

	metadata := map[string]interface{}{
		"created_at": time.Now().Format(time.RFC3339),
		"version":    "1.0",
		"size":       len(pairs),
		"language":   "en",
		"domain":     "programming",
	}
	dataset["metadata"] = metadata

	data := make([]map[string]interface{}, len(pairs))
	for i, pair := range pairs {
		entry := make(map[string]interface{})
		entry["input"] = pair["input"]
		entry["output"] = pair["output"]
		entry["embeddings"] = createEmbeddings(pair["input"], 384)

		metrics := map[string]interface{}{
			"input_tokens":    len(pair["input"]),
			"output_tokens":   len(pair["output"]),
			"quality_score":   0.95, // Simulated quality score
			"diversity_score": 0.85, // Simulated diversity score
		}
		entry["metrics"] = metrics

		data[i] = entry
	}
	dataset["data"] = data

	return dataset
}

func main() {
	fmt.Println("Flexon AI Data Example (Go)")
	fmt.Println("===========================\n")

	// Create AI prompt with context
	context := []map[string]string{
		{
			"role":    "system",
			"content": "You are a helpful AI assistant specializing in programming.",
		},
		{
			"role":    "user",
			"content": "How do I implement a binary search tree in Go?",
		},
	}

	// Generate sample embeddings
	embeddings := make([]float64, 10)
	for i := range embeddings {
		embeddings[i] = math.Sin(float64(i) * 0.1)
	}

	// Create AI prompt
	prompt := createAIPrompt(
		"Explain binary search tree implementation with code examples.",
		"gpt-4",
		context,
		embeddings,
	)

	// Save prompt to JSON
	promptFile := "prompt.json"
	flexonFile := "ai_data.flexon"
	schemaFile := "../../../FlexonCLI/AI/Schemas/prompt_schema.json"

	fmt.Println("1. Saving AI prompt...")
	jsonData, err := json.MarshalIndent(prompt, "", "  ")
	if err != nil {
		fmt.Printf("Error marshaling JSON: %v\n", err)
		return
	}
	err = ioutil.WriteFile(promptFile, jsonData, 0644)
	if err != nil {
		fmt.Printf("Error writing JSON file: %v\n", err)
		return
	}

	// Create training data
	trainingPairs := []map[string]string{
		{
			"input":  "What is a binary search tree?",
			"output": "A binary search tree is a data structure where each node has at most two children...",
		},
		{
			"input":  "How to insert into BST?",
			"output": "To insert into a BST, compare the value with the root and recursively insert into left or right subtree...",
		},
		{
			"input":  "BST search algorithm?",
			"output": "To search in a BST, start at root, compare value, and traverse left or right based on comparison...",
		},
	}

	trainingData := createTrainingDataset(
		trainingPairs,
		"bst-tutor-v1",
		"Binary Search Tree tutorial dataset",
	)

	trainingFile := "training.json"
	trainingSchemaFile := "../../../FlexonCLI/AI/Schemas/training_schema.json"
	trainingFlexonFile := "training.flexon"

	fmt.Println("2. Saving training data...")
	trainingJSON, err := json.MarshalIndent(trainingData, "", "  ")
	if err != nil {
		fmt.Printf("Error marshaling training data: %v\n", err)
		return
	}
	err = ioutil.WriteFile(trainingFile, trainingJSON, 0644)
	if err != nil {
		fmt.Printf("Error writing training file: %v\n", err)
		return
	}

	flexon := &FlexonHelper{}

	// Validate and serialize prompt
	fmt.Println("3. Validating and serializing prompt...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o %s -s %s -e aikey ChaCha20", promptFile, flexonFile, schemaFile))
	if err != nil {
		fmt.Printf("Error serializing prompt: %v\n", err)
		return
	}

	// Validate and serialize training data
	fmt.Println("4. Validating and serializing training data...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o %s -s %s", trainingFile, trainingFlexonFile, trainingSchemaFile))
	if err != nil {
		fmt.Printf("Error serializing training data: %v\n", err)
		return
	}

	// Load and verify
	fmt.Println("5. Loading AI data...")
	err = os.MkdirAll("ai_output", 0755)
	if err != nil {
		fmt.Printf("Error creating directory: %v\n", err)
		return
	}
	err = flexon.RunCommand(fmt.Sprintf("deserialize -i %s -o ai_output/prompt_decoded.json -e aikey", flexonFile))
	if err != nil {
		fmt.Printf("Error deserializing prompt: %v\n", err)
		return
	}

	loadedData, err := ioutil.ReadFile("ai_output/prompt_decoded.json")
	if err != nil {
		fmt.Printf("Error reading loaded data: %v\n", err)
		return
	}

	var loadedPrompt map[string]interface{}
	err = json.Unmarshal(loadedData, &loadedPrompt)
	if err != nil {
		fmt.Printf("Error unmarshaling loaded data: %v\n", err)
		return
	}

	fmt.Println("\nVerification:")
	fmt.Printf("Prompt loaded: %v\n", loadedPrompt["prompt"] != nil)
	fmt.Printf("Metadata present: %v\n", loadedPrompt["metadata"] != nil)
	fmt.Printf("Context count: %d\n", len(loadedPrompt["context"].([]interface{})))
	fmt.Printf("Embeddings size: %d\n", len(loadedPrompt["embeddings"].([]interface{})))

	// Test different encryption methods
	fmt.Println("\n6. Testing different encryption methods...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o ai_aes.flexon -e aikey AES256", promptFile))
	if err != nil {
		fmt.Printf("Error with AES-256: %v\n", err)
	}

	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o ai_tripledes.flexon -e aikey TripleDES", promptFile))
	if err != nil {
		fmt.Printf("Error with TripleDES: %v\n", err)
	}

	fmt.Println("\nFile size comparison:")
	promptInfo, _ := os.Stat(promptFile)
	chacha20Info, _ := os.Stat(flexonFile)
	aesInfo, _ := os.Stat("ai_aes.flexon")
	tripledesInfo, _ := os.Stat("ai_tripledes.flexon")

	fmt.Printf("Original JSON: %d bytes\n", promptInfo.Size())
	fmt.Printf("ChaCha20: %d bytes\n", chacha20Info.Size())
	fmt.Printf("AES-256: %d bytes\n", aesInfo.Size())
	fmt.Printf("TripleDES: %d bytes\n", tripledesInfo.Size())
}
