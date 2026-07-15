# FLEXON v2 interoperability vectors

`v2-package-none.json` is packaged under the UTF-8 key `sample.json`, with no compression and no encryption. The exact resulting FLEXON bytes are stored as lowercase hexadecimal in `v2-package-none.hex`.

Implementations in other languages must decode the vector to the original JSON value. Deterministic writers should also reproduce the exact bytes. Compression and authenticated encryption are intentionally excluded because compressed representations may vary and secure encryption must use random salt and nonce values.
