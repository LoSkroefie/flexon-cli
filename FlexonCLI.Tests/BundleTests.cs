using System.Text.Json;
using Flexon;

namespace FlexonCLI.Tests;

public sealed class BundleTests
{
    [Fact]
    public void BundleRoundTripPreservesDocumentAndNativeBinary()
    {
        using var document = JsonDocument.Parse("""{"image":"assets/logo.png","items":[1,null,2]}""");
        var bytes = Enumerable.Range(0, 1024).Select(index => (byte)(index * 17)).ToArray();
        var source = new FlexonBundle("project.json", document.RootElement, new[]
        {
            new FlexonBundleAttachment("assets/logo.png", bytes, "image/png")
        });

        var restored = Assert.IsType<FlexonBundle>(FlexonSerializer.Deserialize(FlexonSerializer.Serialize(source)));
        Assert.Equal("project.json", restored.DocumentName);
        Assert.True(JsonAssert.DeepEquals(document.RootElement, restored.Document));
        var attachment = Assert.Single(restored.Attachments);
        Assert.Equal("assets/logo.png", attachment.Path);
        Assert.Equal("image/png", attachment.MediaType);
        Assert.Equal(bytes, attachment.Data.ToArray());
    }

    [Fact]
    public void BundleWithIncorrectAttachmentHashIsRejected()
    {
        var payload = new Dictionary<string, object?>
        {
            ["$flexon"] = FlexonBundle.Profile,
            ["document"] = new Dictionary<string, object?> { ["name"] = "project.json", ["content"] = new Dictionary<string, object?>() },
            ["attachments"] = new List<object?>
            {
                new Dictionary<string, object?>
                {
                    ["path"] = "asset.bin",
                    ["length"] = 3L,
                    ["sha256"] = new string('0', 64),
                    ["mediaType"] = "application/octet-stream",
                    ["data"] = new byte[] { 1, 2, 3 }
                }
            }
        };

        var encoded = FlexonSerializer.Serialize(payload);
        var exception = Assert.Throws<FlexonFormatException>(() => FlexonSerializer.Deserialize(encoded));
        Assert.Contains("SHA-256", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../escape.bin")]
    [InlineData("/absolute.bin")]
    [InlineData("C:\\absolute.bin")]
    [InlineData("assets//empty.bin")]
    [InlineData("assets/bad:name.bin")]
    [InlineData("assets/CON.txt")]
    public void UnsafeLogicalAttachmentPathsAreRejected(string path)
    {
        Assert.Throws<FlexonFormatException>(() => new FlexonBundleAttachment(path, new byte[] { 1 }));
    }

    [Fact]
    public void DocumentAndAttachmentExtractionPathsCannotConflict()
    {
        using var document = JsonDocument.Parse("{}");
        Assert.Throws<FlexonFormatException>(() => new FlexonBundle("project.json", document.RootElement, new[]
        {
            new FlexonBundleAttachment("PROJECT.JSON", new byte[] { 1 })
        }));
        Assert.Throws<FlexonFormatException>(() => new FlexonBundle("project.json", document.RootElement, new[]
        {
            new FlexonBundleAttachment("assets", new byte[] { 1 }),
            new FlexonBundleAttachment("assets/logo.png", new byte[] { 2 })
        }));
        Assert.Throws<FlexonFormatException>(() => new FlexonBundle("project.json", document.RootElement, new[]
        {
            new FlexonBundleAttachment("assets/Logo.png", new byte[] { 1 }),
            new FlexonBundleAttachment("assets/logo.png", new byte[] { 1 })
        }));
    }
}
