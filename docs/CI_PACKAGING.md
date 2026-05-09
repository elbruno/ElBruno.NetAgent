# CI Packaging Validation

## Overview

The CI workflow (`.github/workflows/ci.yml`) validates that the ElBruno.NetAgent project produces a well-formed NuGet package. **It does NOT publish to NuGet.**

## What CI Validates

Each CI run (on push to `main` and on pull requests) performs the following steps:

1. **Restore** — `dotnet restore ElBruno.NetAgent.sln`
2. **Build** — `dotnet build ElBruno.NetAgent.sln --configuration Release --no-restore`
3. **Test** — `dotnet test ElBruno.NetAgent.sln --configuration Release --no-build --verbosity normal`
4. **Pack** — `dotnet pack src/ElBruno.NetAgent/ElBruno.NetAgent.csproj --configuration Release --no-build --output artifacts/packages`
5. **Smoke Test** — `./scripts/smoke-test-local-package.ps1` (static inspection only, no network operations)

## What CI Does NOT Do

- **Does NOT publish** to NuGet.org or any NuGet feed
- **Does NOT execute** the application or any network code
- **Does NOT require** secrets or credentials
- **Does NOT mutate** any external state

## Smoke Test Details

The smoke test (`scripts/smoke-test-local-package.ps1`) performs static inspection of the generated `.nupkg`:

- Verifies the package file exists
- Lists package entries
- Confirms `README.md` is present in the package
- Confirms `elbruno-netagent-icon.png` is present in the package
- Parses the `.nuspec` and confirms `<icon>` metadata is present
- Identifies any `.dll` or `.exe` entries inside the package
- Searches source files for "dry" to help confirm dry-run defaults

The smoke test performs ZIP inspection only — no network operations, no package installation, no publishing.

## Artifacts

The generated `.nupkg` is uploaded as a workflow artifact named `packages` (retained for 7 days). Download it from the Actions run page.

## Workflow Triggers

- **Push** to `main` branch
- **Pull request** (any branch)

## Local Validation

To validate locally before pushing:

```powershell
dotnet build .\ElBruno.NetAgent.sln
dotnet test .\ElBruno.NetAgent.sln
dotnet pack .\src\ElBruno.NetAgent\ElBruno.NetAgent.csproj --configuration Release --no-build --output .\artifacts
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-test-local-package.ps1
```
