Local Package Install / Inspection Smoke Test

Purpose
- Provide a reproducible, local smoke test that inspects the generated NuGet package without publishing or performing network operations.

What the smoke test script does (automated)
- Verifies the .nupkg file exists (default: artifacts\ElBruno.NetAgent.0.1.0.nupkg)
- Lists package entries
- Confirms README.md is present in the package
- Confirms elbruno-netagent-icon.png is present in the package
- Parses the .nuspec and confirms <icon> metadata is present
- Identifies any .dll or .exe entries inside the package
- Performs a quick search of source files for occurrences of the word "dry" to help confirm dry-run defaults

What the smoke test does NOT do
- Does not install the package into system/global NuGet feeds
- Does not publish anything to NuGet.org or other feeds
- Does not change runtime behavior or require admin privileges

Manual checks (recommended)
- If the project includes a WPF/tray UI, manual validation is recommended:
  1. Create a small test project (Console/WPF) that references the generated package as a local source or uses the packaged DLL directly.
  2. Run the app and confirm the agent runs in dry-run mode by default (no external actions).
  3. Ensure any UI/tray launch is opt-in and does not auto-enable "live" network behavior.

How to run the automated smoke test
1. Build/tests/pack (project root):
   dotnet build .\ElBruno.NetAgent.sln
   dotnet test .\ElBruno.NetAgent.sln
   dotnet pack .\src\ElBruno.NetAgent\ElBruno.NetAgent.csproj --configuration Release --no-build --output .\artifacts

2. Run the script (from repo root):
   .\scripts\smoke-test-local-package.ps1

Notes
- The script performs ZIP inspection of the .nupkg file — no network operations.
- Keep live mode blocked by ensuring any runtime flag that enables live actions remains opt-in and disabled by default.
