# Releasing the NuGet packages

GitHub Releases are the authoritative changelog, and the release tag is the single source of package versions. The `0.1.0` version in the project files is a local-build fallback only.

Every release publishes three packages with the same version:

- `TheBuilder.AIImageDisclosure`
- `TheBuilder.AIImageDisclosure.OpenAI`
- `TheBuilder.AIImageDisclosure.OpenAI.UmbracoAI`

## One-time setup

1. Create the `nuget` GitHub environment.
2. Add a `NUGET_USER` environment secret containing the nuget.org username `the_builder`.
3. Add required reviewers to the environment if publishing should require explicit approval.
4. Configure a trusted publishing policy on nuget.org with:
   - Package owner: `the_builder`
   - Publisher: GitHub Actions
   - Repository owner: `thebuilder`
   - Repository: `umbraco-ai-image-disclosure`
   - Workflow: `publish-nuget.yml`
   - Environment: `nuget`

The trusted publishing policy must be active before the first release. It exchanges GitHub's short-lived OIDC token for a temporary NuGet API key, so no long-lived NuGet API key is stored in GitHub.

## Pre-release verification

Before creating a release:

1. Confirm all CI jobs pass for the commit to publish, including Umbraco 17.3, the latest 17.x, the latest 18.x, and both OpenAI integration targets.
2. Run `dotnet list TheBuilder.AIImageDisclosure.slnx package --vulnerable --include-transitive` and resolve any runtime high or critical findings.
3. Inspect the packages produced by CI before approving the `nuget` environment deployment.

## Publish a prerelease

1. Create a GitHub Release from the commit to publish.
2. Give it a unique prerelease tag such as `v0.1.0-preview.1`.
3. Select **Set as a pre-release**.
4. Add the release notes.
5. Publish the release.

The workflow validates that the tag is SemVer, applies it to all three NuGet packages and the Umbraco package manifest, links the NuGet release notes to the matching GitHub Release, runs the test suites, packs and validates the packages, uploads them as a workflow artifact, and publishes them to nuget.org after any configured environment approval.

NuGet package versions are immutable. Increment the prerelease number for every publish, even when a previous prerelease is unlisted.

## Publish a stable release

1. Create a GitHub Release from the commit to publish.
2. Use a new stable SemVer tag such as `v0.1.0`.
3. Ensure **Set as a pre-release** is not selected.
4. Add the release notes.
5. Publish the release.

The GitHub Release type and version must agree: prereleases require a prerelease version, and stable releases require a stable version.

## Retrying a failed publish

The publish job pushes packages in dependency order and skips versions that already exist on nuget.org. If a push fails partway, re-run the failed job and it will publish only the missing packages.

## Rollback

NuGet packages cannot be overwritten or deleted. If a release is faulty:

1. Unlist the affected versions on nuget.org.
2. Fix the issue on `main` and let CI complete.
3. Publish a new version. Never reuse the affected version number.
4. Document the replacement version in the affected GitHub Release.
