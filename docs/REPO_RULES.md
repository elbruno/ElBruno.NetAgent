# ElBruno.NetAgent – Repository Rules

## 1. Purpose

These rules define how the repository must be organized and maintained.

They are designed for human contributors and AI coding agents.

## 2. Repository layout

Required root layout:

```text
/
  README.md
  LICENSE
  .gitignore
  ElBruno.NetAgent.sln
  docs/
  src/
  tests/
  assets/
  prompts/
  .github/
```

## 3. Documentation rules

All documentation must live in `docs/`, except:

- `README.md`
- `LICENSE`

Required docs:

```text
docs/
  PRD.md
  ARCHITECTURE.md
  IMPLEMENTATION.md
  REPO_RULES.md
  PUBLISHING.md
  CONFIGURATION.md
  PROMOTION.md
  IMAGE_PROMPTS.md
  ROADMAP.md
  SECURITY.md
  CHANGELOG.md
  SQUAD_EXECUTION_PROMPT.md
```

## 4. Source code rules

All source code must live in `src/`.

Required source layout:

```text
src/
  ElBruno.NetAgent/
```

Do not place production code in:

- root folder
- docs folder
- tests folder
- assets folder
- prompts folder

## 5. Test rules

All automated tests must live in `tests/`.

Required test layout:

```text
tests/
  ElBruno.NetAgent.Tests/
```

Testing priorities:

1. Decision engine
2. Quality score calculation
3. Adapter classification
4. Configuration validation
5. Anti-flapping rules

## 6. Assets rules

Generated images and media must live in:

```text
assets/images/
```

Image prompts must live in:

```text
docs/IMAGE_PROMPTS.md
prompts/
```

## 7. Naming rules

Use:

- Repository: `ElBruno.NetAgent`
- Main project: `ElBruno.NetAgent`
- Main namespace: `ElBruno.NetAgent`
- Future CLI project: `ElBruno.NetAgent.CLI`
- Future command: `netagent`

Avoid:

- `ElBruno.NetAgent.CLI` as the main app name
- generic names like `NetworkSwitcher`
- names that imply Wi-Fi only

## 8. Code style

- Use file-scoped namespaces.
- Enable nullable reference types.
- Treat warnings seriously.
- Prefer records for immutable DTOs.
- Prefer small services.
- Use async where IO or Windows operations are involved.
- Avoid blocking calls on UI thread.
- Avoid hidden static state.

## 9. Dependency rules

Allowed by default:

- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.Options`
- `System.Text.Json`
- `xUnit` or `MSTest` for tests

Requires explicit review:

- UI frameworks beyond WPF
- packet capture libraries
- VPN/proxy libraries
- libraries that require drivers
- libraries that require admin installation

## 10. Commit rules

Use small commits.

Recommended pattern:

```text
chore: scaffold solution
feat: add tray shell
feat: detect network interfaces
feat: monitor network quality
feat: add decision engine
feat: add manual switching
feat: add automatic failover
docs: add promotion materials
```

## 11. AI agent rules

AI agents must:

- Read `docs/IMPLEMENTATION.md` before coding.
- Implement one phase only unless asked.
- Keep a summary of files changed.
- Run build/tests after each phase.
- Update docs when behavior changes.
- Avoid adding speculative features.

## 12. License

Use MIT License.

## 13. Versioning

Use semantic versioning:

```text
MAJOR.MINOR.PATCH
```

Examples:

- `0.1.0` first MVP preview
- `0.2.0` auto mode preview
- `1.0.0` stable release

## 14. Branching

Recommended branches:

- `main`
- `feature/<short-name>`
- `docs/<short-name>`
- `fix/<short-name>`

## 15. Release tags

Use:

```text
vX.Y.Z
```

Examples:

- `v0.1.0`
- `v1.0.0`

## Core engineering principles

A concise set of principles to preserve long-term code quality, build/test discipline, and safety constraints. The authoritative, detailed principles live in docs/TEAM_RULES.md — avoid duplicating them here.

See: [Core engineering principles](TEAM_RULES.md)
