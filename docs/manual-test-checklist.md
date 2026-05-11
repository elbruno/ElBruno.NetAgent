Manual test checklist

1. Start the app:
   - Run: dotnet run --project src/ElBruno.NetAgent
2. Verify tray icon appears in Windows notification area.
   - If you provided assets/icons/netagent.ico it will be used; otherwise the app falls back to a default system icon.
3. Open Status:
   - Right-click tray icon → Open Status (or use the app menu)
   - The Status window should open without errors.
4. Open Settings:
   - Right-click tray icon → Show Config Status or Open Config
   - The Settings window should open without errors.
5. Open Network Selector:
   - Right-click tray icon → Interfaces → select an interface
   - The Network Selector window or adapter preview should open without errors.
6. Preview Best Switch (Dry-run):
   - Right-click tray icon → Preview Best Switch (Dry-run)
   - A balloon tip will show the selected adapter and decision; no system changes will be applied.
7. Exit:
   - Right-click tray icon → Exit
   - Application should request shutdown and exit cleanly.

Notes:
- The app preserves DryRunMode=true and AutoModeEnabled=false by default.
- Tests validate DI resolution and core tray actions without performing any network mutations.
- To provide a custom tray icon, add an .ico at assets/icons/netagent.ico and restart the app.

## Unit tests vs Desktop/Tray integration tests

Unit tests are executed by default in CI using disable-parallel.runsettings which disables parallel execution and excludes desktop-only tests (Category=Desktop or Category=Integration).

Desktop/tray integration tests must be run separately on a desktop-capable runner (Windows with interactive session and UI automation support). To run unit tests locally:

```text
dotnet test -c Release --no-build --settings disable-parallel.runsettings
```

To run desktop/tray tests on a desktop runner (example):

```text
dotnet test -c Release --no-build --filter "Category=Integration|Category=Desktop" --verbosity normal
```
