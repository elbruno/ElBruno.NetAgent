# Decisions

## copilot-directive-2026-05-10T11-17-53
### 2026-05-10T11:17:53: User directive
**By:** Bruno Capuano (via Copilot)
**What:** A phase is not complete unless:
- dotnet build -c Release passes with 0 warnings
- dotnet test -c Release --no-build passes
- dotnet run -- --smoke-test passes
**Why:** User requested process rule added to repository and team.

---

## copilot-directive-2026-05-10T15-20-01-04:00
### 2026-05-10T15:20:01-04:00: User directive
**By:** Bruno Capuano (via Copilot)
**What:** A quality gate is not valid unless tests finish quickly.
**Why:** User request — captured for team memory

---

## orion-investigation-2026-05-10T15-58-39
Summary:
- Investigation of vstest/testhost diagnostics found no TestExecution.RecordStart entries without matching RecordEnd/RecordResult in provided logs.
- Suspect integration/hardened tests that touch NotifyIcon/STA (TrayIntegrationTests, TrayIconServiceExitTests_Hardened) can cause OS-level locks; they are already skipped but earlier runs may have executed them.

Directive / Next steps:
1) Prevent native NotifyIcon creation in CI by converting any hardened/integration tests to use INotifyIconAdapter fakes or explicitly Skip them in unit runs.
2) Add timeouts/cancellation to hosted-service test helpers where background loops may run.
3) Add a short post-test check in CI to detect lingering testhost processes and fail fast.

Verification:
- Run: dotnet test --filter FullyQualifiedName=ElBruno.NetAgent.Tests.TrayIconServiceExitTests.ExitClick_ShutdownsHost_And_DisposesTrayResources
- After tests, run PowerShell: Get-Process -Name testhost -ErrorAction SilentlyContinue

Notes:
- No active testhost.exe processes were found on investigation host at time of analysis.
- If a lingering process is observed, Stop-Process -Id <pid> (graceful) then with -Force if necessary; use Sysinternals handle.exe to inspect locked files.

---

## orion-test-fix-2026-05-10T16-10-26.468-0400
Directive: Convert tray tests to adapter-based fakes or mark as integration.

Summary:
- Replace direct use of System.Windows.Forms.NotifyIcon in unit tests with INotifyIconAdapter test doubles to avoid touching native NotifyIcon and interactive WPF.
- Ensure CI uses disable-parallel.runsettings to avoid STA/dispatcher conflicts.
- Quality gate: unit tests must be fast (<30s) and deterministic; integration tests that require OS/GUI should remain skipped.

Files changed (proposed):
- tests/ElBruno.NetAgent.Tests/TrayIconServiceExitTests_Hardened.cs (replaced NotifyIcon usage with TestNotifyIconAdapter)
- tests/ElBruno.NetAgent.Tests/TrayIconServiceExitTests.cs (made TestNotifyIconAdapter.Text non-nullable)
- tests/ElBruno.NetAgent.Tests/SettingsViewModelTests.cs (added null checks to satisfy nullable analysis)

Rationale:
Using an internal INotifyIconAdapter abstraction allows injecting a test double in unit tests and prevents reliance on native NotifyIcon or WPF dispatcher. Keep the hardened/integration tests skipped in CI; they can be un-skipped on interactive machines.

Timestamp: 2026-05-10T16:10:26.468-04:00
Author: Orion (backend)



