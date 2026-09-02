# 08 · Unity Pipeline 本地 API 使用指南（AI Agent 直连 Editor）

> 2026-08-29 实测验证（2026-08-30 修复 macOS 启动失败，见第 8 节）。本工程的 `com.unity.pipeline 0.5.0-exp.1`
> 已提升为**嵌入式包**（`Packages/com.unity.pipeline`，含 macOS 端口探测补丁；manifest.json 不再声明该条目），
> Unity 编辑器运行时会启动一个 **仅回环（127.0.0.1）的 HTTP 服务**，任何本机脚本/AI Agent 都可以直接调用它驱动 Editor。
> 已验证：读状态、枚举命令、执行 `console` 命令均成功。**注意：本文件包含的用法可让 agent 实际修改工程，使用时保持谨慎。**

## 0. 已内化的子代理（优先派发，无需手写 curl）

本能力已封装为用户级 + 工程级子代理（`~/.zcode/agents/` 与 `.zcode/agents/`，同名时**工程级覆盖**全局版；会话启动时加载）：

| 子代理 | 职责 | 边界 |
|---|---|---|
| `unity-pipeline` | 驱动运行中 Editor：recompile / run_tests / create_* / build / bake / audit / capture / console | 可执行（有副作用），按最小副作用纪律 |
| `unity-pipeline-observe` | 只读观测：状态 / Console / 测试清单 / 异步进度 | 只读，exec 白名单限只读命令 |

另有一套覆盖 Unity 全流程的子代理（architect / runtime-dev / editor-dev / ui-dev / asset-scene / tester / debugger / build / reviewer），其中 tester、asset-scene、debugger、build 也会按本文 API 执行受限操作。**完整清单与派发矩阵见 [09-unity-subagents.md](09-unity-subagents.md)**。

派发规则：Editor 运行中且需要"实际执行/验证"→ `unity-pipeline`；只需看状态/日志 → `unity-pipeline-observe`；Editor 未运行且需无头验证 → `unity-build`；纯日志分析 → `unity-debugger`。

## 1. 服务发现

Editor 启动后（[InitializeOnLoad] 自动启动），描述符写入：

```
Library/Pipeline/.unity-pipeline-port
```

内容（JSON）：`pid`、`port`、`projectPath`、`unityVersion`、`mode`（editor）、`startedAt`、`lastHeartbeat`、`evalToken`。
**端口动态分配**（本次实测 7800），每次读这个文件获取当前端口与 token。

## 2. 认证

所有请求带 `Authorization: Bearer <evalToken>`（token 用 `SecurityTokenManager` 管理，会轮换——以描述符里的最新值为准，服务端校验实时 token）。

## 3. API 端点

| 端点 | 方法 | 说明 |
|---|---|---|
| `/api/status` | GET | 服务状态：`settling`（启动导入/编译窗口）或 `ready` |
| `/api/editor_status` | GET | `compiling` / `domainReloadInProgress` / `playMode` / 项目路径 / 版本 |
| `/api/commands` | GET | **自描述命令清单**（实测 142 个），含名称/描述/参数 |
| `/api/exec` | POST | 执行命令（见下） |
| `/api/progress` | GET | 异步任务进度 |
| `/api/job`、`/api/job/cancel` | GET/POST | 后台任务（detached job）查询与取消 |

## 4. 执行命令（/api/exec）

```bash
TOKEN=$(python -c "import json;print(json.load(open('Library/Pipeline/.unity-pipeline-port'))['evalToken'])")
PORT=$(python -c "import json;print(json.load(open('Library/Pipeline/.unity-pipeline-port'))['port'])")
curl -s -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"command":"console","parameters":{},"timeout":15000}' \
  "http://127.0.0.1:$PORT/api/exec"
```

请求体：`{"command": "<命令名>", "parameters": {…}, "timeout": 毫秒, "job": false}`。
响应：`{"success": true, "command": …, "result": …, "executionTimeMs": …, "error": …}`。
启动后的"settle 窗口"内主线程命令会被 503 拒绝（可重试）；轮询 `/api/status` 等到 `ready` 再执行。

## 5. 对 Agent 最有价值的命令（142 个中的代表）

- **观测**：`console`（读 Console）、`editor_status`、`capture_game_view`/`capture_scene_view`（截图为 PNG）
- **编译/测试**：测试运行与取消（`cancel_tests` 对应的运行命令见 /api/commands）、`recompile_status` 类轮询
- **资产操作**：`create_script`、`create_asset`、`copy_asset`、`create_folder`、`find_assets`、`import` 系列
- **场景/对象**：`create_scene`、`create_gameobject`、`add_component`、`attach_script`、`create_prefab`(+variant)、`apply_prefab_overrides`
- **动画**：Animator/Timeline 的 layer/state/parameter/track/clip 全套
- **构建/烘焙**：`build`、`add_scene_to_build`、`bake_lighting`、`bake_navmesh`、`bake_occlusion_culling`（均异步+进度）
- **静态分析**：`audit`（Project Auditor 扫描）+ `audit_status`

完整清单每次以 `GET /api/commands` 实时为准。

## 6. 与 Unity CLI 的关系

- 两台机器均已装 **Unity CLI 0.1.0-beta.3**：
  - Windows: `C:\Users\cielevel\AppData\Local\Unity\bin\unity.exe`
  - macOS（2026-08-30 官方脚本安装）: `~/.unity/bin/unity`，PATH 经 `~/.zshrc` 源 `~/.unity/env`；
    官方安装命令 `curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash`
- 当前 beta 仅含管理命令（auth/editors/install/install-modules/install-path/language/projects/open/uninstall/upgrade/changelog…），
  **尚无** `command`/`eval`/`pipeline` 命令组——博客功能领先分发构建。
- 在 CLI 推出带 `command` 组的新 beta 前，直接按本文用 HTTP API 即可（功能等价，官方 CLI 本就是这层 API 的封装）。
- Pipeline 包随工程 git 同步；CLI 自身升级用 `unity upgrade`。

## 7. 注意事项

- `-automated` 警告：交互模式启动 Editor 时服务会警告"模态弹窗可能打断自动化流"。仅提醒；要消除可用
  `-automated` 参数启动 Editor（如 `Unity.exe -projectPath <路径> -automated`）。
- 服务仅监听回环地址（2026-08-30 起在 socket 层即真回环，见第 8 节），token 门控 eval——不暴露局域网；
  `Library/` 在 .gitignore 中，token 不会入库。
- Play 模式/`--runtime <player>` 的监听默认关闭（仅 dev 构建手动开启）。
- 包为 experimental（0.5.0-exp.1），API 可能随版本演进。

## 8. 修复记录：macOS "No available ports in range 7800-7849"（2026-08-30）

**现象**：Editor 启动即报 `Failed to start Pipeline Server: No available ports in range 7800-7849`，
`Library/Pipeline/.unity-pipeline-port` 不生成，整个 HTTP API 不可用。重启 Editor 无效。

**根因**：Unity 6000.0（Mono）的 `HttpListener` 对强通配符主机名 `http://+:port/` 抛
`SocketException: The requested address is not valid in this context`（EINVAL），而
`BasePipelineServer.IsPortAvailable` 正是用它逐个探测 7800-7849 共 50 个端口 → 全部被判定"不可用"。
实测系统层绑定（127.0.0.1 / ::1 / 0.0.0.0 / `*` 弱通配 / 显式回环前缀）全部正常，**并非端口被占**。

**修复**（已落地并验证 /api/status=ready + console 命令可用）：
- 包由 registry 提升为**嵌入式包**：`Packages/com.unity.pipeline`（已从 manifest.json 移除条目，
  packages-lock 中 source=embedded），排除缓存签名文件（`.signature` / `.attestation.p7m`）。
- 修补 `Runtime/Common/BasePipelineServer.cs` 的 `AddLoopbackPrefixes`：`http://+:port/` 改为显式
  回环三前缀 `http://127.0.0.1:port/`、`http://localhost:port/`、`http://[::1]:port/`（Mono 按 Host
  字面匹配前缀，三种写法都要覆盖；文档示例用 127.0.0.1 即可）。附带收益：监听在 socket 层真正只绑
  回环（原先 `+` 会绑全部网卡再靠代码层回环校验兜底），比原实现更安全。
- **升级该包版本时补丁会丢失，需按上述内容重打。**

**排查教训**：Editor 出现"不自动刷新、不重编译、AppleScript quit/activate 无响应、Editor.log 长时间静默"
时，先怀疑资产管道僵死（本次伴随 `Unexpected transport error from import worker 0`）。此时 `sample`
主线程只见空闲事件循环（非模态卡死），`osascript quit` 无效，但 `kill -TERM` 可 2 秒内优雅退出，
重启后完整包解析/编译/服务启动即恢复正常。

## 9. 工程侧自定义命令：`axu_test_*` / `axu_window_screenshot`（AxuTestFramework，2026-08-31 新增）

来源：嵌入式包 `Packages/com.cielevel.axu-test-framework/Editor/`（AxuTestFramework.Editor 程序集）经
`[CliCommand]` 由 TypeCache 自动发现注册，**pipeline 包源码零改动**。测试框架完整说明见包内 README。

### 命令一览

| 命令 | 参数 | 说明 |
|---|---|---|
| `axu_test_run` | `mode`(editor/playmode/all，默认 editor)、`assembly`、`category`(逗号分隔，**区分大小写**)、`filter`+`filter_type`(testName/assembly/category)、`clear_console`(默认 true)、`report`(默认 true)、`out_path`(可选留档；不以 .json 结尾视为目录)、`timeout_ms`(默认 120000) | 发起测试运行。**安全内置**：未显式指定 assembly 时自动限定"工程测试程序集"并**排除 `Unity.Pipeline.Tests*`**（返回值含 `ExcludedAssemblies`） |
| `axu_test_status` | 无 | 轮询运行状态（读 `Temp/axu_test_run_status.json`）；PlayMode 必用。running 超过 15 分钟视为陈旧不阻塞新运行 |
| `axu_test_report` | `latest`(bool) 或 `path` | 返回报告 JSON 原文；超过 256KB 返回 `{status:"too_large"}` 信封 |
| `axu_window_screenshot` | `window`(必填，EditorWindow 标题精确匹配)、`menu`(可选，窗口不存在时经此菜单自动打开)、`output`(可选 PNG 路径) | **Windows 专属、仅适用于没有 CaptureToRenderTexture 能力的 Unity 版本（< 6000.7，6000.7+ 不编译，请用 `capture_editor_element`）**。后台无感截取任意浮动 EditorWindow（PrintWindow 离屏渲染）：窗口可被遮挡/失焦，不抢焦点不置顶；限制：最小化/关闭(可经 menu 重开)/停靠状态无法截取。成功返回 `=> OK bytes=... path=...`。**macOS 无需实现**（2026-08-31 决策）：运行时平台守卫在任何 Win32 调用前返回说明文本，不崩溃无副作用；Mac 截图走跨平台路径（`AxuCapture` + `capture_game_view`/`capture_scene_view`/`screenshot`），不规划 CGWindowListCreateImage 变体。⚠️ **实测限制（2026-09-02）**：PrintWindow 对 Unity 这类 D3D 呈现内容可能返回 OK 但客户区**全白**（对照截 Hierarchy 同样白图，机制性限制）；需要窗口真实内容时回退 = eval 反射调用窗口方法（如 AxuTestRunner 的 `SetPage`）+ Show/Focus/Repaint 后按窗口矩形 `CopyFromScreen` 抓屏 |

### 调用示例

```bash
TOKEN=$(python3 -c "import json;print(json.load(open('Library/Pipeline/.unity-pipeline-port'))['evalToken'])")
PORT=$(python3 -c "import json;print(json.load(open('Library/Pipeline/.unity-pipeline-port'))['port'])")

# EditMode：同步返回完整结果（内部轮询至 completed）
curl -s -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"command":"axu_test_run","parameters":{"mode":"editor","assembly":"AxuFrameKit.Tests.Editor"}}' \
  "http://127.0.0.1:$PORT/api/exec"

# PlayMode：异步（进出 Play 触发域重载，必须异步）→ 轮询 → 收报告
curl -s -X POST ... -d '{"command":"axu_test_run","parameters":{"mode":"playmode","assembly":"AxuFrameKit.Tests.Runtime"}}' ...
curl -s -X POST ... -d '{"command":"axu_test_status","parameters":{}}' ...          # 循环至 completed
curl -s -X POST ... -d '{"command":"axu_test_report","parameters":{"latest":true}}' ...
```

# 后台无感截取浮动 EditorWindow（窗口可被遮挡/失焦；menu 可省略则只截已打开的）
curl -s -X POST ... -d '{"command":"axu_window_screenshot","parameters":{"window":"AxuTestRunner","menu":"AxuFrameKit/AxuTestRunner"}}' ...
# => "pw=1 ... => OK bytes=... path=<工程>/Temp/pipeline-screenshots/axu_win_AxuTestRunner_<时间戳>.png"
```

返回关键节：`Status`(completed/playmode_running/…)、`Summary{Total,Passed,Failed,Skipped,Inconclusive}`、
`ReportPath`、`ExcludedAssemblies`、`AllPassed`。

### 报告与产物

- 报告 JSON：`Library/AxuTestReports/{yyyyMMdd-HHmmss}-{mode}.json`，schema `axu-test-report/1`
  （`StartedAt/EndedAt/Mode/Filter/Summary/Results[]/Console/Artifacts[]/Environment`，失败含 Message/StackTrace）。
- 截图 artifacts：`Library/AxuTestReports/artifacts/`（测试内 `AxuCapture.ScreenshotCoroutine` 产生）。
- 报告由程序集级 `[assembly: TestRunCallback]`（AxuTestReportWriter）在 RunFinished 落盘——**域重载开/关两种形态均验证可用**（2026-08-31 spike，见 `.zcode/reports/2026-08-31-playmode-spike.md`）。
- pipeline 自带 `run_tests` 发起的运行同样会出报告（mode 记 `unknown`）。

### 事故警示（2026-08-31 实证）

pipeline 自带 `run_tests` **无过滤裸跑**会实跑 `Unity.Pipeline.Tests.*`，其中 `SecurityTokenTests` 的
SetUp/TearDown 调 `SecurityTokenManager.ClearCache()` 毒化会话 token → 之后所有请求 401 死锁，需触发
域重载重启服务恢复。**直用 `run_tests` 必须带 assembly 过滤；优先改用 `axu_test_run`**。

### 配套窗口

菜单 `AxuFrameKit/AxuTestRunner`（UITK 看板：套件树/结果树/模式切换/最近报告），与 HTTP 命令共用
`AxuTestRunService` 编排核心；与内置 `run_tests` 之间有并发检测（`Temp/pipeline_test_request.json`）。

---

## 10. 场景测试命令：`axu_scenario_*` / `axu_player_*` / `axu_trend_*`（AxuTestFramework v2，2026-09-01 新增）

来源：`Packages/com.cielevel.axu-test-framework/Editor.Pipeline/`（AxuTestFramework.Editor.Pipeline 程序集，
`AXU_PIPELINE_PRESENT` 守护——未装 pipeline 包不编译，不影响包内其余功能）。**与 `axu_test_run` 共享互斥闸**：
UTF / scenario / player 三类运行禁止在同一 Editor 并发驱动；**Play 长跑（soak 等）期间严禁并行派发其他命令**
（2026-09-02 事故实证：并行取消链在 Play Mode 中触发 UTF 收尾任务崩溃污染运行）。词汇隔离：`axu_test_*`
永远指 UTF 测试，本节命令永远指 scenario 套件。

### 命令一览

| 命令 | 参数 | 说明 |
|---|---|---|
| `axu_scenario_list` | — | 枚举内建能力（echo/perf/loadtime/smoke/soak/stress/monkey + gamehook 注册规范）+ Assets 下 `*.suite.json` / `*.scenario.json`（含 scenarioIds 摘要） |
| `axu_scenario_run` | `suite`（必填，工程相对/绝对路径）、`scenario`（可选，逗号分隔 id 过滤） | Editor PlayMode 内驱动 Agent Runner。**异步启动即返**，经 `axu_scenario_status` 轮询；完成后看板/服务自动趋势入库并退 Play |
| `axu_scenario_status` | — | 读 `Temp/axu_scenario_status.json` 原文（running/completed/error + completedCount/totalCount/reportPath/exitCode/summary；idle 时返回 idle 信封）。Play Mode 域重载窗口内 HTTP 可能短暂拿不到响应，以状态文件为准 |
| `axu_player_suite_build` | `output`（默认 `Builds/AxuScenarioPlayer/`） | Development Build；构建期临时确保 `AXU_TEST_AGENT` define 开启，`finally` 还原；场景清单 = Build Settings |
| `axu_player_suite_run` | `suite`（必填）、`player`（默认 `Builds/AxuScenarioPlayer`，支持 .app/目录/可执行）、`scenario`、`seed`、`warn_as_fail`、`timeout_ms`（默认 3600000，0=无限，超时 Kill） | 启动 Player 进程（`-axuSuite -axuQuitOnDone -axuReport`）→ 等退出码 → 回拉报告到 `Library/AxuTestReports/scenario/player-{时间戳}-{suite}/` → 趋势入库 |
| `axu_trend_summary` | `scenario` / `suite`（可选过滤）、`last_n`（默认 10） | 读趋势 CSV，输出各指标最近 N 次序列与环比（方向感知：**正值恒=变差**） |

退出码语义（scenario 侧）：0=全 pass；1=有 fail；2=仅 warn（`warn_as_fail` 可升级为 fail）。

### 调用示例

```bash
# Editor 内跑 suite：异步 → 轮询状态文件 → 收报告
curl -s -X POST ... -d '{"command":"axu_scenario_run","parameters":{"suite":"Assets/Tests/Scenarios/echo-demo.suite.json"}}' ...
curl -s -X POST ... -d '{"command":"axu_scenario_status","parameters":{}}' ...   # 循环至 completed/error
# 报告在 reportPath 字段指向的 Library/AxuTestReports/scenario/{时间戳}-{suite名}.json

# 真机/独立 Player：构建 + 运行（报告自动回拉并入库趋势）
curl -s -X POST ... -d '{"command":"axu_player_suite_build","parameters":{}}' ...
curl -s -X POST ... -d '{"command":"axu_player_suite_run","parameters":{"suite":"echo-demo.suite.json"}}' ...

# 趋势：某 scenario 最近 10 次各指标环比
curl -s -X POST ... -d '{"command":"axu_trend_summary","parameters":{"scenario":"echo.demo"}}' ...
```

### 产物

- 报告：`Library/AxuTestReports/scenario/{yyyyMMdd-HHmmss}-{suite名}.json`（schema `axu-scenario-report/1`；
  soak 中断残留为同目录 `{报告名}.partial.json`，`truncated:true`）。
- 趋势：`Library/AxuTestReports/trend/trend.csv`（schema `axu-trend/1` 追加式长表；metric 键与 baselineDiff 扁平键同源）。
- 基线：suite 旁 `baselines/{scenarioId}.baseline.json`（schema `axu-baseline/1`）；基线跨机不可比
  （录制环境不同会出 BASELINE_REGRESSION 假阳性，换机需重录）。
- suite 编写/接入步骤见包 README「场景测试接入一页纸」；schema 全集见包内 `DOC/ScenarioSchema.md`。
