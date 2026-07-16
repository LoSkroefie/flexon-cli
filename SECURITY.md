# Security policy

Report suspected vulnerabilities privately through GitHub Security Advisories. Do not open a public issue containing exploit details or real secrets.

## Supported version

Only FLEXON 2.x receives security fixes. Version 1 files are supported only through the constrained unencrypted compatibility reader.

## Encryption model

FLEXON v2 supports AES-256-GCM and ChaCha20-Poly1305. Both authenticate the ciphertext and envelope metadata. Passwords are derived with PBKDF2-SHA256 using a random 16-byte salt stored in the file and at least 100,000 iterations; the default is 210,000.

Password-based encryption cannot compensate for a weak password. Use a password manager or secret store, prefer `--password-env` or `--password-file`, restrict access to password files, and avoid command-line passwords because process arguments may be visible to other users.

## Decoder boundaries

The decoder rejects unknown versions and flags, invalid lengths, duplicate object keys, trailing bytes, checksum failures, authentication failures, excessive nesting, oversized values, oversized collections, and decompression beyond the configured limit. Package extraction accepts simple filenames only and checks final path containment.

FLEXON v2 currently buffers a bounded payload in memory so authenticated encryption can be applied to the whole message. The default maximum is 512 MiB. Applications handling untrusted data should lower limits to the smallest practical values.

## Reference-aware bundle boundaries

Automatic attachment discovery is opt-in. It only includes existing relative file references beneath the configured base directory; URLs, missing paths, rooted paths, traversal, unsafe portable names, and paths outside the root are not included. Marked `$flexonFile` references are stricter and fail when missing or unsafe. Discovery rejects symbolic-link/reparse-point traversal, and extraction rejects reparse points and normalized destinations outside its output root.

Use `--attachments marked` or explicit `--attach` values for security-sensitive packaging. Run `--dry-run` before discovery when JSON comes from another party. Review attachment paths and hashes, keep the base directory narrow, and set `--max-attachment-bytes` to the smallest useful limit. Explicit attachment sources are an authorization decision by the caller and may originate outside the JSON base directory, but their logical package paths remain constrained.
