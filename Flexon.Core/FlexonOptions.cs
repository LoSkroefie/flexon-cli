namespace Flexon;

public enum CompressionMethod : byte
{
    None = 0,
    GZip = 1,
    Deflate = 2,
    Brotli = 3
}

public enum EncryptionAlgorithm : byte
{
    None = 0,
    Aes256Gcm = 1,
    ChaCha20Poly1305 = 2
}

public sealed class FlexonOptions
{
    public CompressionMethod Compression { get; init; } = CompressionMethod.GZip;
    public EncryptionAlgorithm Encryption { get; init; } = EncryptionAlgorithm.None;
    public string? Password { get; init; }
    public int KdfIterations { get; init; } = 210_000;
    public long MaxPayloadBytes { get; init; } = 512L * 1024 * 1024;
    public int MaxDepth { get; init; } = 128;
    public int MaxCollectionItems { get; init; } = 1_000_000;
    public int MaxValueBytes { get; init; } = 256 * 1024 * 1024;
    public bool AllowLegacyV1Read { get; init; } = true;

    internal void ValidateForWrite()
    {
        if (!Enum.IsDefined(Compression))
            throw new ArgumentOutOfRangeException(nameof(Compression));
        if (!Enum.IsDefined(Encryption))
            throw new ArgumentOutOfRangeException(nameof(Encryption));
        if (Encryption != EncryptionAlgorithm.None && string.IsNullOrEmpty(Password))
            throw new ArgumentException("A password is required when encryption is enabled.", nameof(Password));
        if (KdfIterations is < 100_000 or > 10_000_000)
            throw new ArgumentOutOfRangeException(nameof(KdfIterations), "KDF iterations must be between 100,000 and 10,000,000.");
        ValidateLimits();
    }

    internal void ValidateForRead()
    {
        ValidateLimits();
    }

    private void ValidateLimits()
    {
        if (MaxPayloadBytes is < 1 or > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(MaxPayloadBytes), $"Payload limit must be between 1 and {int.MaxValue} bytes.");
        if (MaxDepth is < 1 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(MaxDepth));
        if (MaxCollectionItems is < 1)
            throw new ArgumentOutOfRangeException(nameof(MaxCollectionItems));
        if (MaxValueBytes is < 1)
            throw new ArgumentOutOfRangeException(nameof(MaxValueBytes));
    }
}
