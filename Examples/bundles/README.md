# Reference-aware bundle example

From the repository root:

```powershell
dotnet run --project FlexonCLI/FlexonCLI.csproj -- serialize `
  -i Examples/bundles/project.json `
  -o Examples/bundles/project.flexon `
  --resolve-files

dotnet run --project FlexonCLI/FlexonCLI.csproj -- deserialize `
  -i Examples/bundles/project.flexon `
  -o Examples/bundles/restored
```

Only `assets/preview.bin` is attached. The HTTPS URL and missing future output are preserved as JSON strings but are not packaged.
