# Recipes

Copy-paste flows. Every command here was verified against CLI `1.0.0-beta.10` /
`com.unity.pipeline` `0.7.0-exp.1`; `~/Proj` stands in for an absolute project path.

## 1. Bring a fresh machine up

```bash
unity --version                         # CLI installed?
unity auth status --format json         # signed in? else: unity auth login
unity license status --format json      # a seat? else: unity license activate

unity releases --stream lts --limit 5 --format json     # pick an LTS
unity install lts --module android --module ios --yes --accept-eula
unity editors --installed --format json                 # confirm

unity templates list --editor lts --format json         # real template ids, don't guess
unity projects create "MyGame" --path ~/UnityProjects \
  --editor-version lts --template com.unity.template.3d --open
```

Pick the target platforms **before** installing — that decides the modules. Modules can be
added later (`unity install-modules`), but a project cannot build for a platform whose module
is missing.

To put the new project under source control in the same step, add `--vcs github` (or `gitlab`,
or a self-hosted host, or `--vcs uvcs`), `--git-namespace`, `--git-repo`,
`--git-visibility private`, and `--git-token-stdin <<<"$GIT_TOKEN"`. Add `--git-lfs` for
asset-heavy projects; UVCS handles binaries natively and needs no LFS. `--no-initial-commit`
defers the first commit until after you have added packages and assets.

Locally-initialized git instead:

```bash
cd ~/UnityProjects/MyGame
git init -b main
curl -fsSL https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore -o .gitignore
git add -A && git status          # Library/ Temp/ obj/ Build/ must NOT be staged
git commit -m "Initial Unity project: MyGame"
```

## 2. Open a project with the correct Editor

```bash
unity projects info ~/Proj --format json    # read "editorVersion"
unity editors --installed --format json     # is it installed?
unity open ~/Proj                           # warns when that version is missing
unity open ~/Proj --wait                    # block until the Editor exits (macOS/Linux)
```

`unity <version> <path>` is shorthand for `unity open <path> --editor-version <version>` and
accepts `lts`, `latest`, or a full version.

## 3. Work on a live Editor (and verify what you did)

```bash
unity status --format json                  # ready + the project path you must target
unity command list_open_scenes              # which scene is ACTIVE — check before writing
unity command create_gameobject --name Prop
unity command attach_script --target Prop --script Assets/Scripts/Prop.cs
unity command set_component_properties --target Prop --component Prop --properties '{"speed":3}'
unity command save_scene
unity command screenshot --path Temp/verify.png     # look at the result
```

If several Editors are open, add `--project-path /abs/path` to every one of these.

For anything without a dedicated command, drop to C# or the menu:

```bash
unity command eval 'return GameObject.Find("Prop").transform.position;'
unity command menu --path "GameObject/3D Object/Cube"
```

## 4. Batch asset work (no clicking)

```bash
unity command create_folder --path Assets/Art/Props
unity command find_assets --filter "t:Texture2D" --folder Assets/Art/Props
unity command get_import_settings --path Assets/Art/Props/crate.png
unity command set_import_settings --path Assets/Art/Props/crate.png \
  --settings '{"textureType":"Sprite","spritePixelsPerUnit":100,"maxTextureSize":1024}'
unity command import_asset --path Assets/Art/Props/crate.png
unity command save_all
```

Loop this over folders via `eval_file` when the work is repetitive — one Editor round-trip
beats N shell invocations.

## 5. Add a UPM package to a project

```bash
unity command package_search --query "cinemachine"          # inspect the registry
unity command package_add --identifier com.unity.cinemachine --confirm true
unity command package_list --format json
unity command package_resolve                               # apply pending resolution
```

For fully headless scripting of UPM (versions, scoped registries, tarballs) the CLI is
deliberately *not* the tool — use Unity's `unity-package-management` skill (the C#
`UnityEditor.PackageManager.Client` API).

## 6. CI: run tests and publish a report

```bash
unity test ~/Proj \
  --editor-version 6000.0.23f1 \
  --mode EditMode \
  --report-format junit --output ./test-results.xml \
  --allow-install --timeout 900
case $? in
  0) echo "All tests passed" ;;
  8) echo "Tests failed — report, do not retry" ;;
  *) echo "Run never produced a verdict — infrastructure, safe to retry" ;;
esac
```

JUnit is ingested natively by GitHub Actions and GitLab, and the report is written even when
tests fail. Useful additions:

- `--shard 1/4` with `--shard-inventory <full-run-report.xml>` to split one suite across four
  parallel jobs.
- `--retries 2` to re-run failures; anything that passes on a retry is reported as **flaky**
  rather than failing the job.
- `--rerun-failed` for a fast inner loop on the previous report.
- `--affected` (with `--since <ref>`) to run only the tests a change can reach; start with
  `--affected-compare` to see what it *would* skip before trusting it.
- `--coverage` (needs the Code Coverage package) with `--coverage-output <dir>`.

Inside an already-open Editor, the same idea lives one layer down:

```bash
unity command set_autotick --enable true
unity command run_tests --mode editor --filter MyFixture.MyTest --async_tests true
unity command test_status
```

## 7. CI: headless build

```bash
unity build ~/Proj \
  --target StandaloneLinux64 \
  --execute-method Builder.PerformBuild \
  --output-path ./Build/Game \
  --allow-install
echo "exit=$?"
```

- Without `--execute-method`, the CLI runs Unity's built-in build — then `--output-path` is
  required. With it, your method owns the output path (`-buildOutput` is forwarded).
- `--profile <name-or-asset>` is the supported route for platforms `--target` cannot name
  (Meta Quest and other partner/package-provided platforms). Discover with
  `--list-profiles`, create with `--create-profile <target>`.
- Logs land in `<project>/Logs/build-<target>-<timestamp>.log` and stream to the console
  (`--no-tail` to silence). Exit `143` means a runner timeout killed the build after cleanup.

Verify what you built by running it:

```bash
unity build run --path ~/Proj/Builds/StandaloneLinux64
```

## 8. A session against a development Player

```bash
# 1. Build a dev Player (Editor side, via a build command or your own build pipeline)
unity command switch_build_target --target StandaloneOSX
unity command build --output Builds/Dev/MyGame.app

# 2. Launch it, then drive the RUNTIME server (ports 7900+)
unity status --format json                       # the Player appears as an instance
unity command --runtime MyGame status
unity command --runtime MyGame log --level error
unity command --runtime MyGame eval "return UnityEngine.Time.timeScale;"

# 3. Reload a gameplay tweak without restarting
#    (tag the method [CodeReload], edit its body on disk, then push it to the Player:)
unity command reload_file_player_interpreter --filename Assets/Spinner.cs --player -1
```

`reload_file_player_interpreter` works on **IL2CPP** players too — the editor compiles and the
device interprets. For Mono Editor/desktop sessions use `unity command reload_file --filename
<file>` (`Assembly.Load` backend, accepts `--pdb` for breakpoints). Methods must be `public`
(instance or static) and return `void` or `IEnumerator`.

## 9. Diagnose a machine or a CLI problem

```bash
unity doctor --format json           # auth + installed editors + recent errors, one shot
unity env --format json              # paths, versions, resolved proxy
unity status --format json           # live instances
unity pipeline list                  # Pipeline presence / Safe Mode / versions
unity logs --follow --level info     # tail the Hub log (e.g. during an install)
unity diagnose proxy                 # explicit proxy diagnosis
unity cache                          # inspect / clear the download cache
```

Symptom-driven triage lives in `troubleshooting.md`.

## 10. Agent skills: install and keep current

```bash
# Unity-authored skills, materialized into THIS project next to your code
unity skill install claude-code --local --yes     # or cursor | windsurf | vscode | cline | codex | grok
unity skill show --list                            # what the embedded skill contains
unity skill refresh                                # re-render every tracked install after a CLI update

# Or pull the public collection (31 skills: URP migration, IAP, sprite atlas, …)
npx skills add Unity-Technologies/skills
```

The project-authored skills that ship with the `com.cielevel.axu-unity-cli-kit` package are
installed from the Editor instead — `AxuFrameKit ▸ CLI Agent Kit ▸ Install Skills into This Project` —
and re-synced with **Refresh Installed Skills** after the package updates. See
`agent-integration.md`.

## 11. A long-running job without blocking yourself

```bash
unity command run_tests --mode playmode --async_tests true
# ... do other work ...
unity command test_status
unity command cancel_tests                 # if you need the instance back now
```

`unity job` covers detached Editor jobs at the CLI level, and `unity watch test` re-runs
`unity test --affected` every time a project file changes — handy as a background quality gate
while you edit.
