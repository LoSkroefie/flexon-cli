param([Parameter(Mandatory)][string]$ArtifactDirectory)

$ErrorActionPreference = 'Stop'
$directory = [System.IO.Path]::GetFullPath($ArtifactDirectory)
$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
[xml]$props = Get-Content -Raw (Join-Path $repo 'Directory.Build.props')
$version = [string]$props.Project.PropertyGroup.VersionPrefix
$required = @(
    "Flexon.Core.$version.nupkg",
    "FlexonCLI.$version.nupkg",
    'flexon-cli-win-x64.zip',
    'flexon-cli-linux-x64.zip',
    'flexon-cli-osx-x64.zip',
    'SHA256SUMS.txt'
)

foreach ($name in $required) {
    $path = Join-Path $directory $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing release artifact: $name" }
    if ((Get-Item -LiteralPath $path).Length -eq 0) { throw "Empty release artifact: $name" }
}

$expected = @{}
Get-Content -LiteralPath (Join-Path $directory 'SHA256SUMS.txt') | ForEach-Object {
    if ($_ -match '^([a-f0-9]{64})  (.+)$') { $expected[$matches[2]] = $matches[1] }
}
foreach ($name in $required | Where-Object { $_ -ne 'SHA256SUMS.txt' }) {
    $actual = (Get-FileHash -LiteralPath (Join-Path $directory $name) -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($expected[$name] -ne $actual) { throw "Checksum mismatch: $name" }
}

Write-Output "Verified $($required.Count - 1) artifacts and checksums in $directory"
