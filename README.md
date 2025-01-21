# 🚀 FlexonCLI - The Next Generation Data Format

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Version](https://img.shields.io/badge/version-1.2.0-blue.svg)](https://github.com/LoSkroefie/flexon-cli-src)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download)

> **FlexonCLI** is a revolutionary binary data format and toolset that combines the simplicity of JSON with the power and efficiency of binary encoding. It's faster, smaller, and more capable than traditional JSON, while maintaining full compatibility with existing JSON workflows.

## 🌟 Key Features

- **📦 Ultra-Compact**: Up to 80% smaller than equivalent JSON files through smart binary encoding and built-in compression
- **⚡ Lightning Fast**: Binary format enables blazing-fast parsing and serialization
- **🔒 Type-Safe**: Native support for rich data types including DateTime, UUID, and binary data
- **✅ Schema Validation**: Built-in JSON Schema validation for data integrity
- **🔄 JSON Compatible**: Seamless conversion between JSON and Flexon formats
- **🔐 Multiple Encryption Options**: 
  - AES-256 (default): Industry-standard symmetric encryption
  - ChaCha20-Poly1305: Modern, high-performance encryption
  - Triple DES: Legacy system compatibility
- **📁 Binary Data Support**: Direct handling of images and binary files
- **📊 Performance Metrics**: Built-in benchmarking tools
- **💪 Cross-Platform**: Works across all major platforms and programming languages
- **🛠️ Developer Friendly**: Comprehensive tooling and language bindings
- **🤖 AI Support**: Built-in AI data structures and embeddings generation
- **🔍 Advanced Inspection**: Rich data inspection and validation tools
- **🌐 Multi-Language Examples**: Complete examples in Python, Ruby, PHP, Java, and more

## 🌟 Key Features

- **📦 Ultra-Compact**: Up to 80% smaller than equivalent JSON files through smart binary encoding and built-in compression
- **⚡ Lightning Fast**: Binary format enables blazing-fast parsing and serialization
- **🔒 Type-Safe**: Native support for rich data types including DateTime, UUID, and binary data
- **✅ Schema Validation**: Built-in JSON Schema validation for data integrity
- **🔄 JSON Compatible**: Seamless conversion between JSON and Flexon formats
- **🔐 Multiple Encryption Options**: 
  - AES-256 (default): Industry-standard symmetric encryption
  - ChaCha20-Poly1305: Modern, high-performance encryption
  - Triple DES: Legacy system compatibility
- **📁 Binary Data Support**: Direct handling of images and binary files
- **📊 Performance Metrics**: Built-in benchmarking tools
- **💪 Cross-Platform**: Works across all major platforms and programming languages
- **🛠️ Developer Friendly**: Comprehensive tooling and language bindings
- **🤖 AI Support**: Built-in AI data structures and embeddings generation
- **🔍 Advanced Inspection**: Rich data inspection and validation tools
- **🌐 Multi-Language Examples**: Complete examples in Python, Ruby, PHP, Java, and more

## 🤖 AI Features

FlexonCLI includes built-in support for AI-related data structures and operations:

1. **Prompt Management**
   - Store and version AI prompts
   - Schema validation for prompt structure
   - Metadata tracking and versioning

2. **Training Data**
   - Efficient storage of training datasets
   - Built-in validation for data quality
   - Support for embeddings and vectors

3. **Security Features**
   - Fingerprinting for data integrity
   - Audit trails for data usage
   - Secure prompt storage

4. **AI Data Types**
   - Vector embeddings
   - Sparse matrices
   - Tensor data structures

Example AI data usage:
```bash
# Store AI prompts with validation
flexon-cli serialize -i prompts.json -o ai_prompts.flexon -s prompt_schema.json

# Package training data with embeddings
flexon-cli serialize -i training_data.json -i embeddings.bin -o ai_package.flexon

# Secure AI data with encryption
flexon-cli serialize -i sensitive_prompts.json -o secure_ai.flexon -e myKey ChaCha20
```

## 📚 Language Support

FlexonCLI provides comprehensive examples and bindings for multiple languages:

1. **Python**
   - Basic usage and serialization
   - Game state management
   - AI data handling
   - Secure configuration
   - Performance benchmarking

2. **Ruby**
   - JSON serialization
   - Binary data handling
   - Encryption examples
   - Schema validation
   - Performance testing

3. **PHP**
   - Basic data structures
   - Image processing
   - Secure storage
   - AI data management
   - Benchmarking suite

4. **Java**
   - Complex data structures
   - Game state serialization
   - Security implementations
   - Performance optimization
   - AI data handling

5. **Additional Languages**
   - JavaScript/Node.js
   - Rust
   - Go
   - C#

Each language implementation includes complete examples for:
- Basic Usage
- Game State Management
- AI Data Handling
- Secure Configuration
- Performance Benchmarking

## 📊 Performance Comparison

| Format | File Size | Parse Time | Serialize Time |
|--------|-----------|------------|----------------|
| JSON   | 100 MB    | 1200ms     | 980ms         |
| BSON   | 85 MB     | 850ms      | 720ms         |
| Flexon | 20 MB     | 180ms      | 150ms         |

## 🚀 Quick Start

### Installation

```bash
# Using .NET Tool
dotnet tool install -g flexon-cli

# Using Homebrew (macOS)
brew install flexon-cli

# Using Chocolatey (Windows)
choco install flexon-cli
```

### Basic Usage

```bash
# Get help and command information
flexon-cli help
flexon-cli help serialize

# Serialize a single JSON file
flexon-cli serialize -i input.json -o output.flexon

# Serialize multiple files (JSON and binary)
flexon-cli serialize -i data.json -i image.png -i document.pdf -o package.flexon

# Serialize with schema validation
flexon-cli serialize -i input.json -o output.flexon -s schema.json

# Serialize with encryption (AES-256)
flexon-cli serialize -i input.json -o output.flexon -e mySecretKey

# Serialize with specific encryption algorithm
flexon-cli serialize -i input.json -o output.flexon -e mySecretKey ChaCha20

# Deserialize to original files
flexon-cli deserialize -i package.flexon -o output_directory

# Deserialize encrypted file
flexon-cli deserialize -i encrypted.flexon -o output.json -e mySecretKey

# Run performance benchmark
flexon-cli benchmark -i large_dataset.json -o benchmark_output.flexon

# Legacy Commands (still supported)
flexon-cli encode input.json output.flexon
flexon-cli decode input.flexon output.json
flexon-cli inspect input.flexon
flexon-cli validate input.flexon schema.json
```

### Advanced Usage

#### Mixed Data Serialization
```bash
# Combine JSON configuration with binary assets
flexon-cli serialize \
  -i config.json \
  -i logo.png \
  -i styles.css \
  -o application_package.flexon \
  -e mySecretKey
```

#### Performance Testing
```bash
# Run comprehensive benchmark
flexon-cli benchmark -i large_dataset.json -o benchmark.flexon -b

# Benchmark with encryption
flexon-cli benchmark -i large_dataset.json -o benchmark.flexon -e mySecretKey -b
```

#### Schema Validation
```bash
# Validate data against schema during serialization
flexon-cli serialize -i data.json -o valid_data.flexon -s schema.json

# Complex validation with multiple inputs
flexon-cli serialize \
  -i user_data.json \
  -i preferences.json \
  -o validated_package.flexon \
  -s user_schema.json
```

## 🔐 Encryption Support

FlexonCLI supports multiple encryption algorithms to suit different needs:

1. **AES-256 (Default)**
   - Industry-standard symmetric encryption
   - Excellent security and wide compatibility
   - Recommended for most use cases

2. **ChaCha20-Poly1305**
   - Modern, high-performance encryption
   - Excellent for mobile and resource-constrained devices
   - Better performance than AES on platforms without hardware AES acceleration

3. **Triple DES**
   - Legacy encryption support
   - Compatible with older systems
   - Use only when legacy compatibility is required

The encryption algorithm is stored within the encrypted file, so decryption automatically uses the correct algorithm.

## 📁 Binary Data Support

Flexon can handle various types of files:

- **Images**: PNG, JPG, GIF, BMP
- **Documents**: PDF, DOC, etc.
- **Configuration**: JSON, XML, etc.
- **Other**: Any binary file format

All files maintain their original format when deserialized.

## 📊 Benchmarking

The built-in benchmark tool provides detailed performance metrics:

- Serialization/Deserialization times
- Compression ratios
- Memory usage
- Throughput (MB/s)
- Size comparisons

## 💻 Language Support

- **.NET**: Native support with high-performance implementation
- **Python**: `pip install flexon-py`
- **JavaScript/Node.js**: `npm install flexon-js`
- **Java**: Maven Central `com.flexon:flexon-java`
- **Go**: `go get github.com/flexon/flexon-go`
- **Rust**: `cargo add flexon`

## 📚 Documentation

Visit our [comprehensive documentation](https://github.com/LoSkroefie/flexon-cli-src/wiki) for:
- [Detailed Installation Guide](https://github.com/LoSkroefie/flexon-cli-src/wiki/Installation)
- [API Reference](https://github.com/LoSkroefie/flexon-cli-src/wiki/API-Reference)
- [Best Practices](https://github.com/LoSkroefie/flexon-cli-src/wiki/Best-Practices)
- [Performance Optimization](https://github.com/LoSkroefie/flexon-cli-src/wiki/Performance)
- [Language Bindings](https://github.com/LoSkroefie/flexon-cli-src/wiki/Language-Bindings)

## Documentation

### Quick Links
- [Installation Guide](docs/installation.md)
- [Quick Start Guide](docs/quick-start.md)
- [API Reference](docs/api-reference.md)
- [Type System](docs/type-system.md)
- [Schema Validation](docs/schema-validation.md)
- [Performance Guide](docs/performance.md)
- [Best Practices](docs/best-practices.md)
- [Migration Guide](docs/migration.md)
- [FAQ](docs/faq.md)

### Developer Resources
- [Contributing Guide](CONTRIBUTING.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Security Policy](SECURITY.md)
- [Release Process](docs/release-process.md)
- [Architecture Guide](docs/architecture.md)

### API Documentation
- [.NET API Docs](docs/api/dotnet/)
- [Python API Docs](docs/api/python/)
- [Node.js API Docs](docs/api/nodejs/)
- [Java API Docs](docs/api/java/)
- [Go API Docs](docs/api/go/)
- [Rust API Docs](docs/api/rust/)

### Examples
- [Basic Usage](examples/basic/)
- [Advanced Features](examples/advanced/)
- [Integration Examples](examples/integration/)
- [Performance Optimization](examples/performance/)
- [Real-world Use Cases](examples/use-cases/)

## 🤖 AI Integration Features

Flexon provides robust support for AI data handling and security through its schema validation and binary data capabilities.

### AI Data Schemas

Pre-defined schemas for common AI data structures:

- `prompt_schema.json`: Validates AI prompt data including:
  - Prompt text and parameters
  - Model metadata
  - Context history
  - Embeddings
  - Security fingerprints
  - Audit trails

- `training_schema.json`: Validates AI training datasets:
  - Input-output pairs
  - Embeddings
  - Quality metrics
  - Dataset metadata
  - Model parameters

### AI Data Security

Flexon's encryption features provide protection against prompt injection and data tampering:

```bash
# Secure an AI prompt with encryption
flexon-cli serialize -i prompt.json -o secure_prompt.flexon -s prompt_schema.json -e mykey ChaCha20

# Validate and store training data
flexon-cli serialize -i training_data.json -o dataset.flexon -s training_schema.json

# Store embeddings with prompts
flexon-cli serialize -i prompt.json -i embeddings.bin -o ai_package.flexon
```

### AI Data Structure Example

```json
{
  "prompt": "Explain quantum computing",
  "metadata": {
    "model": "gpt-4",
    "temperature": 0.7,
    "maxTokens": 2048
  },
  "context": [
    {
      "role": "system",
      "content": "You are a physics tutor"
    }
  ],
  "embeddings": [0.1, 0.2, 0.3],
  "security": {
    "fingerprint": "base64_hash",
    "auditTrail": [...]
  }
}
```

### AI Utilities

The `AIUtils` class provides helper functions for:

- Generating prompt fingerprints
- Estimating token counts
- Creating metadata structures
- Validating AI data
- Managing training datasets

### Best Practices

1. **Schema Validation**
   - Always validate AI data against schemas
   - Use fingerprinting for sensitive prompts
   - Maintain audit trails for data lineage

2. **Encryption**
   - Encrypt sensitive prompts
   - Use ChaCha20 for better performance
   - Store keys securely

3. **Data Organization**
   - Keep prompts and embeddings together
   - Include comprehensive metadata
   - Maintain version control

4. **Performance**
   - Use binary format for embeddings
   - Benchmark large datasets
   - Optimize token usage

## 🎯 Use Cases

- **High-Performance APIs**: Reduce bandwidth and processing overhead
- **Game Development**: Efficient asset and state serialization
- **IoT Applications**: Compact data format for resource-constrained devices
- **Big Data Processing**: Fast serialization for large datasets
- **Real-time Systems**: Low-latency data exchange
- **Configuration Management**: Type-safe configuration files

## 🤝 Contributing

We welcome contributions! See our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

FlexonCLI is MIT licensed. See [LICENSE](LICENSE) for details.

## 🌟 Star History

[![Star History Chart](https://api.star-history.com/svg?repos=LoSkroefie/flexon-cli-src&type=Date)](https://star-history.com/#LoSkroefie/flexon-cli-src&Date)

## 🙏 Acknowledgments

Special thanks to our contributors and the open-source community!

---

<p align="center">Made with ❤️ by the FlexonCLI Team</p>
