# ElBruno.NetAgent – Implementation Plan for Copilot, SQUAD, and Local Models

## 1. Purpose

This document is the primary execution plan for GitHub Copilot CLI, SQUAD, and local Ollama models.

The implementation must be incremental. Do not build everything in a single pass.

This repository is intentionally designed to be implemented with a hybrid workflow:

```text
Cloud model = planning and architecture
Local model = implementation and validation
```

The expected local environment is:

- Windows VM
- CPU-only
- GitHub Copilot CLI
- SQUAD
- Ollama
- Local models
- GitHub Copilot Free Tier used sparingly

## 2. Global implementation rules

1. Use .NET 10.
2. Use WPF.
3. Use latest stable package versions available at implementation time.
4. Keep code simple.
5. Keep classes small.
6. Add interfaces for core services.
7. Avoid large dependencies.
8. Use async APIs where appropriate.
9. Do not block the UI thread.
10. Do not implement future phases early.
11. Do not silently ignore errors.
12. Log important decisions.
13. Commit after each completed phase or micro-task.
14. Build and test after each completed phase or micro-task.
15. Verify file system changes after every local-model task.
16. Do not trust model summaries as proof that files were created.

## 3. Local model execution rules

This repository is expected to be implemented with GitHub Copilot CLI, SQUAD, and local Ollama models running on a CPU-only VM.

Local models may understand the requested task but fail to actually modify files or execute commands unless the prompt explicitly requires tool usage.

### 3.1 File creation must be verified

A response saying that files were created is not enough.

After every scaffold or implementation task, verify the repository state with:

```powershell
git status
Get-ChildItem -Recurse -Depth 3
```

For Phase 0, the following files and folders must exist:

```text
ElBruno.NetAgent.sln
src/ElBruno.NetAgent/ElBruno.NetAgent.csproj
tests/ElBruno.NetAgent.Tests/ElBruno.NetAgent.Tests.csproj
```

If those files do not exist, Phase 0 is not complete.

### 3.2 Scaffold tasks must use terminal commands

For repository setup tasks, do not ask the model only to “create the solution” or “create the projects”.

Bad prompt:

```text
Create the solution, WPF project, and test project.
```

Good prompt:

```text
Use the terminal/shell tool to execute the following PowerShell commands exactly.

Do not only describe the commands.
Do not say files were created unless the commands actually ran.
After running the commands, verify with git status.
```

### 3.3 Prefer deterministic commands for scaffolding

For Phase 0, use deterministic .NET CLI commands.

Required commands:

```powershell
dotnet new sln -n ElBruno.NetAgent

New-Item -ItemType Directory -Force -Path src
New-Item -ItemType Directory -Force -Path tests

dotnet new wpf -n ElBruno.NetAgent -o src/ElBruno.NetAgent
dotnet new xunit -n ElBruno.NetAgent.Tests -o tests/ElBruno.NetAgent.Tests

dotnet sln ElBruno.NetAgent.sln add src/ElBruno.NetAgent/ElBruno.NetAgent.csproj
dotnet sln ElBruno.NetAgent.sln add tests/ElBruno.NetAgent.Tests/ElBruno.NetAgent.Tests.csproj

dotnet build
dotnet test
git status
```

### 3.4 Keep local model prompts small

CPU-only local models work best with small, direct prompts.

Avoid:

```text
Read PRD, ARCHITECTURE, IMPLEMENTATION, SQUAD, and implement the app.
```

Prefer:

```text
Read docs/IMPLEMENTATION.md.

Focus only on Phase 0.

Use the terminal/shell tool to run the commands listed in the Phase 0 scaffold section.

Do not implement other phases.
Stop after Phase 0.
```

### 3.5 Do not trust simulated execution

Some local models may return a convincing summary such as:

```text
Files created:
- src/WpfApp.sln
- src/WpfApp.csproj
- src/WpfApp.Tests.csproj
```

This is not valid unless the files exist on disk.

Always verify with:

```powershell
git status
Get-ChildItem -Recurse -Depth 3
```

### 3.6 One task, one verification

Every local-model task must end with:

```text
Return:
- commands executed
- files created or modified
- build result
- test result
- git status summary
```

If the model cannot verify the task, the task is not complete.

## 4. Recommended local model workflow

Use this workflow for every implementation step:

```text
1. Select one small task.
2. Provide explicit target files.
3. Provide explicit constraints.
4. Ask the model to execute commands or modify files.
5. Verify with git status.
6. Run dotnet build.
7. Run dotnet test if applicable.
8. Commit.
9. Continue to the next micro-task.
```

Recommended maximum task size:

```text
1 to 3 files changed per task
```

Recommended maximum prompt scope:

```text
1 phase or 1 file
```

## 5. Phase 0 – Repository scaffold

### Objective

Create the .NET solution, WPF application project, test project, and basic repository structure.

### Agent

Recommended SQUAD agents:

```text
Seldon + Tessa
```

### Tasks

- Create solution file.
- Create `src` folder.
- Create `tests` folder.
- Create WPF project.
- Create test project.
- Add projects to solution.
- Build solution.
- Run tests.
- Verify repository state.

### Required commands

Use the terminal/shell tool to execute these PowerShell commands exactly:

```powershell
dotnet new sln -n ElBruno.NetAgent

New-Item -ItemType Directory -Force -Path src
New-Item -ItemType Directory -Force -Path tests

dotnet new wpf -n ElBruno.NetAgent -o src/ElBruno.NetAgent
dotnet new xunit -n ElBruno.NetAgent.Tests -o tests/ElBruno.NetAgent.Tests

dotnet sln ElBruno.NetAgent.sln add src/ElBruno.NetAgent/ElBruno.NetAgent.csproj
dotnet sln ElBruno.NetAgent.sln add tests/ElBruno.NetAgent.Tests/ElBruno.NetAgent.Tests.csproj

dotnet build
dotnet test
git status
```

### Expected files

After Phase 0, these files must exist:

```text
ElBruno.NetAgent.sln
src/ElBruno.NetAgent/ElBruno.NetAgent.csproj
tests/ElBruno.NetAgent.Tests/ElBruno.NetAgent.Tests.csproj
```

### Acceptance criteria

- `dotnet build` succeeds.
- `dotnet test` succeeds.
- `git status` shows expected created files.
- No networking logic is implemented.
- No tray logic is implemented.
- No business logic is implemented.

### Commit message

```text
chore: scaffold .NET solution
```

## 6. Phase 1 – WPF tray shell

### Objective

Create a tray-first WPF application shell.

### Agent

Recommended SQUAD agent:

```text
Bayta
```

### Tasks

- Configure WPF app startup.
- Integrate `Microsoft.Extensions.Hosting`.
- Register dependency injection.
- Add `TrayIconService`.
- Use `System.Windows.Forms.NotifyIcon`.
- Start minimized to tray.
- Add a basic tray context menu.
- Add clean shutdown.

### Initial tray menu

```text
ElBruno.NetAgent
Status: Starting
---
Open Status
Refresh Now
Open Logs
Open Config
Exit
```

### Constraints

- Do not implement network detection.
- Do not implement config reload.
- Do not implement decision engine.
- Do not implement network switching.
- Keep the tray shell minimal.

### Recommended micro-task 1

```text
Add Microsoft.Extensions.Hosting to the WPF app and configure application startup.
```

Target files:

```text
src/ElBruno.NetAgent/App.xaml
src/ElBruno.NetAgent/App.xaml.cs
src/ElBruno.NetAgent/ElBruno.NetAgent.csproj
```

### Recommended micro-task 2

```text
Add TrayIconService with a placeholder tray icon and Exit menu item.
```

Target files:

```text
src/ElBruno.NetAgent/UI/Tray/TrayIconService.cs
src/ElBruno.NetAgent/App.xaml.cs
```

### Acceptance criteria

- App starts.
- App appears in the Windows tray.
- App does not show the main window by default.
- Exit works from tray menu.
- `dotnet build` succeeds.

### Commit message

```text
feat: add WPF tray shell
```

## 7. Phase 2 – Configuration system

### Objective

Add strongly typed configuration.

### Agent

Recommended SQUAD agents:

```text
Golan + Bayta
```

### Tasks

- Create `NetAgentOptions`.
- Load config from `%LOCALAPPDATA%\ElBruno.NetAgent\config.json`.
- Create default config if missing.
- Add validation.
- Add sample config in `docs/config.sample.json`.
- Add tray menu item to open config file.
- Add tray menu item to open app data folder.

### Target files

```text
src/ElBruno.NetAgent/Core/Options/NetAgentOptions.cs
src/ElBruno.NetAgent/Services/Configuration/ConfigurationService.cs
src/ElBruno.NetAgent/UI/Tray/TrayIconService.cs
docs/config.sample.json
```

### Acceptance criteria

- Missing config is created.
- Invalid config logs warning.
- Default values are used safely.
- Config path is documented.
- `dotnet build` succeeds.

### Commit message

```text
feat: add configuration system
```

## 8. Phase 3 – Network inventory

### Objective

Detect and classify network interfaces.

### Agent

Recommended SQUAD agents:

```text
Golan + Hober
```

### Tasks

- Add models: `NetworkInterfaceInfo`, `NetworkAdapterKind`, `NetworkOperationalState`.
- Add service: `INetworkInventoryService`, `NetworkInventoryService`.
- Use `NetworkInterface.GetAllNetworkInterfaces()`.
- Detect Wi-Fi, Ethernet, USB tethering heuristics, Virtual/VPN, Loopback.
- Add tests for classification logic.
- Show detected interfaces in logs.
- Show detected interfaces in tray submenu.

### Target files

```text
src/ElBruno.NetAgent/Core/Models/NetworkInterfaceInfo.cs
src/ElBruno.NetAgent/Core/Enums/NetworkAdapterKind.cs
src/ElBruno.NetAgent/Services/Network/INetworkInventoryService.cs
src/ElBruno.NetAgent/Services/Network/NetworkInventoryService.cs
tests/ElBruno.NetAgent.Tests/NetworkAdapterClassificationTests.cs
```

### Acceptance criteria

- Active interfaces are listed.
- Wi-Fi is identified.
- USB tethering appears as candidate if connected.
- Virtual adapters are not preferred by default.
- Tests pass.

### Commit message

```text
feat: detect network interfaces
```

## 9. Phase 4 – Network quality monitor

### Objective

Measure connection quality.

### Agent

Recommended SQUAD agents:

```text
Golan + Hober
```

### Tasks

- Add models: `NetworkQualitySnapshot`, `EndpointPingResult`, `NetworkQualityLevel`.
- Add service: `INetworkQualityService`, `PingNetworkQualityService`.
- Ping configured endpoints.
- Calculate average latency, min latency, max latency, packet loss, quality level, quality score.
- Add rolling history per interface.
- Log summary every cycle.
- Avoid noisy logs by using debug-level details.

### Target files

```text
src/ElBruno.NetAgent/Core/Models/NetworkQualitySnapshot.cs
src/ElBruno.NetAgent/Core/Models/EndpointPingResult.cs
src/ElBruno.NetAgent/Core/Enums/NetworkQualityLevel.cs
src/ElBruno.NetAgent/Services/Monitoring/INetworkQualityService.cs
src/ElBruno.NetAgent/Services/Monitoring/PingNetworkQualityService.cs
tests/ElBruno.NetAgent.Tests/NetworkQualityScoreTests.cs
```

### Acceptance criteria

- App measures latency.
- App handles ping failures.
- App does not crash offline.
- Tray tooltip can show current quality.
- Tests pass.

### Commit message

```text
feat: monitor network quality
```

## 10. Phase 5 – Decision engine

### Objective

Create deterministic network decision logic.

### Agent

Recommended SQUAD agents:

```text
Golan + Hober
```

### Tasks

- Add models: `NetworkDecisionContext`, `NetworkDecision`, `NetworkDecisionReason`.
- Add service: `INetworkDecisionEngine`, `NetworkDecisionEngine`.
- Implement: healthy current network = stay; unhealthy current network + better candidate = switch; cooldown = stay; insufficient samples = wait; score delta too small = stay.
- Add unit tests for every rule.

### Target files

```text
src/ElBruno.NetAgent/Core/Models/NetworkDecisionContext.cs
src/ElBruno.NetAgent/Core/Models/NetworkDecision.cs
src/ElBruno.NetAgent/Core/Enums/NetworkDecisionReason.cs
src/ElBruno.NetAgent/Services/Decision/INetworkDecisionEngine.cs
src/ElBruno.NetAgent/Services/Decision/NetworkDecisionEngine.cs
tests/ElBruno.NetAgent.Tests/NetworkDecisionEngineTests.cs
```

### Acceptance criteria

- Decision engine is pure logic.
- Decision engine has no WPF dependency.
- Decision engine has no Windows API dependency.
- All decision paths return a human-readable reason.
- Tests pass.

### Commit message

```text
feat: add network decision engine
```

## 11. Phase 6 – Network controller

### Objective

Allow Windows to prefer a selected interface safely.

### Agent

Recommended SQUAD agents:

```text
Golan + Tessa
```

### Tasks

- Add service: `INetworkController`, `WindowsNetworkController`.
- Start with safe metric-based approach.
- Implement dry-run mode, get current metrics, prefer selected interface, restore automatic metrics, admin detection, failure diagnostics.
- Log all attempted changes.
- Do not disable adapters.

### Target files

```text
src/ElBruno.NetAgent/Services/Control/INetworkController.cs
src/ElBruno.NetAgent/Services/Control/WindowsNetworkController.cs
src/ElBruno.NetAgent/Infrastructure/Windows/WindowsAdminService.cs
tests/ElBruno.NetAgent.Tests/NetworkControllerDryRunTests.cs
```

### Acceptance criteria

- Manual switch can call controller.
- Dry-run logs intended commands.
- Restore option is available.
- Errors are visible in logs and tray status.
- No adapter is disabled.
- Tests pass where possible.

### Commit message

```text
feat: add safe network controller
```

## 12. Phase 7 – Manual switching UI

### Objective

Expose manual network switching from the tray.

### Agent

Recommended SQUAD agents:

```text
Bayta + Golan
```

### Tasks

- Update tray menu with detected interface list.
- Add click handler per candidate interface.
- Show result notification or status update.
- Log manual action.
- Refresh quality after manual switch.

### Acceptance criteria

- User can manually select target interface.
- App reports success or failure.
- App remains running after failure.
- `dotnet build` succeeds.

### Commit message

```text
feat: add manual network switching from tray
```

## 13. Phase 8 – Auto mode

### Objective

Enable automatic switching.

### Agent

Recommended SQUAD agents:

```text
Golan + Hober
```

### Tasks

- Add background monitor loop using hosted service.
- Respect config: `AutoModeEnabled`, thresholds, cooldowns.
- Evaluate decision engine on interval.
- Call network controller only when decision says switch.
- Update tray status.
- Add toggle in tray menu.
- Persist auto mode setting.

### Acceptance criteria

- Auto mode can be enabled or disabled.
- Auto mode switches only when rules are met.
- Cooldown prevents flapping.
- All switches are logged.
- Tests pass.

### Commit message

```text
feat: add automatic network failover
```

## 14. Phase 9 – Diagnostics and status window

### Objective

Provide a small status UI.

### Agent

Recommended SQUAD agent:

```text
Bayta
```

### Tasks

- Add simple WPF status window.
- Show current interface, detected interfaces, latency, quality score, auto mode, last decision, last switch.
- Add buttons: Refresh, Open Logs, Open Config, Restore Metrics.
- Keep UI simple.

### Acceptance criteria

- Status window opens from tray.
- Status is readable.
- UI does not block monitoring.
- `dotnet build` succeeds.

### Commit message

```text
feat: add status window
```

## 15. Phase 10 – Packaging

### Objective

Prepare release-ready package artifacts.

### Agent

Recommended SQUAD agent:

```text
Tessa
```

### Tasks

- Add version metadata to `.csproj`.
- Add icon.
- Add publish profile or documented command.
- Add GitHub Actions CI.
- Add GitHub Actions publish workflow.
- Add docs/PUBLISHING.md.
- Add release artifact upload.

### Acceptance criteria

- CI builds on push.
- Release workflow packs or publishes.
- Artifact is downloadable from GitHub Actions.

### Commit message

```text
chore: add packaging and release workflow
```

## 16. Phase 11 – Promotional materials

### Objective

Generate launch materials.

### Agent

Recommended SQUAD agents:

```text
Scribe + Tessa
```

### Tasks

- Add Twitter/X post.
- Add LinkedIn post.
- Add blog post.
- Add image prompts.
- Add t2i usage commands.
- Generate images into `assets/images/` when available.
- Include NuGet logo image.

### Acceptance criteria

- `docs/PROMOTION.md` is complete.
- `docs/IMAGE_PROMPTS.md` is complete.
- Assets exist or generation commands are documented.

### Commit message

```text
docs: add promotional materials
```

## 17. Stop conditions

Stop and ask for human review when:

- A dependency requires admin install.
- Network switching requires disabling adapters.
- Tests cannot validate decision logic.
- NuGet packaging conflicts with WPF app constraints.
- A Windows command would make destructive changes.
- The model claims files were created but `git status` does not show them.
- The model cannot run validation commands.
- `dotnet build` fails and the fix is not obvious.

## 18. Recommended SQUAD execution pattern

Use one agent for each area:

```text
Seldon = architecture and review
Bayta = WPF tray UI
Golan = network/core implementation
Hober = tests
Tessa = CI, packaging, release
Scribe = documentation and promotional materials
Ralph = optional monitor only; do not use continuously on CPU-only VM unless needed
```

Each agent should produce small commits or patch-sized changes.

## 19. Current model lessons

Known observations:

```text
qwen3.5:
- supports thinking in Copilot CLI
- can timeout on CPU-only VM for broad prompts

qwen2.5-coder:
- good local coding model
- Copilot CLI may reject it because it does not support thinking

deepseek-coder:
- promising coding model
- Copilot CLI may reject it because it does not support thinking

nemotron-3-nano:4b:
- works with Copilot CLI + SQUAD
- can answer tasks
- can report file creation without creating files
- must be explicitly told to use terminal/shell tools
```

## 20. Final rule

For this repository:

```text
No task is complete until files exist on disk and validation commands run.
```
