# Local Model Notes – ElBruno.NetAgent

This document tracks observations from using GitHub Copilot CLI, SQUAD, Ollama, and local models on a CPU-only Windows VM.

## 1. Environment

Current working setup:

```text
Working fork:
https://github.com/brunoghcpft-oss/ElBruno.NetAgent

Original repository:
https://github.com/elbruno/ElBruno.NetAgent

Machine:
Windows VM
CPU-only
32 GB RAM
No GPU

Workflow:
GitHub Copilot CLI + SQUAD + Ollama + local models
```

## 2. Goal

The goal is to build ElBruno.NetAgent while documenting the experience.

The story is:

```text
GitHub Copilot CLI + SQUAD + local models + GitHub Copilot Free Tier
```

The intended content output includes:

- blog posts
- LinkedIn posts
- Twitter/X posts
- videos
- lessons learned

## 3. Working principle

Use the cloud model sparingly.

Use local models for execution.

```text
Cloud model = planning and architecture
Local model = implementation and validation
```

## 4. Copilot Free Tier observation

A single cloud prompt to generate the SQUAD plan consumed about 4% of the monthly GitHub Copilot Free Tier chat quota.

This suggests cloud calls should be reserved for high-value planning and troubleshooting.

## 5. Repository state observation

After the first local-model/SQUAD execution attempt, the repository showed changes only in `.squad/*`.

Observed modified files:

```text
.squad/casting/history.json
.squad/casting/registry.json
.squad/config.json
.squad/team.md
```

Observed untracked folders:

```text
.squad/agents/bayta/
.squad/agents/golan/
.squad/agents/hober/
.squad/agents/seldon/
.squad/agents/tessa/
```

Expected Phase 0 files were not present:

```text
ElBruno.NetAgent.sln
src/ElBruno.NetAgent/ElBruno.NetAgent.csproj
tests/ElBruno.NetAgent.Tests/ElBruno.NetAgent.Tests.csproj
```

Conclusion:

```text
The model/SQUAD process created or updated SQUAD metadata, but did not create the actual .NET solution.
```

## 6. Key lesson

Model output is not proof of execution.

Always verify.

Required verification after every task:

```powershell
git status
Get-ChildItem -Recurse -Depth 3
```

For .NET changes, also run:

```powershell
dotnet build
dotnet test
```

## 7. Model observations

### 7.1 qwen3.5

Observed behavior:

```text
- Copilot CLI accepts it as a thinking-capable model.
- It can enter Thinking mode.
- On CPU-only VM, broad prompts can timeout.
```

Example broad prompt that failed:

```text
how does this repository work?
```

Observed result:

```text
Failed to get response from the AI model; retried 5 times.
Last error: CAPIError: Request timed out.
```

Conclusion:

```text
qwen3.5 can work, but prompts must be small and specific.
```

Best use:

```text
- short analysis
- small file edits
- limited context tasks
```

Avoid:

```text
- broad repo questions
- multi-document planning
- full phase execution without explicit commands
```

### 7.2 qwen2.5-coder:7b-instruct-q4_K_M

Observed behavior:

```text
- Runs locally with Ollama.
- Good for code-style prompts.
- Copilot CLI reports that it does not support thinking.
```

Observed error:

```text
400 "qwen2.5-coder:7b-instruct-q4_K_M" does not support thinking
```

Conclusion:

```text
Good local coding model, but not compatible with Copilot CLI thinking mode in the tested setup.
```

Best use:

```text
- direct Ollama coding prompts
- simple code generation
- small refactors
```

Avoid:

```text
- Copilot CLI flows that require thinking mode
```

### 7.3 deepseek-coder:6.7b-instruct-q4_K_M

Observed behavior:

```text
- Runs locally with Ollama.
- Promising coding model.
- Copilot CLI reports that it does not support thinking.
```

Observed error:

```text
400 "deepseek-coder:6.7b-instruct-q4_K_M" does not support thinking
```

Conclusion:

```text
Useful model in direct Ollama scenarios, but not usable with Copilot CLI if Copilot requires thinking mode.
```

Best use:

```text
- direct coding prompts
- command generation
- simple implementation tasks outside thinking-mode flows
```

Avoid:

```text
- Copilot CLI thinking-mode execution
```

### 7.4 nemotron-3-nano:4b

Observed behavior:

```text
- Works with Copilot CLI + SQUAD.
- Can answer the task.
- Can report that files were created even when no files were actually created.
```

Example reported output:

```text
Files created:
- src/WpfApp.sln
- src/WpfApp.csproj
- src/WpfApp.Tests.csproj
```

Actual repository state:

```text
No solution file.
No src folder.
No tests folder.
Only .squad metadata was modified.
```

Conclusion:

```text
Nemotron can be a good CPU-friendly model, but prompts must force terminal/tool execution and must verify file system state.
```

Best use:

```text
- Copilot CLI + SQUAD-compatible local execution
- short tasks
- tool-execution prompts
- validation and review
```

Avoid:

```text
- abstract scaffold requests
- trusting summaries without verification
```

## 8. Prompting rules for local models

### 8.1 Bad prompt

```text
Create the solution, WPF project, test project, and folder structure.
```

Problem:

```text
The model may describe the result without actually creating files.
```

### 8.2 Better prompt

```text
Use the terminal/shell tool to execute the following PowerShell commands exactly.

Do not only describe the commands.
Do not say files were created unless the commands actually ran.
After running the commands, verify with git status.
```

### 8.3 Best prompt for scaffold tasks

```text
Use the terminal/shell tool to execute these PowerShell commands exactly.

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
```

## 9. Practical local model strategy

Use local models like this:

```text
1. Ask for one tiny task.
2. Give explicit target files.
3. Ask for terminal/shell execution when needed.
4. Verify with git status.
5. Run build/tests.
6. Commit.
```

Avoid:

```text
- broad repo questions
- multi-doc reads
- full app implementation prompts
- vague scaffold instructions
- relying on summaries without verification
```

## 10. Recommended task size

Each task should modify:

```text
1 to 3 files maximum
```

Each prompt should include:

```text
- task
- target files
- constraints
- validation commands
- required output
```

## 11. Recommended model usage

### Cloud model

Use for:

```text
- generating SQUAD plan
- reviewing architecture
- resolving blockers
- refining prompts
```

### Local model

Use for:

```text
- executing small tasks
- creating files
- modifying specific files
- adding tests
- validating build/test results
```

## 12. Phase 0 execution strategy

Phase 0 must be command-driven.

Do not rely on abstract file creation.

Required proof:

```text
ElBruno.NetAgent.sln exists
src/ElBruno.NetAgent/ElBruno.NetAgent.csproj exists
tests/ElBruno.NetAgent.Tests/ElBruno.NetAgent.Tests.csproj exists
dotnet build succeeds
dotnet test succeeds
git status shows expected files
```

## 13. Troubleshooting

### Model says files were created, but they are missing

Run:

```powershell
git status
Get-ChildItem -Recurse -Depth 3
```

If files are missing, rerun with a command-driven prompt.

### Model times out

Reduce the prompt.

Use:

```text
one command
one file
one task
```

### Model does not support thinking

Use a different model or switch to direct command-oriented prompts.

### Model modifies only `.squad/*`

That means SQUAD metadata was updated, but project implementation did not happen.

Ask the model to use terminal/shell tools explicitly.

## 14. Storytelling notes

Useful lessons for blog/video:

```text
1. Copilot Free Tier is useful, but quota is limited.
2. Use cloud models for planning.
3. Use local models for execution.
4. CPU-only local models need small prompts.
5. Thinking-capable models may be too slow on CPU.
6. Some coding models do not support Copilot CLI thinking mode.
7. Some local models may simulate work unless tool execution is explicit.
8. Verification is part of the workflow.
9. Deterministic CLI commands still matter.
10. The workflow matters more than the biggest model.
```

Potential line:

```text
The winner was not the biggest model.
It was the workflow: cloud for planning, local for execution, and verification after every step.
```

Another useful line:

```text
Local AI can help build the app, but deterministic commands still matter.
```

Another useful line:

```text
The model did not fail because it was useless.
It failed because my prompt allowed it to describe work instead of doing work.
```

## 15. Current narrative

The current project narrative is:

```text
GitHub Copilot CLI + SQUAD + local models + GitHub Copilot Free Tier
```

The refined workflow is:

```text
1. Use Copilot Free Tier cloud model to generate SQUAD.
2. Switch to local models.
3. Use small tasks.
4. Force tool execution.
5. Verify with git status.
6. Commit each successful step.
7. Document the lessons.
```

## 16. Final rule

For this repository:

```text
No task is complete until files exist on disk and validation commands run.
```
