# Changelog

All notable changes to the Flexon CLI project will be documented in this file.

## [1.2.0] - 2025-01-21

### Added
- Binary data support for direct handling of images and other binary files
- New compression options: Brotli compression alongside existing GZip and Deflate
- Enhanced schema validation with detailed error reporting
- Performance optimizations using SIMD instructions
- Buffer pooling for improved memory management
- Thread-safe operations for concurrent processing
- Comprehensive benchmark suite for performance testing

### Changed
- Improved serialization performance by up to 40%
- Reduced memory usage during large file processing
- Enhanced error messages for better debugging
- Updated documentation with new features and examples
- Reorganized code structure for better maintainability

### Fixed
- Memory leak in compression stream handling
- Race condition in concurrent file operations
- Schema validation error reporting accuracy
- File handling on case-sensitive file systems

## [1.1.0] - 2024-12-15

### Added
- Initial encryption support (AES-256, ChaCha20-Poly1305, Triple DES)
- Basic schema validation
- JSON compatibility layer
- Command-line interface
- Cross-platform support (Windows, Linux, macOS)

### Changed
- Initial public release
- Basic documentation and examples

[1.2.0]: https://github.com/LoSkroefie/flexon-cli/releases/tag/v1.2.0
[1.1.0]: https://github.com/LoSkroefie/flexon-cli/releases/tag/v1.1.0
