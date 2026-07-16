# Maintained examples

These examples exercise the FLEXON v2 implementation in this repository.

- `CSharp/` uses the `Flexon.Core` API and is built with the solution.
- `CLI/demo.ps1` runs an encrypted package round trip through the CLI.
- `data/` contains small deterministic input and schema files.
- `bundles/` demonstrates opt-in discovery of a local attachment while leaving a URL and missing output path untouched.

The former language samples only launched the CLI, contained unsupported commands, and were not independent bindings. They were removed rather than presented as implementations. Other-language implementations should start from `docs/FORMAT.md` and shared conformance vectors.
