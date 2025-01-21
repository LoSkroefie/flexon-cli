# FLEXON API Reference

## Core Classes

### FlexonSerializer

The main class for serialization and deserialization operations.

```csharp
public class FlexonSerializer
{
    // Constructor with default options
    public FlexonSerializer();
    
    // Constructor with custom options
    public FlexonSerializer(FlexonOptions options);
    
    // Serialize object to binary
    public byte[] Serialize<T>(T value);
    
    // Serialize object to stream
    public Task SerializeAsync<T>(T value, Stream stream);
    
    // Deserialize from binary
    public T Deserialize<T>(byte[] data);
    
    // Deserialize from stream
    public Task<T> DeserializeAsync<T>(Stream stream);
}
```

#### Example Usage
```csharp
var serializer = new FlexonSerializer();

// Serialize
var data = new MyData { Name = "Test" };
byte[] binary = serializer.Serialize(data);

// Deserialize
var restored = serializer.Deserialize<MyData>(binary);
```

### FlexonOptions

Configuration options for serialization behavior.

```csharp
public class FlexonOptions
{
    // Compression settings
    public bool EnableCompression { get; set; }
    public CompressionLevel CompressionLevel { get; set; }
    
    // Validation settings
    public bool EnableValidation { get; set; }
    public string SchemaPath { get; set; }
    
    // Performance settings
    public bool UsePooledBuffers { get; set; }
    public int BufferSize { get; set; }
    
    // Type handling
    public TypeHandling TypeHandling { get; set; }
    public ITypeResolver TypeResolver { get; set; }
}
```

### FlexonStream

Low-level stream operations for custom implementations.

```csharp
public class FlexonStream : Stream
{
    // Write primitive types
    public void WriteInt32(int value);
    public void WriteInt64(long value);
    public void WriteString(string value);
    public void WriteDateTime(DateTime value);
    public void WriteUuid(Guid value);
    
    // Read primitive types
    public int ReadInt32();
    public long ReadInt64();
    public string ReadString();
    public DateTime ReadDateTime();
    public Guid ReadUuid();
}
```

## Schema Validation

### FlexonSchema

Schema definition and validation.

```csharp
public class FlexonSchema
{
    // Load schema from file
    public static FlexonSchema FromFile(string path);
    
    // Load schema from string
    public static FlexonSchema FromString(string json);
    
    // Validate data against schema
    public ValidationResult Validate<T>(T data);
    
    // Compile schema for better performance
    public static CompiledSchema Compile(string schema);
}
```

### ValidationResult

Result of schema validation.

```csharp
public class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<ValidationError> Errors { get; }
    public string ErrorMessage { get; }
}
```

## Type Extensions

### IFlexonType

Interface for custom type serialization.

```csharp
public interface IFlexonType
{
    byte TypeCode { get; }
    void Serialize(FlexonWriter writer);
    void Deserialize(FlexonReader reader);
}
```

### FlexonTypeAttribute

Attribute for type customization.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class FlexonTypeAttribute : Attribute
{
    public byte TypeCode { get; }
    public string Schema { get; set; }
    public bool IsVersioned { get; set; }
}
```

## Advanced Features

### Streaming Support

```csharp
public class FlexonStreamReader : IDisposable
{
    public FlexonStreamReader(Stream stream);
    public bool HasMore { get; }
    public T ReadNext<T>();
    public Task<T> ReadNextAsync<T>();
}

public class FlexonStreamWriter : IDisposable
{
    public FlexonStreamWriter(Stream stream);
    public void Write<T>(T value);
    public Task WriteAsync<T>(T value);
    public void Flush();
}
```

### Compression

```csharp
public class FlexonCompression
{
    // Compress data
    public static byte[] Compress(byte[] data, CompressionLevel level);
    
    // Decompress data
    public static byte[] Decompress(byte[] data);
    
    // Streaming compression
    public static Stream CreateCompressStream(Stream stream, CompressionLevel level);
    public static Stream CreateDecompressStream(Stream stream);
}
```

### Performance Optimization

```csharp
public class FlexonPool
{
    // Buffer pooling
    public static ArrayPool<byte> BufferPool { get; }
    
    // Object pooling
    public static ObjectPool<T> CreatePool<T>() where T : class, new();
    
    // Writer/Reader pooling
    public static FlexonWriter RentWriter();
    public static void ReturnWriter(FlexonWriter writer);
}
```

## Error Handling

### FlexonException

Base exception for all FLEXON errors.

```csharp
public class FlexonException : Exception
{
    public FlexonErrorCode ErrorCode { get; }
    public string DetailedMessage { get; }
}
```

### Common Error Codes

```csharp
public enum FlexonErrorCode
{
    InvalidData = 1,
    ValidationFailed = 2,
    CompressionError = 3,
    TypeMismatch = 4,
    BufferOverflow = 5,
    SchemaError = 6
}
```

## Integration

### ASP.NET Core Integration

```csharp
public static class FlexonMvcExtensions
{
    // Add FLEXON formatters
    public static IMvcBuilder AddFlexonFormatters(this IMvcBuilder builder);
    
    // Configure options
    public static IMvcBuilder AddFlexon(this IMvcBuilder builder, Action<FlexonOptions> configure);
}
```

### Entity Framework Integration

```csharp
public static class FlexonEfExtensions
{
    // Configure value conversion
    public static PropertyBuilder<T> HasFlexonConversion<T>(this PropertyBuilder<T> builder);
    
    // Configure complex type storage
    public static PropertyBuilder<T> HasFlexonStorage<T>(this PropertyBuilder<T> builder);
}
```

## Best Practices

### Performance
- Use compiled schemas for validation
- Enable buffer pooling for large datasets
- Use streaming for large files
- Implement proper disposal patterns

### Memory Management
- Dispose of streams properly
- Use pooled buffers when appropriate
- Clear sensitive data
- Monitor memory usage

### Thread Safety
- Use separate instances per thread
- Pool thread-safe objects
- Implement proper synchronization
- Handle concurrent access

### Security
- Validate all input
- Handle sensitive data properly
- Implement proper access control
- Use secure configuration

## Examples

### Basic Serialization
```csharp
var data = new MyData { Id = 1, Name = "Test" };
var serializer = new FlexonSerializer();

// Serialize
byte[] binary = serializer.Serialize(data);

// Deserialize
var restored = serializer.Deserialize<MyData>(binary);
```

### Streaming Large Files
```csharp
using var file = File.OpenRead("large.flexon");
using var reader = new FlexonStreamReader(file);

while (reader.HasMore)
{
    var item = reader.ReadNext<DataItem>();
    ProcessItem(item);
}
```

### Custom Type Implementation
```csharp
[FlexonType(TypeCode = 0x0A)]
public class GeoPoint : IFlexonType
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public void Serialize(FlexonWriter writer)
    {
        writer.WriteDouble(Latitude);
        writer.WriteDouble(Longitude);
    }

    public void Deserialize(FlexonReader reader)
    {
        Latitude = reader.ReadDouble();
        Longitude = reader.ReadDouble();
    }
}
```
