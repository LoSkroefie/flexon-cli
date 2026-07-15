using System.Text.Json;
using Flexon;

namespace FlexonCLI.Tests;

public sealed class CliTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "flexon-tests-" + Guid.NewGuid().ToString("N"));

    public CliTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void EncodeDecodeRoundTripReturnsSuccess()
    {
        var input = Path.Combine(_root, "input.json");
        var flexon = Path.Combine(_root, "output.flexon");
        var decoded = Path.Combine(_root, "decoded.json");
        File.WriteAllText(input, """{"value":null,"items":[1,null,2]}""");

        Assert.Equal(0, Run("encode", input, flexon));
        Assert.Equal(0, Run("decode", flexon, decoded));
        using var expected = JsonDocument.Parse(File.ReadAllText(input));
        using var actual = JsonDocument.Parse(File.ReadAllText(decoded));
        Assert.True(JsonAssert.DeepEquals(expected.RootElement, actual.RootElement));
    }

    [Fact]
    public void InvalidCommandReturnsUsageExitCode()
    {
        Assert.Equal(2, Run("does-not-exist"));
    }

    [Fact]
    public void DeserializeRejectsPathTraversal()
    {
        var package = new Dictionary<string, object?> { ["../escape.txt"] = "blocked" };
        var input = Path.Combine(_root, "malicious.flexon");
        File.WriteAllBytes(input, FlexonSerializer.Serialize(package));
        var output = Path.Combine(_root, "extract");
        Assert.Equal(3, Run("deserialize", "-i", input, "-o", output));
        Assert.False(File.Exists(Path.Combine(_root, "escape.txt")));
    }

    [Fact]
    public void EncryptedPackageCanUseEnvironmentPassword()
    {
        var input = Path.Combine(_root, "input.json");
        var flexon = Path.Combine(_root, "output.flexon");
        var output = Path.Combine(_root, "extract");
        File.WriteAllText(input, """{"secure":true}""");
        var variable = "FLEXON_TEST_PASSWORD_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(variable, "test-password");
        try
        {
            Assert.Equal(0, Run("serialize", "-i", input, "-o", flexon, "--encryption", "AES256", "--password-env", variable));
            Assert.Equal(0, Run("deserialize", "-i", flexon, "-o", output, "--password-env", variable));
            Assert.True(File.Exists(Path.Combine(output, "input.json")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Fact]
    public void KeygenSignAndVerifySignatureWorkEndToEnd()
    {
        var input = Path.Combine(_root, "payload.flexon");
        var privateKey = Path.Combine(_root, "private.pem");
        var publicKey = Path.Combine(_root, "public.pem");
        var signature = Path.Combine(_root, "payload.sig");
        File.WriteAllBytes(input, FlexonSerializer.Serialize(new { value = 42 }));

        Assert.Equal(0, Run("keygen", privateKey, publicKey));
        Assert.Equal(0, Run("sign", input, privateKey, signature));
        Assert.Equal(0, Run("verify-signature", input, publicKey, signature));

        var bytes = File.ReadAllBytes(input);
        bytes[^1] ^= 1;
        File.WriteAllBytes(input, bytes);
        Assert.Equal(6, Run("verify-signature", input, publicKey, signature));
    }

    private static int Run(params string[] args) => CliApplication.Run(args, new StringWriter(), new StringWriter());

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
