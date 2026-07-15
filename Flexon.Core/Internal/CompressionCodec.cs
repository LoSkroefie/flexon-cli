using System.IO.Compression;

namespace Flexon.Internal;

internal static class CompressionCodec
{
    public static byte[] Compress(ReadOnlySpan<byte> input, CompressionMethod method)
    {
        if (method == CompressionMethod.None) return input.ToArray();
        using var output = new MemoryStream();
        using (var compressor = Create(output, method, CompressionMode.Compress, leaveOpen: true))
        {
            compressor.Write(input);
        }
        return output.ToArray();
    }

    public static byte[] Decompress(ReadOnlySpan<byte> input, CompressionMethod method, long maxBytes)
    {
        if (method == CompressionMethod.None)
        {
            if (input.Length > maxBytes) throw new FlexonFormatException("Payload exceeds the configured decompression limit.");
            return input.ToArray();
        }

        using var source = new MemoryStream(input.ToArray(), writable: false);
        using var decompressor = Create(source, method, CompressionMode.Decompress, leaveOpen: false);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            int read;
            try { read = decompressor.Read(buffer, 0, buffer.Length); }
            catch (InvalidDataException ex) { throw new FlexonFormatException("The compressed FLEXON payload is invalid.", ex); }
            if (read == 0) break;
            if (output.Length + read > maxBytes)
                throw new FlexonFormatException("Decompressed payload exceeds the configured limit.");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }

    private static Stream Create(Stream stream, CompressionMethod method, CompressionMode mode, bool leaveOpen) => method switch
    {
        CompressionMethod.GZip => new GZipStream(stream, mode, leaveOpen),
        CompressionMethod.Deflate => new DeflateStream(stream, mode, leaveOpen),
        CompressionMethod.Brotli => new BrotliStream(stream, mode, leaveOpen),
        _ => throw new FlexonFormatException($"Unsupported compression method: {method}.")
    };
}
