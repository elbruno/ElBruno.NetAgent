Manual test: Exit behavior

Purpose
- Verify that clicking "Exit" disposes the tray icon, requests host shutdown, closes WPF windows, and the process exits cleanly.

Steps
1. Run ElBruno.NetAgent (normal run, not --smoke-test) on a Windows machine with UI.
2. Locate the tray icon for ElBruno.NetAgent and right-click to open the context menu.
3. Click "Exit".

Expected
- No unhandled exceptions.
- The tray icon disappears immediately.
- Hosted services receive a stop request and stop within ~5s.
- Any open windows are closed.
- Application process exits.

Notes
- This project runs in dry-run mode by default. The Exit flow is log-only and will not modify network adapters.
- If the host does not stop within 5 seconds, the host StopAsync will be canceled and a warning logged.
