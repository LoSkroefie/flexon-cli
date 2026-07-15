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
