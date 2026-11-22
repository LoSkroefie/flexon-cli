using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Flexon;

namespace FlexonCLI.Tests;

public sealed class JsonCorrectnessTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "flexon-correctness-" + Guid.NewGuid().ToString("N"));
    public JsonCorrectnessTests() => Directory.CreateDirectory(_root);

    [Theory]
    [InlineData("0")]
    [InlineData("-0.00e99")]
    [InlineData("18446744073709551616")]
    [InlineData("79228162514264337593543950335")]
    [InlineData("1.2300e2")]
    [InlineData("0.1")]
    [InlineData("1.2e100")]
    [InlineData("9007199254740993e20")]
    [InlineData("1e-29")]
    [InlineData("-79228162514264337593543950335")]
    [InlineData("1.234567890123456789012345678900")]
    [InlineData("5e-324")]
    [InlineData("1.7976931348623157e308")]
    public void SupportedNumbersPreserveExactDecodedJsonValue(string json)
    {
        using var document = JsonDocument.Parse(json);
        var bytes = FlexonSerializer.Serialize(document.RootElement);
        var actual = JsonSerializer.Serialize(FlexonSerializer.Deserialize(bytes));
        Assert.Equal(Normalize(json), Normalize(actual));
    }

    // Independent exact base-ten comparison; spelling, scale and signed zero are immaterial.
    private static (BigInteger, BigInteger) Normalize(string text)
    {
        var parts = text.ToLowerInvariant().Split('e');
        var exponent = parts.Length == 2 ? BigInteger.Parse(parts[1], CultureInfo.InvariantCulture) : BigInteger.Zero;
        var dot = parts[0].IndexOf('.');
        if (dot >= 0) exponent -= parts[0].Length - dot - 1;
        var coefficient = BigInteger.Parse(parts[0].Replace(".", ""), CultureInfo.InvariantCulture);
        if (coefficient.IsZero) return (BigInteger.Zero, BigInteger.Zero);
        while (coefficient % 10 == 0) { coefficient /= 10; exponent++; }
        return (coefficient, exponent);
    }

    [Theory]
    [InlineData("79228162514264337593543950336")]
    [InlineData("123456789012345678901234567890")]
    [InlineData("0.123456789012345678901234567891")]
    [InlineData("1.00000000000000000000000000001")]
    [InlineData("1e-1000")]
    [InlineData("1e1000")]
    [InlineData("900719925474099312345e20")]
    public void InexactNumbersAreRejectedBeforeStreamIsWritten(string json)
    {
        using var document = JsonDocument.Parse(json);
        using var destination = new MemoryStream();
        destination.WriteByte(42);
        var error = Assert.Throws<FlexonFormatException>(() => FlexonSerializer.Serialize(document.RootElement, destination));
        Assert.Contains("without loss", error.Message);
        Assert.Equal(new byte[] { 42 }, destination.ToArray());
    }

    [Theory]
    [InlineData("{\"a\":1,\"a\":2}")]
    [InlineData("{\"nested\":[{\"a\":1,\"\\u0061\":2}]}")]
    public void DuplicateKeysAreRejectedInValuesAndBundles(string json)
    {
        using var document = JsonDocument.Parse(json);
        var bundle = new FlexonBundle("project.json", document.RootElement, Array.Empty<FlexonBundleAttachment>());
        foreach (var value in new object[] { document.RootElement, bundle })
            Assert.Contains("Duplicate object key 'a'", Assert.Throws<FlexonFormatException>(() => FlexonSerializer.Serialize(value)).Message);
    }

    [Theory]
    [InlineData("encode")]
    [InlineData("serialize")]
    [InlineData("marked")]
    [InlineData("discover")]
    [InlineData("explicit")]
    public void InvalidJsonNeverReplacesExistingCliOutput(string mode)
    {
        var input = Path.Combine(_root, "project.json");
        var output = Path.Combine(_root, "existing.flexon");
        var attachment = Path.Combine(_root, "asset.bin");
        File.WriteAllBytes(attachment, new byte[] { 1 });
        foreach (var json in new[] { "{\"nested\":[{\"a\":1,\"a\":2}]}", "{\"n\":123456789012345678901234567890}" })
        {
            File.WriteAllText(input, json);
            File.WriteAllText(output, "preserve");
            var args = mode switch
            {
                "encode" => new[] { "encode", input, output },
                "serialize" => new[] { "serialize", "-i", input, "-o", output },
                "explicit" => new[] { "serialize", "-i", input, "-o", output, "--attach", attachment },
                _ => new[] { "serialize", "-i", input, "-o", output, "--attachments", mode }
            };
            var error = new StringWriter();
            Assert.Equal(3, CliApplication.Run(args, new StringWriter(), error));
            Assert.Equal("preserve", File.ReadAllText(output));
            Assert.Contains(json.Contains("nested") ? "Duplicate object key" : "without loss", error.ToString());
        }
    }

    [Fact]
    public void MarkedMixedNestedArraysRestoreDocumentAndAttachmentBytes()
    {
        var input = Path.Combine(_root, "project.json");
        var output = Path.Combine(_root, "project.flexon");
        var restored = Path.Combine(_root, "restored");
        var data = new byte[] { 0, 1, 127, 255 };
        File.WriteAllBytes(Path.Combine(_root, "asset.bin"), data);
        File.WriteAllText(input, """{"items":[1,true,false,null,"text",{"ordinary":[2,{"ok":true}]},[3,{"$flexonFile":"asset.bin"}]]}""");
        Assert.Equal(0, CliApplication.Run(new[] { "serialize", "-i", input, "-o", output, "--attachments", "marked" }, new StringWriter(), new StringWriter()));
        Assert.Equal(0, CliApplication.Run(new[] { "deserialize", "-i", output, "-o", restored }, new StringWriter(), new StringWriter()));
        using var expected = JsonDocument.Parse("""{"items":[1,true,false,null,"text",{"ordinary":[2,{"ok":true}]},[3,"asset.bin"]]}""");
        using var actual = JsonDocument.Parse(File.ReadAllText(Path.Combine(restored, "project.json")));
        Assert.True(JsonAssert.DeepEquals(expected.RootElement, actual.RootElement));
        Assert.Equal(data, File.ReadAllBytes(Path.Combine(restored, "asset.bin")));
    }

    [Fact]
    public void DistinctCaseAndSeparateObjectScopesRemainValid()
    {
        const string json = """{"a":1,"A":2,"items":[{"a":3},{"a":4}]}""";
        using var document = JsonDocument.Parse(json);
        var decoded = JsonSerializer.SerializeToElement(FlexonSerializer.Deserialize(FlexonSerializer.Serialize(document.RootElement)));
        Assert.True(JsonAssert.DeepEquals(document.RootElement, decoded));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
