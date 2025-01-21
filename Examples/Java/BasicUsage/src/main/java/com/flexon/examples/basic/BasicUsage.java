package com.flexon.examples.basic;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Instant;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class BasicUsage {
    private static final ObjectMapper mapper = new ObjectMapper()
            .enable(SerializationFeature.INDENT_OUTPUT);

    static class User {
        public String name;
        public int age;
        public String email;
        public List<String> interests;
        public Map<String, Object> metadata;

        public User() {
            // Default constructor for Jackson
        }

        public User(String name, int age, String email, List<String> interests) {
            this.name = name;
            this.age = age;
            this.email = email;
            this.interests = interests;
            this.metadata = new HashMap<>();
            this.metadata.put("created_at", Instant.now().toString());
            this.metadata.put("last_login", Instant.now().toString());
            Map<String, Object> preferences = new HashMap<>();
            preferences.put("theme", "dark");
            preferences.put("notifications", true);
            this.metadata.put("preferences", preferences);
        }
    }

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

    public static void main(String[] args) {
        try {
            System.out.println("Flexon Basic Usage Example (Java)");
            System.out.println("=================================\n");

            // Create sample data
            User user = new User(
                "Alice Smith",
                28,
                "alice@example.com",
                Arrays.asList("programming", "AI", "data science")
            );

            // Save to JSON
            String jsonFile = "user.json";
            String flexonFile = "user.flexon";
            String encryptedFile = "user_encrypted.flexon";
            String decryptedFile = "user_decrypted.json";

            System.out.println("1. Creating JSON file...");
            mapper.writeValue(new File(jsonFile), user);

            // Basic serialization
            System.out.println("2. Converting to Flexon format...");
            FlexonHelper.runCommand("serialize -i " + jsonFile + " -o " + flexonFile);

            // Encryption
            System.out.println("3. Creating encrypted Flexon file...");
            FlexonHelper.runCommand("serialize -i " + jsonFile + " -o " + encryptedFile + " -e mysecretkey");

            // Decryption
            System.out.println("4. Decrypting Flexon file...");
            FlexonHelper.runCommand("deserialize -i " + encryptedFile + " -o " + decryptedFile + " -e mysecretkey");

            // Verify results
            System.out.println("\nResults:");
            System.out.println("Original JSON size: " + Files.size(Path.of(jsonFile)) + " bytes");
            System.out.println("Flexon size: " + Files.size(Path.of(flexonFile)) + " bytes");
            System.out.println("Encrypted size: " + Files.size(Path.of(encryptedFile)) + " bytes");

            // Compare contents
            User originalUser = mapper.readValue(new File(jsonFile), User.class);
            User decryptedUser = mapper.readValue(new File(decryptedFile), User.class);

            System.out.println("\nData verification:");
            System.out.println("Name matches: " + originalUser.name.equals(decryptedUser.name));
            System.out.println("Age matches: " + (originalUser.age == decryptedUser.age));
            System.out.println("Email matches: " + originalUser.email.equals(decryptedUser.email));
            System.out.println("Interests match: " + originalUser.interests.equals(decryptedUser.interests));

            // Test different encryption algorithms
            System.out.println("\n5. Testing different encryption algorithms...");
            
            // ChaCha20
            System.out.println("\nTesting ChaCha20...");
            FlexonHelper.runCommand("serialize -i " + jsonFile + " -o user_chacha20.flexon -e mysecretkey ChaCha20");
            
            // TripleDES
            System.out.println("Testing TripleDES...");
            FlexonHelper.runCommand("serialize -i " + jsonFile + " -o user_tripledes.flexon -e mysecretkey TripleDES");

            // Compare file sizes
            System.out.println("\nFile size comparison:");
            System.out.println("Original JSON: " + Files.size(Path.of(jsonFile)) + " bytes");
            System.out.println("AES-256: " + Files.size(Path.of(encryptedFile)) + " bytes");
            System.out.println("ChaCha20: " + Files.size(Path.of("user_chacha20.flexon")) + " bytes");
            System.out.println("TripleDES: " + Files.size(Path.of("user_tripledes.flexon")) + " bytes");

        } catch (IOException e) {
            System.err.println("Error: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
