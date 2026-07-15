using System.Text.Json;
using Flexon;

namespace FlexonCLI.Tests;

public sealed class ValidationAndExtractionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "flexon-validation-" + Guid.NewGuid().ToString("N"));

    public ValidationAndExtractionTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void AllPackageNamesAreValidatedBeforeAnyFileIsWritten()
    {
        var package = new Dictionary<string, object?>
        {
            ["safe.txt"] = "must-not-be-written",
            ["../escape.txt"] = "blocked"
        };
        var input = Path.Combine(_root, "malicious.flexon");
        var output = Path.Combine(_root, "output");
        File.WriteAllBytes(input, FlexonSerializer.Serialize(package));

        var exitCode = CliApplication.Run(new[] { "deserialize", "-i", input, "-o", output }, new StringWriter(), new StringWriter());
        Assert.Equal(3, exitCode);
        Assert.False(File.Exists(Path.Combine(output, "safe.txt")));
        Assert.False(File.Exists(Path.Combine(_root, "escape.txt")));
    }

    [Fact]
    public void UnsignedIntegerMatchesIntegerSchemaType()
    {
        using var value = JsonDocument.Parse("18446744073709551615");
        using var schema = JsonDocument.Parse("""{"type":"integer"}""");
        Assert.Empty(JsonSchemaValidator.Validate(value.RootElement, schema.RootElement));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
