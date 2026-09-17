# Changelog — Axu CLI Agent Kit

All notable changes to this package are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Each entry names the `unity` CLI version the documentation was verified against.

## [0.1.0] — 2026-09-17

First release. Verified against `unity` CLI `1.0.0-beta.10` and `com.unity.pipeline` `0.5.0-exp.1`
(Unity `6000.0.23f1`, macOS).

### Added

- **Skill `unity-cli-workflows`** (`Skills~/`) — agent-facing entry point with the
  live-Editor-first preflight, the edit→recompile→test loop, result-reading rules
  (`--format json`, `success`, exit codes) and a route-by-task table, plus references:
  - `cli-essentials.md` — global flags, environment variables, exit-code semantics, the
    machine-output envelope, and the command groups as of CLI `1.0.0-beta.10`.
  - `live-editor.md` — `com.unity.pipeline` connection model, ports, command discovery,
    targeting one of several Editors, the async recompile/test loop, C# eval, scene /
    GameObject / prefab / asset authoring commands, Project Auditor, runtime server and
    hot reload.
  - `recipes.md` — end-to-end recipes: machine bootstrap, open-with-right-Editor, live
    scene edit, package add, CI test with JUnit, headless build, dev Player, batch asset
    operations, diagnostics, skill refresh.
  - `troubleshooting.md` — symptom → cause → action matrix, including Safe Mode, sandboxed
    shells, `AMBIGUOUS_EDITOR`, domain-reload connection drops, and exit-code branching.
  - `agent-integration.md` — where each AI client reads skills from, what this package
    installs, how refreshing works, and how to add project-specific skills.
- **Editor tooling** (`Editor/`, assembly `AxuCliKit.Editor`, Editor-only, no dependencies):
  - `AxuCliKitInstaller` — idempotent installer/refresher/uninstaller that materializes
    skills into eight targets: `.claude/skills/`, `.zcode/skills/`, `.agents/skills/`
    (the cross-tool location read by Codex, Cursor, Copilot, Gemini CLI, Zed, OpenCode,
    Cline, Windsurf and others), `.cursor/rules/`, `.windsurf/rules/`, `.clinerules/`,
    `.github/instructions/` and a managed block in `AGENTS.md`.
  - Target suggestions on first run come from **detecting the client folders that already
    exist** in the project (`.claude/`, `.zcode/`, `.agents/`, `.cursor/`, `.windsurf/`,
    `.clinerules/`), falling back to `claude-code`; a **Detect clients** button re-runs the
    detection in the window.
  - Content-hash based drift detection: hand-edited files are reported, never silently
    overwritten. State lives in `<project>/.axu-cli-kit/state.json`.
  - `AxuCliKitCli` — locates the `unity` executable on `PATH` and in the common install
    directories, and runs CLI commands capturing JSON output.
  - `AxuCliKitWindow` — status panel: CLI version, Editor connection, installed targets,
    drift, plus one-click install / refresh / uninstall and "install official Unity skills".
  - First-import prompt with three distinct answers: **Install** (installs now), **Not now**
    (silent for the rest of this Editor session, asks again next launch), **Don't ask again**
    (persisted per machine/project). The dialog waits until the Editor is idle and in front of
    the user rather than firing during an import. Plus the `AxuFrameKit ▸ CLI Agent Kit` menu.
- **Package documentation** (`Documentation~/`) — install & update for consumers, agent
  integration, CLI basics, live-Editor pipeline, recipes, troubleshooting, and how to
  extend or re-publish the kit.
- **Samples** (`Samples~/`) — a GitHub Actions CI workflow, a ready-made `AGENTS.md` block,
  and a custom-skill template.
- **EditMode self-checks** (`Tests/`) — installer path/hash/frontmatter behaviour.

### Notes

- The package ships no runtime code and declares no dependencies, so it can be pulled into
  any project; the `unity` CLI and `com.unity.pipeline` are detected, not required.
- Unity-authored skills are never redistributed here. The kit calls
  `unity skill install <client> --local` so the official `unity-cli` and `unity-pipeline`
  skills stay current with the installed CLI.
