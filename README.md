# 🚀 FlexonCLI - The Next Generation Data Format

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Version](https://img.shields.io/badge/version-1.1.0-blue.svg)](https://github.com/LoSkroefie/flexon-cli-src)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download)

> **FlexonCLI** is a revolutionary binary data format and toolset that combines the simplicity of JSON with the power and efficiency of binary encoding. It's faster, smaller, and more capable than traditional JSON, while maintaining full compatibility with existing JSON workflows.

## 🌟 Key Features

- **📦 Ultra-Compact**: Up to 80% smaller than equivalent JSON files through smart binary encoding and built-in compression
- **⚡ Lightning Fast**: Binary format enables blazing-fast parsing and serialization
- **🔒 Type-Safe**: Native support for rich data types including DateTime, UUID, and binary data
- **✅ Schema Validation**: Built-in JSON Schema validation for data integrity
- **🔄 JSON Compatible**: Seamless conversion between JSON and Flexon formats
- **💪 Cross-Platform**: Works across all major platforms and programming languages
- **🛠️ Developer Friendly**: Comprehensive tooling and language bindings

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
# Convert JSON to Flexon
flexon-cli encode input.json output.flexon

# Convert Flexon back to JSON
flexon-cli decode input.flexon output.json

# Inspect Flexon file
flexon-cli inspect data.flexon

# Validate Flexon against schema
flexon-cli validate data.flexon schema.json
```

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
