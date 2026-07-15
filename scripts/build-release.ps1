param(
    [string]$Configuration = 'Release',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../artifacts')
)

$ErrorActionPreference = 'Stop'
$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$solution = Join-Path $repo 'FlexonCLI.sln'
$output = [System.IO.Path]::GetFullPath($OutputDirectory)

function Invoke-DotNet([string[]]$DotNetArguments) {
    & dotnet @DotNetArguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($DotNetArguments -join ' ') failed with exit code $LASTEXITCODE" }
}

if ($output.StartsWith($repo + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) -and (Test-Path $output)) {
    Remove-Item -LiteralPath $output -Recurse -Force
}
New-Item -ItemType Directory -Path $output -Force | Out-Null

Invoke-DotNet @('restore', $solution, '--disable-parallel')
Invoke-DotNet @('build', $solution, '--configuration', $Configuration, '--no-restore', '-m:1')
Invoke-DotNet @('test', $solution, '--configuration', $Configuration, '--no-build', '-m:1')
Invoke-DotNet @('pack', (Join-Path $repo 'Flexon.Core/Flexon.Core.csproj'), '--configuration', $Configuration, '--no-build', '--output', $output)
Invoke-DotNet @('pack', (Join-Path $repo 'FlexonCLI/FlexonCLI.csproj'), '--configuration', $Configuration, '--no-build', '--output', $output)

$runtimes = @('win-x64', 'linux-x64', 'osx-x64')
foreach ($runtime in $runtimes) {
    $publish = Join-Path $output "publish-$runtime"
    Invoke-DotNet @('publish', (Join-Path $repo 'FlexonCLI/FlexonCLI.csproj'), '--configuration', $Configuration, '-m:1', '--runtime', $runtime, '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:DebugType=None', '--output', $publish)
    Compress-Archive -Path (Join-Path $publish '*') -DestinationPath (Join-Path $output "flexon-cli-$runtime.zip") -Force
    Remove-Item -LiteralPath $publish -Recurse -Force
}

$checksumLines = Get-ChildItem -LiteralPath $output -File | Sort-Object Name | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $($_.Name)"
}
[System.IO.File]::WriteAllLines((Join-Path $output 'SHA256SUMS.txt'), $checksumLines, [System.Text.UTF8Encoding]::new($false))

& (Join-Path $PSScriptRoot 'verify-release.ps1') -ArtifactDirectory $output
if ($LASTEXITCODE -ne 0) { throw "Release verification failed with exit code $LASTEXITCODE" }
