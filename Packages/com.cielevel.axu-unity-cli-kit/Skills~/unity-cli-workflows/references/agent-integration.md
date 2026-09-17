# How skills reach an agent (and how to add your own)

## The `SKILL.md` convention

A skill is a folder containing `SKILL.md` at its top level. YAML frontmatter carries three
fields:

| Field | Rule |
|---|---|
| `name` | Matches the folder name, kebab-case. |
| `description` | The **trigger**. Written in terms of when to use it, including the vocabulary a user or task would actually use — this is the only text most clients match on. |
| `allowed-tools` | Optional. Omit when there is no restriction. |

Anything longer than a screen of instructions belongs in `references/*.md`, linked from
`SKILL.md`. Files alongside it — `CHANGELOG.md`, extra `.md`, scripts, resources — are part of
the skill and travel with the folder.

## Where clients read skills from

Project-local (these are the ones that travel with the repo):

| Client | Location |
|---|---|
| **Shared / cross-tool** | **`.agents/skills/<name>/SKILL.md`** — read by Codex, Cursor, GitHub Copilot, Gemini CLI, Zed, OpenCode, Cline, Windsurf, Kilo Code, Amp, Antigravity, Droid and more |
| Claude Code | `.claude/skills/<name>/SKILL.md` |
| ZCode | `.zcode/skills/<name>/SKILL.md` (scanned before `.agents/skills`) |
| Cursor rules | `.cursor/rules/*.mdc` (a different mechanism from skills) |
| Windsurf rules | `.windsurf/rules/*.md` |
| Cline rules | `.clinerules/*.md` |
| VS Code / Copilot instructions | `.github/instructions/*.instructions.md` |
| Any `AGENTS.md` consumer | `AGENTS.md` at the repo root |

User-global equivalents exist per client (`~/.agents/skills/`, `~/.codex/skills/`, …), but a
project-local copy is what makes a team consistent — it is reviewed, versioned and pulled like
code.

**`.agents/skills/` is plural.** `.agent/skills/` (singular) is not a path any known tool reads;
if you see it, it is a typo or a private convention.

## What `unity skill install` gives you

The `unity` binary embeds Unity's own `unity-cli` skill, and `com.unity.pipeline` ships a
`unity-pipeline` skill. Both are invisible while they sit in `Library/PackageCache`, so
materialize them into the project:

```bash
unity skill install <client> --local --yes   # claude-code | cursor | windsurf | vscode | cline | codex | grok
unity skill install <client>                 # user-global instead of project-local
unity skill install --list                   # supported clients + install status
unity skill show --list                      # files the embedded skill contains
unity skill refresh                          # re-render every tracked install (run after CLI updates)
```

`unity skill install <client> --local` writes **both** the `unity-cli` skill and, when a
project has the Pipeline package, the `unity-pipeline` skill — into `.claude/skills/…` for
`claude-code`. Those two are Unity-authored and licensed under the Unity Companion License:
consume them, don't paste them into your own package.

The public collection (31 Unity skills: URP migration, sprite atlas, localization, IAP, …) is
installed with `npx skills add Unity-Technologies/skills`.

## What the Axu CLI Agent Kit adds

The `com.cielevel.axu-unity-cli-kit` package ships **project-authored** skills under its
`Skills~/` folder (Unity ignores `~`-suffixed folders, so they never enter the AssetDatabase).
Its Editor tooling (`AxuFrameKit ▸ CLI Agent Kit`) installs them where each client looks:

| Target | Written as |
|---|---|
| Claude Code | Full copy: `.claude/skills/<name>/{SKILL.md,references/…}` |
| ZCode | Full copy: `.zcode/skills/<name>/…` |
| Shared (cross-tool) | Full copy: `.agents/skills/<name>/…` |
| Cursor | `.cursor/rules/<name>.mdc` — rule frontmatter + a pointer to the installed copy |
| Windsurf | `.windsurf/rules/<name>.md` |
| Cline | `.clinerules/<name>.md` |
| VS Code / Copilot | `.github/instructions/<name>.instructions.md` |
| `AGENTS.md` | A managed block between `<!-- axu-cli-kit:begin -->` and `<!-- axu-cli-kit:end -->`, rewritten in place |

On first run the installer suggests targets by detecting which client folders already exist in
the project, and falls back to `claude-code` when it detects none.

Behaviour you can rely on:

- **Idempotent.** Identical content is not rewritten; re-running is safe.
- **Non-destructive.** Files whose current content matches neither the shipped content nor the
  hash recorded at install time are treated as hand-edited: reported, not overwritten. Force
  only to take them over deliberately.
- **Recorded.** `<project>/.axu-cli-kit/state.json` lists every target and file hash the kit
  wrote, so uninstall removes exactly those and nothing else.
- **Pointers, not copies, for rule-file clients.** Those clients get a short rule that names the
  package path, so the content has one source of truth.

Commit the generated files (`.claude/skills/…`, `.cursor/rules/…`, the `AGENTS.md` block) so
every teammate and every CI agent reads the same rules; commit or ignore
`.axu-cli-kit/state.json` as you prefer.

## Adding a project skill

1. Create `Skills~/<your-skill-name>/SKILL.md` in the package (folder name = `name`, kebab-case).
2. Write a **precise trigger** in `description`: the task wording, the tools involved, the
   failure modes. Vague descriptions are why skills don't fire.
3. Keep `SKILL.md` to the decision rules and the map; put depth in `references/*.md` and link
   them with relative paths.
4. Prefer commands that are **discoverable at runtime** (`unity command`, `unity command
   <name> --help`) over frozen transcripts — say how to discover, not just what you saw.
5. Install from the window and test with realistic prompts before committing.

Never edit the installed copies directly — the next refresh reports them as modified. Edit the
package source, then refresh.

## Sourcing rules worth keeping

- Don't vendor Unity-authored skill text into a package you redistribute; the CLI installs it
  for you, version-locked to the binary.
- Don't restate a nested skill's whole content in yours; name it and say when to defer to it —
  two copies drift apart and an agent then follows the stale one.
- A skill that only makes sense inside one company's network or one private project belongs in
  that project's package, not in a public one.
