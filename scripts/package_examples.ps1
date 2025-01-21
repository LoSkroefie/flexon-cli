# PowerShell script to package example files

# Set paths
$examplesDir = "../Examples"
$outputDir = "../Website/downloads/examples"

# Create basic examples package
Compress-Archive -Path "$examplesDir/*/BasicUsage" -DestinationPath "$outputDir/basic-examples.zip" -Force

# Create advanced examples package
$advancedExamples = @(
    "$examplesDir/*/GameState",
    "$examplesDir/*/AIData",
    "$examplesDir/*/SecureConfig"
)
Compress-Archive -Path $advancedExamples -DestinationPath "$outputDir/advanced-examples.zip" -Force

# Create benchmark suite package
Compress-Archive -Path "$examplesDir/*/Benchmarking" -DestinationPath "$outputDir/benchmark-suite.zip" -Force

Write-Output "Example packages created successfully!"
