package com.flexon.examples.benchmark;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import org.openjdk.jmh.annotations.*;
import org.openjdk.jmh.runner.Runner;
import org.openjdk.jmh.runner.RunnerException;
import org.openjdk.jmh.runner.options.Options;
import org.openjdk.jmh.runner.options.OptionsBuilder;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Instant;
import java.util.*;
import java.util.concurrent.TimeUnit;

@State(Scope.Benchmark)
@BenchmarkMode(Mode.AverageTime)
@OutputTimeUnit(TimeUnit.MILLISECONDS)
@Warmup(iterations = 2, time = 1)
@Measurement(iterations = 3, time = 1)
@Fork(1)
public class Benchmarking {
    private static final ObjectMapper mapper = new ObjectMapper()
            .enable(SerializationFeature.INDENT_OUTPUT);

    private String jsonFile;
    private String flexonFile;
    private String encryptedFile;
    private TestData testData;

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

    static class TestData {
        public String id;
        public String name;
        public String description;
        public Map<String, Object> metadata;
        public List<Map<String, Object>> records;

        public TestData() {
            this.id = UUID.randomUUID().toString();
            this.name = "Benchmark Dataset";
            this.description = "Large dataset for benchmarking Flexon performance";
            this.metadata = new HashMap<>();
            this.metadata.put("created_at", Instant.now().toString());
            this.metadata.put("version", "1.0");
            this.metadata.put("type", "benchmark");
            
            this.records = new ArrayList<>();
            Random random = new Random();
            
            for (int i = 0; i < 1000; i++) {
                Map<String, Object> record = new HashMap<>();
                record.put("id", UUID.randomUUID().toString());
                record.put("timestamp", Instant.now().toString());
                record.put("value", random.nextDouble() * 1000);
                
                Map<String, Object> metrics = new HashMap<>();
                metrics.put("cpu", random.nextDouble() * 100);
                metrics.put("memory", random.nextDouble() * 16384);
                metrics.put("disk", random.nextDouble() * 1024);
                record.put("metrics", metrics);
                
                List<String> tags = new ArrayList<>();
                String[] possibleTags = {"web", "database", "cache", "compute", "storage"};
                for (String tag : possibleTags) {
                    if (random.nextBoolean()) {
                        tags.add(tag);
                    }
                }
                record.put("tags", tags);
                
                this.records.add(record);
            }
        }
    }

    @Setup
    public void setup() throws IOException {
        testData = new TestData();
        jsonFile = "benchmark_data.json";
        flexonFile = "benchmark_data.flexon";
        encryptedFile = "benchmark_encrypted.flexon";
        
        mapper.writeValue(new File(jsonFile), testData);
    }

    @Benchmark
    public void jsonSerialize() throws IOException {
        mapper.writeValue(new File("test_serialize.json"), testData);
    }

    @Benchmark
    public void jsonDeserialize() throws IOException {
        mapper.readValue(new File(jsonFile), TestData.class);
    }

    @Benchmark
    public void flexonSerialize() {
        FlexonHelper.runCommand("serialize -i " + jsonFile + " -o test_flexon.flexon");
    }

    @Benchmark
    public void flexonSerializeEncrypted() {
        FlexonHelper.runCommand("serialize -i " + jsonFile + " -o test_encrypted.flexon -e benchmarkkey");
    }

    @Benchmark
    public void flexonDeserialize() {
        FlexonHelper.runCommand("deserialize -i " + flexonFile + " -o test_decoded.json");
    }

    @Benchmark
    public void flexonDeserializeEncrypted() {
        FlexonHelper.runCommand("deserialize -i " + encryptedFile + " -o test_decrypted.json -e benchmarkkey");
    }

    public static void main(String[] args) {
        try {
            System.out.println("Flexon Benchmarking Example (Java)");
            System.out.println("=================================\n");

            // Create test data
            TestData data = new TestData();
            String jsonFile = "benchmark_data.json";
            String flexonFile = "benchmark_data.flexon";
            String encryptedFile = "benchmark_encrypted.flexon";

            System.out.println("1. Creating test data...");
            mapper.writeValue(new File(jsonFile), data);

            // Basic serialization
            System.out.println("2. Testing basic serialization...");
            FlexonHelper.runCommand("serialize -i " + jsonFile + " -o " + flexonFile);

            // Encrypted serialization
            System.out.println("3. Testing encrypted serialization...");
            FlexonHelper.runCommand("serialize -i " + jsonFile + " -o " + encryptedFile + " -e benchmarkkey");

            // Test different encryption algorithms
            System.out.println("\n4. Testing different encryption algorithms...");
            
            // AES-256
            System.out.println("Testing AES-256...");
            FlexonHelper.runCommand("serialize -i " + jsonFile + " -o benchmark_aes.flexon -e benchmarkkey AES256");
            
            // ChaCha20
            System.out.println("Testing ChaCha20...");
            FlexonHelper.runCommand("serialize -i " + jsonFile + " -o benchmark_chacha20.flexon -e benchmarkkey ChaCha20");
            
            // TripleDES
            System.out.println("Testing TripleDES...");
            FlexonHelper.runCommand("serialize -i " + jsonFile + " -o benchmark_tripledes.flexon -e benchmarkkey TripleDES");

            // Compare file sizes
            System.out.println("\nFile size comparison:");
            System.out.println("Original JSON: " + Files.size(Path.of(jsonFile)) + " bytes");
            System.out.println("Flexon: " + Files.size(Path.of(flexonFile)) + " bytes");
            System.out.println("AES-256: " + Files.size(Path.of("benchmark_aes.flexon")) + " bytes");
            System.out.println("ChaCha20: " + Files.size(Path.of("benchmark_chacha20.flexon")) + " bytes");
            System.out.println("TripleDES: " + Files.size(Path.of("benchmark_tripledes.flexon")) + " bytes");

            // Run JMH benchmarks
            System.out.println("\n5. Running JMH benchmarks...");
            Options opt = new OptionsBuilder()
                    .include(Benchmarking.class.getSimpleName())
                    .build();
            new Runner(opt).run();

        } catch (IOException | RunnerException e) {
            System.err.println("Error: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
