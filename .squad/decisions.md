# Squad Decisions

## Active Decisions

No decisions recorded yet.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

\n### astra-phase1-fix.md — 2026-05-09T14:38:55.7601155-04:00

Fix for Phase 1 syntax errors and safe tray service behavior

What I changed:
- Fixed Program.cs to use a synchronous STA Main and proper host start/stop pattern compatible with net9.0-windows.
- Updated TrayIconService to safely create Windows Forms NotifyIcon via WPF dispatcher when an Application is present; avoided UI actions when no Application exists.
- Adjusted tests to valid C# syntax and kept them non-UI.

Safety considerations:
- No network calls or system modifications were added.
- Tray icon creation is skipped when no WPF Application is present to keep tests and non-UI hosts safe.
- Exit menu triggers host stop via IHostApplicationLifetime.StopApplication when available.

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>



\n### astra-phase1.md — 2026-05-09T14:36:08.9723414-04:00

Phase 1 decisions by Astra

- Implemented tray-first WPF host using Microsoft.Extensions.Hosting
- Used SystemIcons.Application as runtime tray icon; added placeholder asset files
- Auto Mode toggle is visual-only and defaults to off



\n### copilot-directive-2026-05-09T162308.md — 2026-05-09T16:24:01.1907983-04:00

### 2026-05-09T16:23:08Z: User directive
**By:** (captured via Squad)
**What:** Our goal is not only to make the code work, but also to preserve long-term repository quality and architectural consistency.

Core engineering principles:

1. Build and test quality
- A phase is NOT complete if build warnings remain.
- Always finish with:
  - dotnet build -c Release
  - dotnet test -c Release --no-build
- Treat warnings as defects unless explicitly documented and justified.
- Prefer fixing warnings immediately instead of postponing them.

2. Avoid duplication
- Before creating a file, search the repository for similar files/classes/interfaces.
- Never create duplicate implementations, duplicate interfaces, or duplicate models.
- Keep one authoritative implementation for each service/interface/model.
- Prefer refactoring and moving files over copying files.
- Avoid namespace drift and folder duplication.

3. Architecture consistency
- Keep authoritative abstractions under:
  - Core = interfaces/models/contracts
  - Services = implementations
  - Tests = tests only
- Do not create parallel project structures accidentally.
- Preserve clear dependency direction:
  - Tests -> Services/Core
  - Services -> Core
  - Core -> no service dependencies

4. Safety constraints
- NEVER mutate real network state.
- NEVER disable/enable adapters.
- NEVER execute netsh or PowerShell network commands.
- Preserve DryRunMode.
- Keep AutoMode disabled by default.
- Dangerous operations must remain abstraction-only or dry-run only.

5. Commit quality
- Prefer fewer meaningful commits over many corrective commits.
- Avoid “fixup after fixup” loops.
- Before committing:
  - inspect git diff
  - check for accidental duplication
  - verify namespaces/folders
- Use clear commit messages.

6. Autonomous workflow discipline
- Work one phase at a time.
- Do not start the next phase until:
  - build passes
  - tests pass
  - warnings are resolved
  - git status is clean
- If architectural inconsistencies appear:
  - stop feature work
  - repair the architecture first

7. Testing expectations
- Add tests for all new logic.
- Prefer deterministic unit tests with mocks/fakes.
- Integration tests requiring real system mutation must remain skipped/placeholders.
- Avoid reflection-heavy fragile tests unless absolutely necessary.

8. Token efficiency
- Avoid unnecessary repository-wide rereads.
- Avoid recursive agent loops.
- Avoid repeated analysis of unchanged files.
- Keep prompts and execution focused on the active phase.

9. Human escalation rules
- Stop and ask for human review if:
  - duplicate architecture appears
  - a refactor affects multiple projects
  - real system mutation becomes necessary
  - build/test failures persist after 2 repair attempts
  - transient API failures create unstable execution behavior

10. Final quality gate before stopping
Always end with:
- clean git status
- successful Release build
- successful tests
- warning summary
- files changed summary
- architecture consistency check
- explicit confirmation that no real network mutation exists

**Captured at:** 2026-05-09T16:23:08 (local)



\n### copilot-directive-model-gpt5mini-2026-05-09T162320.md — 2026-05-09T16:25:15.4593145-04:00

### 2026-05-09T16:23:20Z: User directive
**By:** (captured via Squad)
**What:** Enforce use of gpt-5-mini for all agent spawns in this repository — gpt-5-mini is the only allowed model here.

**Why:** User reported agents were spawned with other models (claude-haiku-4.5). Set a persistent default model to avoid future mismatches.

Captured at: 2026-05-09T16:23:20 (local)



\n### orion-config-phase2.md — 2026-05-09T15:02:15.9265008-04:00

Decision: Implement Phase 2 configuration system

Summary:
- Add strongly-typed NetAgentOptions with DryRunMode preserved.
- Implement IConfigurationService and a ConfigurationService that reads/writes config.json under %LOCALAPPDATA%\ElBruno.NetAgent.
- Create a docs/config.sample.json showing defaults.

Rationale:
- Keep config handling simple, local only, no network calls.
- Provide OpenConfigFile/OpenConfigFolder helpers for tray menu.
- Create defaults when missing and validate simple numeric values.

Consequences:
- DryRunMode default is true to avoid accidental system changes until user disables it.
- ConfigurationService will log warnings for invalid config and continue with safe defaults.

Date: 2026-05-09
Author: Orion



\n### orion-inventory-phase3.md — 2026-05-09T15:11:50.9993129-04:00

Decisions for Phase 3 - Network inventory

- USB tethering detection is heuristic-only: match description/name for keywords (Remote NDIS, RNDIS, USB, Android, Pixel, Mobile), require interface to be Ethernet, up, and have a gateway. No external checks or system changes.
- Classifier uses conservative heuristics for Virtual/VPN/Bluetooth/Cellular to keep behavior testable.
- Service registered as singleton; it's stateless and inexpensive to call.


