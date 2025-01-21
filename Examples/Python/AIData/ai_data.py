import json
import subprocess
import os
from datetime import datetime
import numpy as np

class FlexonHelper:
    @staticmethod
    def run_command(args):
        try:
            result = subprocess.run(['flexon-cli'] + args.split(), 
                                  capture_output=True, text=True, check=True)
            return result.stdout
        except subprocess.CalledProcessError as e:
            print(f"Error running Flexon command: {e.stderr}")
            raise

def create_embeddings(text, dim=384):
    """Simulate embeddings creation (in real use, you'd use a proper embedding model)"""
    # Use hash of text to create deterministic but seemingly random embeddings
    np.random.seed(hash(text) % 2**32)
    return np.random.normal(0, 1, dim).tolist()

def create_ai_prompt(prompt_text, model="gpt-4", context=None, embeddings=None):
    """Create an AI prompt with metadata and security features"""
    return {
        "prompt": prompt_text,
        "metadata": {
            "model": model,
            "created_at": datetime.utcnow().isoformat(),
            "version": "1.0",
            "language": "en",
            "domain": "programming",
            "security_level": "medium"
        },
        "context": context or [],
        "embeddings": embeddings or create_embeddings(prompt_text),
        "security": {
            "fingerprint": hash(prompt_text + datetime.utcnow().isoformat()),
            "encrypted": True,
            "access_level": "user",
            "audit_trail": [
                {
                    "timestamp": datetime.utcnow().isoformat(),
                    "action": "created",
                    "user": "system"
                }
            ]
        }
    }

def create_training_dataset(pairs, name, description):
    """Create a training dataset with metadata and quality metrics"""
    dataset = {
        "name": name,
        "description": description,
        "metadata": {
            "created_at": datetime.utcnow().isoformat(),
            "version": "1.0",
            "size": len(pairs),
            "language": "en",
            "domain": "programming"
        },
        "data": []
    }

    for input_text, output_text in pairs:
        entry = {
            "input": input_text,
            "output": output_text,
            "embeddings": create_embeddings(input_text),
            "metrics": {
                "input_tokens": len(input_text.split()),
                "output_tokens": len(output_text.split()),
                "quality_score": 0.95,  # Simulated quality score
                "diversity_score": 0.85  # Simulated diversity score
            }
        }
        dataset["data"].append(entry)

    return dataset

def main():
    print("Flexon AI Data Example (Python)")
    print("===============================\n")

    # Create AI prompt with context
    context = [
        {
            "role": "system",
            "content": "You are a helpful AI assistant specializing in programming."
        },
        {
            "role": "user",
            "content": "How do I implement a binary search tree in Python?"
        }
    ]

    # Generate sample embeddings
    embeddings = [np.sin(i * 0.1) for i in range(10)]

    # Create AI prompt
    prompt = create_ai_prompt(
        "Explain binary search tree implementation with code examples.",
        "gpt-4",
        context,
        embeddings
    )

    # Save prompt to JSON
    prompt_file = "prompt.json"
    flexon_file = "ai_data.flexon"
    schema_file = "../../../FlexonCLI/AI/Schemas/prompt_schema.json"

    print("1. Saving AI prompt...")
    with open(prompt_file, 'w') as f:
        json.dump(prompt, f, indent=2)

    # Create training data
    training_pairs = [
        ("What is a binary search tree?", 
         "A binary search tree is a data structure where each node has at most two children..."),
        ("How to insert into BST?", 
         "To insert into a BST, compare the value with the root and recursively insert into left or right subtree..."),
        ("BST search algorithm?", 
         "To search in a BST, start at root, compare value, and traverse left or right based on comparison...")
    ]

    training_data = create_training_dataset(
        training_pairs,
        "bst-tutor-v1",
        "Binary Search Tree tutorial dataset"
    )

    training_file = "training.json"
    training_schema_file = "../../../FlexonCLI/AI/Schemas/training_schema.json"
    training_flexon_file = "training.flexon"

    print("2. Saving training data...")
    with open(training_file, 'w') as f:
        json.dump(training_data, f, indent=2)

    # Validate and serialize prompt
    print("3. Validating and serializing prompt...")
    FlexonHelper.run_command(f"serialize -i {prompt_file} -o {flexon_file} -s {schema_file} -e aikey ChaCha20")

    # Validate and serialize training data
    print("4. Validating and serializing training data...")
    FlexonHelper.run_command(f"serialize -i {training_file} -o {training_flexon_file} -s {training_schema_file}")

    # Load and verify
    print("5. Loading AI data...")
    os.makedirs("ai_output", exist_ok=True)
    FlexonHelper.run_command(f"deserialize -i {flexon_file} -o ai_output/prompt_decoded.json -e aikey")

    with open("ai_output/prompt_decoded.json", 'r') as f:
        loaded_prompt = json.load(f)

    print("\nVerification:")
    print(f"Prompt loaded: {'prompt' in loaded_prompt}")
    print(f"Metadata present: {'metadata' in loaded_prompt}")
    print(f"Context count: {len(loaded_prompt['context'])}")
    print(f"Embeddings size: {len(loaded_prompt['embeddings'])}")

    # Test different encryption methods
    print("\n6. Testing different encryption methods...")
    FlexonHelper.run_command(f"serialize -i {prompt_file} -o ai_aes.flexon -e aikey AES256")
    FlexonHelper.run_command(f"serialize -i {prompt_file} -o ai_tripledes.flexon -e aikey TripleDES")

    print("\nFile size comparison:")
    print(f"Original JSON: {os.path.getsize(prompt_file)} bytes")
    print(f"ChaCha20: {os.path.getsize(flexon_file)} bytes")
    print(f"AES-256: {os.path.getsize('ai_aes.flexon')} bytes")
    print(f"TripleDES: {os.path.getsize('ai_tripledes.flexon')} bytes")

if __name__ == "__main__":
    main()
