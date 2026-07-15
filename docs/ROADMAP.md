# Evidence-driven roadmap

## Implemented and testable

- Versioned and bounded FLEXON v2 format.
- Authenticated AES-256-GCM and ChaCha20-Poly1305 encryption.
- Detached ECDSA P-256 signatures.
- Deterministic uncompressed interoperability vector.
- JSON Schema subset validation, safe extraction, compression, and cross-platform CLI packages.

## Feasible next releases

1. Independently tested Python and Rust reference implementations using the published vector.
2. Reproducible BenchmarkDotNet comparisons with datasets, hardware metadata, raw output, and CI regression thresholds. Performance claims must be scoped to measured workloads.
3. ML-KEM plus AEAD hybrid encryption and ML-DSA signatures in a new format version, using NIST-standard parameter sets and third-party interoperability vectors.
4. Streaming/chunked envelopes for datasets larger than memory, with per-chunk authentication and recovery boundaries.
5. Optional AI dataset profiles for prompts, embeddings, lineage, and model metadata. These are schemas and storage conventions, not model-generated embeddings or prompt-injection prevention.

## Claims that will not be made

- “Quantum compression” without a real algorithm, implementation, and reproducible evidence.
- Universally faster or smaller than JSON or established binary formats.
- Encryption prevents malicious prompt content.
- A language is supported before its implementation passes the shared vectors and compatibility suite.
