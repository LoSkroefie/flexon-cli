# Reference-aware bundles

FLEXON bundles keep one JSON document and its local file dependencies in a single authenticated, compressible envelope. They use the `flexon-bundle/1` profile inside the existing FLEXON v2 value format. Current explicit multi-input packages remain supported and unchanged.

## Attachment modes

The default is explicit legacy packaging:

```powershell
flexon-cli serialize -i settings.json -i logo.png -o package.flexon
```

Reference-aware operation requires exactly one primary JSON document and one of the following opt-in mechanisms.

### Discover existing references

```powershell
flexon-cli serialize -i project.json -o project.flexon --attachments discover
# --resolve-files is an alias
```

Every JSON string is considered, but a file is attached only when the value is a safe relative logical path and resolves to an existing regular file beneath the base directory. The default base is the JSON document's directory. URLs, missing paths, unsafe paths, directories, and strings that are not files are ignored. Existing reparse points cause an error rather than being followed.

The JSON is preserved unchanged. If it contains `"image": "assets/logo.png"`, extraction recreates `assets/logo.png` beneath the output directory.

### Mark required references

```json
{
  "image": { "$flexonFile": "assets/logo.png" }
}
```

```powershell
flexon-cli serialize -i project.json -o project.flexon --attachments marked
```

A marker must contain exactly the `$flexonFile` property with a non-empty string. The referenced file is required: missing, unsafe, external, or linked files fail the operation before the output package is replaced. In the bundled and extracted JSON, the marker becomes the ordinary string `"assets/logo.png"`.

### Attach files explicitly

```powershell
flexon-cli serialize -i project.json -o project.flexon `
  --attach "manuals/guide.pdf=C:\source\guide.pdf" `
  --attach "assets/icon.png=C:\source\icon.png"
```

`--attach` is repeatable and can be combined with marked or discover mode. Without `logical=`, the source filename is used as the logical path. An explicit source may be outside the JSON base directory because the caller named it directly; the logical destination must still be a safe relative portable path.

## Controls and preview

```powershell
flexon-cli serialize -i project.json --resolve-files --dry-run
flexon-cli serialize -i project.json -o project.flexon --resolve-files `
  --base-dir C:\project `
  --max-attachment-bytes 100MB
```

Dry-run mode prints every logical path, byte length, and SHA-256 without writing output. Limits accept bytes or `KB`, `MB`, and `GB` suffixes. The normal FLEXON payload/value limits still apply to the completed package.

## Extraction

```powershell
flexon-cli deserialize -i project.flexon -o restored
```

Before writing, the reader validates the profile, required fields, duplicate and case-colliding paths, declared lengths, SHA-256 hashes, normalized destinations, and existing reparse points. It then writes the JSON document and native attachment bytes into their logical relative paths.

## Library API

```csharp
using var document = JsonDocument.Parse("""{"image":"assets/logo.png"}""");
var bundle = new FlexonBundle("project.json", document.RootElement, new[]
{
    new FlexonBundleAttachment("assets/logo.png", File.ReadAllBytes("assets/logo.png"), "image/png")
});

var encoded = FlexonSerializer.Serialize(bundle);
var decoded = (FlexonBundle)FlexonSerializer.Deserialize(encoded)!;
```

Attachments use FLEXON's native Binary value rather than JSON Base64. The manifest stores path, length, media type, and SHA-256. Automatic discovery is a CLI policy; applications may implement schema-specific resolvers and construct the same public bundle model directly.

## Intended applications

- Game saves with screenshots, maps, avatars, or replay fragments.
- AI/ML datasets with prompts, labels, provenance, images, audio, and outputs.
- Claims, invoices, or case records with PDFs, scans, photographs, and emails.
- CMS articles, offline API jobs, application backups, and deployable configuration sets.
- 3D scenes with models and textures, IoT batches with sensor captures, and signed evidence packages.

FLEXON does not infer the meaning of an attachment, download remote URLs, or claim that packaging content makes it safe. Applications remain responsible for content validation and authorization.
