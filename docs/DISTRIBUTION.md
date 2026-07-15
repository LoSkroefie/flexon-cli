# Distribution

Tagged releases build and verify the source, tests, NuGet packages, and self-contained Windows, Linux, and macOS archives. The release workflow publishes archives to GitHub Releases, NuGet packages to GitHub Packages and NuGet.org, and creates GitHub build-provenance attestations.

NuGet.org publication uses trusted publishing with short-lived credentials. Before the first tag:

1. On NuGet.org, create a trusted-publishing policy with package owner `jvrsoftware`, GitHub repository owner `LoSkroefie`, repository `flexon-cli`, and workflow file `release.yml`. No GitHub environment is used.
2. Add the GitHub repository secret `NUGET_USER` with value `jvrsoftware`, the NuGet.org profile name rather than an email address.
3. Confirm that `FlexonCLI` is owned by `jvrsoftware` and that the new package ID `Flexon.Core` is available.
4. Push and merge the release changes, then create the matching version tag such as `v3.0.0`.

The NuGet.org steps safely skip when `NUGET_USER` is absent. GitHub Packages uses the repository-scoped `GITHUB_TOKEN`; no long-lived NuGet API key is stored in GitHub.

## Recover a partially published tag

Fix and merge the workflow on `main`, then manually dispatch it against the existing tag:

```powershell
gh workflow run release.yml --repo LoSkroefie/flexon-cli -f tag=v3.0.0
```

Manual recovery checks out and rebuilds the tagged commit, not the current branch. It uploads GitHub Release assets with `--clobber` when the release already exists and pushes each `.nupkg` separately with `--skip-duplicate`. Published tags must never be moved to incorporate later workflow or documentation commits.

## Verify publication

Do not treat a successful workflow as the only proof. Verify all channels:

```powershell
gh release view v3.0.0 --repo LoSkroefie/flexon-cli
dotnet tool search FlexonCLI --detail
dotnet package search Flexon.Core --exact-match
```

Also open the [FlexonCLI](https://www.nuget.org/packages/FlexonCLI) and [Flexon.Core](https://www.nuget.org/packages/Flexon.Core) NuGet pages after indexing. Download the published packages, confirm that their package metadata and README render, and compare their SHA-256 hashes with the verified workflow artifacts where the feed preserves the exact package bytes.
