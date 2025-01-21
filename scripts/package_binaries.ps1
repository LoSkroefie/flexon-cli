# PowerShell script to package binary files

# Set paths
$binDir = "../FlexonCLI/bin/Release/net8.0"
$outputDir = "../Website/downloads"
$version = "1.2.0"

# Copy DLL to downloads
Copy-Item "$binDir/FlexonCLI.dll" "$outputDir/dll/" -Force

# Package Windows binaries
$winFiles = @(
    "$binDir/FlexonCLI.exe",
    "$binDir/FlexonCLI.dll",
    "$binDir/FlexonCLI.runtimeconfig.json"
)
Compress-Archive -Path $winFiles -DestinationPath "$outputDir/FlexonCLI-v$version-win-x64.zip" -Force

# Package Linux binaries (assuming they're built)
$linuxFiles = @(
    "$binDir/linux-x64/FlexonCLI",
    "$binDir/linux-x64/libFlexonCLI.so",
    "$binDir/linux-x64/FlexonCLI.runtimeconfig.json"
)
Compress-Archive -Path $linuxFiles -DestinationPath "$outputDir/FlexonCLI-v$version-linux-x64.zip" -Force

# Package macOS binaries (assuming they're built)
$macFiles = @(
    "$binDir/osx-x64/FlexonCLI",
    "$binDir/osx-x64/libFlexonCLI.dylib",
    "$binDir/osx-x64/FlexonCLI.runtimeconfig.json"
)
Compress-Archive -Path $macFiles -DestinationPath "$outputDir/FlexonCLI-v$version-osx-x64.zip" -Force

Write-Output "Binary packages created successfully!"
