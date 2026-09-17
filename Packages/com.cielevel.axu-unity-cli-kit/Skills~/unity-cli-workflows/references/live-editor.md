# Driving a live Unity instance (`com.unity.pipeline`)

The Pipeline package turns the Editor — and a development Player — into a local HTTP service the
`unity` CLI can call. Everything below assumes the CLI is on `PATH`.

## Two servers, two port ranges

| Server | Runs in | Ports | Started by |
|---|---|---|---|
| **Editor** | the Unity Editor | 7800–7849 (test harness servers use 7850–7899) | automatically when the Editor opens the project |
| **Runtime** | a **development** Player build | 7900–7999 (base range 7900–7949) | `RuntimePipelineManager` in the scene, on `Awake` |

They expose different command sets. `log`, `set_timescale`, `runtime_status` and friends are
Player-only; scene/prefab/asset authoring is Editor-only.

## Install, verify, connect

```bash
unity pipeline install                 # adds com.unity.pipeline to the current project (Unity 6+)
unity pipeline install --force         # re-resolve to the latest version
unity pipeline list                    # every Editor instance + its Pipeline status
unity pipeline upgrade                 # update the package when a newer version exists

unity status --format json             # state: ready | starting | unreachable | …
unity command editor_status            # does the Editor server answer?
```

## Discovery comes first

Command names are **defined by the Editor that is currently running**, so they depend on the
project and on which packages are installed. Never assume — ask:

```bash
unity command                          # list what this instance exposes (human table)
unity command --format json            # same, parseable, with parameters and descriptions
unity list                             # tool discovery for the connected Editor
unity commands --format json           # every CLI command + subcommand (whole CLI, not an instance)
```

Use `--result-only` on a command to get the bare JSON result without the envelope:

```bash
unity command recompile_status --result-only
```

## Targeting the right instance

| Situation | Flag |
|---|---|
| One Editor, and your shell's cwd is inside its project | nothing needed |
| Several Editors open | `--project-path /abs/path/to/Project` on **every** call |
| You know the endpoint | `--instance host:port` |
| A running Player instead of an Editor | `--runtime <process-name>` or `--runtime-path <dir-with-port-file>` — placed **after** `command`, before the command name |

With several instances running and no way to choose, `unity command` fails with
`AMBIGUOUS_EDITOR` and lists the candidates. Pass a filter, don't guess.

## What a typical Editor exposes

Discovery output is the contract; this is the shape you should expect (about 160 commands in an
Editor with the common packages). Groups seen in practice:

- **Editor control** — `editor_status`, `editor_play`, `editor_pause`, `editor_stop`,
  `editor_focus`, `set_autotick`, `menu` (invoke any menu item by path).
- **Scripts** — `recompile`, `recompile_status`, `eval`, `eval_file`, `read_text_file`,
  `write_text_file`, `console` (tail / level / since cursor), `clear_console`.
- **Scene & GameObjects** — `create_scene`, `open_scene`, `save_scene`, `list_open_scenes`,
  `set_active_scene`, `create_gameobject`, `create_gameobjects`, `find_gameobjects`,
  `rename_gameobject`, `delete_gameobject`, `set_active`, `create_prefab`,
  `instantiate_prefab`, `create_prefab_variant`, `unpack_prefab`, `save_prefab_contents`,
  `apply_prefab_overrides`, `revert_prefab_overrides`.
- **Components** — `add_component`, `attach_script`, `remove_component`,
  `get_component_properties`, `set_component_properties`.
- **Assets** — `create_asset`, `create_folder`, `copy_asset`, `move_asset`, `rename_asset`,
  `delete_asset`, `import_asset`, `find_assets`, `search`, `get_import_settings`,
  `set_import_settings`, `create_script`, `create_animation_clip`, `create_animator_controller`,
  `create_timeline`.
- **Settings** — `get_*` / `set_*` for graphics, quality, lighting, audio, input, physics, build
  settings, and `list_open_scenes`, `list_shaders`, `list_build_targets`, `list_build_profiles`.
- **Baking** — `bake_lighting`, `bake_navmesh`, `bake_navmesh_surfaces`,
  `bake_occlusion_culling`, plus `*_bake_status` and `cancel_*`.
- **Build settings** — `add_scene_to_build`, `remove_scene_from_build`, `switch_build_target`,
  `get_build_settings`, `set_build_settings`.
- **Packages** — `package_list`, `package_add`, `package_remove`, `package_search`,
  `package_resolve`, `package_status`.
- **Tests** — `list_tests`, `run_tests`, `test_status`, `cancel_tests`.
- **Observability** — `screenshot`, `capture_scene_view`, `capture_game_view`,
  `get_performance_stats`, `get_scene_stats`, `get_authoring_root`, `progress`.

Treat any name above as *expected*, not guaranteed: re-run `unity command` and check.

## The loop: edit → recompile → test

```bash
# 1. REQUIRED before any headless/unfocused work: keep the Editor ticking.
unity command set_autotick --enable true

# 2. Edit C# on disk with normal file tools. (For scene/prefab/asset DATA, use the live
#    commands instead — see the rule in SKILL.md §1.)

# 3. Trigger compilation. It is async: a successful compile destroys the managed domain,
#    so the trigger request cannot also return the result.
unity command recompile
unity command recompile_status        # poll until completed | up_to_date; failed=true → errors[]
#    Connection errors mid-reload are expected. Retry; do not conclude the Editor died.

# 4. Tests.
unity command list_tests --mode editor                 # all | editor | playmode
unity command run_tests --mode editor --filter MyFixture.MyTest
unity command run_tests --mode playmode --async_tests true
unity command test_status                             # poll; cancel with cancel_tests
```

`run_tests` accepts `--filter` with `--filter_type testName|assembly|category`.

Without `set_autotick`, an unfocused Editor throttles its update loop and recompile or tests can
appear to hang. If something is stuck and you set the tick *after* triggering, re-trigger.

## Evaluating C# in the Editor

The fastest way to inspect or change live state — and often the right tool when no dedicated
command exists:

```bash
unity command eval 'return UnityEditor.EditorUserBuildSettings.activeBuildTarget;'
unity command eval 'var go = new UnityEngine.GameObject("Probe"); return go.scene.name;'
unity command eval_file Assets/Scratch.cs        # run the contents of a .cs file
```

Keep snippets self-contained and side-effect-aware: this runs in the Editor's real state, so a
statement that saves or deletes acts immediately.

## Play mode, screenshots, console

```bash
unity command editor_play                        # enter Play mode
unity command editor_pause
unity command editor_stop                        # back to edit mode
unity command screenshot --path Temp/shot.png    # Editor window
unity command capture_game_view --path Temp/game.png
unity command capture_scene_view --path Temp/scene.png
unity command console --tail 50 --level warn     # or clear_console
```

Capturing the rendered view is the cheapest way to **verify** a visual change instead of
asserting it landed. `unity command menu --path "GameObject/Light/Directional Light"` drives
anything that has a menu entry when no dedicated command exists.

## Static analysis: Project Auditor

```bash
unity command audit --categories Code,ProjectSetting --output Temp/audit.csv
unity command audit_status                       # poll until a terminal state
```

Terminal states: `completed` (`csvPath`, `issueCount`), `failed`, `unavailable`, `interrupted`
(a domain reload killed the scan — just re-run). `unavailable` means no Project Auditor or no
rules; in a built-in-module Editor the rules live in `com.unity.project-auditor-rules`
(`unity command package_add --identifier com.unity.project-auditor-rules --confirm true`).
One scan at a time: a second `audit` returns `busy`. Read `message` — the command never reports
a clean empty scan to mean "no analyzer".

## Development Player: build it, then drive it

```bash
# Editor: build a development Player (project-specific build pipelines usually wrap this)
unity command switch_build_target --target StandaloneOSX
unity command build --output Builds/Dev/MyGame.app

# Then, with the Player running:
unity command --runtime MyGame status
unity command --runtime MyGame log --level error
unity status --format json                       # Runtime instances also show up here
```

### Code reload (formerly "hot reload")

Change gameplay code in a **running** game with no domain reload. Tag the method
`[CodeReload]` (`Unity.Pipeline.CodeReload`), edit its body on disk, then apply it with one of
three commands — the tag must already be there before entering Play Mode or building, because
the dispatch prologue is woven in at compile time.

| Command | Backend | Where it works |
|---|---|---|
| `reload_file` | `Assembly.Load` | Editor Play Mode, Mono desktop dev builds. Full C#. |
| `reload_file_editor_interpreter` | IlInterpreter | Any Editor session, **IL2CPP-safe** (C# subset). |
| `reload_file_player_interpreter` | IlInterpreter | Pushes the IL to connected dev players over PlayerConnection — **works for IL2CPP players**, no open port needed. |

```bash
unity command reload_file --filename Assets/Spinner.cs
unity command reload_file --filename Assets/Spinner.cs --pdb          # debuggable (portable PDB)
unity command reload_file_editor_interpreter --filename Assets/Spinner.cs
unity command --runtime MyGame reload_file_player_interpreter --filename Assets/Spinner.cs
unity command reload_file_player_interpreter --filename Assets/HotReload --player -1   # folder → all players
```

Constraints: the method must be **public** (instance *or static*) and return **`void` or
`IEnumerator`**; `--pdb` is required for breakpoints on the `Assembly.Load` backend. A reload
can also declare *new* methods and call them, and `[OnCodeReload]` on a parameterless instance
method runs after a reload lands (the place to re-init state — think a domain-reload-free
`OnEnable`). The interpreter backend supports a C# subset only; see the package's
`Documentation~/code-reload.md` for the exact limits.

The old `[HotReload]` / `reload_file_override` / `hotreload_status` / `cleanup_hotreload`
surface is **gone** in pipeline 0.7.0 — if a skill or script still names it, that text predates
the rename.

## Exposing your own commands

The command surface is extensible from your own project — this is how a studio teaches an agent
its own build/verify steps. Mark a static method in an **Editor** assembly:

```csharp
using Unity.Pipeline.Commands;

[CliCommand("my_build_verify", "Build the player and verify it starts.",
            MainThreadRequired = true, Tags = new[] { "build/verify" })]
public static object BuildAndVerify(
    [CliArg("target", "Build target, e.g. StandaloneOSX", Required = true)] string target,
    [CliArg("timeout", "Seconds to wait", DefaultValue = 120)] int timeout = 120)
{
    // return an object; the CLI serializes it as the result
    return new { ok = true, target, timeout };
}
```

- `CliCommandAttribute(name, description)` with `MainThreadRequired` (default `true`),
  `RuntimeOnly` (hidden from Editor listings) and `Tags` (path-style, e.g. `"assets/import"`).
- `CliArgAttribute(name, description)` with `Required` and `DefaultValue`, on parameters or on
  the fields/properties of an `IStructuredCommandInput` DTO for nested object schemas.
- Commands are discoverable immediately (the CLI generates JSON schemas for them) — no extra
  registration.

Anything project-specific you add shows up in `unity command` for every agent, which is
preferable to documenting a multi-step shell incantation in a skill.

## Gotchas

- **`set_autotick` first.** Everything else can stall without it.
- **Async commands poll.** `recompile` → `recompile_status`; `run_tests --async_tests` →
  `test_status`; `audit` → `audit_status`; bakes → `*_bake_status`. The trigger response is
  never the result.
- **Hot reload needs a live game.** Enter Play mode or run a dev Player first.
- **Player commands need a dev Player** — the Runtime server does not exist in the Editor.
- **One command at a time per instance.** A long PlayMode/soak run holds the gate; don't
  dispatch into the same Editor while it runs.
- **Domain reloads drop connections.** That is the mechanism working, not a failure.
- **Tab completion of command names is not the contract** — `unity command` is.
