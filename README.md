# ElBruno.NetAgent

Windows-first network quality agent for developers, travelers, presenters, and anyone who has ever trusted hotel Wi-Fi and regretted it 30 seconds later.

ElBruno.NetAgent is a lightweight .NET 10 WPF system tray application that monitors available network interfaces and helps Windows prefer the best connection based on real-time quality checks.

Initial scenario:

- Hotel Wi-Fi is connected.
- A Google Pixel or other phone is connected through USB-C tethering.
- ElBruno.NetAgent monitors both options.
- The app helps switch to the better path when the current connection becomes unstable.

## Status

**Phase 10 — Hard Safety Review Complete.**

This application is **dry-run only**. No real network changes are executed.

### Safety Guarantees

- `DryRunMode` defaults to `true` — enforced by configuration validation
- `LiveModeAllowed` defaults to `false` — explicit opt-in required for live execution
- `AutoModeEnabled` defaults to `false` — automatic switching requires explicit opt-in
- The `WindowsNetworkController` enforces a hard safety gate: even if `NetworkSwitchMode.Live` is requested, it will be forced to `DryRun` unless `LiveModeAllowed` is explicitly `true`
- Live mode is **not yet implemented** — even with `LiveModeAllowed = true`, the controller returns "not yet implemented"
- Configuration validation rejects any config that enables live mode
- All audit log entries clearly mark whether they are dry-run or live

## Repository goals

This repository is designed to be implemented with GitHub Copilot, SQUAD, and local models.

Recommended first command after repository creation:

```text
Read docs/IMPLEMENTATION.md and docs/SQUAD_EXECUTION_PROMPT.md.
Implement Phase 0 only.
Do not continue to Phase 1 until the build passes and the README has been updated with the current state.
```

## Main documents

- `docs/PRD.md`
- `docs/ARCHITECTURE.md`
- `docs/IMPLEMENTATION.md`
- `docs/REPO_RULES.md`
- `docs/PUBLISHING.md`
- `docs/PROMOTION.md`
- `docs/IMAGE_PROMPTS.md`
- `docs/SQUAD_EXECUTION_PROMPT.md`
- `docs/ROADMAP.md`
- `docs/SECURITY.md`

## License

MIT License.
