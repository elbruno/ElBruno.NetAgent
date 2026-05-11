# ElBruno.NetAgent

Windows-first network quality agent for developers, travelers, presenters, and anyone who has ever trusted hotel Wi-Fi and regretted it 30 seconds later.

ElBruno.NetAgent is a lightweight .NET 10 WPF system tray application that monitors available network interfaces and helps Windows prefer the best connection based on real-time quality checks.

Initial scenario:

- Hotel Wi-Fi is connected.
- A Google Pixel or other phone is connected through USB-C tethering.
- ElBruno.NetAgent monitors both options.
- The app helps switch to the better path when the current connection becomes unstable.

## Status

Planning / initial implementation blueprint.

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
\nPhase 0 scaffold: minimal WPF solution added.

Smoke test

To run a lightweight smoke-test that builds the Host, resolves core services, and exits without starting the WPF run loop or background hosted services, run:

```text
dotnet run --project src\ElBruno.NetAgent -- --smoke-test
```

The smoke-test attempts to resolve these services: IConfigurationService, INetworkInventoryService, INetworkQualityMonitor, and IDecisionEngine. It exits with code 0 on success, non-zero on failure. No network changes are performed.
