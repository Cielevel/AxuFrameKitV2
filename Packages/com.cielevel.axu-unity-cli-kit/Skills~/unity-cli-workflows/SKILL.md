---
name: unity-cli-workflows
description: Use when a task touches Unity from outside the Editor or needs a running Editor driven — checking whether a Unity Editor is live and then controlling it (scenes, GameObjects, prefabs, assets, play mode, screenshots, C# eval, recompile), running EditMode/PlayMode tests, headless build or run, installing or licensing Editors and modules, wiring Unity into CI, or triaging a failing `unity` CLI / Pipeline command. Also covers how this project's agent skills are installed and refreshed.
allowed-tools:
  - Bash
  - Read
  - Edit
  - Write
---

# Unity CLI workflows

Unity gives you two ways to work: **edit files**, or **drive a live process**. Almost every
expensive mistake in this area comes from picking the first when the second was available —
hand-written `.unity` / `.prefab` / `.asset` YAML that the Editor never notices, scene edits
applied to the wrong scene, GUIDs invented by hand.

This skill is the routing layer: decide *which* path, run the loop, read the result correctly.
Depth lives in `references/`.

## 0. Orient before planning (three commands)

```bash
unity --version                       # is the CLI installed, and which version
unity status --format json            # is an Editor live, and where
unity command                         # what can THIS Editor do right now
```

`unity status` printing no instances is **not** proof that no Editor is running — see
[False negatives](#false-negatives-read-this-before-concluding-no-editor). Command names are
defined by the Editor and vary with installed packages, so discover them; never assume one
exists because it is listed here.

## 1. Live Editor first

If `unity status` shows an instance whose `state` is `ready`, **drive it**:

```bash
unity command create_gameobject --name Joe      # acts on the ACTIVE scene
unity command save_scene                        # persist
unity command screenshot --path Temp/shot.png   # look at what you did
unity command eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'
```

Rules that follow from this:

- **Never hand-edit `.unity`, `.prefab` or `.asset` YAML while a live Editor is reachable.**
  fileIDs and GUIDs are assigned by the Editor, and raw-file edits stay invisible until a
  reimport — producing a change that looks right and does nothing.
- **Confirm which scene/asset is active** before writing (`unity command list_open_scenes`,
  `unity command get_authoring_root`). The Editor acts on *its* active scene, which is often
  not the file you assumed.
- **Editing C# source on disk is the normal, correct path.** The Editor picks it up through a
  recompile (§2). Only *data* files (scene/prefab/asset) need the live path.

When more than one Editor may be running, pass the target explicitly — otherwise the command
resolves against the shell's working directory and can hit the wrong project:

```bash
unity command editor_status --project-path /abs/path/to/MyProject
```

## 2. The loop: edit → recompile → test

```bash
unity command set_autotick --enable true     # FIRST. Without it a background Editor throttles
                                             # and recompile/tests can hang.
# ... edit C# on disk ...
unity command recompile                      # async: triggers a domain reload
unity command recompile_status               # poll until completed | up_to_date
#   Connection errors during the reload are EXPECTED — retry.
#   failed=true → read the "errors" array and fix before running tests.

unity command list_tests --mode editor       # what exists (all | editor | playmode)
unity command run_tests --mode editor --filter MyFixture.MyTest
```

`run_tests` is synchronous by default; for long runs use `--async_tests true` and poll
`unity command test_status` (abort with `cancel_tests`). Filters use
`--filter_type testName|assembly|category`.

Everything async follows the same shape: **the trigger call is not the result.** Poll the
matching status command until it reaches a terminal state.

## 3. Read results from stdout, and branch on the right field

Use `--format json` (or `--json`) whenever a program will parse the output.

- Failures still write a complete document to **stdout**: `success: false` plus an `errors`
  array. Branch on `success` and the process exit code — not on `data` (which is `null` on
  most failures but not all) and not on stderr (human diagnostics only).
- `exit 8` is `unity test`-specific: **the tests ran and failed.** Every other test-run failure
  (compile error, no license, timeout, crash) exits `6`. Retry infrastructure failures; never
  retry a failing test.
- Full exit-code table: `references/cli-essentials.md`.

## 4. Route by task

| Task | Read |
|---|---|
| Install / license / update Editors, modules, projects, templates, auth | `references/cli-essentials.md` |
| Drive a running Editor; recompile; tests; eval; scene/prefab/asset authoring; hot reload | `references/live-editor.md` |
| End-to-end "how do I actually do X" — bootstrap a machine, CI test, headless build, Player, batch assets | `references/recipes.md` |
| Something failed — no Editor found, hangs, opaque test output, exit codes, logs | `references/troubleshooting.md` |
| Where skills live in this project, how to refresh or add one | `references/agent-integration.md` |

Not covered here, on purpose: **UPM package management from C#** (Unity's
`unity-package-management` skill), **IAP / ads / LiveOps** (`implement-in-app-purchases`,
`levelplay-unity-integration`, `build-live-game`), **new-project ideation**
(`new-unity-project`). To get those, see §5.

## 5. The official skills, and the ones this project ships

Two skill families can coexist in a project:

- **Unity-authored, version-locked to the CLI** — `unity-cli` (flag-level command reference)
  and `unity-pipeline` (live-Editor detail). They live inside the `unity` binary and inside
  `com.unity.pipeline`, so they are invisible to an agent until materialized:

  ```bash
  unity skill install <client> --local --yes   # claude-code | cursor | windsurf | vscode | cline | codex
  unity skill refresh                          # re-render every install after a CLI update
  unity skill show --list                      # what the embedded skill contains
  ```

- **Project-authored, committed with the repo** — this project's own skills (including this
  one). They are delivered by the `com.cielevel.axu-unity-cli-kit` package and installed by
  its Editor window (`AxuFrameKit ▸ CLI Agent Kit`) or its menu items.

After updating the CLI or that package, re-run the matching refresh so the on-disk copies
catch up. Details and per-client paths: `references/agent-integration.md`.

## False negatives — read this before concluding "no Editor"

Both of these look exactly like a genuinely closed Editor:

- **Safe Mode.** An Editor that fails to compile its C# boots into Safe Mode, where the
  Pipeline package never loads, so `unity status` / `unity command` cannot connect even though
  the process is running. Confirm with `unity pipeline list`; if it reports Safe Mode, fix the
  compile errors and restart the Editor. That is the correct action, not a fallback to file
  editing.
- **A sandboxed shell.** A restrictive sandbox can hide a running Editor from this CLI's view
  of the machine. "No instances" is not evidence. Say that your sandbox may be blocking the
  view and ask whether an Editor is open, rather than quietly improvising a third path.

Only after ruling both out, fall back to editing files directly — and **say so explicitly**
("no live Editor detected, editing the file directly").

## Guardrails

- Don't invent command names or flags — `unity command` (bare) and `unity <group> --help` are
  the source of truth; this document is a map, not a manifest.
- Don't run a bare `run_tests` with no assembly or name filter on a shared Editor: unrelated
  or hostile test suites will run. Filter to what you changed.
- Don't put secrets (tokens, licenses, service-account credentials) in shell history or
  process arguments. Prefer stdin flags (`--git-token-stdin`, `--secret-from-stdin`) fed from
  a secret store or environment variable.
- Long PlayMode/soak runs occupy the Editor's single command gate: **don't dispatch another
  CLI command at the same instance while one is in flight.**
- When you change project state through the CLI, leave the project in a legible state — save
  what you saved, report what you touched.
