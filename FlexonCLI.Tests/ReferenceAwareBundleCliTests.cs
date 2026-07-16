using System.Text.Json;

namespace FlexonCLI.Tests;

public sealed class ReferenceAwareBundleCliTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "flexon-bundle-tests-" + Guid.NewGuid().ToString("N"));

    public ReferenceAwareBundleCliTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void DiscoverModeAttachesExistingReferencesAndPreservesPaths()
    {
        var assets = Path.Combine(_root, "assets");
        Directory.CreateDirectory(assets);
        var binary = Enumerable.Range(0, 512).Select(index => (byte)(index * 29)).ToArray();
        File.WriteAllBytes(Path.Combine(assets, "logo.png"), binary);
        var documentPath = Path.Combine(_root, "project.json");
        File.WriteAllText(documentPath, """{"image":"assets/logo.png","remote":"https://example.com/logo.png","future":"missing.bin"}""");
        var package = Path.Combine(_root, "project.flexon");
        var restored = Path.Combine(_root, "restored");

        Assert.Equal(0, Run("serialize", "-i", documentPath, "-o", package, "--resolve-files"));
        Assert.Equal(0, Run("deserialize", "-i", package, "-o", restored));
        Assert.Equal(binary, File.ReadAllBytes(Path.Combine(restored, "assets", "logo.png")));
        using var expected = JsonDocument.Parse(File.ReadAllText(documentPath));
        using var actual = JsonDocument.Parse(File.ReadAllText(Path.Combine(restored, "project.json")));
        Assert.True(JsonAssert.DeepEquals(expected.RootElement, actual.RootElement));
        Assert.False(File.Exists(Path.Combine(restored, "missing.bin")));
    }

    [Fact]
    public void MarkedModeAttachesRequiredFileAndRewritesMarkerToPath()
    {
        var assets = Path.Combine(_root, "assets");
        Directory.CreateDirectory(assets);
        File.WriteAllBytes(Path.Combine(assets, "sound.wav"), new byte[] { 10, 20, 30, 40 });
        var documentPath = Path.Combine(_root, "project.json");
        File.WriteAllText(documentPath, """{"sound":{"$flexonFile":"assets/sound.wav"}}""");
        var package = Path.Combine(_root, "project.flexon");
        var restored = Path.Combine(_root, "restored");

        Assert.Equal(0, Run("serialize", "-i", documentPath, "-o", package, "--attachments", "marked"));
        Assert.Equal(0, Run("deserialize", "-i", package, "-o", restored));
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(restored, "project.json")));
        Assert.Equal("assets/sound.wav", document.RootElement.GetProperty("sound").GetString());
        Assert.Equal(new byte[] { 10, 20, 30, 40 }, File.ReadAllBytes(Path.Combine(restored, "assets", "sound.wav")));
    }

    [Fact]
    public void ExplicitAttachmentCanUseLogicalPathAndExternalSource()
    {
        var documentPath = Path.Combine(_root, "project.json");
        var source = Path.Combine(_root, "source.bin");
        File.WriteAllText(documentPath, """{"asset":"models/model.bin"}""");
        File.WriteAllBytes(source, new byte[] { 7, 8, 9 });
        var package = Path.Combine(_root, "project.flexon");
        var restored = Path.Combine(_root, "restored");

        Assert.Equal(0, Run("serialize", "-i", documentPath, "-o", package, "--attach", $"models/model.bin={source}"));
        Assert.Equal(0, Run("deserialize", "-i", package, "-o", restored));
        Assert.Equal(new byte[] { 7, 8, 9 }, File.ReadAllBytes(Path.Combine(restored, "models", "model.bin")));
    }

    [Fact]
    public void DryRunReportsAttachmentsWithoutWritingPackage()
    {
        var asset = Path.Combine(_root, "asset.bin");
        var document = Path.Combine(_root, "project.json");
        var package = Path.Combine(_root, "should-not-exist.flexon");
        File.WriteAllBytes(asset, new byte[] { 1, 2, 3 });
        File.WriteAllText(document, """{"asset":"asset.bin"}""");
        var output = new StringWriter();

        var result = CliApplication.Run(new[] { "serialize", "-i", document, "-o", package, "--resolve-files", "--dry-run" }, output, new StringWriter());
        Assert.Equal(0, result);
        Assert.Contains("asset.bin", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("Dry run: 1 attachment", output.ToString(), StringComparison.Ordinal);
        Assert.False(File.Exists(package));
    }

    [Fact]
    public void UnsafeOrMissingMarkedReferenceFailsBeforeOutputIsWritten()
    {
        var document = Path.Combine(_root, "project.json");
        var package = Path.Combine(_root, "existing.flexon");
        File.WriteAllText(document, """{"asset":{"$flexonFile":"../secret.bin"}}""");
        File.WriteAllText(package, "preserve");

        Assert.Equal(3, Run("serialize", "-i", document, "-o", package, "--attachments", "marked"));
        Assert.Equal("preserve", File.ReadAllText(package));
    }

    [Fact]
    public void ReferenceAwareModeRequiresOneJsonDocument()
    {
        var binary = Path.Combine(_root, "input.bin");
        File.WriteAllBytes(binary, new byte[] { 1 });
        Assert.Equal(2, Run("serialize", "-i", binary, "-o", Path.Combine(_root, "output.flexon"), "--resolve-files"));
    }

    [Fact]
    public void AttachmentLimitFailsBeforeExistingOutputIsReplaced()
    {
        var asset = Path.Combine(_root, "asset.bin");
        var document = Path.Combine(_root, "project.json");
        var package = Path.Combine(_root, "existing.flexon");
        File.WriteAllBytes(asset, new byte[] { 1, 2, 3, 4 });
        File.WriteAllText(document, """{"asset":"asset.bin"}""");
        File.WriteAllText(package, "preserve");

        Assert.Equal(3, Run("serialize", "-i", document, "-o", package, "--resolve-files", "--max-attachment-bytes", "3"));
        Assert.Equal("preserve", File.ReadAllText(package));
    }

    private static int Run(params string[] args) => CliApplication.Run(args, new StringWriter(), new StringWriter());

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
