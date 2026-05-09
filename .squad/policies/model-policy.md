# Model Policy

Repository policy: All Squad agent spawns and automated agent tasks in this repository MUST use the gpt-5-mini model.

Rationale:
- This repo is configured to run only gpt-5-mini (consistency and cost control).
- Existing runs that used other models cannot be retroactively changed; record and move forward.

Enforcement:
- Set `.squad/config.json` -> `defaultModel` = "gpt-5-mini" (already set).
- Do NOT pass explicit `model` parameters other than "gpt-5-mini" when spawning agents.
- If an agent requires a different model for valid reasons, it must be approved by a human and recorded in `.squad/decisions.md`.

Action: This file is authoritative guidance for Squad coordinator and agents.
