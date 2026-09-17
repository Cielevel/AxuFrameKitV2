# Troubleshooting: symptom → cause → action

## First, gather evidence

```bash
unity doctor --format json          # auth, installed editors, recent errors
unity env --format json             # paths, versions, resolved proxy
unity status --format json          # live instances and their state
unity pipeline list                 # Pipeline presence, version, Safe Mode
unity <cmd> --verbose               # full error detail incl. stack + cause chain
unity command console --level error --tail 50      # Editor-side errors and warnings
```

Then branch on the exit code from `cli-essentials.md` — it is designed to tell you *what kind*
of failure this is before you read a single message.

## Connection and "no Editor"

| Symptom | Likely cause | Action |
|---|---|---|
| `unity status` lists nothing, but an Editor is visibly open | **Safe Mode** — the project has compile errors, so `com.unity.pipeline` never loads | `unity pipeline list`; fix the C# errors; restart the Editor |
| Same, and no compile errors | **Sandboxed shell** hiding the running process from this CLI | Do not treat it as proof. Say your sandbox may be blocking the view and ask; or re-run outside the sandbox |
| Same, after checking both | Pipeline package genuinely absent, or the Editor is on a different project | `unity pipeline install`; `unity status --project-path <path>` |
| `AMBIGUOUS_EDITOR` | Several Editors running, no way to choose | Pass `--project-path <abs path>` (or `--instance host:port`) |
| Commands time out | Editor unfocused and throttled; long job holding the gate; wrong instance | `unity command set_autotick --enable true`; check `unity command editor_status`; make sure no soak/PlayMode run is in flight on that instance |
| Connection error *during* a command | Domain reload in flight (recompile, PlayMode start/stop) | Expected. Retry until the status command reports a terminal state |
| Nothing binds / "No available ports in range" | Port range exhausted or blocked (Editor 7800–7849, test servers 7850–7899, Runtime 7900–7999) | Close stray Editor/test processes; check for another app on the range; recent Pipeline versions probe for a free port on macOS |
| A running Player never shows up | Not a **development** build, no `RuntimePipelineManager` in the scene, or the port file was not written | Build development; add the component; check `Library`/build folder for the runtime port file |

## Compile and script errors

| Symptom | Action |
|---|---|
| `recompile_status` → `failed: true` | Read its `errors[]`; fix; recompile again |
| `recompile_status` never leaves `triggered` | The Editor is not ticking — `set_autotick --enable true`, then re-trigger |
| Compiles in the IDE but not in the Editor | The IDE's project files are stale; the Editor compiles from `.csproj`-independent assembly definitions. Check asmdef references and platform filters (`includePlatforms`) |
| Editor stuck in Safe Mode | Fix the error, then restart the Editor — Safe Mode does not load packages, so the CLI cannot help from inside |

## Tests

| Symptom | Action |
|---|---|
| `run_tests` returns an opaque result instead of failure details (known caveat) | Re-run a narrower `--filter`, or read `unity command console --level error --tail 50` / the Test Runner window |
| Test run hangs | Poll `test_status`; `cancel_tests` to reclaim the instance; prefer `--async_tests true` for long PlayMode suites |
| CI job exits 8 | The tests ran and **failed** — do not retry the job. Fix them |
| CI job exits 6 during tests | The run never produced a verdict (compile error, no license, timeout, crash) — safe to retry after checking logs |
| PlayMode tests behave differently than locally | Domain/scene reload settings differ; reset statics in `[UnitySetUp]` rather than relying on a fresh domain |
| Tests pass locally, fail in CI | Check Editor version parity, `--timeout`, and whether the CI machine has the platform modules the tests need |

## Build

| Symptom | Action |
|---|---|
| `--target X` rejected | `unity build --list-targets` for the exact spelling; some platforms are only reachable via `--profile` |
| Build succeeds but produces nothing | With `--execute-method`, your method owns the output path — verify it honored `-buildOutput` |
| Exit 143 | The runner (or `--timeout`) killed the Editor; give it more time or a faster machine |
| Android keystore prompts in CI | Supply credentials via environment/args, not interactive input — CI must run with `--non-interactive` |

## Auth, license, network

| Symptom | Action |
|---|---|
| Exit 3 | Authentication — `unity auth login`, or set `UNITY_SERVICE_ACCOUNT_ID` + `UNITY_SERVICE_ACCOUNT_SECRET` |
| Exit 4 | Precondition: no active license / floating server unconfigured — `unity license status`, `unity license activate` |
| Downloads fail behind a corporate network | `unity env --format json` shows the resolved proxy; `unity diagnose proxy`; override with `--proxy` or `UNITY_PROXY` |
| Module install flaky | `UNITY_INSTALL_RETRIES` (or `--retries`), and `--resume` recovers an interrupted module download |
| Secrets in shell history | Use stdin flags: `--git-token-stdin`, `--secret-from-stdin`, or the service-account env vars |

## Agents and skills

| Symptom | Action |
|---|---|
| An agent "doesn't know" the Unity CLI/Pipeline | Skills inside `Library/PackageCache` are invisible to clients. Materialize them: `unity skill install <client> --local` (official) and `AxuFrameKit ▸ CLI Agent Kit` (project skills) |
| The official skill is stale after a CLI update | `unity skill refresh` re-renders every tracked install; `unity skill show --list` shows what the binary currently contains |
| The project's skill files are stale after the package updated | `AxuFrameKit ▸ CLI Agent Kit ▸ Refresh Installed Skills` (or re-run install) |
| Installing overwrites my hand edits | It should not: the installer compares content hashes and reports conflicts. If a file was edited by hand it is reported, not replaced — use Force only when you mean it |
| Client doesn't pick the skill up after install | Most clients scan at session start — start a new session. Confirm the file is where that client expects (see `agent-integration.md`) |
| Two copies of the same skill disagree | Prefer one source. Remove the stale copy (`.axu-cli-kit/state.json` lists what this kit installed) rather than letting two versions drift |

## CLI oddities worth knowing

- **Empty stdout with a non-zero exit** on a few older commands: they still print
  `{"error": …}` to stderr. Re-run with `--verbose`; treat it as that command's bug.
- **Paging surprises**: `unity projects list` pages *in-process* and only on a TTY, and
  `--format tsv`/`github` fall through to the human table there. Redirect stdout or pass
  `--no-pager` in scripts.
- **`unity command` with no name** is the only reliable way to know an instance's command set —
  versions differ per project because commands are registered by installed packages.
- **Terminal output is sanitized**: control characters from server-provided values are stripped
  in table/list/tree output. Do not depend on raw bytes for those values.
