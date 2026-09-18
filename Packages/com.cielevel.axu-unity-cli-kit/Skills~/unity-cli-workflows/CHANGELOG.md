# Changelog — `unity-cli-workflows` skill

Documentation-only, like the official Unity skills: nothing automated reads this file. Each
entry names the `unity` CLI version the content was verified against.

## Kit 0.2.0 — 2026-09-18

- `references/agent-integration.md` — documented opencode's native project skill folder
  (`.opencode/skills/`, scanned before `.agents/skills/`) and added it to the kit's target table.

## CLI `1.0.0-beta.10` — 2026-09-17

First release, shipped inside `com.cielevel.axu-unity-cli-kit` 0.1.0. Verified on macOS against
CLI `1.0.0-beta.10`, `com.unity.pipeline` `0.7.0-exp.1`, Unity `6000.0.23f1`.

- `SKILL.md` — orientation commands, live-Editor-first rule, edit→recompile→test loop,
  result-reading rules (`success` + exit codes, stdout not stderr), route-by-task table,
  skill families and refresh commands, false-negative warnings (Safe Mode, sandboxed shells),
  guardrails.
- `references/cli-essentials.md` — install/update, global flags, environment variables,
  machine-output envelope, the full exit-code table, the grouped command map, `build` / `test` /
  `run` option tables, `ProjectSettings/UnityCliConfig.json` precedence and `unity config
  resolve`, auth and license for CI.
- `references/live-editor.md` — Editor/Runtime servers and port ranges, discovery, targeting
  multiple instances, the observed command surface by group, the async loop, C# eval, play mode
  and captures, Project Auditor, dev Player and hot reload, `[CliCommand]` extension, gotchas.
- `references/recipes.md` — eleven end-to-end recipes from machine bootstrap to CI build and
  dev-Player hot reload.
- `references/troubleshooting.md` — evidence gathering plus symptom → cause → action tables for
  connectivity, compilation, tests, builds, auth/network and skill installation.
- `references/agent-integration.md` — the `SKILL.md` convention, per-client directories
  (including the cross-tool `.agents/skills/` and ZCode's `.zcode/skills/`), what
  `unity skill install` writes, what this kit adds, and how to author a project skill.

## pipeline `0.7.0-exp.1` — 2026-09-17

Command surface realigned after upgrading `com.unity.pipeline` from `0.5.0-exp.1` to
`0.7.0-exp.1` (160 → 171 commands, verified against the live Editor):

- `get_console_logs` → `console` (`--tail` / `--level` / `--since` cursor).
- `[HotReload]` → `[CodeReload]`; `reload_file` keeps the `Assembly.Load` backend, and
  `reload_file_editor_interpreter` / `reload_file_player_interpreter` (IL2CPP-safe, the latter
  pushes to connected players over PlayerConnection) are new.
- Removed: `reload_file_override`, `hotreload_status`, `cleanup_hotreload`, `progress`.
- `RuntimePipelineManager` → `RuntimePipelineDriver` / `RuntimePipelineBootstrap`.

Deliberately **not** covered: UPM automation from C#, IAP/ads/LiveOps, new-project ideation —
those are separate Unity-authored skills, and this file set points at them instead of copying.
