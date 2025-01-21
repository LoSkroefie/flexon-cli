# PowerShell script to prepare release files

Write-Output "Starting release preparation..."

# 1. Package examples
Write-Output "Packaging examples..."
./package_examples.ps1

# 2. Package binaries
Write-Output "Packaging binaries..."
./package_binaries.ps1

# 3. Generate checksums
Write-Output "Generating checksums..."
$checksums = ./generate_checksums.ps1

# 4. Update downloads.html with checksums
Write-Output "Updating downloads.html with checksums..."
$downloadsPath = "../Website/downloads.html"
$content = Get-Content $downloadsPath -Raw

foreach ($line in $checksums) {
    if ($line -match "^([a-f0-9]{64})\s+(.+)$") {
        $hash = $matches[1]
        $file = $matches[2]
        
        $placeholder = switch -Wildcard ($file) {
            "*win-x64.zip" { "[Windows-ZIP-CHECKSUM]" }
            "*linux-x64.zip" { "[Linux-ZIP-CHECKSUM]" }
            "*osx-x64.zip" { "[MacOS-ZIP-CHECKSUM]" }
            "*FlexonCLI.dll" { "[DLL-CHECKSUM]" }
            "*basic-examples.zip" { "[BASIC-EXAMPLES-CHECKSUM]" }
            "*advanced-examples.zip" { "[ADVANCED-EXAMPLES-CHECKSUM]" }
            "*benchmark-suite.zip" { "[BENCHMARK-SUITE-CHECKSUM]" }
        }
        
        if ($placeholder) {
            $content = $content -replace [regex]::Escape($placeholder), $hash
        }
    }
}

Set-Content $downloadsPath $content

Write-Output "Release preparation completed!"
