using System.Collections;
using System.Security.Cryptography;
using System.Text.Json;

namespace Flexon;

/// <summary>A reference-aware JSON document and its native binary attachments.</summary>
public sealed class FlexonBundle
{
    public const string Profile = "flexon-bundle/1";

    public string DocumentName { get; }
    public JsonElement Document { get; }
    public IReadOnlyList<FlexonBundleAttachment> Attachments { get; }

    public FlexonBundle(string documentName, JsonElement document, IEnumerable<FlexonBundleAttachment> attachments)
    {
        DocumentName = ValidateDocumentName(documentName);
        Document = document.Clone();
        ArgumentNullException.ThrowIfNull(attachments);
        var items = attachments.ToArray();
        var duplicate = items.GroupBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new FlexonFormatException($"Bundle attachment path '{duplicate.Key}' is duplicated.");
        var allPaths = items.Select(item => item.Path).Append(DocumentName).ToArray();
        for (var left = 0; left < allPaths.Length; left++)
        {
            for (var right = left + 1; right < allPaths.Length; right++)
            {
                if (string.Equals(allPaths[left], allPaths[right], StringComparison.OrdinalIgnoreCase) ||
                    allPaths[left].StartsWith(allPaths[right] + "/", StringComparison.OrdinalIgnoreCase) ||
                    allPaths[right].StartsWith(allPaths[left] + "/", StringComparison.OrdinalIgnoreCase))
                    throw new FlexonFormatException($"Bundle paths '{allPaths[left]}' and '{allPaths[right]}' conflict on extraction.");
            }
        }
        Attachments = Array.AsReadOnly(items);
    }

    internal Dictionary<string, object?> ToPayload() => new(StringComparer.Ordinal)
    {
        ["$flexon"] = Profile,
        ["document"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = DocumentName,
            ["content"] = Document
        },
        ["attachments"] = Attachments.Select(item => item.ToPayload()).ToList()
    };

    internal static bool TryFromPayload(object? value, out FlexonBundle? bundle)
    {
        bundle = null;
        if (value is not Dictionary<string, object?> root ||
            !root.TryGetValue("$flexon", out var profile) || profile is not string profileName ||
            !string.Equals(profileName, Profile, StringComparison.Ordinal))
            return false;

        if (!root.TryGetValue("document", out var documentValue) || documentValue is not Dictionary<string, object?> document ||
            !document.TryGetValue("name", out var nameValue) || nameValue is not string name ||
            !document.TryGetValue("content", out var content))
            throw new FlexonFormatException("The FLEXON bundle document metadata is incomplete.");

        if (!root.TryGetValue("attachments", out var attachmentsValue) || attachmentsValue is not IList attachmentList)
            throw new FlexonFormatException("The FLEXON bundle attachment list is missing or invalid.");

        var attachments = new List<FlexonBundleAttachment>(attachmentList.Count);
        foreach (var item in attachmentList)
        {
            if (item is not Dictionary<string, object?> attachment ||
                !attachment.TryGetValue("path", out var pathValue) || pathValue is not string path ||
                !attachment.TryGetValue("data", out var dataValue) || dataValue is not byte[] data ||
                !attachment.TryGetValue("sha256", out var hashValue) || hashValue is not string hash ||
                !attachment.TryGetValue("length", out var lengthValue) || lengthValue is not long length)
                throw new FlexonFormatException("A FLEXON bundle attachment entry is incomplete or invalid.");

            var mediaType = attachment.TryGetValue("mediaType", out var mediaTypeValue) && mediaTypeValue is string text
                ? text
                : "application/octet-stream";
            attachments.Add(FlexonBundleAttachment.FromPayload(path, data, length, hash, mediaType));
        }

        JsonElement json;
        try { json = JsonSerializer.SerializeToElement(content); }
        catch (NotSupportedException ex) { throw new FlexonFormatException("The FLEXON bundle document is not JSON-compatible.", ex); }
        bundle = new FlexonBundle(name, json, attachments);
        return true;
    }

    public static string NormalizeLogicalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new FlexonFormatException("Bundle paths cannot be empty.");
        if (path.IndexOf('\0') >= 0 || path.StartsWith('/') || path.StartsWith('\\') ||
            (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':'))
            throw new FlexonFormatException($"Bundle path '{path}' must be relative.");

        var segments = path.Replace('\\', '/').Split('/');
        if (segments.Any(IsUnsafeSegment))
            throw new FlexonFormatException($"Bundle path '{path}' contains an unsafe segment.");
        return string.Join('/', segments);
    }

    private static bool IsUnsafeSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) || segment is "." or ".." || segment.Any(char.IsControl) ||
            segment.IndexOfAny(new[] { '<', '>', ':', '"', '|', '?', '*' }) >= 0 ||
            segment.EndsWith(' ') || segment.EndsWith('.'))
            return true;
        var stem = segment.Split('.')[0];
        return stem.Equals("CON", StringComparison.OrdinalIgnoreCase) || stem.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
               stem.Equals("AUX", StringComparison.OrdinalIgnoreCase) || stem.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
               (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
                                     stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) &&
                stem[3] is >= '1' and <= '9');
    }

    private static string ValidateDocumentName(string name)
    {
        var normalized = NormalizeLogicalPath(name);
        if (normalized.Contains('/')) throw new FlexonFormatException("The bundle document name must be a simple filename.");
        return normalized;
    }
}

/// <summary>A single file stored natively inside a <see cref="FlexonBundle"/>.</summary>
public sealed class FlexonBundleAttachment
{
    private readonly byte[] _data;

    public string Path { get; }
    public ReadOnlyMemory<byte> Data => _data;
    public long Length => _data.LongLength;
    public string Sha256 { get; }
    public string MediaType { get; }

    public FlexonBundleAttachment(string path, byte[] data, string? mediaType = null)
    {
        Path = FlexonBundle.NormalizeLogicalPath(path);
        ArgumentNullException.ThrowIfNull(data);
        _data = data.ToArray();
        Sha256 = Convert.ToHexString(SHA256.HashData(_data)).ToLowerInvariant();
        MediaType = string.IsNullOrWhiteSpace(mediaType) ? "application/octet-stream" : mediaType;
    }

    internal Dictionary<string, object?> ToPayload() => new(StringComparer.Ordinal)
    {
        ["path"] = Path,
        ["length"] = Length,
        ["sha256"] = Sha256,
        ["mediaType"] = MediaType,
        ["data"] = _data
    };

    internal static FlexonBundleAttachment FromPayload(string path, byte[] data, long declaredLength, string declaredHash, string mediaType)
    {
        var attachment = new FlexonBundleAttachment(path, data, mediaType);
        if (declaredLength != attachment.Length)
            throw new FlexonFormatException($"Bundle attachment '{attachment.Path}' length does not match its manifest.");
        if (!string.Equals(declaredHash, attachment.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new FlexonFormatException($"Bundle attachment '{attachment.Path}' SHA-256 does not match its manifest.");
        return attachment;
    }
}
