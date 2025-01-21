package com.flexon.examples.ai;

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

public class AIData {
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

    static double[] createEmbeddings(String text, int dim) {
        try {
            // Create deterministic but seemingly random embeddings based on text hash
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            byte[] hash = digest.digest(text.getBytes());
            long seed = ((hash[0] & 0xFFL) << 56) |
                       ((hash[1] & 0xFFL) << 48) |
                       ((hash[2] & 0xFFL) << 40) |
                       ((hash[3] & 0xFFL) << 32) |
                       ((hash[4] & 0xFFL) << 24) |
                       ((hash[5] & 0xFFL) << 16) |
                       ((hash[6] & 0xFFL) << 8) |
                       (hash[7] & 0xFFL);
            
            Random random = new Random(seed);
            double[] embeddings = new double[dim];
            for (int i = 0; i < dim; i++) {
                embeddings[i] = random.nextGaussian();
            }
            return embeddings;
        } catch (NoSuchAlgorithmException e) {
            throw new RuntimeException(e);
        }
    }

    static Map<String, Object> createAIPrompt(String promptText, String model, List<Map<String, String>> context, double[] embeddings) {
        Map<String, Object> prompt = new HashMap<>();
        prompt.put("prompt", promptText);
        
        Map<String, Object> metadata = new HashMap<>();
        metadata.put("model", model);
        metadata.put("created_at", Instant.now().toString());
        metadata.put("version", "1.0");
        metadata.put("language", "en");
        metadata.put("domain", "programming");
        metadata.put("security_level", "medium");
        prompt.put("metadata", metadata);
        
        prompt.put("context", context);
        prompt.put("embeddings", embeddings);
        
        Map<String, Object> security = new HashMap<>();
        try {
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            byte[] hash = digest.digest((promptText + Instant.now().toString()).getBytes());
            security.put("fingerprint", Base64.getEncoder().encodeToString(hash));
            security.put("encrypted", true);
            security.put("access_level", "user");
            
            List<Map<String, String>> auditTrail = new ArrayList<>();
            Map<String, String> audit = new HashMap<>();
            audit.put("timestamp", Instant.now().toString());
            audit.put("action", "created");
            audit.put("user", "system");
            auditTrail.add(audit);
            security.put("audit_trail", auditTrail);
            
            prompt.put("security", security);
        } catch (NoSuchAlgorithmException e) {
            throw new RuntimeException(e);
        }
        
        return prompt;
    }

    static Map<String, Object> createTrainingDataset(List<Map.Entry<String, String>> pairs, String name, String description) {
        Map<String, Object> dataset = new HashMap<>();
        dataset.put("name", name);
        dataset.put("description", description);
        
        Map<String, Object> metadata = new HashMap<>();
        metadata.put("created_at", Instant.now().toString());
        metadata.put("version", "1.0");
        metadata.put("size", pairs.size());
        metadata.put("language", "en");
        metadata.put("domain", "programming");
        dataset.put("metadata", metadata);
        
        List<Map<String, Object>> data = new ArrayList<>();
        for (Map.Entry<String, String> pair : pairs) {
            Map<String, Object> entry = new HashMap<>();
            entry.put("input", pair.getKey());
            entry.put("output", pair.getValue());
            entry.put("embeddings", createEmbeddings(pair.getKey(), 384));
            
            Map<String, Object> metrics = new HashMap<>();
            metrics.put("input_tokens", pair.getKey().split("\\s+").length);
            metrics.put("output_tokens", pair.getValue().split("\\s+").length);
            metrics.put("quality_score", 0.95);  // Simulated quality score
            metrics.put("diversity_score", 0.85);  // Simulated diversity score
            entry.put("metrics", metrics);
            
            data.add(entry);
        }
        dataset.put("data", data);
        
        return dataset;
    }

    public static void main(String[] args) {
        try {
            System.out.println("Flexon AI Data Example (Java)");
            System.out.println("=============================\n");

            // Create AI prompt with context
            List<Map<String, String>> context = new ArrayList<>();
            Map<String, String> systemContext = new HashMap<>();
            systemContext.put("role", "system");
            systemContext.put("content", "You are a helpful AI assistant specializing in programming.");
            context.add(systemContext);
            
            Map<String, String> userContext = new HashMap<>();
            userContext.put("role", "user");
            userContext.put("content", "How do I implement a binary search tree in Java?");
            context.add(userContext);

            // Generate sample embeddings
            double[] embeddings = new double[10];
            for (int i = 0; i < 10; i++) {
                embeddings[i] = Math.sin(i * 0.1);
            }

            // Create AI prompt
            Map<String, Object> prompt = createAIPrompt(
                "Explain binary search tree implementation with code examples.",
                "gpt-4",
                context,
                embeddings
            );

            // Save prompt to JSON
            String promptFile = "prompt.json";
            String flexonFile = "ai_data.flexon";
            String schemaFile = "../../../FlexonCLI/AI/Schemas/prompt_schema.json";

            System.out.println("1. Saving AI prompt...");
            mapper.writeValue(new File(promptFile), prompt);

            // Create training data
            List<Map.Entry<String, String>> trainingPairs = Arrays.asList(
                new AbstractMap.SimpleEntry<>("What is a binary search tree?", 
                    "A binary search tree is a data structure where each node has at most two children..."),
                new AbstractMap.SimpleEntry<>("How to insert into BST?", 
                    "To insert into a BST, compare the value with the root and recursively insert into left or right subtree..."),
                new AbstractMap.SimpleEntry<>("BST search algorithm?", 
                    "To search in a BST, start at root, compare value, and traverse left or right based on comparison...")
            );

            Map<String, Object> trainingData = createTrainingDataset(
                trainingPairs,
                "bst-tutor-v1",
                "Binary Search Tree tutorial dataset"
            );

            String trainingFile = "training.json";
            String trainingSchemaFile = "../../../FlexonCLI/AI/Schemas/training_schema.json";
            String trainingFlexonFile = "training.flexon";

            System.out.println("2. Saving training data...");
            mapper.writeValue(new File(trainingFile), trainingData);

            // Validate and serialize prompt
            System.out.println("3. Validating and serializing prompt...");
            FlexonHelper.runCommand("serialize -i " + promptFile + " -o " + flexonFile + " -s " + schemaFile + " -e aikey ChaCha20");

            // Validate and serialize training data
            System.out.println("4. Validating and serializing training data...");
            FlexonHelper.runCommand("serialize -i " + trainingFile + " -o " + trainingFlexonFile + " -s " + trainingSchemaFile);

            // Load and verify
            System.out.println("5. Loading AI data...");
            Files.createDirectories(Path.of("ai_output"));
            FlexonHelper.runCommand("deserialize -i " + flexonFile + " -o ai_output/prompt_decoded.json -e aikey");

            Map<String, Object> loadedPrompt = mapper.readValue(
                new File("ai_output/prompt_decoded.json"), Map.class
            );

            System.out.println("\nVerification:");
            System.out.println("Prompt loaded: " + loadedPrompt.containsKey("prompt"));
            System.out.println("Metadata present: " + loadedPrompt.containsKey("metadata"));
            System.out.println("Context count: " + ((List<?>) loadedPrompt.get("context")).size());
            System.out.println("Embeddings size: " + ((List<?>) loadedPrompt.get("embeddings")).size());

            // Test different encryption methods
            System.out.println("\n6. Testing different encryption methods...");
            FlexonHelper.runCommand("serialize -i " + promptFile + " -o ai_aes.flexon -e aikey AES256");
            FlexonHelper.runCommand("serialize -i " + promptFile + " -o ai_tripledes.flexon -e aikey TripleDES");

            System.out.println("\nFile size comparison:");
            System.out.println("Original JSON: " + Files.size(Path.of(promptFile)) + " bytes");
            System.out.println("ChaCha20: " + Files.size(Path.of(flexonFile)) + " bytes");
            System.out.println("AES-256: " + Files.size(Path.of("ai_aes.flexon")) + " bytes");
            System.out.println("TripleDES: " + Files.size(Path.of("ai_tripledes.flexon")) + " bytes");

        } catch (IOException e) {
            System.err.println("Error: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
