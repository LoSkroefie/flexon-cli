# PowerShell script to generate SHA-256 checksums for release files

$files = @(
    "../Website/downloads/FlexonCLI-v1.2.0-win-x64.zip",
    "../Website/downloads/FlexonCLI-v1.2.0-linux-x64.zip",
    "../Website/downloads/FlexonCLI-v1.2.0-osx-x64.zip",
    "../Website/downloads/examples/basic-examples.zip",
    "../Website/downloads/examples/advanced-examples.zip",
    "../Website/downloads/examples/benchmark-suite.zip",
    "../Website/downloads/dll/FlexonCLI.dll"
)

foreach ($file in $files) {
    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot $file))
    if (Test-Path $fullPath) {
        $hash = Get-FileHash -Path $fullPath -Algorithm SHA256
        Write-Output "$($hash.Hash)  $($file)"
    } else {
        Write-Output "File not found: $file"
    }
}
