using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Flexon;

namespace FlexonCLI.Tests;

public sealed class SerializerTests
{
    private const string ComplexJson = """
        {
          "null": null,
          "booleans": [true, false],
          "integers": [-9223372036854775808, 18446744073709551615],
          "decimal": 12345.6789,
          "double": 1.2e100,
          "unicode": "Hello 🌍",
          "nested": { "items": [1, null, 2] }
        }
        """;

    [Theory]
    [InlineData(CompressionMethod.None)]
    [InlineData(CompressionMethod.GZip)]
    [InlineData(CompressionMethod.Deflate)]
    [InlineData(CompressionMethod.Brotli)]
    public void JsonRoundTripPreservesAllJsonKinds(CompressionMethod compression)
    {
        using var document = JsonDocument.Parse(ComplexJson);
        var bytes = FlexonSerializer.Serialize(document.RootElement, new FlexonOptions { Compression = compression });
        var decoded = JsonSerializer.SerializeToElement(FlexonSerializer.Deserialize(bytes));
        Assert.True(JsonAssert.DeepEquals(document.RootElement, decoded));
        Assert.Equal("FLXN", Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.Equal(FlexonSerializer.CurrentFormatVersion, bytes[4]);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Aes256Gcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305)]
    public void AuthenticatedEncryptionRoundTrips(EncryptionAlgorithm algorithm)
    {
        using var document = JsonDocument.Parse(ComplexJson);
        var writeOptions = new FlexonOptions { Encryption = algorithm, Password = "correct horse battery staple", Compression = CompressionMethod.Brotli };
        var bytes = FlexonSerializer.Serialize(document.RootElement, writeOptions);
        var decoded = JsonSerializer.SerializeToElement(FlexonSerializer.Deserialize(bytes, new FlexonOptions { Password = writeOptions.Password }));
        Assert.True(JsonAssert.DeepEquals(document.RootElement, decoded));
    }

    [Fact]
    public void WrongPasswordIsRejected()
    {
        var bytes = FlexonSerializer.Serialize("secret", new FlexonOptions { Encryption = EncryptionAlgorithm.Aes256Gcm, Password = "right-password" });
        Assert.Throws<FlexonAuthenticationException>(() => FlexonSerializer.Deserialize(bytes, new FlexonOptions { Password = "wrong-password" }));
    }

    [Fact]
    public void CorruptionIsDetected()
    {
        var bytes = FlexonSerializer.Serialize(new[] { 1, 2, 3 });
        bytes[^1] ^= 0x7F;
        Assert.Throws<FlexonFormatException>(() => FlexonSerializer.Deserialize(bytes));
    }

    [Fact]
    public void TruncationIsDetected()
    {
        var bytes = FlexonSerializer.Serialize(new[] { 1, 2, 3 });
        Assert.Throws<FlexonFormatException>(() => FlexonSerializer.Deserialize(bytes.AsSpan(0, bytes.Length - 1)));
    }

    [Fact]
    public void PayloadLimitIsEnforced()
    {
        var bytes = FlexonSerializer.Serialize(new string('x', 1024), new FlexonOptions { Compression = CompressionMethod.None });
        Assert.Throws<FlexonFormatException>(() => FlexonSerializer.Deserialize(bytes, new FlexonOptions { MaxPayloadBytes = 128 }));
    }

    [Fact]
    public void LegacyV1UnencryptedFileCanBeRead()
    {
        using var encoded = new MemoryStream();
        encoded.WriteByte(1);
        using (var gzip = new GZipStream(encoded, CompressionMode.Compress, leaveOpen: true))
        using (var writer = new BinaryWriter(gzip, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((byte)0x0A);
            writer.Write((byte)0x05);
            WriteLegacyString(writer, "answer");
            writer.Write((byte)0x03);
            writer.Write(42);
            writer.Write((byte)0x00);
        }
        var result = Assert.IsType<Dictionary<string, object?>>(FlexonSerializer.Deserialize(encoded.ToArray()));
        Assert.Equal(42, result["answer"]);
    }

    [Fact]
    public void SchemaValidatorReportsNestedErrors()
    {
        using var value = JsonDocument.Parse("""{"name":"x","extra":true}""");
        using var schema = JsonDocument.Parse("""{"type":"object","required":["count"],"properties":{"name":{"type":"string","minLength":2}},"additionalProperties":false}""");
        var errors = JsonSchemaValidator.Validate(value.RootElement, schema.RootElement);
        Assert.Equal(3, errors.Count);
    }

    [Fact]
    public void DetachedSignatureAuthenticatesExactBytes()
    {
        var keys = FlexonSignature.GenerateKeyPair();
        var data = Encoding.UTF8.GetBytes("signed FLEXON payload");
        var signature = FlexonSignature.Sign(data, keys.PrivateKeyPem);

        Assert.True(FlexonSignature.Verify(data, signature, keys.PublicKeyPem));
        data[0] ^= 1;
        Assert.False(FlexonSignature.Verify(data, signature, keys.PublicKeyPem));
    }

    private static void WriteLegacyString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
}
