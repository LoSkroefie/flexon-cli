# FlexonCLI Language Bindings

## Overview

FlexonCLI provides native bindings for multiple programming languages, ensuring seamless integration across different platforms and tech stacks.

## Language Support Matrix

| Language | Package Name | Version | Platform Support | Status |
|----------|--------------|---------|------------------|---------|
| .NET     | FlexonCLI   | 1.1.0   | All             | Stable  |
| Python   | flexon-py   | 1.0.0   | All             | Stable  |
| Node.js  | flexon-js   | 1.0.0   | All             | Stable  |
| Java     | flexon-java | 1.0.0   | All             | Beta    |
| Go       | flexon-go   | 1.0.0   | All             | Beta    |
| Rust     | flexon      | 1.0.0   | All             | Beta    |

## .NET Implementation

```csharp
using FlexonCLI;

// Basic usage
var data = new { Name = "John", Age = 30 };
byte[] flexonData = FlexonBinary.Encode(data);

// With type handling
var person = FlexonBinary.Decode<Person>(flexonData);

// Async operations
await using var stream = File.OpenWrite("data.flexon");
await FlexonBinary.EncodeAsync(data, stream);
```

## Python Implementation

```python
from flexon import Flexon

# Basic usage
data = {"name": "John", "age": 30}
flexon_data = Flexon.encode(data)

# With type handling
person = Flexon.decode(flexon_data, Person)

# Async operations
async with aiofiles.open('data.flexon', 'wb') as f:
    await Flexon.encode_async(data, f)
```

## Node.js Implementation

```javascript
const { Flexon } = require('flexon-js');

// Basic usage
const data = { name: "John", age: 30 };
const flexonData = Flexon.encode(data);

// With type handling
const person = Flexon.decode(flexonData, Person);

// Promise-based operations
await Flexon.encodeToFile(data, 'data.flexon');
```

## Java Implementation

```java
import com.flexon.FlexonBinary;

// Basic usage
Map<String, Object> data = Map.of("name", "John", "age", 30);
byte[] flexonData = FlexonBinary.encode(data);

// With type handling
Person person = FlexonBinary.decode(flexonData, Person.class);

// Async operations
CompletableFuture<Void> future = FlexonBinary.encodeAsync(data, "data.flexon");
```

## Go Implementation

```go
package main

import "github.com/flexon/flexon-go"

// Basic usage
data := map[string]interface{}{"name": "John", "age": 30}
flexonData, err := flexon.Encode(data)

// With type handling
var person Person
err = flexon.Decode(flexonData, &person)

// Goroutine-friendly
go flexon.EncodeToFile(data, "data.flexon")
```

## Rust Implementation

```rust
use flexon::Flexon;

// Basic usage
let data = json!({
    "name": "John",
    "age": 30
});
let flexon_data = Flexon::encode(&data)?;

// With type handling
let person: Person = Flexon::decode(&flexon_data)?;

// Async operations
async fn save_data(data: &Value) -> Result<()> {
    Flexon::encode_to_file(data, "data.flexon").await?;
    Ok(())
}
```

## Common Features Across All Bindings

1. **Type Safety**
   - Strong type checking
   - Custom type serialization
   - Generic type support

2. **Performance**
   - Native implementation
   - Memory efficiency
   - Streaming support

3. **Error Handling**
   - Consistent error types
   - Detailed error messages
   - Stack trace support

4. **Compression**
   - Built-in compression
   - Configurable levels
   - Streaming compression

5. **Schema Validation**
   - JSON Schema support
   - Custom validators
   - Error reporting

## Installation Guide

### .NET
```bash
dotnet add package FlexonCLI
```

### Python
```bash
pip install flexon-py
```

### Node.js
```bash
npm install flexon-js
```

### Java
```xml
<dependency>
    <groupId>com.flexon</groupId>
    <artifactId>flexon-java</artifactId>
    <version>1.0.0</version>
</dependency>
```

### Go
```bash
go get github.com/flexon/flexon-go
```

### Rust
```bash
cargo add flexon
```

## Best Practices

1. **Type Handling**
   - Use strongly-typed models when possible
   - Implement custom type serializers for complex types
   - Validate data structures

2. **Error Handling**
   - Implement proper try-catch blocks
   - Log errors appropriately
   - Use type-specific error handlers

3. **Performance**
   - Use async/await where available
   - Implement proper resource disposal
   - Use streaming for large data sets

4. **Memory Management**
   - Dispose of resources properly
   - Use streaming for large files
   - Implement proper cleanup

## Contributing

Each language binding has its own repository:

- [flexon-py](https://github.com/flexon/flexon-py)
- [flexon-js](https://github.com/flexon/flexon-js)
- [flexon-java](https://github.com/flexon/flexon-java)
- [flexon-go](https://github.com/flexon/flexon-go)
- [flexon-rust](https://github.com/flexon/flexon-rust)

Please refer to each repository's CONTRIBUTING.md for specific guidelines.
