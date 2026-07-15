# Changelog

All notable changes are documented here. This project follows semantic versioning.

## [Unreleased]

### Fixed

- Release publication now enumerates NuGet packages explicitly on PowerShell instead of passing an unexpanded wildcard to `dotnet nuget push`.
- A failed tagged publication can be safely resumed with a manual tag input; existing GitHub Release assets are replaced while package feeds use duplicate-safe pushes.
- Distribution documentation now records the actual NuGet package owner, repository owner, trusted-publishing policy, recovery command, and live verification steps.

## [3.0.0] - 2026-07-15

### Added

- Versioned FLEXON v2 envelope with magic header, explicit lengths, checksum, and bounded decoding.
- Reusable `Flexon.Core` library and properly configured `FlexonCLI` .NET tool package.
- AES-256-GCM and ChaCha20-Poly1305 authenticated encryption with stored random salt and nonce.
- Atomic output writes and safe package extraction.
- Consistent non-zero CLI exit codes.
- Automated correctness, corruption, encryption, legacy, schema, and traversal tests.
- Cross-platform CI and tag-driven release workflows.
- Formal format, security, architecture, migration, and benchmark-methodology documentation.
- Detached ECDSA P-256 signatures with CLI and library APIs.
- Deterministic interoperability vectors enforced by the test suite.
- GitHub Packages, NuGet.org trusted-publishing, and build-provenance release automation.

### Changed

- Collections are length-prefixed, so nested nulls round-trip correctly.
- Package and CLI versioning is unified at `3.0.0`; the on-disk format version remains v2.
- Unsupported quantum, TripleDES, package-manager, binding, and benchmark claims were removed.
- Generated binaries, archives, Java class files, and benchmark output are no longer source-controlled.

### Compatibility

- New writes use v2.
- Unencrypted, unambiguous v1 files remain readable.
- Encrypted v1 files are unrecoverable because v1 did not store the random KDF salt.

## [1.1.0] - 2025-01-15

- Initial public release. Retained for historical reference; v2 replaces its file writer.

[Unreleased]: https://github.com/LoSkroefie/flexon-cli/compare/v3.0.0...HEAD
[3.0.0]: https://github.com/LoSkroefie/flexon-cli/compare/v1.1.0...v3.0.0
[1.1.0]: https://github.com/LoSkroefie/flexon-cli/releases/tag/v1.1.0
