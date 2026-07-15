# JSON Schema support

The built-in validator intentionally implements a documented subset rather than claiming full JSON Schema compliance.

Supported keywords:

- `type`: null, boolean, object, array, string, number, integer
- `enum`
- Objects: `required`, `properties`, `additionalProperties: false`
- Arrays: `items`, `minItems`, `maxItems`
- Strings: `minLength`, `maxLength`, `pattern`
- Numbers: `minimum`, `maximum`

Regex evaluation uses a one-second timeout. Unsupported keywords are ignored. Applications requiring a specific JSON Schema draft should validate with a dedicated standards-compliant library before writing FLEXON.
