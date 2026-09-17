# Example reference

A reference file holds the depth: options, worked examples, edge cases, and the exact commands
that were verified. Keep the entry rules in `SKILL.md` and put the detail here, so the agent only
pays for this when it needs it.

## What belongs here

- Command tables with the flags that matter, and what each returns.
- Two or three worked examples, in the order someone would actually run them.
- Failure modes: the message, the cause, the fix.
- Anything version-specific, with the version it was verified against.

## What does not

- The decision rules (those go in `SKILL.md`).
- A copy of another skill's content — link to it instead. Two copies drift, and an agent will
  eventually follow the stale one.
- Vendor documentation pasted wholesale. Summarize what the task needs, cite the source.
