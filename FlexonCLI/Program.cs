using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

public class Program
{
    public static void Main(string[] args)
    {
        // Display CLI header with updated version
        Console.WriteLine("=====================================");
        Console.WriteLine(" FLEXON CLI Utility v1.1.0");
        Console.WriteLine(" Developed by JVR Software");
        Console.WriteLine("=====================================\n");

        if (args.Length < 2)
        {
            DisplayUsage();
            return;
        }

        var command = args[0].ToLower();
        var inputPath = args[1];
        var outputPath = args.Length > 2 ? args[2] : null;

        try
        {
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException($"Input file '{inputPath}' not found.");
            }

            switch (command)
            {
                case "encode":
                    if (outputPath == null) throw new ArgumentException("Output path is required for encoding.");
                    EncodeJsonToFlexon(inputPath, outputPath);
                    Console.WriteLine($"Successfully encoded {inputPath} to {outputPath}");
                    break;

                case "decode":
                    if (outputPath == null) throw new ArgumentException("Output path is required for decoding.");
                    DecodeFlexonToJson(inputPath, outputPath);
                    Console.WriteLine($"Successfully decoded {inputPath} to {outputPath}");
                    break;

                case "inspect":
                    InspectFlexon(inputPath, outputPath);
                    Console.WriteLine($"Successfully inspected {inputPath}" + (outputPath != null ? $" and exported to {outputPath}" : ""));
                    break;

                case "validate":
                    if (outputPath == null) throw new ArgumentException("Schema path is required for validation.");
                    ValidateFlexon(inputPath, outputPath);
                    Console.WriteLine($"Validation completed for {inputPath} against schema {outputPath}");
                    break;

                default:
                    throw new ArgumentException("Invalid command. Use 'encode', 'decode', 'inspect', or 'validate'.");
            }
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"File Error: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Argument Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected Error: {ex.Message}");
        }
    }

    static void ValidateFlexon(string inputPath, string schemaPath)
    {
        using var inputStream = new FileStream(inputPath, FileMode.Open);
        using var compressedStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var reader = new BinaryReader(compressedStream);

        var data = FlexonBinary.Decode(reader);
        var schemaJson = File.ReadAllText(schemaPath);
        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        var errors = new List<string>();
        if (!FlexonBinary.Validate(data, schema, errors: errors))
        {
            Console.WriteLine("Validation failed:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
            return;
        }

        Console.WriteLine("Validation passed: FLEXON data matches the schema.");
    }

    static void EncodeJsonToFlexon(string inputPath, string outputPath)
    {
        var json = File.ReadAllText(inputPath);
        var data = JsonSerializer.Deserialize<object>(json);

        using var outputStream = new FileStream(outputPath, FileMode.Create);
        using var compressedStream = new GZipStream(outputStream, CompressionMode.Compress);
        using var writer = new BinaryWriter(compressedStream);
        FlexonBinary.Encode(data, writer);
    }

    static void DecodeFlexonToJson(string inputPath, string outputPath)
    {
        using var inputStream = new FileStream(inputPath, FileMode.Open);
        using var compressedStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var reader = new BinaryReader(compressedStream);
        var data = FlexonBinary.Decode(reader);

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
    }

    static void InspectFlexon(string inputPath, string outputPath)
    {
        using var inputStream = new FileStream(inputPath, FileMode.Open);
        using var compressedStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var reader = new BinaryReader(compressedStream);
        var data = FlexonBinary.Decode(reader);

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("FLEXON Data Inspection:");
        Console.WriteLine(json);

        if (outputPath != null)
        {
            File.WriteAllText(outputPath, json);
        }
    }

    private static void DisplayUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  flexon-cli encode <input.json> <output.flexon>");
        Console.WriteLine("  flexon-cli decode <input.flexon> <output.json>");
        Console.WriteLine("  flexon-cli inspect <input.flexon> [output.json]");
        Console.WriteLine("  flexon-cli validate <input.flexon> <schema.json>");
    }
}

public static class FlexonBinary
{
    public static void Encode(object data, BinaryWriter writer)
    {
        if (data == null)
        {
            writer.Write((byte)0x00); // Null
        }
        else if (data is bool boolean)
        {
            writer.Write((byte)(boolean ? 0x01 : 0x02)); // Boolean
        }
        else if (data is int integer)
        {
            writer.Write((byte)0x03); // Integer
            writer.Write(integer);
        }
        else if (data is double dbl)
        {
            writer.Write((byte)0x04); // Float
            writer.Write(dbl);
        }
        else if (data is string str)
        {
            writer.Write((byte)0x05); // String
            var bytes = Encoding.UTF8.GetBytes(str);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
        else if (data is byte[] binary)
        {
            writer.Write((byte)0x06); // Binary
            writer.Write(binary.Length);
            writer.Write(binary);
        }
        else if (data is DateTime dt)
        {
            writer.Write((byte)0x07); // Date
            writer.Write(dt.ToBinary());
        }
        else if (data is Guid guid)
        {
            writer.Write((byte)0x08); // UUID
            writer.Write(guid.ToByteArray());
        }
        else if (data is System.Collections.IList list)
        {
            writer.Write((byte)0x09); // List
            foreach (var item in list)
            {
                Encode(item, writer);
            }
            writer.Write((byte)0x00); // End of list marker
        }
        else if (data is System.Collections.IDictionary dict)
        {
            writer.Write((byte)0x0A); // Object
            foreach (var key in dict.Keys)
            {
                Encode(key, writer); // Key
                Encode(dict[key], writer); // Value
            }
            writer.Write((byte)0x00); // End of object marker
        }
        else
        {
            throw new NotSupportedException($"Unsupported type: {data.GetType()}");
        }
    }

    public static object Decode(BinaryReader reader)
    {
        byte typeIndicator = reader.ReadByte();

        return typeIndicator switch
        {
            0x00 => null,
            0x01 => true,
            0x02 => false,
            0x03 => reader.ReadInt32(),
            0x04 => reader.ReadDouble(),
            0x05 => Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32())),
            0x06 => reader.ReadBytes(reader.ReadInt32()),
            0x07 => DateTime.FromBinary(reader.ReadInt64()),
            0x08 => new Guid(reader.ReadBytes(16)),
            0x09 => DecodeList(reader),
            0x0A => DecodeDictionary(reader),
            _ => throw new InvalidOperationException($"Unknown type indicator: {typeIndicator}")
        };
    }

    private static List<object> DecodeList(BinaryReader reader)
    {
        var list = new List<object>();
        while (reader.PeekChar() != 0x00)
        {
            list.Add(Decode(reader));
        }
        reader.ReadByte();
        return list;
    }

    private static Dictionary<string, object> DecodeDictionary(BinaryReader reader)
    {
        var dict = new Dictionary<string, object>();
        while (reader.PeekChar() != 0x00)
        {
            var key = (string)Decode(reader);
            var value = Decode(reader);
            dict[key] = value;
        }
        reader.ReadByte();
        return dict;
    }

    public static bool Validate(object data, JsonElement schema, string propertyPath = "", List<string> errors = null)
    {
        if (errors == null) errors = new List<string>();
        string type = schema.GetProperty("type").GetString();
        propertyPath = string.IsNullOrEmpty(propertyPath) ? "(root)" : propertyPath;

        if (type == "string")
        {
            if (data is not string str)
            {
                errors.Add($"Property '{propertyPath}': Expected string, got {data?.GetType().Name ?? "null"}.");
                return false;
            }

            if (schema.TryGetProperty("minLength", out var minLength) && str.Length < minLength.GetInt32())
            {
                errors.Add($"Property '{propertyPath}': Length {str.Length} is less than minimum {minLength.GetInt32()}.");
                return false;
            }

            if (schema.TryGetProperty("maxLength", out var maxLength) && str.Length > maxLength.GetInt32())
            {
                errors.Add($"Property '{propertyPath}': Length {str.Length} exceeds maximum {maxLength.GetInt32()}.");
                return false;
            }
        }
        else if (type == "integer")
        {
            if (data is not int integer)
            {
                errors.Add($"Property '{propertyPath}': Expected integer, got {data?.GetType().Name ?? "null"}.");
                return false;
            }

            if (schema.TryGetProperty("minimum", out var minimum) && integer < minimum.GetInt32())
            {
                errors.Add($"Property '{propertyPath}': Value {integer} is less than minimum {minimum.GetInt32()}.");
                return false;
            }

            if (schema.TryGetProperty("maximum", out var maximum) && integer > maximum.GetInt32())
            {
                errors.Add($"Property '{propertyPath}': Value {integer} exceeds maximum {maximum.GetInt32()}.");
                return false;
            }
        }
        else if (type == "object")
        {
            if (data is not Dictionary<string, object> dict)
            {
                errors.Add($"Property '{propertyPath}': Expected object, got {data?.GetType().Name ?? "null"}.");
                return false;
            }

            var properties = schema.GetProperty("properties");
            foreach (var property in properties.EnumerateObject())
            {
                string key = property.Name;
                var subSchema = property.Value;

                if (!dict.ContainsKey(key))
                {
                    if (schema.TryGetProperty("required", out var requiredFields) &&
                        requiredFields.EnumerateArray().Any(r => r.GetString() == key))
                    {
                        errors.Add($"Property '{propertyPath}.{key}': Missing required property.");
                        return false;
                    }
                }
                else
                {
                    Validate(dict[key], subSchema, $"{propertyPath}.{key}", errors);
                }
            }
        }
        else if (type == "array")
        {
            if (data is not List<object> list)
            {
                errors.Add($"Property '{propertyPath}': Expected array, got {data?.GetType().Name ?? "null"}.");
                return false;
            }

            var itemsSchema = schema.GetProperty("items");
            for (int i = 0; i < list.Count; i++)
            {
                Validate(list[i], itemsSchema, $"{propertyPath}[{i}]", errors);
            }
        }
        else
        {
            errors.Add($"Property '{propertyPath}': Unsupported type '{type}' in schema.");
            return false;
        }

        return !errors.Any();
    }

}

