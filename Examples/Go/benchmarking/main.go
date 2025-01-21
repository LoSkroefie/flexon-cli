package main

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"math/rand"
	"os"
	"os/exec"
	"testing"
	"time"
)

type TestData struct {
	ID          string                   `json:"id"`
	Name        string                   `json:"name"`
	Description string                   `json:"description"`
	Metadata    map[string]interface{}   `json:"metadata"`
	Records     []map[string]interface{} `json:"records"`
}

type FlexonHelper struct{}

func (f *FlexonHelper) RunCommand(args string) error {
	cmd := exec.Command("cmd.exe", "/c", "flexon-cli "+args)
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	return cmd.Run()
}

func createTestData() TestData {
	data := TestData{
		ID:          fmt.Sprintf("test-%d", time.Now().UnixNano()),
		Name:        "Benchmark Dataset",
		Description: "Large dataset for benchmarking Flexon performance",
		Metadata: map[string]interface{}{
			"created_at": time.Now().Format(time.RFC3339),
			"version":    "1.0",
			"type":       "benchmark",
		},
		Records: make([]map[string]interface{}, 0),
	}

	possibleTags := []string{"web", "database", "cache", "compute", "storage"}

	// Generate 1000 records
	for i := 0; i < 1000; i++ {
		record := make(map[string]interface{})
		record["id"] = fmt.Sprintf("record-%d", i)
		record["timestamp"] = time.Now().Format(time.RFC3339)
		record["value"] = rand.Float64() * 1000

		metrics := make(map[string]interface{})
		metrics["cpu"] = rand.Float64() * 100
		metrics["memory"] = rand.Float64() * 16384
		metrics["disk"] = rand.Float64() * 1024
		record["metrics"] = metrics

		tags := make([]string, 0)
		for _, tag := range possibleTags {
			if rand.Float32() > 0.5 {
				tags = append(tags, tag)
			}
		}
		record["tags"] = tags

		data.Records = append(data.Records, record)
	}

	return data
}

func BenchmarkJSONMarshal(b *testing.B) {
	data := createTestData()
	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		_, err := json.Marshal(data)
		if err != nil {
			b.Fatal(err)
		}
	}
}

func BenchmarkJSONUnmarshal(b *testing.B) {
	data := createTestData()
	jsonData, _ := json.Marshal(data)
	var result TestData
	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		err := json.Unmarshal(jsonData, &result)
		if err != nil {
			b.Fatal(err)
		}
	}
}

func main() {
	fmt.Println("Flexon Benchmarking Example (Go)")
	fmt.Println("================================\n")

	// Create test data
	data := createTestData()
	jsonFile := "benchmark_data.json"
	flexonFile := "benchmark_data.flexon"
	encryptedFile := "benchmark_encrypted.flexon"

	fmt.Println("1. Creating test data...")
	jsonData, err := json.MarshalIndent(data, "", "  ")
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
	fmt.Println("2. Testing basic serialization...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o %s", jsonFile, flexonFile))
	if err != nil {
		fmt.Printf("Error with basic serialization: %v\n", err)
		return
	}

	// Encrypted serialization
	fmt.Println("3. Testing encrypted serialization...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o %s -e benchmarkkey", jsonFile, encryptedFile))
	if err != nil {
		fmt.Printf("Error with encrypted serialization: %v\n", err)
		return
	}

	// Test different encryption algorithms
	fmt.Println("\n4. Testing different encryption algorithms...")

	// AES-256
	fmt.Println("Testing AES-256...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o benchmark_aes.flexon -e benchmarkkey AES256", jsonFile))
	if err != nil {
		fmt.Printf("Error with AES-256: %v\n", err)
	}

	// ChaCha20
	fmt.Println("Testing ChaCha20...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o benchmark_chacha20.flexon -e benchmarkkey ChaCha20", jsonFile))
	if err != nil {
		fmt.Printf("Error with ChaCha20: %v\n", err)
	}

	// TripleDES
	fmt.Println("Testing TripleDES...")
	err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o benchmark_tripledes.flexon -e benchmarkkey TripleDES", jsonFile))
	if err != nil {
		fmt.Printf("Error with TripleDES: %v\n", err)
	}

	// Compare file sizes
	fmt.Println("\nFile size comparison:")
	jsonInfo, _ := os.Stat(jsonFile)
	flexonInfo, _ := os.Stat(flexonFile)
	aesInfo, _ := os.Stat("benchmark_aes.flexon")
	chacha20Info, _ := os.Stat("benchmark_chacha20.flexon")
	tripledesInfo, _ := os.Stat("benchmark_tripledes.flexon")

	fmt.Printf("Original JSON: %d bytes\n", jsonInfo.Size())
	fmt.Printf("Flexon: %d bytes\n", flexonInfo.Size())
	fmt.Printf("AES-256: %d bytes\n", aesInfo.Size())
	fmt.Printf("ChaCha20: %d bytes\n", chacha20Info.Size())
	fmt.Printf("TripleDES: %d bytes\n", tripledesInfo.Size())

	// Run benchmarks
	fmt.Println("\n5. Running Go benchmarks...")
	testing.Benchmark(BenchmarkJSONMarshal)
	testing.Benchmark(BenchmarkJSONUnmarshal)

	// Run Flexon benchmarks
	fmt.Println("\n6. Running Flexon benchmarks...")
	
	// Benchmark serialization
	start := time.Now()
	for i := 0; i < 10; i++ {
		err = flexon.RunCommand(fmt.Sprintf("serialize -i %s -o benchmark_test.flexon", jsonFile))
		if err != nil {
			fmt.Printf("Error in serialization benchmark: %v\n", err)
			return
		}
	}
	serializationTime := time.Since(start) / 10
	fmt.Printf("Average Flexon serialization time: %v\n", serializationTime)

	// Benchmark deserialization
	start = time.Now()
	for i := 0; i < 10; i++ {
		err = flexon.RunCommand(fmt.Sprintf("deserialize -i %s -o benchmark_test.json", flexonFile))
		if err != nil {
			fmt.Printf("Error in deserialization benchmark: %v\n", err)
			return
		}
	}
	deserializationTime := time.Since(start) / 10
	fmt.Printf("Average Flexon deserialization time: %v\n", deserializationTime)
}
