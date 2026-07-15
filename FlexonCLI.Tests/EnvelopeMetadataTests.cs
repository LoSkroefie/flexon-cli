using Flexon;

namespace FlexonCLI.Tests;

public sealed class EnvelopeMetadataTests
{
    [Fact]
    public void UnencryptedEnvelopeIncludesChecksum()
    {
        var bytes = FlexonSerializer.Serialize("value");
        Assert.Equal(32, bytes[16]);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Aes256Gcm)]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305)]
    public void EncryptedEnvelopeDoesNotLeakPlaintextChecksum(EncryptionAlgorithm algorithm)
    {
        var bytes = FlexonSerializer.Serialize("value", new FlexonOptions { Encryption = algorithm, Password = "password" });
        Assert.Equal(0, bytes[16]);
        Assert.Equal("value", FlexonSerializer.Deserialize(bytes, new FlexonOptions { Password = "password" }));
    }
}
