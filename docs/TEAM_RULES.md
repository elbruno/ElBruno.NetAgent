# Core engineering principles

Our goal is not only to make the code work, but also to preserve long-term repository quality and architectural consistency.

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
