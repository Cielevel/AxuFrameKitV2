# 实时 Editor（com.unity.pipeline）

`com.unity.pipeline` 把 Unity Editor（以及开发版 Player）变成一个本地 HTTP 服务，
`unity` CLI 通过它执行命令。这是"让 Agent 真正操作 Unity"的那一层。

## 两个服务

| 服务 | 运行在 | 端口 | 谁启动 |
|---|---|---|---|
| Editor | Unity Editor | 7800–7849（测试用 7850–7899） | 打开工程时自动 |
| Runtime | **开发版** Player | 7900–7999（基段 7900–7949） | 场景里的 `RuntimePipelineManager`，`Awake` 时 |

两者命令集不同：`log`、`set_timescale`、`runtime_status` 等只在 Player 上存在。

## 安装与确认

```bash
unity pipeline install          # 装进当前工程（Unity 6+）
unity pipeline list             # 列出实例、包版本、是否 Safe Mode
unity pipeline upgrade          # CLI 较新时报 "too old to parse command lines" 就用它
unity status --format json      # state: ready | starting | unreachable
unity command editor_status     # Editor 服务是否应答
```

## 命令发现优先于记忆

命令由**当时运行的 Editor** 决定，随工程装的包而变：

```bash
unity command                   # 列出该实例暴露的全部命令（人类表格）
unity command --format json     # 带参数与描述的机器可读版本
unity command <名称> --help      # 单个命令的参数
unity command <名称> --result-only   # 只回结果，不带信封
```

多个 Editor 同时在线时必须指定目标，否则按当前 shell 的 cwd 解析：

```bash
unity command editor_status --project-path /abs/path/To/Project
unity command --instance 127.0.0.1:7801 editor_status
```

选不出来会返回 `AMBIGUOUS_EDITOR` 并给出候选。

## 核心闭环：改 → 编译 → 测

```bash
unity command set_autotick --enable true     # 先做。否则后台 Editor 会节流，命令看似卡死
# 在磁盘上改 C# …
unity command recompile                      # 异步：会触发域重载
unity command recompile_status               # 轮询到 completed | up_to_date
#   重载期间连接报错是【预期】；failed=true 时读 errors[]
unity command list_tests --mode editor
unity command run_tests --mode editor --filter MyFixture.MyTest
unity command run_tests --mode playmode --async_tests true
unity command test_status                    # 长跑用异步 + 轮询；cancel_tests 收回实例
```

**触发 ≠ 结果**：所有异步命令都必须轮询对应的状态命令。

## 数据文件改还是驱动改

| 对象 | 正确做法 |
|---|---|
| C# 源码 | 直接改磁盘，然后 `recompile` |
| 场景 / Prefab / 资产（`.unity`/`.prefab`/`.asset`） | **Editor 在线时一律用 `unity command`**，不要手写 YAML |

手写 YAML 的三个坑：fileID/GUID 由 Editor 分配；编辑器不重导入就看不到；
写的可能不是它当前打开的那个场景。

```bash
unity command list_open_scenes          # 先确认"活动场景"是哪个
unity command create_gameobject --name Prop
unity command attach_script --target Prop --script Assets/Scripts/Prop.cs
unity command set_component_properties --target Prop --component Prop --properties '{"speed":3}'
unity command save_scene
unity command screenshot --path Temp/verify.png     # 用图验证，而不是靠断言
```

## 没有专用命令时

```bash
unity command eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'
unity command eval_file Assets/Scratch.cs
unity command menu --path "GameObject/Light/Directional Light"
```

`eval` 在 Editor 的真实状态里执行——有副作用就会立刻生效。

## 常见命令族

约 160 条（含包提供的命令），典型分组：

- **编辑器控制**：`editor_play` / `editor_pause` / `editor_stop` / `editor_focus` / `set_autotick` / `menu`
- **脚本**：`recompile` / `recompile_status` / `eval` / `eval_file` / `read_text_file` / `write_text_file` / `console`（tail / level / since 游标）/ `clear_console`
- **场景与对象**：`create_scene` / `open_scene` / `save_scene` / `create_gameobject` / `find_gameobjects` / `create_prefab` / `apply_prefab_overrides` / …
- **组件**：`add_component` / `attach_script` / `get_component_properties` / `set_component_properties`
- **资产**：`create_asset` / `create_folder` / `copy_asset` / `move_asset` / `delete_asset` / `import_asset` / `find_assets` / `get_import_settings` / `set_import_settings`
- **烘焙**：`bake_lighting` / `bake_navmesh` / `bake_occlusion_culling` + 对应 `*_status` / `cancel_*`
- **构建设置**：`add_scene_to_build` / `switch_build_target` / `get_build_settings` / `set_build_settings`
- **包**：`package_list` / `package_add` / `package_remove` / `package_resolve`
- **测试**：`list_tests` / `run_tests` / `test_status` / `cancel_tests`
- **观测**：`screenshot` / `capture_scene_view` / `capture_game_view` / `get_performance_stats` / `progress`
- **静态分析**：`audit` + `audit_status`（Project Auditor，输出 CSV）

## 给工程加自己的命令

这是把"公司的构建/验收步骤"教给 Agent 的正道——比在 skill 里写一长串 shell 步骤好得多：

```csharp
using Unity.Pipeline.Commands;

[CliCommand("my_build_verify", "Build the player and verify it starts.",
            MainThreadRequired = true, Tags = new[] { "build/verify" })]
public static object BuildAndVerify(
    [CliArg("target", "Build target, e.g. StandaloneOSX", Required = true)] string target,
    [CliArg("timeout", "Seconds to wait", DefaultValue = 120)] int timeout = 120)
{
    return new { ok = true, target, timeout };   // 返回值会被序列化为命令结果
}
```

- `CliCommandAttribute(name, description)`：`MainThreadRequired`（默认 true）、
  `RuntimeOnly`（只在 Player 侧可见）、`Tags`（路径式标签）。
- `CliArgAttribute(name, description)`：`Required`、`DefaultValue`；也支持用在
  `IStructuredCommandInput` DTO 的字段/属性上，形成嵌套对象 schema。
- 注册后即时可发现，Agent 通过 `unity command` 就能看到。

## 开发版 Player 与热重载

```bash
# 构建开发版 Player 后
unity command --runtime MyGame status
unity command --runtime MyGame log --level error
```

热重载（Mono 限定：Editor Play Mode 或 Mono 桌面开发版，不支持 IL2CPP）：

打 `[CodeReload]`（`Unity.Pipeline.CodeReload`），改磁盘上的方法体，再按后端选命令：

| 命令 | 后端 | 适用范围 |
|---|---|---|
| `reload_file` | `Assembly.Load` | Editor Play Mode、Mono 桌面开发版；完整 C# |
| `reload_file_editor_interpreter` | IlInterpreter | 任意 Editor 会话，**IL2CPP 安全**（C# 子集） |
| `reload_file_player_interpreter` | IlInterpreter | 把 IL 推给已连接开发版 Player（走 PlayerConnection，**支持 IL2CPP**，不需要开端口） |

```bash
unity command reload_file --filename Assets/Spinner.cs
unity command reload_file --filename Assets/Spinner.cs --pdb          # 可断点（portable PDB）
unity command reload_file_editor_interpreter --filename Assets/Spinner.cs
unity command --runtime MyGame reload_file_player_interpreter --filename Assets/Spinner.cs
unity command reload_file_player_interpreter --filename Assets/HotReload --player -1   # 整个目录 → 所有 Player
```

约束：方法必须 `public`（实例或静态）、返回 `void` 或 `IEnumerator`；**标记必须在进 Play/构建之前就在**，
因为派发序言是编译期织入的。另可在无参实例方法上打 `[OnCodeReload]`，在重载落地后跑一次（相当于免域重载的
`OnEnable`，用来重建状态）。

> 0.7.0 起旧的 `[HotReload]` / `reload_file_override` / `hotreload_status` / `cleanup_hotreload`
> **已全部移除**；看到这些名字的文档都是旧的。

## 踩坑清单

- `set_autotick` 一定要先开。
- 异步命令一律轮询：`recompile`→`recompile_status`、`run_tests --async_tests`→`test_status`、
  `audit`→`audit_status`、烘焙→`*_bake_status`。
- 域重载期间的连接错误是**机制本身**，不是故障。
- 一个实例同一时刻只跑一件事：长 PlayMode / soak 运行期间别往里派新命令。
- Player 命令要有开发版 Player 才存在。

症状 → 处置的完整对照见 [故障排查](06-troubleshooting.md)，
给 Agent 看的精简版在 `Skills~/unity-cli-workflows/references/live-editor.md`。
