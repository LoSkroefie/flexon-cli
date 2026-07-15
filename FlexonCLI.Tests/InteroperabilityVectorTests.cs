using System.Text.Json;
using Flexon;

namespace FlexonCLI.Tests;

public sealed class InteroperabilityVectorTests
{
    [Fact]
    public void CanonicalUncompressedPackageMatchesPublishedVector()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var vectorRoot = Path.Combine(root, "Examples", "vectors");
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(vectorRoot, "v2-package-none.json")));
        var package = new Dictionary<string, object?> { ["sample.json"] = document.RootElement };

        var actual = FlexonSerializer.Serialize(package, new FlexonOptions { Compression = CompressionMethod.None });
        var expected = Convert.FromHexString(File.ReadAllText(Path.Combine(vectorRoot, "v2-package-none.hex")).Trim());

        Assert.Equal(expected, actual);
        var decoded = Assert.IsType<Dictionary<string, object?>>(FlexonSerializer.Deserialize(expected));
        var decodedJson = JsonSerializer.SerializeToElement(decoded["sample.json"]);
        Assert.True(JsonAssert.DeepEquals(document.RootElement, decodedJson));
    }
}
