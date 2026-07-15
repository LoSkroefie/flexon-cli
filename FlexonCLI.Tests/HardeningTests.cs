using Flexon;

namespace FlexonCLI.Tests;

public sealed class HardeningTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "flexon-hardening-" + Guid.NewGuid().ToString("N"));

    public HardeningTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void BinaryPackageRoundTripIsByteExact()
    {
        var input = Path.Combine(_root, "random.bin");
        var package = Path.Combine(_root, "package.flexon");
        var output = Path.Combine(_root, "output");
        var bytes = Enumerable.Range(0, 4096).Select(index => (byte)(index * 31)).ToArray();
        File.WriteAllBytes(input, bytes);
        Assert.Equal(0, Run("serialize", "-i", input, "-o", package, "-c", "deflate"));
        Assert.Equal(0, Run("deserialize", "-i", package, "-o", output));
        Assert.Equal(bytes, File.ReadAllBytes(Path.Combine(output, "random.bin")));
    }

    [Fact]
    public void MissingAndWrongPasswordsReturnAuthenticationExitCode()
    {
        var file = Path.Combine(_root, "secure.flexon");
        File.WriteAllBytes(file, FlexonSerializer.Serialize("secret", new FlexonOptions
        {
            Encryption = EncryptionAlgorithm.Aes256Gcm,
            Password = "right-password"
        }));
        Assert.Equal(4, Run("inspect", file));
        Assert.Equal(4, Run("inspect", file, "wrong-password"));
    }

    [Fact]
    public void InvalidInputDoesNotReplaceExistingOutput()
    {
        var input = Path.Combine(_root, "invalid.json");
        var output = Path.Combine(_root, "existing.flexon");
        File.WriteAllText(input, "{ invalid json");
        File.WriteAllText(output, "keep-me");
        Assert.Equal(3, Run("encode", input, output));
        Assert.Equal("keep-me", File.ReadAllText(output));
    }

    [Fact]
    public void ExcessiveWriteDepthIsRejected()
    {
        object? value = null;
        for (var index = 0; index < 8; index++) value = new[] { value };
        Assert.Throws<FlexonFormatException>(() => FlexonSerializer.Serialize(value, new FlexonOptions { MaxDepth = 4 }));
    }

    [Fact]
    public void EncryptionMetadataTamperingIsRejected()
    {
        var bytes = FlexonSerializer.Serialize("secret", new FlexonOptions
        {
            Encryption = EncryptionAlgorithm.ChaCha20Poly1305,
            Password = "password"
        });
        bytes[6] = (byte)CompressionMethod.Brotli;
        Assert.Throws<FlexonAuthenticationException>(() => FlexonSerializer.Deserialize(bytes, new FlexonOptions { Password = "password" }));
    }

    private static int Run(params string[] args) => CliApplication.Run(args, new StringWriter(), new StringWriter());

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
