# Migrating from v1 to v2

## Why v2 is required

Version 1 used a null byte both as a JSON null value and a collection terminator, causing ambiguous or failed decoding. Its password derivation created a random salt but never stored it, making encrypted files undecryptable. The ChaCha20 writer also did not persist its detached output stream.

Version 2 introduces explicit collection lengths, a magic/version header, stored KDF parameters, authenticated encryption, checksums, limits, and exact file-length validation.

## Migration procedure

1. Back up existing files.
2. Decode each unencrypted v1 file with the v2 CLI.
3. Verify the restored JSON or extracted files against the source.
4. Re-encode with v2.
5. Keep the original until application-level verification is complete.

```powershell
flexon-cli decode old-v1.flexon restored.json
flexon-cli encode restored.json new-v2.flexon
```

Encrypted v1 files cannot be reliably migrated because the random KDF salt was not stored. Ambiguous v1 collections containing nulls also cannot be reconstructed automatically. Recover those records from their original JSON or another trusted backup.
