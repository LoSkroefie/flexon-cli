# Flexon PHP Converter and Tutorial

This directory contains a complete PHP implementation for converting various file formats to and from Flexon format. The implementation includes all necessary features like validation, inspection, and encryption.

## Features

- Convert between Flexon and various formats:
  - JSON
  - BSON
  - XML
  - CSV/XSV
  - Flexson
- File upload functionality
- Validation and inspection tools
- Encryption support
- Standalone operation

## Requirements

- PHP 7.4 or higher
- Apache/WAMP Server
- Required PHP extensions:
  - json
  - xml
  - mongodb (for BSON support)
  - mbstring
  - openssl

## Installation

1. Copy the entire directory to your WAMP server's www folder
2. Ensure all required PHP extensions are enabled
3. Set appropriate permissions for the upload directory
4. Access through your web browser: `http://localhost/flexon-php/`

## Directory Structure

```
flexon-php/
├── index.php           # Main interface
├── converter.php       # Core conversion logic
├── upload.php         # File upload handler
├── includes/
│   ├── config.php     # Configuration
│   ├── functions.php  # Utility functions
│   └── types/         # Type handlers
├── assets/            # CSS/JS files
└── uploads/           # Upload directory
```

## Usage

1. Access the web interface through your browser
2. Upload a file or paste content directly
3. Select source and target formats
4. Configure additional options (validation, encryption, etc.)
5. Convert and download the result

## Examples

See the `examples` directory for sample code and use cases.
