$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$inputFile = Join-Path $repo 'Examples/data/sample.json'
$outputFile = Join-Path $env:TEMP 'flexon-example.flexon'
$extractDir = Join-Path $env:TEMP 'flexon-example-output'
$env:FLEXON_EXAMPLE_PASSWORD = 'example-only-password'

dotnet run --project (Join-Path $repo 'FlexonCLI/FlexonCLI.csproj') -- serialize -i $inputFile -o $outputFile --encryption AES256 --password-env FLEXON_EXAMPLE_PASSWORD
dotnet run --project (Join-Path $repo 'FlexonCLI/FlexonCLI.csproj') -- deserialize -i $outputFile -o $extractDir --password-env FLEXON_EXAMPLE_PASSWORD

Get-Content -Raw (Join-Path $extractDir 'sample.json')
