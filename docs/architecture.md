# Architecture

FLEXON separates format mechanics from user interaction.

```text
CLI arguments and filesystem
        |
        v
FlexonCLI: validation, atomic writes, safe extraction, exit codes
        |
        v
Flexon.Core: object codec -> compression -> integrity/encryption envelope
        |
        v
Versioned FLEXON file
```

`Flexon.Core` has no third-party runtime dependencies. `BinaryCodec` handles typed values with explicit lengths. `CompressionCodec` applies the selected standard compression algorithm and enforces the decompressed-size limit. `EnvelopeCodec` creates and verifies the v2 header, checksum, KDF metadata, authenticated encryption, and exact total length. `LegacyV1Reader` is isolated so compatibility cannot silently weaken v2 parsing.

`FlexonCLI` translates commands into core options. It writes through a same-directory temporary file, flushes it, and atomically replaces the destination. Package extraction accepts simple names only and validates the normalized final path.

Reference-aware bundles add a profile layer without changing the v2 envelope. `ReferenceAwareBundle` resolves opt-in JSON references beneath a controlled base directory and produces `FlexonBundle` values. The core serializes attachment data with the native Binary tag and reconstructs bundles only after manifest validation. Bundle extraction prevalidates all logical destinations and hashes, creates safe nested directories, rejects reparse points, then writes the document and attachments atomically per file.

The implementation currently buffers one bounded payload. This makes the authenticated envelope simple and auditable, but it is not intended for multi-gigabyte streaming. A future chunked format would require a new version with independently authenticated frames.
