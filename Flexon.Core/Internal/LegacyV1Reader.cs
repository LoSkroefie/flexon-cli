using System.IO.Compression;
using System.Text;

namespace Flexon.Internal;

internal static class LegacyV1Reader
{
    public static object? Read(ReadOnlySpan<byte> file, FlexonOptions options)
    {
        if (!options.AllowLegacyV1Read)
            throw new FlexonFormatException("This is not a FLEXON v2 file and legacy reading is disabled.");
        if (file.IsEmpty) throw new FlexonFormatException("The legacy FLEXON file is empty.");

        var compression = file[0] switch
        {
            0 => CompressionMethod.None,
            1 => CompressionMethod.GZip,
            2 => CompressionMethod.Deflate,
            3 => CompressionMethod.Brotli,
            _ => throw new FlexonFormatException("The file is neither FLEXON v2 nor a supported unencrypted v1 file. Encrypted v1 files cannot be recovered because v1 did not store its random KDF salt.")
        };

        var payload = CompressionCodec.Decompress(file[1..], compression, options.MaxPayloadBytes);
        using var stream = new MemoryStream(payload, writable: false);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        var value = ReadValue(reader, options, 0, reader.ReadByte());
        if (stream.Position != stream.Length)
            throw new FlexonFormatException("The legacy FLEXON payload contains trailing data.");
        return value;
    }

    private static object? ReadValue(BinaryReader reader, FlexonOptions options, int depth, byte type)
    {
        if (depth > options.MaxDepth) throw new FlexonFormatException("Legacy FLEXON nesting exceeds the configured limit.");
        try
        {
            return type switch
            {
                0x00 => null,
                0x01 => true,
                0x02 => false,
                0x03 => reader.ReadInt32(),
                0x04 => reader.ReadDouble(),
                0x05 => Encoding.UTF8.GetString(ReadBytes(reader, options)),
                0x06 => ReadBytes(reader, options),
                0x07 => DateTime.FromBinary(reader.ReadInt64()),
                0x08 => new Guid(ReadExact(reader, 16)),
                0x09 => ReadList(reader, options, depth),
                0x0A => ReadObject(reader, options, depth),
                _ => throw new FlexonFormatException($"Unknown legacy FLEXON value type 0x{type:X2}.")
            };
        }
        catch (EndOfStreamException ex)
        {
            throw new FlexonFormatException("The legacy FLEXON payload ended unexpectedly.", ex);
        }
    }

    private static List<object?> ReadList(BinaryReader reader, FlexonOptions options, int depth)
    {
        var result = new List<object?>();
        while (true)
        {
            var type = reader.ReadByte();
            if (type == 0) return result;
            if (result.Count >= options.MaxCollectionItems) throw new FlexonFormatException("Legacy collection exceeds the configured limit.");
            result.Add(ReadValue(reader, options, depth + 1, type));
        }
    }

    private static Dictionary<string, object?> ReadObject(BinaryReader reader, FlexonOptions options, int depth)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        while (true)
        {
            var type = reader.ReadByte();
            if (type == 0) return result;
            if (result.Count >= options.MaxCollectionItems) throw new FlexonFormatException("Legacy object exceeds the configured limit.");
            var key = ReadValue(reader, options, depth + 1, type) as string
                ?? throw new FlexonFormatException("Legacy object key is not a string.");
            if (!result.TryAdd(key, ReadValue(reader, options, depth + 1, reader.ReadByte())))
                throw new FlexonFormatException($"Duplicate legacy object key '{key}'.");
        }
    }

    private static byte[] ReadBytes(BinaryReader reader, FlexonOptions options)
    {
        var length = reader.ReadInt32();
        if (length < 0 || length > options.MaxValueBytes) throw new FlexonFormatException("Legacy value length is outside the configured limit.");
        return ReadExact(reader, length);
    }

    private static byte[] ReadExact(BinaryReader reader, int length)
    {
        var value = reader.ReadBytes(length);
        if (value.Length != length) throw new EndOfStreamException();
        return value;
    }
}
