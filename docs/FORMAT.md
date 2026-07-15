# FLEXON v2 format

All multibyte numbers are little-endian. Readers must reject unknown versions, flags, identifiers, invalid lengths, trailing bytes, and limit violations.

## Envelope header

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 4 | ASCII magic `FLXN` |
| 4 | 1 | Format version (`2`) |
| 5 | 1 | Flags; bit 0 means encrypted, all other bits must be zero |
| 6 | 1 | Compression identifier |
| 7 | 1 | Encryption identifier |
| 8 | 1 | Reserved; must be zero |
| 9 | 4 | PBKDF2 iteration count; zero when unencrypted |
| 13 | 1 | Salt length |
| 14 | 1 | Nonce length |
| 15 | 1 | Authentication-tag length |
| 16 | 1 | Checksum length; 32 when unencrypted, 0 when encrypted |
| 17 | 8 | Stored payload length |

The fixed 25-byte header is followed by an optional SHA-256 checksum, salt, nonce, authentication tag, and stored payload in that order. The total file length must match these declared lengths exactly.

Compression identifiers: `0` none, `1` GZip, `2` Deflate, `3` Brotli.

Encryption identifiers: `0` none, `1` AES-256-GCM, `2` ChaCha20-Poly1305.

For encrypted files, salt is 16 bytes, nonce is 12 bytes, tag is 16 bytes, and PBKDF2-SHA256 derives a 32-byte key. Associated authenticated data is `header || salt || nonce`. Encrypted files omit the redundant plaintext checksum to avoid leaking a stable payload fingerprint; authentication is provided by the AEAD tag. Unencrypted files store SHA-256 of the compressed payload. The stored payload is ciphertext when encrypted and compressed plaintext otherwise.

## Binary value payload

The decompressed payload contains exactly one value. Lengths and counts are signed 32-bit integers and must be non-negative and within configured limits.

| Tag | Value | Encoding |
| ---: | --- | --- |
| `0` | Null | No body |
| `1` | False | No body |
| `2` | True | No body |
| `3` | Signed integer | 64-bit integer |
| `4` | Unsigned integer | 64-bit integer |
| `5` | Floating point | IEEE 754 binary64 |
| `6` | Decimal | Four 32-bit values from the .NET decimal representation |
| `7` | String | 32-bit UTF-8 byte length, then bytes |
| `8` | Binary | 32-bit byte length, then bytes |
| `9` | DateTime | 64-bit .NET `DateTime.ToBinary()` value |
| `10` | DateTimeOffset | 64-bit ticks, then signed 16-bit offset minutes |
| `11` | UUID | 16 bytes in .NET `Guid.ToByteArray()` order |
| `12` | Array | 32-bit item count, then that many values |
| `13` | Object | 32-bit property count; each property is a length-prefixed UTF-8 key followed by one value |

Object keys must be unique. A decoder must consume the payload exactly. This explicit framing replaces the ambiguous v1 null/end sentinel.

## Compatibility

Writers must emit v2. Readers may offer a separate v1 compatibility path, but v1 is not part of this specification. Any future incompatible change requires a new version byte.
