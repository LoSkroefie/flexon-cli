# FLEXON CLI

[![CI](https://github.com/LoSkroefie/flexon-cli/actions/workflows/ci.yml/badge.svg)](https://github.com/LoSkroefie/flexon-cli/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-3.0.0-blue.svg)](CHANGELOG.md)
[![FlexonCLI on NuGet](https://img.shields.io/nuget/v/FlexonCLI.svg?label=FlexonCLI)](https://www.nuget.org/packages/FlexonCLI)
[![Flexon.Core on NuGet](https://img.shields.io/nuget/v/Flexon.Core.svg?label=Flexon.Core)](https://www.nuget.org/packages/Flexon.Core)

FLEXON is a compact, versioned binary envelope for JSON-compatible values and file packages. Version 2 focuses on correctness, predictable resource limits, corruption detection, authenticated encryption, and safe command-line automation.

## What ships

- Explicitly versioned `FLXN` v2 files with length-prefixed values and collections.
- Complete JSON value support, including nested `null`, signed and unsigned 64-bit integers, decimals, and doubles.
- GZip, Deflate, Brotli, or no compression.
- AES-256-GCM and ChaCha20-Poly1305 authenticated encryption.
- PBKDF2-SHA256 password derivation with a random stored salt and 210,000 iterations by default.
- SHA-256 corruption detection for unencrypted payloads; AEAD authentication tags for encrypted payloads.
- Configurable limits for input size, decompressed size, nesting depth, collection count, and value length.
- Safe package extraction that rejects absolute paths and directory traversal.
- Read compatibility for unencrypted v1 files where the old sentinel-based format is unambiguous.
- Detached ECDSA P-256/SHA-256 signatures with key generation and verification.
- A deterministic interoperability vector for independently implemented readers and writers.
- A .NET library (`Flexon.Core`) and global-tool package (`FlexonCLI`).

FLEXON does not claim to outperform established formats without reproducible benchmark evidence. The repository contains a benchmark command and a methodology document, but no marketing numbers are treated as facts.

## Build and test

Requirements: .NET 8 SDK or newer.

```powershell
dotnet restore FlexonCLI.sln
dotnet build FlexonCLI.sln --configuration Release --no-restore
dotnet test FlexonCLI.sln --configuration Release --no-build
```

Run locally:

```powershell
dotnet run --project FlexonCLI/FlexonCLI.csproj -- --help
```

Install the published CLI or library:

```powershell
dotnet tool install --global FlexonCLI --version 3.0.0
dotnet add package Flexon.Core --version 3.0.0
```

## CLI quick start

Encode and decode one JSON value:

```powershell
flexon-cli encode input.json output.flexon
flexon-cli decode output.flexon restored.json
```

Package multiple files and extract them:

```powershell
flexon-cli serialize -i settings.json -i image.png -o package.flexon -c brotli
flexon-cli deserialize -i package.flexon -o restored
```

Use authenticated encryption without placing the password in process arguments:

```powershell
$env:FLEXON_PASSWORD = "use-a-secret-manager-in-production"
flexon-cli serialize -i settings.json -o secure.flexon --encryption AES256 --password-env FLEXON_PASSWORD
flexon-cli deserialize -i secure.flexon -o restored --password-env FLEXON_PASSWORD
```

Supported commands:

| Command | Purpose |
| --- | --- |
| `serialize` | Package one or more JSON or binary files. |
| `deserialize` | Safely extract a package. |
| `encode` / `decode` | Convert a single JSON value. |
| `inspect` | Print decoded content as JSON. |
| `validate` | Validate decoded content against the documented JSON Schema subset. |
| `encrypt` / `decrypt` | Rewrap an existing FLEXON file. |
| `keygen` / `sign` / `verify-signature` | Create and verify detached ECDSA signatures. |
| `benchmark` | Measure a local encode/decode round trip. |

Errors are written to stderr and return a non-zero exit code, making the CLI safe to use in scripts and CI.

## .NET library

```csharp
using System.Text.Json;
using Flexon;

using var json = JsonDocument.Parse("""{"name":"FLEXON","value":null}""");
var bytes = FlexonSerializer.Serialize(json.RootElement, new FlexonOptions
{
    Compression = CompressionMethod.Brotli
});

var restored = FlexonSerializer.Deserialize(bytes);
```

Release packages are published to [NuGet.org](https://www.nuget.org/profiles/jvrsoftware) and GitHub Packages by the verified tag workflow. See [docs/DISTRIBUTION.md](docs/DISTRIBUTION.md) for provenance, recovery, and feed-verification instructions.

## Compatibility

Version 2 is intentionally a new format. The v1 implementation used `0x00` for both `null` and end-of-collection and did not store encryption KDF salts. Consequently:

- New writes always use v2.
- Unencrypted v1 files without ambiguous nested nulls can be read.
- Encrypted v1 files cannot be reliably recovered because the required random salt was never stored.
- Consumers should use `Examples/vectors/` and `docs/FORMAT.md` before implementing another language.

## Repository layout

- `Flexon.Core/` — format, compression, encryption, validation, and legacy reader.
- `FlexonCLI/` — CLI parsing, atomic file writes, and safe extraction.
- `FlexonCLI.Tests/` — correctness, security, compatibility, and CLI tests.
- `Examples/` — maintained examples and deterministic interoperability vectors.
- `docs/` — format, security, signatures, distribution, roadmap, migration, and benchmarking guidance.
- `scripts/` — repeatable release build and verification.
- `.github/workflows/` — cross-platform CI and tagged release automation.

See [SECURITY.md](SECURITY.md), [docs/SIGNATURES.md](docs/SIGNATURES.md), [docs/DISTRIBUTION.md](docs/DISTRIBUTION.md), and [docs/ROADMAP.md](docs/ROADMAP.md).
