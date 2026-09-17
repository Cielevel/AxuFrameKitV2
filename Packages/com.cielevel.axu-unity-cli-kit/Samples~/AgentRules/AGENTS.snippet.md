# Sample: an `AGENTS.md` / `CLAUDE.md` block

The kit's `agents-md` target writes a block like this one into your project's `AGENTS.md`,
delimited so it can be rewritten without touching the rest of the file:

```
<!-- axu-cli-kit:begin -->
...
<!-- axu-cli-kit:end -->
```

If your project keeps its rules in `CLAUDE.md`, `.cursor/rules/`, or a similar file rather than
`AGENTS.md`, paste this block there by hand — or enable the matching target in
`AxuFrameKit ▸ CLI Agent Kit` and let the installer maintain it.

---

<!-- axu-cli-kit:begin -->
## Unity command-line work (installed by com.cielevel.axu-unity-cli-kit)

Use when a task touches Unity from outside the Editor or needs a running Editor driven —
checking whether a Unity Editor is live and then controlling it (scenes, GameObjects, prefabs,
assets, play mode, screenshots, C# eval, recompile), running EditMode/PlayMode tests, headless
build or run, installing or licensing Editors and modules, wiring Unity into CI, or triaging a
failing `unity` CLI / Pipeline command.

Full skill: `.claude/skills/unity-cli-workflows/SKILL.md`

## Guardrails

- Don't invent command names or flags — `unity command` (bare) and `unity <group> --help` are
  the source of truth.
- Don't run a bare `run_tests` with no filter on a shared Editor.
- Don't put secrets in shell history or process arguments — prefer `--git-token-stdin` /
  `--secret-from-stdin` fed from a secret store.
- Don't dispatch a second CLI command at an instance while a long PlayMode/soak run is in flight.
<!-- axu-cli-kit:end -->
