# Sample: a custom project skill

Copy the `my-project-skill/` folder into the package's `Skills~/` directory and rename the folder
to match the `name` you put in its frontmatter (kebab-case, lowercase). Then, in the Editor:

1. `AxuFrameKit ▸ CLI Agent Kit ▸ Open Kit Window`
2. Tick the client targets you want
3. **Install / Refresh**

The installer discovers every folder under `Skills~/` that has a `SKILL.md`, so nothing else is
needed to publish a new skill to everyone who pulls the package.

## Rules that make a skill actually fire

- **`description` is the whole game.** Write it as the trigger: the task wording, the tools
  involved, the failure modes a user would mention. If it reads like a summary instead of a
  trigger, the agent will not load the skill at the right moment.
- **Keep `SKILL.md` short.** Decision rules and a map; depth goes into `references/*.md`, linked
  with relative paths. A reference nobody links is dead weight (the package's EditMode tests
  check for orphaned references).
- **Prefer discoverable commands over transcripts.** Say how to find the current command
  (`unity command`, `unity <group> --help`) instead of freezing a command list that will drift.
- **Say what to do when the tool is missing.** Every skill that shells out should state the
  install command and the failure mode.

`my-project-skill/` shows the shape: frontmatter, hard rules, a route table, and one reference.
