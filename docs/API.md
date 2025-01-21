# FlexonCLI API Reference

## Core API

### Encoding

```csharp
// C#
using FlexonCLI;

// Encode object to Flexon
byte[] flexonData = FlexonBinary.Encode(myObject);

// With compression
using var stream = new MemoryStream();
using var gzip = new GZipStream(stream, CompressionMode.Compress);
FlexonBinary.Encode(myObject, gzip);
```

```python
# Python
from flexon import FlexonEncoder

# Encode object to Flexon
flexon_data = FlexonEncoder.encode(my_object)

# With compression
with open('data.flexon', 'wb') as f:
    FlexonEncoder.encode_compressed(my_object, f)
```

```javascript
// JavaScript/TypeScript
import { FlexonEncoder } from 'flexon-js';

// Encode object to Flexon
const flexonData = FlexonEncoder.encode(myObject);

// With compression
const compressedData = FlexonEncoder.encodeCompressed(myObject);
```

### Decoding

```csharp
// C#
using FlexonCLI;

// Decode Flexon to object
var obj = FlexonBinary.Decode(flexonData);

// With compression
using var stream = new MemoryStream(compressedData);
using var gzip = new GZipStream(stream, CompressionMode.Decompress);
var obj = FlexonBinary.Decode(gzip);
```

```python
# Python
from flexon import FlexonDecoder

# Decode Flexon to object
obj = FlexonDecoder.decode(flexon_data)

# With compression
with open('data.flexon', 'rb') as f:
    obj = FlexonDecoder.decode_compressed(f)
```

```javascript
// JavaScript/TypeScript
import { FlexonDecoder } from 'flexon-js';

// Decode Flexon to object
const obj = FlexonDecoder.decode(flexonData);

// With compression
const obj = FlexonDecoder.decodeCompressed(compressedData);
```

## Type Support

### Supported Types

| Type | Binary Format | Description |
|------|--------------|-------------|
| Null | 0x00 | Null value |
| Boolean | 0x01/0x02 | True (0x01) or False (0x02) |
| Integer | 0x03 + 4 bytes | 32-bit integer |
| Float | 0x04 + 8 bytes | 64-bit double |
| String | 0x05 + length + bytes | UTF-8 encoded string |
| Binary | 0x06 + length + bytes | Raw binary data |
| DateTime | 0x07 + 8 bytes | Binary DateTime |
| UUID | 0x08 + 16 bytes | GUID/UUID |
| List | 0x09 + items + 0x00 | Array of items |
| Object | 0x0A + pairs + 0x00 | Key-value pairs |

### Custom Type Registration

```csharp
// C#
public class CustomTypeHandler : IFlexonTypeHandler
{
    public byte TypeCode => 0x0B; // Custom type code
    
    public void Encode(object value, BinaryWriter writer)
    {
        // Custom encoding logic
    }
    
    public object Decode(BinaryReader reader)
    {
        // Custom decoding logic
    }
}

FlexonBinary.RegisterTypeHandler(new CustomTypeHandler());
```

## Schema Validation

### Schema Format

```json
{
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "minLength": 1,
      "maxLength": 100
    },
    "age": {
      "type": "integer",
      "minimum": 0,
      "maximum": 150
    },
    "emails": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "email"
      }
    }
  },
  "required": ["name", "age"]
}
```

### Validation API

```csharp
// C#
var errors = new List<string>();
bool isValid = FlexonBinary.Validate(data, schema, errors: errors);

if (!isValid)
{
    foreach (var error in errors)
    {
        Console.WriteLine(error);
    }
}
```

## Performance Optimization

### Streaming API

```csharp
// C#
using var stream = File.OpenRead("large.flexon");
using var reader = new FlexonStreamReader(stream);

while (reader.HasMore)
{
    var item = reader.ReadNext();
    ProcessItem(item);
}
```

### Batch Processing

```csharp
// C#
using var writer = new FlexonBatchWriter("output.flexon");
foreach (var item in largeDataset)
{
    writer.Write(item);
    if (writer.BatchSize >= 1000)
    {
        writer.Flush();
    }
}
writer.Complete();
```

## Error Handling

```csharp
try
{
    var data = FlexonBinary.Decode(invalidData);
}
catch (FlexonFormatException ex)
{
    // Handle invalid format
    Console.WriteLine($"Invalid format: {ex.Message}");
}
catch (FlexonTypeException ex)
{
    // Handle type mismatch
    Console.WriteLine($"Type mismatch: {ex.Message}");
}
catch (FlexonCompressionException ex)
{
    // Handle compression errors
    Console.WriteLine($"Compression error: {ex.Message}");
}
```

## CLI Commands

### Basic Commands

```bash
# Encode JSON to Flexon
flexon-cli encode input.json output.flexon

# Decode Flexon to JSON
flexon-cli decode input.flexon output.json

# Inspect Flexon file
flexon-cli inspect data.flexon

# Validate against schema
flexon-cli validate data.flexon schema.json
```

### Advanced Options

```bash
# Encode with custom compression level
flexon-cli encode --compression-level 9 input.json output.flexon

# Decode with pretty print
flexon-cli decode --pretty input.flexon output.json

# Inspect with specific fields
flexon-cli inspect --fields "name,age,email" data.flexon

# Validate with custom error format
flexon-cli validate --error-format json data.flexon schema.json
```

## Best Practices

1. **Use Compression Wisely**
   - Enable compression for storage/transmission
   - Disable for frequent read/write operations

2. **Batch Processing**
   - Use batch operations for large datasets
   - Implement proper error handling

3. **Schema Validation**
   - Always validate critical data
   - Cache compiled schemas

4. **Error Handling**
   - Implement proper exception handling
   - Log validation errors appropriately

5. **Performance Optimization**
   - Use streaming API for large files
   - Implement proper disposal of resources
