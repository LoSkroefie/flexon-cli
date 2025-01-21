const fs = require('fs').promises;
const { execSync } = require('child_process');
const path = require('path');
const crypto = require('crypto');

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

function createEmbeddings(text, dim = 384) {
    // Simulate embeddings creation (in real use, you'd use a proper embedding model)
    const hash = crypto.createHash('sha256').update(text).digest('hex');
    const seed = parseInt(hash.slice(0, 8), 16);
    
    // Use the seed to generate deterministic but seemingly random embeddings
    const embeddings = [];
    for (let i = 0; i < dim; i++) {
        const angle = (seed + i) * 0.1;
        embeddings.push(Math.sin(angle));
    }
    return embeddings;
}

function createAIPrompt(promptText, model = "gpt-4", context = null, embeddings = null) {
    return {
        prompt: promptText,
        metadata: {
            model: model,
            created_at: new Date().toISOString(),
            version: "1.0",
            language: "en",
            domain: "programming",
            security_level: "medium"
        },
        context: context || [],
        embeddings: embeddings || createEmbeddings(promptText),
        security: {
            fingerprint: crypto.createHash('sha256')
                .update(promptText + new Date().toISOString())
                .digest('hex'),
            encrypted: true,
            access_level: "user",
            audit_trail: [
                {
                    timestamp: new Date().toISOString(),
                    action: "created",
                    user: "system"
                }
            ]
        }
    };
}

function createTrainingDataset(pairs, name, description) {
    return {
        name: name,
        description: description,
        metadata: {
            created_at: new Date().toISOString(),
            version: "1.0",
            size: pairs.length,
            language: "en",
            domain: "programming"
        },
        data: pairs.map(([input, output]) => ({
            input: input,
            output: output,
            embeddings: createEmbeddings(input),
            metrics: {
                input_tokens: input.split(/\s+/).length,
                output_tokens: output.split(/\s+/).length,
                quality_score: 0.95,  // Simulated quality score
                diversity_score: 0.85  // Simulated diversity score
            }
        }))
    };
}

async function main() {
    console.log("Flexon AI Data Example (JavaScript)");
    console.log("==================================\n");

    // Create AI prompt with context
    const context = [
        {
            role: "system",
            content: "You are a helpful AI assistant specializing in programming."
        },
        {
            role: "user",
            content: "How do I implement a binary search tree in JavaScript?"
        }
    ];

    // Generate sample embeddings
    const embeddings = Array.from({ length: 10 }, (_, i) => Math.sin(i * 0.1));

    // Create AI prompt
    const prompt = createAIPrompt(
        "Explain binary search tree implementation with code examples.",
        "gpt-4",
        context,
        embeddings
    );

    // Save prompt to JSON
    const promptFile = "prompt.json";
    const flexonFile = "ai_data.flexon";
    const schemaFile = "../../../FlexonCLI/AI/Schemas/prompt_schema.json";

    console.log("1. Saving AI prompt...");
    await fs.writeFile(promptFile, JSON.stringify(prompt, null, 2));

    // Create training data
    const trainingPairs = [
        ["What is a binary search tree?", 
         "A binary search tree is a data structure where each node has at most two children..."],
        ["How to insert into BST?", 
         "To insert into a BST, compare the value with the root and recursively insert into left or right subtree..."],
        ["BST search algorithm?", 
         "To search in a BST, start at root, compare value, and traverse left or right based on comparison..."]
    ];

    const trainingData = createTrainingDataset(
        trainingPairs,
        "bst-tutor-v1",
        "Binary Search Tree tutorial dataset"
    );

    const trainingFile = "training.json";
    const trainingSchemaFile = "../../../FlexonCLI/AI/Schemas/training_schema.json";
    const trainingFlexonFile = "training.flexon";

    console.log("2. Saving training data...");
    await fs.writeFile(trainingFile, JSON.stringify(trainingData, null, 2));

    // Validate and serialize prompt
    console.log("3. Validating and serializing prompt...");
    FlexonHelper.runCommand(`serialize -i ${promptFile} -o ${flexonFile} -s ${schemaFile} -e aikey ChaCha20`);

    // Validate and serialize training data
    console.log("4. Validating and serializing training data...");
    FlexonHelper.runCommand(`serialize -i ${trainingFile} -o ${trainingFlexonFile} -s ${trainingSchemaFile}`);

    // Load and verify
    console.log("5. Loading AI data...");
    await fs.mkdir("ai_output", { recursive: true });
    FlexonHelper.runCommand(`deserialize -i ${flexonFile} -o ai_output/prompt_decoded.json -e aikey`);

    const loadedPrompt = JSON.parse(
        await fs.readFile("ai_output/prompt_decoded.json", 'utf8')
    );

    console.log("\nVerification:");
    console.log(`Prompt loaded: ${'prompt' in loadedPrompt}`);
    console.log(`Metadata present: ${'metadata' in loadedPrompt}`);
    console.log(`Context count: ${loadedPrompt.context.length}`);
    console.log(`Embeddings size: ${loadedPrompt.embeddings.length}`);

    // Test different encryption methods
    console.log("\n6. Testing different encryption methods...");
    FlexonHelper.runCommand(`serialize -i ${promptFile} -o ai_aes.flexon -e aikey AES256`);
    FlexonHelper.runCommand(`serialize -i ${promptFile} -o ai_tripledes.flexon -e aikey TripleDES`);

    const stats = await Promise.all([
        fs.stat(promptFile),
        fs.stat(flexonFile),
        fs.stat('ai_aes.flexon'),
        fs.stat('ai_tripledes.flexon')
    ]);

    console.log("\nFile size comparison:");
    console.log(`Original JSON: ${stats[0].size} bytes`);
    console.log(`ChaCha20: ${stats[1].size} bytes`);
    console.log(`AES-256: ${stats[2].size} bytes`);
    console.log(`TripleDES: ${stats[3].size} bytes`);
}

main().catch(console.error);
