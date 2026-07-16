using System.Text.Json;
using System.Text.Json.Nodes;
using Flexon;

namespace FlexonCLI;

internal enum AttachmentMode
{
    Explicit,
    Marked,
    Discover
}

internal static class ReferenceAwareBundle
{
    public static FlexonBundle Build(
        string jsonPath,
        JsonElement document,
        AttachmentMode mode,
        string? baseDirectory,
        IReadOnlyList<string> explicitAttachments,
        long maxAttachmentBytes)
    {
        if (maxAttachmentBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxAttachmentBytes));
        var jsonFullPath = Path.GetFullPath(jsonPath);
        var root = Path.GetFullPath(baseDirectory ?? Path.GetDirectoryName(jsonFullPath) ?? Directory.GetCurrentDirectory());
        if (!Directory.Exists(root)) throw new FlexonFormatException($"Attachment base directory '{root}' does not exist.");

        var attachments = new Dictionary<string, FlexonBundleAttachment>(StringComparer.OrdinalIgnoreCase);
        JsonElement bundledDocument;
        if (mode == AttachmentMode.Marked)
        {
            var node = JsonNode.Parse(document.GetRawText());
            var transformed = TransformMarked(node, root, attachments, maxAttachmentBytes);
            bundledDocument = JsonSerializer.SerializeToElement(transformed);
        }
        else
        {
            bundledDocument = document.Clone();
            if (mode == AttachmentMode.Discover)
                DiscoverReferences(document, root, attachments, maxAttachmentBytes);
        }

        foreach (var specification in explicitAttachments)
            AddExplicit(specification, attachments, maxAttachmentBytes);

        return new FlexonBundle(
            Path.GetFileName(jsonFullPath),
            bundledDocument,
            attachments.Values.OrderBy(item => item.Path, StringComparer.Ordinal));
    }

    private static JsonNode? TransformMarked(
        JsonNode? node,
        string root,
        IDictionary<string, FlexonBundleAttachment> attachments,
        long maxAttachmentBytes)
    {
        if (node is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("$flexonFile", out var marker))
            {
                if (obj.Count != 1 || marker is not JsonValue value || !value.TryGetValue<string>(out var reference) ||
                    string.IsNullOrWhiteSpace(reference))
                    throw new FlexonFormatException("A $flexonFile marker must be an object containing exactly one non-empty string value.");
                AddResolved(reference, root, attachments, maxAttachmentBytes, required: true);
                return JsonValue.Create(reference);
            }

            foreach (var property in obj.ToArray())
                obj[property.Key] = TransformMarked(property.Value, root, attachments, maxAttachmentBytes);
            return obj;
        }

        if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
                array[index] = TransformMarked(array[index], root, attachments, maxAttachmentBytes);
        }
        return node;
    }

    private static void DiscoverReferences(
        JsonElement element,
        string root,
        IDictionary<string, FlexonBundleAttachment> attachments,
        long maxAttachmentBytes)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var reference = element.GetString();
                if (!string.IsNullOrWhiteSpace(reference))
                    AddResolved(reference, root, attachments, maxAttachmentBytes, required: false);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    DiscoverReferences(item, root, attachments, maxAttachmentBytes);
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                    DiscoverReferences(property.Value, root, attachments, maxAttachmentBytes);
                break;
        }
    }

    private static void AddResolved(
        string reference,
        string root,
        IDictionary<string, FlexonBundleAttachment> attachments,
        long maxAttachmentBytes,
        bool required)
    {
        string logicalPath;
        try { logicalPath = FlexonBundle.NormalizeLogicalPath(reference); }
        catch (FlexonFormatException) when (!required) { return; }

        string source;
        try
        {
            var localPath = logicalPath.Replace('/', Path.DirectorySeparatorChar);
            source = Path.GetFullPath(Path.Combine(root, localPath));
        }
        catch (Exception ex) when (!required && ex is ArgumentException or NotSupportedException or PathTooLongException or IOException)
        {
            return;
        }
        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!source.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            if (required) throw new FlexonFormatException($"Marked attachment '{reference}' escapes the attachment base directory.");
            return;
        }
        if (!File.Exists(source))
        {
            if (required) throw new FlexonFormatException($"Marked attachment '{reference}' does not exist beneath '{root}'.");
            return;
        }
        EnsureNoReparsePoints(root, source);
        AddFile(logicalPath, source, attachments, maxAttachmentBytes);
    }

    private static void AddExplicit(
        string specification,
        IDictionary<string, FlexonBundleAttachment> attachments,
        long maxAttachmentBytes)
    {
        if (string.IsNullOrWhiteSpace(specification)) throw new FlexonFormatException("Attachment specifications cannot be empty.");
        var separator = File.Exists(specification) ? -1 : specification.IndexOf('=');
        var sourceText = separator >= 0 ? specification[(separator + 1)..] : specification;
        string source;
        try { source = Path.GetFullPath(sourceText); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new FlexonFormatException($"Explicit attachment source '{sourceText}' is not a valid path.", ex);
        }
        if (!File.Exists(source)) throw new FlexonFormatException($"Explicit attachment source '{sourceText}' does not exist.");
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new FlexonFormatException($"Explicit attachment source '{sourceText}' is a symbolic link or reparse point.");
        var logicalPath = separator >= 0 ? specification[..separator] : Path.GetFileName(source);
        AddFile(FlexonBundle.NormalizeLogicalPath(logicalPath), source, attachments, maxAttachmentBytes);
    }

    private static void AddFile(
        string logicalPath,
        string source,
        IDictionary<string, FlexonBundleAttachment> attachments,
        long maxAttachmentBytes)
    {
        var information = new FileInfo(source);
        if (information.Length > maxAttachmentBytes)
            throw new FlexonFormatException($"Attachment '{logicalPath}' is {information.Length:N0} bytes, above the {maxAttachmentBytes:N0}-byte attachment limit.");
        var candidate = new FlexonBundleAttachment(logicalPath, File.ReadAllBytes(source), GetMediaType(source));
        if (attachments.TryGetValue(candidate.Path, out var existing))
        {
            if (!string.Equals(existing.Path, candidate.Path, StringComparison.Ordinal))
                throw new FlexonFormatException($"Bundle paths '{existing.Path}' and '{candidate.Path}' collide on case-insensitive filesystems.");
            if (!existing.Data.Span.SequenceEqual(candidate.Data.Span))
                throw new FlexonFormatException($"Multiple sources map to bundle attachment path '{candidate.Path}'.");
            return;
        }
        attachments.Add(candidate.Path, candidate);
    }

    private static void EnsureNoReparsePoints(string root, string source)
    {
        var current = root;
        if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            throw new FlexonFormatException($"Attachment base directory '{root}' is a symbolic link or reparse point.");
        var relative = Path.GetRelativePath(root, source);
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new FlexonFormatException($"Attachment source '{source}' traverses a symbolic link or reparse point.");
        }
    }

    private static string GetMediaType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".json" => "application/json",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".svg" => "image/svg+xml",
        ".pdf" => "application/pdf",
        ".txt" or ".md" => "text/plain",
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".mp4" => "video/mp4",
        ".glb" => "model/gltf-binary",
        _ => "application/octet-stream"
    };
}
