const fs = require('fs').promises;
const { execSync } = require('child_process');
const path = require('path');

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

async function main() {
    console.log("Flexon Basic Usage Example (JavaScript)");
    console.log("======================================\n");

    // Create sample data
    const data = {
        user: {
            name: "Alice Smith",
            age: 28,
            email: "alice@example.com",
            interests: ["programming", "AI", "data science"],
            metadata: {
                created_at: new Date().toISOString(),
                last_login: new Date().toISOString(),
                preferences: {
                    theme: "dark",
                    notifications: true
                }
            }
        }
    };

    // Save to JSON
    const jsonFile = "user.json";
    const flexonFile = "user.flexon";
    const encryptedFile = "user_encrypted.flexon";
    const decryptedFile = "user_decrypted.json";

    console.log("1. Creating JSON file...");
    await fs.writeFile(jsonFile, JSON.stringify(data, null, 2));

    // Basic serialization
    console.log("2. Converting to Flexon format...");
    FlexonHelper.runCommand(`serialize -i ${jsonFile} -o ${flexonFile}`);

    // Encryption
    console.log("3. Creating encrypted Flexon file...");
    FlexonHelper.runCommand(`serialize -i ${jsonFile} -o ${encryptedFile} -e mysecretkey`);

    // Decryption
    console.log("4. Decrypting Flexon file...");
    FlexonHelper.runCommand(`deserialize -i ${encryptedFile} -o ${decryptedFile} -e mysecretkey`);

    // Verify results
    console.log("\nResults:");
    const stats = await Promise.all([
        fs.stat(jsonFile),
        fs.stat(flexonFile),
        fs.stat(encryptedFile)
    ]);

    console.log(`Original JSON size: ${stats[0].size} bytes`);
    console.log(`Flexon size: ${stats[1].size} bytes`);
    console.log(`Encrypted size: ${stats[2].size} bytes`);

    // Compare contents
    const [original, decrypted] = await Promise.all([
        fs.readFile(jsonFile, 'utf8'),
        fs.readFile(decryptedFile, 'utf8')
    ]);

    const originalObj = JSON.parse(original);
    const decryptedObj = JSON.parse(decrypted);

    console.log("\nData verification:");
    console.log(`Name matches: ${originalObj.user.name === decryptedObj.user.name}`);
    console.log(`Age matches: ${originalObj.user.age === decryptedObj.user.age}`);
    console.log(`Email matches: ${originalObj.user.email === decryptedObj.user.email}`);
    console.log(`Interests match: ${JSON.stringify(originalObj.user.interests) === JSON.stringify(decryptedObj.user.interests)}`);

    // Test different encryption algorithms
    console.log("\n5. Testing different encryption algorithms...");
    
    // ChaCha20
    console.log("\nTesting ChaCha20...");
    FlexonHelper.runCommand(`serialize -i ${jsonFile} -o user_chacha20.flexon -e mysecretkey ChaCha20`);
    
    // TripleDES
    console.log("Testing TripleDES...");
    FlexonHelper.runCommand(`serialize -i ${jsonFile} -o user_tripledes.flexon -e mysecretkey TripleDES`);

    // Compare file sizes
    const encryptedStats = await Promise.all([
        fs.stat('user_chacha20.flexon'),
        fs.stat('user_tripledes.flexon')
    ]);

    console.log("\nFile size comparison:");
    console.log(`Original JSON: ${stats[0].size} bytes`);
    console.log(`AES-256: ${stats[2].size} bytes`);
    console.log(`ChaCha20: ${encryptedStats[0].size} bytes`);
    console.log(`TripleDES: ${encryptedStats[1].size} bytes`);
}

main().catch(console.error);
