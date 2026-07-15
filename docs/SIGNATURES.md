# Detached signatures

FLEXON v2 supports detached ECDSA P-256 signatures over exact file bytes. Signatures use SHA-256 and ASN.1 DER encoding. They authenticate who signed a file when the verifier already trusts the public key; they do not encrypt content.

```text
flexon-cli keygen private.pem public.pem
flexon-cli sign package.flexon private.pem package.flexon.sig
flexon-cli verify-signature package.flexon public.pem package.flexon.sig
```

Protect and back up the private key. Distribute public keys through an authenticated channel. Verification returns exit code `0` for a valid signature and `6` for a mismatch.

ECDSA P-256 is classical cryptography, not post-quantum cryptography. A future post-quantum signature mode must use a standardized algorithm such as ML-DSA, receive a new algorithm identifier, include published vectors, and pass cross-implementation tests.
