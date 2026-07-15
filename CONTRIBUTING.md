# Contributing

## Development requirements

- .NET 8 SDK or newer.
- PowerShell 7 for release scripts.
- A branch based on the current `main` branch.

## Required checks

```powershell
dotnet restore FlexonCLI.sln
dotnet build FlexonCLI.sln --configuration Release --no-restore
dotnet test FlexonCLI.sln --configuration Release --no-build
dotnet pack FlexonCLI.sln --configuration Release --no-build
```

Warnings are treated as errors. Every format or security change must include tests for success, malformed input, boundary limits, and compatibility consequences.

## Compatibility rules

- Never change the meaning of an existing v2 byte without incrementing the format version.
- Add golden vectors when a new value type or envelope field is introduced.
- Reject unknown flags and invalid lengths instead of guessing.
- Do not weaken KDF limits, authentication, extraction containment, or decoder bounds.
- Do not add performance or availability claims without reproducible public evidence.

## Pull requests

Describe the user-visible change, security impact, compatibility impact, commands used to verify it, and any remaining limitations. Do not commit build outputs, packages, downloaded dependencies, benchmark artifacts, or secrets.
