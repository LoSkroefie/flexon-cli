using System.Collections;
using System.Text;
using System.Text.Json;

namespace Flexon.Internal;

internal static class BinaryCodec
{
    private enum ValueType : byte
    {
        Null = 0,
        False = 1,
        True = 2,
        Int64 = 3,
        UInt64 = 4,
        Double = 5,
        Decimal = 6,
        String = 7,
        Binary = 8,
        DateTime = 9,
        DateTimeOffset = 10,
        Guid = 11,
        Array = 12,
        Object = 13
    }

    public static byte[] Encode(object? value, FlexonOptions options)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        WriteValue(writer, value, options, 0);
        writer.Flush();
        if (stream.Length > options.MaxPayloadBytes)
            throw new FlexonFormatException($"Encoded payload exceeds the {options.MaxPayloadBytes:N0}-byte limit.");
        return stream.ToArray();
    }

    public static object? Decode(ReadOnlySpan<byte> data, FlexonOptions options)
    {
        using var stream = new MemoryStream(data.ToArray(), writable: false);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        var value = ReadValue(reader, options, 0);
        if (stream.Position != stream.Length)
            throw new FlexonFormatException("The FLEXON payload contains trailing data.");
        return value;
    }

    private static void WriteValue(BinaryWriter writer, object? value, FlexonOptions options, int depth)
    {
        EnsureDepth(depth, options);

        if (value is JsonElement element)
        {
            WriteJsonElement(writer, element, options, depth);
            return;
        }

        switch (value)
        {
            case null:
                writer.Write((byte)ValueType.Null);
                return;
            case bool boolean:
                writer.Write((byte)(boolean ? ValueType.True : ValueType.False));
                return;
            case sbyte or byte or short or ushort or int or uint or long:
                writer.Write((byte)ValueType.Int64);
                writer.Write(Convert.ToInt64(value));
                return;
            case ulong unsigned:
                writer.Write((byte)ValueType.UInt64);
                writer.Write(unsigned);
                return;
            case float or double:
                writer.Write((byte)ValueType.Double);
                writer.Write(Convert.ToDouble(value));
                return;
            case decimal decimalValue:
                writer.Write((byte)ValueType.Decimal);
                foreach (var part in decimal.GetBits(decimalValue)) writer.Write(part);
                return;
            case string text:
                writer.Write((byte)ValueType.String);
                WriteBytes(writer, Encoding.UTF8.GetBytes(text), options);
                return;
            case byte[] bytes:
                writer.Write((byte)ValueType.Binary);
                WriteBytes(writer, bytes, options);
                return;
            case DateTime dateTime:
                writer.Write((byte)ValueType.DateTime);
                writer.Write(dateTime.ToBinary());
                return;
            case DateTimeOffset dateTimeOffset:
                writer.Write((byte)ValueType.DateTimeOffset);
                writer.Write(dateTimeOffset.Ticks);
                writer.Write((short)dateTimeOffset.Offset.TotalMinutes);
                return;
            case Guid guid:
                writer.Write((byte)ValueType.Guid);
                writer.Write(guid.ToByteArray());
                return;
            case IDictionary dictionary:
                WriteDictionary(writer, dictionary, options, depth);
                return;
            case IEnumerable enumerable:
                WriteEnumerable(writer, enumerable, options, depth);
                return;
            default:
                WriteJsonElement(writer, JsonSerializer.SerializeToElement(value), options, depth);
                return;
        }
    }

    private static void WriteJsonElement(BinaryWriter writer, JsonElement element, FlexonOptions options, int depth)
    {
        EnsureDepth(depth, options);
        switch (element.ValueKind)
        {
            case JsonValueKind.Undefined:
            case JsonValueKind.Null:
                writer.Write((byte)ValueType.Null);
                break;
            case JsonValueKind.False:
                writer.Write((byte)ValueType.False);
                break;
            case JsonValueKind.True:
                writer.Write((byte)ValueType.True);
                break;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var signed))
                {
                    writer.Write((byte)ValueType.Int64);
                    writer.Write(signed);
                }
                else if (element.TryGetUInt64(out var unsigned))
                {
                    writer.Write((byte)ValueType.UInt64);
                    writer.Write(unsigned);
                }
                else if (element.TryGetDecimal(out var decimalValue))
                {
                    writer.Write((byte)ValueType.Decimal);
                    foreach (var part in decimal.GetBits(decimalValue)) writer.Write(part);
                }
                else
                {
                    writer.Write((byte)ValueType.Double);
                    writer.Write(element.GetDouble());
                }
                break;
            case JsonValueKind.String:
                writer.Write((byte)ValueType.String);
                WriteBytes(writer, Encoding.UTF8.GetBytes(element.GetString() ?? string.Empty), options);
                break;
            case JsonValueKind.Array:
                var array = element.EnumerateArray().ToArray();
                EnsureCount(array.Length, options);
                writer.Write((byte)ValueType.Array);
                writer.Write(array.Length);
                foreach (var item in array) WriteJsonElement(writer, item, options, depth + 1);
                break;
            case JsonValueKind.Object:
                var properties = element.EnumerateObject().ToArray();
                EnsureCount(properties.Length, options);
                writer.Write((byte)ValueType.Object);
                writer.Write(properties.Length);
                foreach (var property in properties)
                {
                    WriteBytes(writer, Encoding.UTF8.GetBytes(property.Name), options);
                    WriteJsonElement(writer, property.Value, options, depth + 1);
                }
                break;
            default:
                throw new FlexonFormatException($"Unsupported JSON value kind: {element.ValueKind}.");
        }
    }

    private static void WriteDictionary(BinaryWriter writer, IDictionary dictionary, FlexonOptions options, int depth)
    {
        EnsureCount(dictionary.Count, options);
        writer.Write((byte)ValueType.Object);
        writer.Write(dictionary.Count);
        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Key is not string key)
                throw new FlexonFormatException("FLEXON object keys must be strings.");
            WriteBytes(writer, Encoding.UTF8.GetBytes(key), options);
            WriteValue(writer, entry.Value, options, depth + 1);
        }
    }

    private static void WriteEnumerable(BinaryWriter writer, IEnumerable enumerable, FlexonOptions options, int depth)
    {
        var values = enumerable.Cast<object?>().ToList();
        EnsureCount(values.Count, options);
        writer.Write((byte)ValueType.Array);
        writer.Write(values.Count);
        foreach (var item in values) WriteValue(writer, item, options, depth + 1);
    }

    private static object? ReadValue(BinaryReader reader, FlexonOptions options, int depth)
    {
        EnsureDepth(depth, options);
        ValueType type;
        try { type = (ValueType)reader.ReadByte(); }
        catch (EndOfStreamException ex) { throw new FlexonFormatException("The FLEXON payload ended unexpectedly.", ex); }

        try
        {
            return type switch
            {
                ValueType.Null => null,
                ValueType.False => false,
                ValueType.True => true,
                ValueType.Int64 => reader.ReadInt64(),
                ValueType.UInt64 => reader.ReadUInt64(),
                ValueType.Double => reader.ReadDouble(),
                ValueType.Decimal => new decimal(new[] { reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32() }),
                ValueType.String => Encoding.UTF8.GetString(ReadBytes(reader, options)),
                ValueType.Binary => ReadBytes(reader, options),
                ValueType.DateTime => DateTime.FromBinary(reader.ReadInt64()),
                ValueType.DateTimeOffset => new DateTimeOffset(reader.ReadInt64(), TimeSpan.FromMinutes(reader.ReadInt16())),
                ValueType.Guid => new Guid(ReadExact(reader, 16)),
                ValueType.Array => ReadArray(reader, options, depth),
                ValueType.Object => ReadObject(reader, options, depth),
                _ => throw new FlexonFormatException($"Unknown FLEXON value type 0x{(byte)type:X2}.")
            };
        }
        catch (EndOfStreamException ex)
        {
            throw new FlexonFormatException("The FLEXON payload ended unexpectedly.", ex);
        }
        catch (ArgumentException ex) when (type == ValueType.Decimal || type == ValueType.DateTimeOffset)
        {
            throw new FlexonFormatException($"The {type} value is invalid.", ex);
        }
    }

    private static List<object?> ReadArray(BinaryReader reader, FlexonOptions options, int depth)
    {
        var count = ReadCount(reader, options);
        var result = new List<object?>(count);
        for (var i = 0; i < count; i++) result.Add(ReadValue(reader, options, depth + 1));
        return result;
    }

    private static Dictionary<string, object?> ReadObject(BinaryReader reader, FlexonOptions options, int depth)
    {
        var count = ReadCount(reader, options);
        var result = new Dictionary<string, object?>(count, StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            var key = Encoding.UTF8.GetString(ReadBytes(reader, options));
            if (!result.TryAdd(key, ReadValue(reader, options, depth + 1)))
                throw new FlexonFormatException($"Duplicate object key '{key}'.");
        }
        return result;
    }

    private static int ReadCount(BinaryReader reader, FlexonOptions options)
    {
        var count = reader.ReadInt32();
        if (count < 0 || count > options.MaxCollectionItems)
            throw new FlexonFormatException($"Collection length {count} is outside the configured limit.");
        return count;
    }

    private static void WriteBytes(BinaryWriter writer, byte[] value, FlexonOptions options)
    {
        if (value.Length > options.MaxValueBytes)
            throw new FlexonFormatException($"Value length {value.Length:N0} exceeds the configured limit.");
        writer.Write(value.Length);
        writer.Write(value);
    }

    private static byte[] ReadBytes(BinaryReader reader, FlexonOptions options)
    {
        var length = reader.ReadInt32();
        if (length < 0 || length > options.MaxValueBytes)
            throw new FlexonFormatException($"Value length {length} is outside the configured limit.");
        return ReadExact(reader, length);
    }

    private static byte[] ReadExact(BinaryReader reader, int length)
    {
        var bytes = reader.ReadBytes(length);
        if (bytes.Length != length) throw new EndOfStreamException();
        return bytes;
    }

    private static void EnsureDepth(int depth, FlexonOptions options)
    {
        if (depth > options.MaxDepth)
            throw new FlexonFormatException($"Value nesting exceeds the configured depth limit of {options.MaxDepth}.");
    }

    private static void EnsureCount(int count, FlexonOptions options)
    {
        if (count > options.MaxCollectionItems)
            throw new FlexonFormatException($"Collection contains more than {options.MaxCollectionItems:N0} items.");
    }
}
