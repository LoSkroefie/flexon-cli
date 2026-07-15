# Distribution

Tagged releases build and verify the source, tests, NuGet packages, and self-contained Windows, Linux, and macOS archives. The release workflow publishes archives to GitHub Releases, NuGet packages to GitHub Packages, and creates GitHub build-provenance attestations.

NuGet.org publication uses trusted publishing with short-lived credentials. Before the first tag:

1. On NuGet.org, create trusted-publishing policies for owner `LoSkroefie`, repository `flexon-cli`, and workflow `release.yml`.
2. Add the GitHub repository secret `NUGET_USER` containing the NuGet.org profile name, not an email address.
3. Confirm that `Flexon.Core` and `FlexonCLI` are available or already owned by that NuGet.org account.
4. Push and merge the release changes, then create the matching version tag such as `v3.0.0`.

The NuGet.org steps safely skip when `NUGET_USER` is absent. GitHub Packages uses the repository-scoped `GITHUB_TOKEN`.
