---
name: my-project-skill
description: Use when someone asks for <the task in the words a user would use>, mentions <the tool or file names involved>, or hits <the failure mode this fixes>. Replace every angle bracket before shipping.
allowed-tools:
  - Bash
  - Read
  - Edit
---

# My project skill

One paragraph of orientation: what this covers, and the one thing that goes wrong when it is
done without this skill.

## Do this first

```bash
# The cheapest check that tells you whether the fast path is available.
```

## Rules

- A rule an agent can follow without judgment.
- Another one — keep these to the decisions that actually change behaviour.
- If a rule needs explanation longer than two lines, move it to `references/`.

## Route by task

| Task | Read |
|---|---|
| <common task> | `references/example.md` |
| <task this skill does not cover> | <the other skill or tool that owns it> |

## Guardrails

- What not to do, and the concrete harm it causes.
