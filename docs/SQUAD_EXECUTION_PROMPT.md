# SQUAD Execution Prompt – ElBruno.NetAgent

Use this document when running GitHub Copilot CLI with SQUAD and local Ollama models.

The project is intended to be built incrementally using:

- GitHub Copilot CLI
- SQUAD
- local Ollama models
- CPU-only Windows VM
- GitHub Copilot Free Tier used sparingly for planning only

## 1. Primary execution strategy

Use cloud models only for high-value planning tasks.

Use local models for implementation.

Recommended split:

```text
Cloud model:
- generate SQUAD plan
- review architecture
- resolve blockers
- refine execution strategy

Local model:
- execute small tasks
- modify files
- create tests
- run validation
```

## 2. Local model constraints

Local models running on CPU may timeout or simulate file creation.

Therefore, prompts must be:

- small
- direct
- command-oriented when scaffolding
- easy to verify
- limited to one phase or one file at a time

Do not ask a local model to read the entire repository unless absolutely necessary.

## 3. Critical rule

Never treat model output as proof of repository changes.

A task is only complete when:

```text
1. files exist on disk
2. git status shows the expected changes
3. dotnet build succeeds
4. dotnet test succeeds when applicable
```

Always verify with:

```powershell
git status
Get-ChildItem -Recurse -Depth 3
```

For .NET changes, also run:

```powershell
dotnet build
dotnet test
```

## 4. Phase 0 execution prompt

Use this exact prompt for Phase 0.

```text
Read docs/IMPLEMENTATION.md.

Focus only on Phase 0.

Use the terminal/shell tool to execute these PowerShell commands exactly.

Do not only describe the commands.
Do not say files were created unless the commands actually ran.
Do not implement other phases.
Do not add business logic.

Commands:

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

Return:
- commands executed
- files created
- build result
- test result
- git status summary

Stop after Phase 0.
```

## 5. Smaller Phase 0 fallback prompts

If the model does not execute the full Phase 0 prompt, use these prompts one by one.

### 5.1 Create solution

```text
Use the terminal/shell tool to execute this PowerShell command exactly:

dotnet new sln -n ElBruno.NetAgent

Do not only describe the command.
Execute it.
Then run:

git status

Return the command output and git status summary.
```

### 5.2 Create folders

```text
Use the terminal/shell tool to execute these PowerShell commands exactly:

New-Item -ItemType Directory -Force -Path src
New-Item -ItemType Directory -Force -Path tests

Do not only describe the commands.
Execute them.
Then run:

git status

Return the command output and git status summary.
```

### 5.3 Create WPF project

```text
Use the terminal/shell tool to execute this PowerShell command exactly:

dotnet new wpf -n ElBruno.NetAgent -o src/ElBruno.NetAgent

Do not only describe the command.
Execute it.
Then run:

git status

Return the command output and git status summary.
```

### 5.4 Create test project

```text
Use the terminal/shell tool to execute this PowerShell command exactly:

dotnet new xunit -n ElBruno.NetAgent.Tests -o tests/ElBruno.NetAgent.Tests

Do not only describe the command.
Execute it.
Then run:

git status

Return the command output and git status summary.
```

### 5.5 Add projects to solution

```text
Use the terminal/shell tool to execute these PowerShell commands exactly:

dotnet sln ElBruno.NetAgent.sln add src/ElBruno.NetAgent/ElBruno.NetAgent.csproj
dotnet sln ElBruno.NetAgent.sln add tests/ElBruno.NetAgent.Tests/ElBruno.NetAgent.Tests.csproj

Do not only describe the commands.
Execute them.
Then run:

dotnet build
dotnet test
git status

Return:
- command output
- build result
- test result
- git status summary
```

## 6. Micro-task execution template

For every phase after Phase 0, use this format.

```text
Read only the required section from docs/IMPLEMENTATION.md.

Task:
<one small task>

Target files:
<explicit file paths>

Constraints:
- Do not modify unrelated files.
- Do not implement future phases.
- Keep the change minimal.
- Add or update tests only if required for this task.

Validation:
- run dotnet build
- run dotnet test when applicable
- run git status

Return:
- files changed
- summary of change
- build result
- test result if applicable
- git status summary
```

## 7. Example Phase 1 micro-task

```text
Read docs/IMPLEMENTATION.md.

Focus only on Phase 1.

Task:
Create the WPF tray shell placeholder.

Target files:
- src/ElBruno.NetAgent/App.xaml
- src/ElBruno.NetAgent/App.xaml.cs
- src/ElBruno.NetAgent/UI/Tray/TrayIconService.cs

Constraints:
- Do not implement network detection.
- Do not implement config.
- Do not implement decision engine.
- Add only a tray icon placeholder and Exit menu item.
- Keep changes minimal.

Validation:
- run dotnet build
- run git status

Return:
- files changed
- summary
- build result
- git status summary
```

## 8. Example Phase 2 micro-task

```text
Read docs/IMPLEMENTATION.md.

Focus only on Phase 2.

Task:
Create the NetAgentOptions class only.

Target files:
- src/ElBruno.NetAgent/Core/Options/NetAgentOptions.cs

Constraints:
- Do not implement config file loading yet.
- Do not modify UI.
- Do not implement networking logic.
- Keep the class simple and strongly typed.

Validation:
- run dotnet build
- run git status

Return:
- files changed
- summary
- build result
- git status summary
```

## 9. Example Phase 3 micro-task

```text
Read docs/IMPLEMENTATION.md.

Focus only on Phase 3.

Task:
Create NetworkAdapterKind enum and NetworkInterfaceInfo model.

Target files:
- src/ElBruno.NetAgent/Core/Enums/NetworkAdapterKind.cs
- src/ElBruno.NetAgent/Core/Models/NetworkInterfaceInfo.cs

Constraints:
- Do not implement NetworkInventoryService yet.
- Do not modify UI.
- Do not implement quality monitoring.
- Keep the model immutable if practical.

Validation:
- run dotnet build
- run git status

Return:
- files changed
- summary
- build result
- git status summary
```

## 10. Model policy

Current repo strategy:

```text
Use GPT-5-mini or another cloud model only for planning and blocker resolution.
Use local Ollama models for execution.
```

Local model candidates:

```text
nemotron-3-nano:4b
qwen3.5
qwen2.5-coder:7b-instruct-q4_K_M
deepseek-coder:6.7b-instruct-q4_K_M
```

## 11. Current tested model observations

### qwen3.5

```text
- supports thinking mode in Copilot CLI
- can timeout on CPU-only VM for broad prompts
- use only small, specific prompts
```

### qwen2.5-coder:7b-instruct-q4_K_M

```text
- useful local coding model
- does not support Copilot CLI thinking mode in the tested setup
```

### deepseek-coder:6.7b-instruct-q4_K_M

```text
- useful coding model
- does not support Copilot CLI thinking mode in the tested setup
```

### nemotron-3-nano:4b

```text
- works with Copilot CLI + SQUAD in the tested setup
- can respond as if files were created without actually creating them
- must be explicitly instructed to use terminal/shell tools
- must be verified with git status and file listing
```

## 12. Recommended workflow

```text
1. Ask cloud model to generate or refine SQUAD plan.
2. Save plan in docs/SQUAD.md or .squad/team.md.
3. Switch to local model.
4. Execute one small task.
5. Verify files on disk.
6. Run build/tests.
7. Commit.
8. Continue with next micro-task.
```

## 13. Commit discipline

Each successful micro-task should end in one small commit.

Examples:

```text
chore: configure SQUAD team
chore: scaffold .NET solution
feat: add tray shell placeholder
feat: add configuration options
feat: detect network interfaces
test: add decision engine tests
```

## 14. Prompting rules for local models

### Bad

```text
Create the solution and projects.
```

### Better

```text
Use the terminal/shell tool to execute the following commands exactly.
```

### Best

```text
Use the terminal/shell tool to execute the following PowerShell commands exactly.

Do not only describe the commands.
Do not say files were created unless the commands actually ran.
After the commands finish, verify with git status.
Return the command output and file changes.
```

## 15. Stop conditions

Stop and ask for human review when:

- The model cannot execute shell commands.
- The model claims files were created but `git status` does not show them.
- `dotnet build` fails.
- `dotnet test` fails.
- The model tries to implement future phases.
- The model modifies unrelated files.
- The model suggests disabling network adapters before Phase 6.
- The model suggests changing DNS, VPN, firewall, or system-wide routing before approval.
