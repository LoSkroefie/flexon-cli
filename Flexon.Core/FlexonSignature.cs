using System.Security.Cryptography;

namespace Flexon;

/// <summary>Creates and verifies detached ECDSA P-256 signatures.</summary>
public static class FlexonSignature
{
    public static FlexonSigningKeyPair GenerateKeyPair()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new FlexonSigningKeyPair(
            key.ExportPkcs8PrivateKeyPem(),
            key.ExportSubjectPublicKeyInfoPem());
    }

    public static byte[] Sign(ReadOnlySpan<byte> data, string privateKeyPem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPem);
        using var key = ECDsa.Create();
        key.ImportFromPem(privateKeyPem);
        return key.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
    }

    public static bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, string publicKeyPem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKeyPem);
        using var key = ECDsa.Create();
        key.ImportFromPem(publicKeyPem);
        return key.VerifyData(data, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
    }
}

public sealed record FlexonSigningKeyPair(string PrivateKeyPem, string PublicKeyPem);
