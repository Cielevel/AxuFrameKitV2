# `unity` CLI essentials

Verified against CLI `1.0.0-beta.10`. The CLI is in beta; the binary itself is the source of
truth — `unity <group> --help`, `unity commands --format json`, and `unity skill show` are all
authoritative and offline.

## Install · update · remove

```bash
which unity && unity --version          # already installed?
```

**macOS / Linux**

```bash
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash
```

**Windows (PowerShell)**

```powershell
$env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex
```

Then open a new shell so `unity` lands on `PATH`.

| Task | Command |
|---|---|
| Update the CLI | `unity self-update` (interrupted downloads resume on the next run) |
| Check what changed | `unity changelog`, `unity version` |
| Remove it | `unity self-uninstall` |
| Install location | macOS/Linux: `~/.unity/bin/unity`; `unity env` prints the paths it uses |

Both install scripts also install **plugins** — external components the CLI drives (the `cm`
client for Unity Version Control, for example). Manage them with `unity plugin list` /
`unity plugin install <name>`.

## Global flags

Every command accepts these (they must appear before the subcommand where noted):

| Flag | Why you'd use it |
|---|---|
| `--format <human\|json\|tsv\|ndjson\|github>` | Machine output. `--json` is shorthand for `--format json`. |
| `--no-banner` | Suppress the branded header in scripts. |
| `--no-pager` | Never page. `unity command` / `releases` / `logs` page through `less` on a TTY. |
| `--non-interactive` | No prompts at all. Combine with `--yes` in CI. |
| `--quiet` | Suppress informational output. |
| `--verbose` | Full error detail (stack trace and cause chain) on failure. |
| `--color <auto\|always\|never>` / `--no-color` | Deterministic ANSI output. |
| `--proxy <url>` / `--proxy-disable` | Per-invocation proxy control (`http://`, `socks5://`, `pac+http://`). |
| `--log-proxy` | Write one redacted record per outbound request — for reproducing proxy issues. |

A few flags are **not** global and only exist on specific commands after the command name:
`--accelerator` / `--no-accelerator` on `build`, `test`, `run`.

## Environment variables

All CLI variables use the `UNITY_` prefix; an explicit flag always wins.

| Variable | Effect |
|---|---|
| `UNITY_FORMAT` | Default output format (`HUB_FORMAT` is a deprecated alias). |
| `UNITY_PROJECT_PATH` | Default project path for `open`, `status` and the cloud commands. |
| `UNITY_EDITOR_VERSION` / `UNITY_ARCHITECTURE` | Default Editor version / chip architecture. |
| `UNITY_NON_INTERACTIVE`, `UNITY_QUIET`, `UNITY_VERBOSE`, `UNITY_NO_BANNER` | Mirror the same-named flags. |
| `UNITY_NO_PAGER`, `UNITY_PAGER`, `PAGER` | Paging behaviour (`UNITY_NO_PAGER` is presence-based — even `0` disables paging). |
| `UNITY_RUN_TIMEOUT`, `UNITY_TEST_TIMEOUT` | Timeouts for `run` / `test`, in seconds. |
| `UNITY_SERVICE_ACCOUNT_ID`, `UNITY_SERVICE_ACCOUNT_SECRET` | Non-interactive CI auth without putting the secret in argv. |
| `UNITY_CLOUD_ORG` | Default Unity Cloud organization. |
| `UNITY_PROXY` | Persisted/global proxy for a single invocation. |
| `UNITY_NO_UPDATE_CHECK` | Turn off the background "update available" notice. |
| `UNITY_NO_CONSENT_PROMPT` | Never absorb the first-run analytics prompt (for wrappers). |
| `UNITY_NO_CRASH_REPORT` | Disable anonymous crash reporting entirely. |
| `UNITY_ACCELERATOR` | Unity Accelerator endpoint (`host:port`, default port 10080). |
| `UNITY_INSTALL_RETRIES` | Retries for a failing module download (`0` disables). |
| `UNITY_CLI_HOME` | Relocate the CLI's own state directory. |

## Machine output: read failures from stdout

`--format json` prints a single envelope; `--format ndjson` streams one JSON object per line
and ends with a `{"type":"result", …}` frame.

```jsonc
{ "success": false, "data": null, "errors": [ { "code": "TESTS_FAILED", "message": "…" } ] }
```

- **Branch on `success`.** `data` is `null` on most failures but *not* all — a partial
  `unity editors add` carries a row per path, and an ambiguous `unity auth switch` carries
  `data.candidates`.
- **Never parse stderr** in machine formats; it holds human diagnostics only. And empty stdout
  is not a failure signal — check the exit code. (A few older commands still write
  `{"error": …}` to stderr with empty stdout; treat that as a bug in that command, not a shape
  to code against.)
- `errors[0].code` is the stable token to switch on — e.g. `TESTS_FAILED` vs
  `TEST_RUN_ERROR` / `TEST_TIMED_OUT`.

## Exit codes

| Code | Meaning | Retry? |
|---|---|---|
| 0 | Success | — |
| 1 | General error | depends |
| 2 | Bad arguments (also an invalid `--proxy` value) | fix the call |
| 3 | Authentication failure (`auth`, `cloud`) | sign in again |
| 4 | Precondition not met (no active license, floating server unconfigured) | fix the precondition |
| 6 | Command-specific failure — including a test run that never produced a verdict | yes, infrastructure |
| 8 | **`unity test` only: the run completed and tests failed** | **no — report to developers** |
| 130 | Interrupted (Ctrl+C / SIGINT) | — |
| 143 | Terminated by SIGTERM (CI runner timeout) — emitted by long commands that clean up first, currently `unity build` | — |

The 6-vs-8 split exists so CI can retry infrastructure noise and never retry a real failure.
Preserve it in any wrapper you write.

## Command map

Run `unity --help` for the live list. Groups, with the ones people actually reach for:

- **Editors & installs** — `editors` (`--installed`, `list`, `running`, `add`, `default`,
  `upgrade`, `verify`, `prune`, `module`), `install [version]`, `uninstall`, `install-modules`,
  `modules`, `releases`, `install-path`.
- **Projects & templates** — `projects` (`list`, `create`, `new`, `clone`, `open`, `link`,
  `upgrade`, `export`, `import`, `pin`, `size`, `clean`, `exec`, `info`, `require`),
  `templates` (`list`, `info`, `create`, `pack`, `delete`), `open`, `close`.
- **Build / test / run** — `build`, `test`, `run`, `watch test`. See below.
- **Live Editor & Player** — `status`, `list`, `command`, `commands`, `pipeline`, `mcp`.
  See `live-editor.md`.
- **Auth, license, cloud** — `auth` (`login`, `logout`, `status`, `list`, `switch`, `default`),
  `license` (`status`, `activate`, `return`, `server`), `cloud` (`org`, `project`).
- **Config** — `config` (`get`, `set`, `list`, `unset`, `resolve`, `proxy`, `update-check`,
  `accelerator`), `context` (`save`, `use`, `list`, `current`, `delete`), `hub`.
- **Assets** — `assets inspect <file.unitypackage>` lists a package's contents (path, GUID,
  payload size, preview flag) **without importing it**, constant-memory even for large ones.
- **Diagnostics** — `doctor`, `env`, `logs` (`--follow`), `diagnose proxy`,
  `diagnose accelerator`, `cache`, `version`, `changelog`, `bug`.
- **Version control** — `vcs …` (GitHub / GitLab / self-hosted git / UVCS) plus a `cm`
  passthrough for Unity Version Control.
- **Agent plumbing** — `skill` (`show`, `install`, `refresh`), `mcp` (`configure`), `plugin`,
  `shell` (a REPL keeping one warm process), `ci init` (scaffold CI that drives the CLI),
  `completion <shell>`, `language`, `analytics`.

`unity commands --format json` dumps every command and subcommand as a machine-readable
manifest — the cheapest way for a tool to bind to this CLI's surface.

### `build`

```bash
unity build ~/Proj --target StandaloneLinux64 --execute-method Builder.PerformBuild --allow-install
unity build --list-targets                    # every valid --target value
unity build --list-profiles                   # a project's build profile assets
unity build --create-profile WebGL            # create a profile, then build with it
unity build run --path ~/Proj/Builds/last     # launch the most recent recorded build
```

- `--target` is required unless `--profile` is used. `--profile` is the supported route for
  partner/package-provided platforms (Meta Quest and friends) that `--target` cannot name.
- `--execute-method` is optional: without it the CLI uses Unity's built-in build (a build
  profile on Unity 6+, otherwise legacy desktop player flags) and **then `-o/--output-path` is
  required**.
- Logs default to `<project>/Logs/build-<target>-<timestamp>.log` and stream to the console
  unless `--no-tail`.

### `test`

```bash
unity test ~/Proj --mode EditMode --report-format junit --output ./test-results.xml \
  --allow-install --timeout 600
case $? in
  0) echo "passed" ;;
  8) echo "tests failed — do not retry" ;;
  *) echo "infrastructure failure — safe to retry" ;;
esac
```

Options worth knowing (all verified on beta.10):

| Option | Use |
|---|---|
| `--mode EditMode\|PlayMode` | Platform to run; omitted = the Editor's default. |
| `--filter <pattern>` | Restrict to matching test names. |
| `--report-format nunit\|junit\|nunit,junit` | JUnit is what GitHub Actions and GitLab ingest natively; the report is written even on failure. |
| `--junit-output <path>` | Where the JUnit half goes when producing both. |
| `--shard <n/m>` + `--shard-inventory <path>` | Split a suite across parallel CI jobs from a full run's report. |
| `--retries <0-10>` | Re-run failures; anything that passes on retry is reported as **flaky**. |
| `--rerun-failed` | Run only what failed in the previous report. |
| `--affected` / `--affected-compare` / `--since <ref>` | Run only tests a change can reach (via the GUID impact graph); the compare variant runs everything and reports what selection *would* have skipped. |
| `--coverage`, `--coverage-output`, `--coverage-options` | Requires the Code Coverage package; the CLI warns and continues if it is absent. |

### `run` (batch mode)

`unity run` spawns the Editor in batch mode — **never pass `-batchmode`/`-quit` yourself.**

```bash
unity run ~/Proj --allow-install -- -executeMethod Builder.PerformBuild -logFile build.log
unity run ~/Proj --command axu_test_run -- mode editor     # run a REGISTERED editor command headlessly
```

`--command <name>` executes a command registered with the Editor (the Pipeline command set)
without an open Editor — args after `--` are parsed against that command's schema. Use
`--timeout <seconds>` (or `UNITY_RUN_TIMEOUT`) so a hung Editor cannot wedge CI.

## Global config vs per-project defaults

```bash
unity config list                      # everything settable
unity config set build.target StandaloneLinux64
unity config resolve build.target ~/Proj    # resolved value + WHICH layer supplied it
```

`unity config resolve` names the layer that won. The layers, lowest precedence first:
the CLI's persisted config (`unity config set …`), then the project's committed
`ProjectSettings/UnityCliConfig.json`, then an explicit flag. Commit that file to give a whole
team the same build/test defaults; `resolve` is how you prove which layer is in effect when a
value surprises you.

`unity context save work` / `unity context use work` pins a named set of account,
organization, project, Editor and install path — useful when you hop between customers or
Unity organizations on one machine.

## Auth & licenses (including CI)

```bash
unity auth status --format json          # signed in?
unity auth login                          # interactive; --client-id + --secret-from-stdin for CI
unity license status --format json        # what entitlement is active (also reports floatingServer / machineId)
unity license activate                    # or --serial / --floating
unity license return --yes                # give the seat back when a CI job ends
```

For CI, set `UNITY_SERVICE_ACCOUNT_ID` and `UNITY_SERVICE_ACCOUNT_SECRET`: both `auth login`
and the rest of the CLI read them, and because they come from the environment the secret never
enters the process argument list or shell history.

## Getting help

```bash
unity --help                        # command list
unity projects create --help        # per-command flags
unity commands --format json        # machine-readable manifest of everything
unity skill show                    # the embedded agent skill (or --list for its files)
```
