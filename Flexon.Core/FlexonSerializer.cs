using System.Text.Json;
using Flexon.Internal;

namespace Flexon;

public static class FlexonSerializer
{
    public const byte CurrentFormatVersion = 2;

    public static void Serialize(object? value, Stream destination, FlexonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite) throw new ArgumentException("Destination stream must be writable.", nameof(destination));
        options ??= new FlexonOptions();
        options.ValidateForWrite();
        var encoded = BinaryCodec.Encode(value, options);
        var envelope = EnvelopeCodec.Write(encoded, options);
        destination.Write(envelope);
    }

    public static byte[] Serialize(object? value, FlexonOptions? options = null)
    {
        using var stream = new MemoryStream();
        Serialize(value, stream, options);
        return stream.ToArray();
    }

    public static object? Deserialize(Stream source, FlexonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead) throw new ArgumentException("Source stream must be readable.", nameof(source));
        options ??= new FlexonOptions();
        options.ValidateForRead();
        var file = ReadBounded(source, options.MaxPayloadBytes + 1024);
        if (EnvelopeCodec.HasMagic(file))
        {
            var payload = EnvelopeCodec.Read(file, options, out _);
            return BinaryCodec.Decode(payload, options);
        }
        return LegacyV1Reader.Read(file, options);
    }

    public static object? Deserialize(ReadOnlySpan<byte> source, FlexonOptions? options = null)
    {
        using var stream = new MemoryStream(source.ToArray(), writable: false);
        return Deserialize(stream, options);
    }

    public static JsonElement DeserializeJson(Stream source, FlexonOptions? options = null) =>
        JsonSerializer.SerializeToElement(Deserialize(source, options));

    private static byte[] ReadBounded(Stream source, long maxBytes)
    {
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = source.Read(buffer, 0, buffer.Length);
            if (read == 0) break;
            if (output.Length + read > maxBytes) throw new FlexonFormatException("FLEXON file exceeds the configured size limit.");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }
}
