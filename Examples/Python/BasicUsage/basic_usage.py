import json
import subprocess
import os
from datetime import datetime

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

def main():
    print("Flexon Basic Usage Example (Python)")
    print("===================================\n")

    # Create sample data
    data = {
        "user": {
            "name": "Alice Smith",
            "age": 28,
            "email": "alice@example.com",
            "interests": ["programming", "AI", "data science"],
            "metadata": {
                "created_at": datetime.utcnow().isoformat(),
                "last_login": datetime.utcnow().isoformat(),
                "preferences": {
                    "theme": "dark",
                    "notifications": True
                }
            }
        }
    }

    # Save to JSON
    json_file = "user.json"
    flexon_file = "user.flexon"
    encrypted_file = "user_encrypted.flexon"
    decrypted_file = "user_decrypted.json"

    print("1. Creating JSON file...")
    with open(json_file, 'w') as f:
        json.dump(data, f, indent=2)

    # Basic serialization
    print("2. Converting to Flexon format...")
    FlexonHelper.run_command(f"serialize -i {json_file} -o {flexon_file}")

    # Encryption
    print("3. Creating encrypted Flexon file...")
    FlexonHelper.run_command(f"serialize -i {json_file} -o {encrypted_file} -e mysecretkey")

    # Decryption
    print("4. Decrypting Flexon file...")
    FlexonHelper.run_command(f"deserialize -i {encrypted_file} -o {decrypted_file} -e mysecretkey")

    # Verify results
    print("\nResults:")
    print(f"Original JSON size: {os.path.getsize(json_file)} bytes")
    print(f"Flexon size: {os.path.getsize(flexon_file)} bytes")
    print(f"Encrypted size: {os.path.getsize(encrypted_file)} bytes")

    # Compare contents
    with open(json_file, 'r') as f:
        original = json.load(f)
    with open(decrypted_file, 'r') as f:
        decrypted = json.load(f)

    print("\nData verification:")
    print(f"Name matches: {original['user']['name'] == decrypted['user']['name']}")
    print(f"Age matches: {original['user']['age'] == decrypted['user']['age']}")
    print(f"Email matches: {original['user']['email'] == decrypted['user']['email']}")
    print(f"Interests match: {original['user']['interests'] == decrypted['user']['interests']}")

if __name__ == "__main__":
    main()
