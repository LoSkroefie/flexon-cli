using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Flexon.Internal;

internal static class EnvelopeCodec
{
    private static readonly byte[] Magic = "FLXN"u8.ToArray();
    private const byte Version = 2;
    private const int HeaderLength = 25;
    private const int ChecksumLength = 32;
    private const int SaltLength = 16;
    private const int NonceLength = 12;
    private const int TagLength = 16;

    public static byte[] Write(ReadOnlySpan<byte> encoded, FlexonOptions options)
    {
        var compressed = CompressionCodec.Compress(encoded, options.Compression);
        if (compressed.LongLength > options.MaxPayloadBytes)
            throw new FlexonFormatException("Compressed payload exceeds the configured limit.");

        var encrypted = options.Encryption != EncryptionAlgorithm.None;
        var salt = encrypted ? RandomNumberGenerator.GetBytes(SaltLength) : Array.Empty<byte>();
        var nonce = encrypted ? RandomNumberGenerator.GetBytes(NonceLength) : Array.Empty<byte>();
        var tag = encrypted ? new byte[TagLength] : Array.Empty<byte>();
        var checksum = encrypted ? Array.Empty<byte>() : SHA256.HashData(compressed);
        var header = BuildHeader(
            encrypted,
            options.Compression,
            options.Encryption,
            encrypted ? options.KdfIterations : 0,
            salt.Length,
            nonce.Length,
            tag.Length,
            checksum.Length,
            compressed.LongLength);
        var payload = compressed;

        if (encrypted)
        {
            var password = options.Password ?? throw new ArgumentException("A password is required for encryption.");
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, options.KdfIterations, HashAlgorithmName.SHA256, 32);
            payload = new byte[compressed.Length];
            var associatedData = Combine(header, checksum, salt, nonce);
            try
            {
                switch (options.Encryption)
                {
                    case EncryptionAlgorithm.Aes256Gcm:
                        using (var aes = new AesGcm(key, TagLength))
                            aes.Encrypt(nonce, compressed, payload, tag, associatedData);
                        break;
                    case EncryptionAlgorithm.ChaCha20Poly1305:
                        using (var chaCha = new ChaCha20Poly1305(key))
                            chaCha.Encrypt(nonce, compressed, payload, tag, associatedData);
                        break;
                    default:
                        throw new FlexonFormatException($"Unsupported encryption algorithm: {options.Encryption}.");
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }

        using var output = new MemoryStream(HeaderLength + checksum.Length + salt.Length + nonce.Length + tag.Length + payload.Length);
        output.Write(header);
        output.Write(checksum);
        output.Write(salt);
        output.Write(nonce);
        output.Write(tag);
        output.Write(payload);
        return output.ToArray();
    }

    public static byte[] Read(ReadOnlySpan<byte> file, FlexonOptions options, out CompressionMethod compression)
    {
        if (file.Length < HeaderLength) throw new FlexonFormatException("The FLEXON file is too short.");
        var header = file[..HeaderLength].ToArray();
        if (!header.AsSpan(0, 4).SequenceEqual(Magic)) throw new FlexonFormatException("The FLEXON magic header is missing.");
        if (header[4] != Version) throw new FlexonFormatException($"Unsupported FLEXON version {header[4]}.");

        var flags = header[5];
        if ((flags & ~1) != 0) throw new FlexonFormatException("The FLEXON header contains unsupported flags.");
        var encrypted = (flags & 1) != 0;
        compression = ParseCompression(header[6]);
        var encryption = ParseEncryption(header[7]);
        if (header[8] != 0) throw new FlexonFormatException("The FLEXON reserved header byte must be zero.");
        var iterations = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(9, 4));
        var saltLength = header[13];
        var nonceLength = header[14];
        var tagLength = header[15];
        var checksumLength = header[16];
        var payloadLength = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(17, 8));

        ValidateHeader(encrypted, encryption, iterations, saltLength, nonceLength, tagLength, checksumLength, payloadLength, options);
        var totalLength = checked((long)HeaderLength + checksumLength + saltLength + nonceLength + tagLength + payloadLength);
        if (totalLength != file.Length) throw new FlexonFormatException("The FLEXON file length does not match its header.");

        var offset = HeaderLength;
        var checksum = file.Slice(offset, checksumLength).ToArray(); offset += checksumLength;
        var salt = file.Slice(offset, saltLength).ToArray(); offset += saltLength;
        var nonce = file.Slice(offset, nonceLength).ToArray(); offset += nonceLength;
        var tag = file.Slice(offset, tagLength).ToArray(); offset += tagLength;
        var storedPayload = file.Slice(offset, checked((int)payloadLength)).ToArray();
        var compressed = storedPayload;

        if (encrypted)
        {
            if (string.IsNullOrEmpty(options.Password))
                throw new FlexonAuthenticationException("This FLEXON file is encrypted; provide a password.");
            var key = Rfc2898DeriveBytes.Pbkdf2(options.Password, salt, iterations, HashAlgorithmName.SHA256, 32);
            compressed = new byte[storedPayload.Length];
            var associatedData = Combine(header, checksum, salt, nonce);
            try
            {
                try
                {
                    switch (encryption)
                    {
                        case EncryptionAlgorithm.Aes256Gcm:
                            using (var aes = new AesGcm(key, TagLength))
                                aes.Decrypt(nonce, storedPayload, tag, compressed, associatedData);
                            break;
                        case EncryptionAlgorithm.ChaCha20Poly1305:
                            using (var chaCha = new ChaCha20Poly1305(key))
                                chaCha.Decrypt(nonce, storedPayload, tag, compressed, associatedData);
                            break;
                        default:
                            throw new FlexonFormatException($"Unsupported encryption algorithm: {encryption}.");
                    }
                }
                catch (AuthenticationTagMismatchException ex)
                {
                    throw new FlexonAuthenticationException("The password is incorrect or the encrypted FLEXON file was modified.", ex);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }

        if (!encrypted)
        {
            var actualChecksum = SHA256.HashData(compressed);
            if (!CryptographicOperations.FixedTimeEquals(checksum, actualChecksum))
                throw new FlexonFormatException("The FLEXON payload checksum is invalid; the file is corrupt or was modified.");
        }
        return CompressionCodec.Decompress(compressed, compression, options.MaxPayloadBytes);
    }

    public static bool HasMagic(ReadOnlySpan<byte> file) => file.Length >= Magic.Length && file[..Magic.Length].SequenceEqual(Magic);

    private static byte[] BuildHeader(bool encrypted, CompressionMethod compression, EncryptionAlgorithm encryption, int iterations,
        int saltLength, int nonceLength, int tagLength, int checksumLength, long payloadLength)
    {
        var header = new byte[HeaderLength];
        Magic.CopyTo(header, 0);
        header[4] = Version;
        header[5] = encrypted ? (byte)1 : (byte)0;
        header[6] = (byte)compression;
        header[7] = (byte)encryption;
        header[8] = 0;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(9, 4), iterations);
        header[13] = checked((byte)saltLength);
        header[14] = checked((byte)nonceLength);
        header[15] = checked((byte)tagLength);
        header[16] = checked((byte)checksumLength);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(17, 8), payloadLength);
        return header;
    }

    private static void ValidateHeader(bool encrypted, EncryptionAlgorithm encryption, int iterations, int saltLength,
        int nonceLength, int tagLength, int checksumLength, long payloadLength, FlexonOptions options)
    {
        if (checksumLength != (encrypted ? 0 : ChecksumLength)) throw new FlexonFormatException("The FLEXON checksum length is invalid.");
        if (payloadLength < 0 || payloadLength > options.MaxPayloadBytes)
            throw new FlexonFormatException("The FLEXON payload length is outside the configured limit.");
        if (encrypted)
        {
            if (encryption == EncryptionAlgorithm.None) throw new FlexonFormatException("Encrypted flag is set without an encryption algorithm.");
            if (iterations is < 100_000 or > 10_000_000) throw new FlexonFormatException("The FLEXON KDF iteration count is unsafe or invalid.");
            if (saltLength != SaltLength || nonceLength != NonceLength || tagLength != TagLength)
                throw new FlexonFormatException("The FLEXON encryption metadata lengths are invalid.");
        }
        else if (encryption != EncryptionAlgorithm.None || iterations != 0 || saltLength != 0 || nonceLength != 0 || tagLength != 0)
        {
            throw new FlexonFormatException("An unencrypted FLEXON file contains encryption metadata.");
        }
    }

    private static CompressionMethod ParseCompression(byte value) => Enum.IsDefined(typeof(CompressionMethod), value)
        ? (CompressionMethod)value
        : throw new FlexonFormatException($"Unsupported compression identifier {value}.");

    private static EncryptionAlgorithm ParseEncryption(byte value) => Enum.IsDefined(typeof(EncryptionAlgorithm), value)
        ? (EncryptionAlgorithm)value
        : throw new FlexonFormatException($"Unsupported encryption identifier {value}.");

    private static byte[] Combine(params byte[][] values)
    {
        var length = values.Sum(value => value.Length);
        var result = new byte[length];
        var offset = 0;
        foreach (var value in values)
        {
            value.CopyTo(result, offset);
            offset += value.Length;
        }
        return result;
    }
}
