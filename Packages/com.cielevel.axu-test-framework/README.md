# AxuTestFramework

AxuFrameKit 体系之外的**通用测试框架包**：覆盖 EditMode 与 PlayMode，为框架包 / 模块包 / indie 包提供统一的测试工具层与结构化报告通道。**零框架依赖**（不引用任何 `AxuFrameKit.*` 程序集），可独立于本工程复用。

设计上与 [Unity Pipeline 本地 API]（`com.unity.pipeline`，见工程 `.zcode/08-unity-pipeline-api.md`）配合，形成
"Agent/人 → pipeline 驱动 → UTF 跑测试 → `[TestRunCallback]` 收结构化 JSON 报告 → 看板"的闭环；人与 AI 代理均可驱动。

## 包定位

- **Runtime 程序集 `AxuTestFramework`**：测试编写工具层（等待 / 场景 / 截图 / 日志监视 / 静态重置 / 报告模型）。
  全平台可编译，`defineConstraints: [UNITY_INCLUDE_TESTS]` 保证正常出包时**不参与编译**，运行时零影响。
- **Core 程序集 `AxuTestFramework.Core`**：场景测试平台的纯 C# 基座（scenario/suite 模型与加载、指标统计、
  预算/基线/趋势、报告模型、JSON 封装）。`noEngineReferences: true`，唯一外部依赖 Newtonsoft.Json。
  schema 见 [`DOC/ScenarioSchema.md`](DOC/ScenarioSchema.md)。
- **Agent 程序集 `AxuTestFramework.Agent`**：Player 可编译的场景测试运行时
  （Bootstrap / Runner / 采集器 / 八能力 / 工具集）。只引用 Core 与 Newtonsoft.Json——对
  `Unity.InputSystem` / `com.unity.ugui` **零硬引用**（backlog #5 正规化）：monkey 输入注入与 UGUI
  降级分别由可选守护程序集 `AxuTestFramework.Agent.Input`（`defineConstraints: [AXU_INPUTSYSTEM_PRESENT]`，
  由 versionDefines 按 `com.unity.inputsystem ≥1.0` 注入）与 `AxuTestFramework.Agent.UGUI`
  （`AXU_UGUI_PRESENT`，按 `com.unity.ugui ≥1.0` 注入）提供；未装对应包时该程序集整体不编译，
  主程序集与包其余功能零影响（后端经 `AgentInputBackends` 按优先级反射发现）。公开入口以
  `AXU_TEST_AGENT` define 守护（不开启则不产生激活代码）。
- **Editor 程序集 `AxuTestFramework.Editor`**：报告落盘（`[TestRunCallback]` → `AxuTestReportWriter`）、
  scenario 运行编排（桥文件 + 看门狗 + 趋势入库）、导出器与看板窗口（菜单 `AxuFrameKit/AxuTestRunner`，
  Tests / Scenario / Trend 四页签）。`autoReferenced: false`，仅 Editor 参与；零 pipeline 引用。
- **Editor 程序集 `AxuTestFramework.Editor.Pipeline`**：全部引用 pipeline 的 `[CliCommand]` 命令
  （`axu_test_*` / `axu_scenario_*` / `axu_player_*` / `axu_trend_*` / `axu_window_screenshot`）经 TypeCache 自动注册。
  `defineConstraints: [AXU_PIPELINE_PRESENT]`（由本程序集 versionDefines 按 `com.unity.pipeline ≥0.5.0-exp` 注入）——
  **未装 pipeline 包的工程该程序集整体不编译，包内其余功能不受影响**。
- **Samples~**：EditMode / PlayMode 各一组最小示例，经 Package Manager → Samples 导入。

## 依赖

| 依赖 | 说明 |
|---|---|
| `com.unity.test-framework` 1.4.6 | UTF（Unity Test Framework），必需 |
| `com.unity.nuget.newtonsoft-json` 3.2.2 | 报告 JSON 序列化，必需（asmdef 引用名 `Unity.Nuget.Newtonsoft-Json`） |
| `com.unity.pipeline` ≥0.5.0-exp.1（本工程 0.7.0-exp.1） | **可选**（0.2.0 起不再声明在 dependencies；`AxuTestFramework.Editor.Pipeline.asmdef` 的 versionDefine 表达式为 `0.5.0-exp`，按最小值语义匹配更高版本，2026-09-17 升到 0.7.0-exp.1 后该程序集仍正常编译）。安装时 `AxuTestFramework.Editor.Pipeline` 程序集经 TypeCache 自动注册 `axu_test_*` 自定义命令到 pipeline 本地 HTTP API，获得"命令行跑测试 + 收报告"能力；未安装则该程序集整体不编译，包内其余功能（工具层 / 报告 / 看板）完整可用。其他工程如需命令驱动再自行拷入该包（升级版本注意 macOS 端口探测补丁，按使用工程内文档重打） |
| `com.unity.inputsystem` ≥1.0 / `com.unity.ugui` ≥1.0 | **可选**（不声明在 dependencies）：分别驱动 `AxuTestFramework.Agent.Input` / `AxuTestFramework.Agent.UGUI` 两个守护程序集（monkey 输入注入主路径 / UGUI 命中与降级点按）。**未装任一时主 Agent 程序集照样编译**；两者都缺时 monkey 仅记录坐标并出一次性 info finding |

## 目录结构

```
com.cielevel.axu-test-framework/
├── package.json / README.md / CHANGELOG.md
├── DOC/ScenarioSchema.md             # axu-scenario/1、axu-suite/1、axu-scenario-report/1、axu-baseline/1 schema 文档
├── Core/                             # asmdef: AxuTestFramework.Core（纯 C#，noEngineReferences，仅依赖 Newtonsoft.Json）
│   ├── Scenarios/                    #   ScenarioDef / SuiteDef / SuiteLoader（profile 扇出）/ ScenarioRunHandle / ScenarioCli（-axu* 命令行解析）
│   ├── Metrics/                      #   MetricStats（百分位/最小二乘斜率）/ RingBuffer / MetricDirection 方向元数据
│   ├── Budget/                       #   BudgetEvaluator（两段键 <collector>.<stat>）
│   ├── Baseline/                     #   BaselineFile（axu-baseline/1）/ BaselineDiff（方向感知）
│   ├── Trend/                        #   TrendPoint / TrendStore（RFC 4180 CSV）/ TrendSummary（环比）
│   ├── Report/                       #   ScenarioReport / Finding / ReportEnvironment（axu-scenario-report/1）
│   └── Json/                         #   AxuJson（Newtonsoft 薄封装 + schema 校验）
├── Agent/                            # asmdef: AxuTestFramework.Agent（Player 可编译；只依赖 Core，输入后端零硬引用）
│   ├── Bootstrap/                    #   AgentBootstrap（RuntimeInitializeOnLoad）/ AxuTestAgent 门面 / 能力·场景注册表
│   ├── Runner/                       #   AgentRunnerHost（嵌套迭代器驱动 + 看门狗）/ ScenarioLaunchOptions / ScenarioStatusFile
│   ├── Capabilities/                 #   Echo / Perf / LoadTime / Smoke / Soak / Stress / Monkey / GameHook（各带 DOC.md）
│   ├── Collectors/                   #   fps / frameTime / memory / rendering / gc 五采集器（默认 250ms 聚合）
│   ├── Metrics/                      #   AxuMetrics 零分配 no-op 门面 / MetricSession
│   ├── Tools/                        #   AgentEnvironment / AgentScenes / AgentCapture / AgentLogWatch / AgentInput（门面，后端反射发现）/ AgentInputBackends / StressSpawner / AgentPaths / AgentWait
│   ├── Input/                        #   asmdef: AxuTestFramework.Agent.Input（AXU_INPUTSYSTEM_PRESENT 守护）——InputSystem 注入后端
│   ├── UGUI/                         #   asmdef: AxuTestFramework.Agent.UGUI（AXU_UGUI_PRESENT 守护）——UGUI 命中/降级后端
│   └── Reports/                      #   ScenarioReportWriter（axu-scenario-report/1 落盘；soak 的 partial flush 在 SoakRunner 内实现）
├── Runtime/                          # asmdef: AxuTestFramework（全平台，UNITY_INCLUDE_TESTS 守护）
│   ├── Wait/AxuWait.cs               #   帧 / 条件 / 秒等待（[UnityTest] 协程 yield）
│   ├── Scene/AxuTestScene.cs         #   场景按路径加载 / 卸载 / 重载
│   ├── Capture/AxuCapture.cs         #   PlayMode 截图到报告 artifacts 目录
│   ├── Logging/                      #   AxuLogWatch / AxuNoErrorsScope / LogEntry
│   ├── State/AxuTestState.cs         #   静态/单例重置登记表（域重载关闭纪律）
│   └── Reports/                      #   UTF 报告 POCO（schema: axu-test-report/1）
├── Editor/                           # asmdef: AxuTestFramework.Editor（Editor-only, autoReferenced:false；零 pipeline 引用）
│   ├── Commands/                     #   UTF 运行编排（AxuTestRunService / 程序集扫描 / 请求·结果模型）
│   ├── Reports/                      #   AxuTestReportWriter（[TestRunCallback]）/ AxuConsoleRing / 路径约定
│   ├── Scenarios/                    #   ScenarioRunService（桥文件+看门狗+趋势入库+自动退 Play）/ 菜单族
│   │                                 #   （Enable Test Agent / Create Scenario·Suite JSON / Run Suite JSON... / Deploy）
│   ├── Export/                       #   JUnitExporter / HtmlSummaryExporter
│   ├── Window/AxuTestRunnerWindow.cs #   UITK 看板（AxuFrameKit/AxuTestRunner，总览 / Tests / Scenario / Trend 页签）
│   ├── Window/AxuTestRunnerWindow.ScenarioTrend.cs  #  Scenario / Trend 页（partial）
│   ├── Window/AxuTestRunnerWindow.Overview.cs       #  总览页（partial，默认页）
│   └── UI/AxuTestTokens.uss          #   窗口样式 token（独立自持，仿 KitBox 意象）
├── Editor.Pipeline/                  # asmdef: AxuTestFramework.Editor.Pipeline（AXU_PIPELINE_PRESENT 守护）
│   └── Commands/
│       ├── AxuTestPipelineCommands.cs       # axu_test_run / axu_test_report / axu_test_status
│       ├── AxuScenarioPipelineCommands.cs   # axu_scenario_list / axu_scenario_run / axu_scenario_status
│       ├── AxuPlayerSuiteCommands.cs        # axu_player_suite_build / axu_player_suite_run
│       ├── AxuTrendCommand.cs               # axu_trend_summary
│       └── AxuWindowScreenshotCommand.cs    # axu_window_screenshot（Windows 专属）
├── Tests/Editor/                     # asmdef: AxuTestFramework.Tests.Editor（包自身契约单测，223 例）
└── Samples~/
    ├── Tests/{Editor,Runtime}/       # UTF 最小示例（PM → Samples 导入）
    └── Scenarios/                    # 每能力一份 example suite + README（PM → Samples 导入）
```

## Editor 层（报告落盘 / axu_test_* 命令 / 看板）

### 报告 schema `axu-test-report/1`

每次测试运行结束（`[TestRunCallback]`，域重载开/关两种形态都成立）自动落盘：

```
Library/AxuTestReports/{yyyyMMdd-HHmmss}-{mode}.json     # 报告（out_path 可覆盖）
Library/AxuTestReports/artifacts/*.png                    # AxuCapture 截图产物
```

| 节 | 内容 |
|---|---|
| `schema` / `startedAt` / `endedAt` / `mode` | 标识与时间（UTC ISO 8601；mode = editor / playmode / unknown） |
| `filter` | `{type, value}`：assembly / category / testName / **testNames**（选定运行，Value 为用例数描述）/ none |
| `summary` | `{total, passed, failed, skipped, inconclusive}` |
| `results[]` | 叶子用例：`fullName / status(NUnit ResultState) / durationMs / message / stackTrace / assembly / categories` |
| `console` | 运行期 Console 采样：`errorCount`（Error+Exception+Assert）/ `warningCount` / `samples[]`（≤20 条，单条截断 500 字） |
| `artifacts[]` | `{test, screenshot, note}`——取 `AxuCapture.CapturedPaths`；无产物时缺省 |
| `environment` | `unityVersion / pipelineVersion / reporterVersion` |

运行参数（mode/filter/report/out_path/startedAt）经 `Temp/axu_test_run_context.json` 在启动前写入、RunFinished 时读回后删除——
这是域重载开启时 PlayMode 报告仍能拿到参数的唯一可靠载体（静态字段跨不过域重载）。

### axu_test_run（pipeline 自定义命令）

```
POST /api/exec  {"command":"axu_test_run","parameters":{...},"timeout":150000}
```

| 参数 | 默认 | 说明 |
|---|---|---|
| `mode` | `editor` | `editor` / `playmode` / `all`（all = EditMode 同步完成后接 PlayMode 异步） |
| `filter` | 空 | 过滤值；`filter_type=testName` 时为**大小写不敏感的全名包含匹配**（`test_names` 非空时不参与） |
| `filter_type` | `testName` | `testName` / `assembly` / `category` |
| `category` | 空 | 便捷参数，等价 `filter_type=category`；逗号分隔多个 |
| `assembly` | 空 | 便捷参数，等价 `filter_type=assembly`；对安全程序集清单做包含匹配 |
| `test_names` | 空 | **"选定测试"通道**：用例**精确全名**的 JSON 数组字符串（如 `["Ns.Class.Method","Ns.Class.Method(1,2)"]`；不要用逗号分隔——参数化用例全名含逗号）。非空时优先于 `filter`（testName 子串），与安全程序集清单 AND 收敛；0 匹配直接报错（含"勾选用例不属于当前模式"提示）。注意：经 `testNames` 点名的 `[Explicit]` 用例会被执行（UTF 语义：点名即跑），全量跑时仍跳过 |
| `clear_console` | `true` | 运行前清空 Editor Console 窗口 |
| `report` | `true` | 是否落盘报告 JSON |
| `out_path` | 空 | 报告路径覆盖：`.json` 结尾视为文件，否则视为目录；相对工程根或绝对路径。artifacts 同步重定向到 `<报告目录>/artifacts/` |
| `timeout_ms` | `120000` | EditMode 完成等待预算；HTTP 请求 timeout 需大于它 |

- **EditMode**：同步等待（内部轮询状态文件）后直接返回 `{status:"completed", summary, allPassed, reportPath, assemblies, ...}`。
- **PlayMode**：**强制异步**——立即返回 `{status:"playmode_running", statusPath, assemblies, ...}`，随后轮询
  `axu_test_status` 至 `completed`，报告路径在状态文件的 `reportPath`。Editor 会自动进入并退出 Play 模式。
- **安全行为（硬性）**：`assemblyNames` 恒为安全测试程序集清单——`CompilationPipeline` 枚举引用了 TestRunner 的
  程序集并**无条件剔除 `Unity.Pipeline.Tests*`**（其 `SecurityTokenTests` 会清空 pipeline 本地 API 鉴权 token，
  导致全部请求 401 的"token 中毒"事故）。返回值中的 `assemblies`（实际运行）与 `excludedAssemblies`（被剔除）即审计线索。
  无程序集范围收敛的裸跑在本命令中不存在。
- **并发**：axu_* 命令与看板窗口之间有静态闸互斥；pipeline 自带 `run_tests` 无法纳入该闸（不改 pipeline 包），
  请勿与 `axu_test_run` 并发调用（UTF 自身单运行约束兜底）。EditMode 同步等待期间 pipeline 的执行闸会阻塞其他命令，
  与内置 `run_tests` 同步模式行为一致。

示例：

```bash
TOKEN=$(python3 -c "import json;print(json.load(open('Library/Pipeline/.unity-pipeline-port'))['evalToken'])")
PORT=$(python3 -c "import json;print(json.load(open('Library/Pipeline/.unity-pipeline-port'))['port'])")

# EditMode：按程序集
curl -s -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"command":"axu_test_run","parameters":{"mode":"editor","assembly":"AxuFrameKitV2.Tests.Editor"},"timeout":150000}' \
  "http://127.0.0.1:$PORT/api/exec"

# PlayMode：异步 + 轮询
curl -s ... -d '{"command":"axu_test_run","parameters":{"mode":"playmode","assembly":"AxuFrameKit.Tests.Runtime"}}' ...
curl -s ... -d '{"command":"axu_test_status","parameters":{}}' ...

# 按类别
... {"mode":"editor","category":"Axu/Smoke"} ...

# 选定测试：精确全名 JSON 数组（优先于 filter；与安全程序集清单取交集）
... {"mode":"editor","test_names":"[\"AxuFrameKit.Tests.Editor.AxuFrameKit.Tests.Editor.CoreMathEaseTests\",\"AxuFrameKit.Tests.Runtime.SomeClass.SomeTest\"]","timeout_ms":120000} ...
```

### axu_test_report / axu_test_status

- `axu_test_report`：`{"latest":true}`（默认；`LastReportPath` → 报告目录里最新文件）或 `{"path":"..."}`；
  返回报告 JSON **原文**。超过 256KB 返回 `{status:"too_large", path, sizeBytes, limitBytes}`，请直接读文件。
- `axu_test_status`：读 `Temp/axu_test_run_status.json` 原文；`status` 为 `running` / `completed` / `error`，
  含 `summary`、`reportPath`、`startedAtUtc`（判断陈旧状态）；无文件返回 `{status:"idle"}`。

### axu_window_screenshot（Windows 专属回退：后台无感窗口截图）

> **适用范围**：① 仅 **Windows**；② 仅适用于**没有 CaptureToRenderTexture 能力的 Unity 版本（< 6000.7）**——
> 本实现在 6000.7+ 不参与编译，届时请改用 pipeline 自带的 `capture_editor_element`（引擎级离屏渲染，跨平台、无需窗口可见）。
>
> **macOS 无需对应实现（2026-08-31 决策）**：命令入口有运行时平台守卫，非 WindowsEditor 在任何 Win32 调用之前
> 即返回一句说明文本，不崩溃、无副作用。Mac 截图能力不缺：测试内截图走 `AxuCapture`（跨平台引擎 API），
> pipeline 侧 `capture_game_view` / `capture_scene_view` / `screenshot` 亦跨平台。若未来 <6000.7 的 Mac
> 确需任意 EditorWindow 后台截图，优先升级 Unity 到 6000.7+ 使用 `capture_editor_element`，
> **不要**另写 CGWindowListCreateImage 变体。

- `POST /api/exec {"command":"axu_window_screenshot","parameters":{...}}`
- `window`（必填）：EditorWindow 标题（titleContent.text 精确匹配）
- `menu`（可选）：窗口不存在时经此菜单路径自动打开（如 `AxuFrameKit/AxuTestRunner`）
- `output`（可选）：PNG 路径，默认 `Temp/pipeline-screenshots/axu_win_<标题>_<时间戳>.png`
- 原理：`PrintWindow(PW_RENDERFULLCONTENT)` 离屏渲染 + `GetDIBits`/`GetBitmapBits` 读回。
  **窗口可处于被遮挡/失焦状态，全程不抢焦点、不置顶**；限制：最小化或已关闭无法截取（可经 menu 重开）、
  停靠（docked）窗口没有独立 OS 窗口无法截取
- 实现说明：`GetDIBits` 在部分系统组合下静默失败，命令内建 `GetBitmapBits` 兜底并统一翻转行序；
  原生窗口按 进程PID + 客户区尺寸最优匹配 定位（Unity 浮动窗口的 Win32 标题可能为空）

### 看板窗口

菜单 **`AxuFrameKit/AxuTestRunner`**（与 AxuKitBox 同级；样式仿 KitBox 但包内自持独立 USS，无任何代码引用）。
窗口为四页签，**默认落在「总览」页**：

- **总览（默认页）**：最近一次 suite 运行的大结果徽章（✓全部通过 / ⚠通过但有警告 / ✗有失败 / —尚无记录）
  + 趋势速览（各场景最近 8 次运行的彩色圆点序列，点击某行直达其趋势页）+ 三个快捷大按钮
  （运行场景套件 / 查看趋势 / 刷新）；面向不看技术细节的使用者，数据与 Scenario/Trend 页同源。
- **Tests 页（单元测试）**：左侧测试套件树（按程序集/类分组，顶部过滤输入做全名包含匹配），
  右侧最近报告结果树（FAIL 默认展开，消息/堆栈/类别可看），
  顶栏 EditMode/PlayMode 切换 + **Run 全部 / Run 选定（N）** 双按钮 + 刷新 + 最近报告路径（点击 Reveal）。
  窗口 Run 与 `axu_test_run` 走同一内部服务。

- **Run 全部**：当前模式全量（过滤输入非空时作为 testName 子串过滤收窄）。
- **Run 选定**：左侧树勾选用例经 `test_names` 精确通道执行（按当前模式；勾选用例不属于当前模式时被预检 0 匹配拦下）。
  用例行带勾选框；程序集/命名空间/类各级 Foldout 头部带**三态勾选框**——点击批量勾选/取消其下当前可见用例
  （全选态点击=全不选，其余态点击=全选；部分勾选显示为橙色填充态），勾选框拦截冒泡不连带折叠层级。
  勾选集合按 FullName 键控，跨树重建/模式切换保留；过滤输入触发的树重建后三态按可见叶子重算（所见即所勾）。

### 与 pipeline 自带 run_tests 的关系

`[TestRunCallback]` 报告器对**所有**测试运行生效（含 pipeline 自带 `run_tests` 发起的运行）——上下文文件缺失时报告
`mode` 记为 `unknown`，其余内容照常收集。若只要 axu 报告链路的完整行为，用 `axu_test_run`。

## Scenario 工具链（v2 Phase 4 起：axu_scenario_* / axu_player_* / axu_trend_*）

词汇隔离：`axu_test_*` 永远指 UTF 测试；`axu_scenario_*` / `axu_player_*` / `axu_trend_*` 指 scenario 套件运行。
六个命令均在 `Editor.Pipeline` 程序集（`AXU_PIPELINE_PRESENT` 守护），与 `axu_test_run` **共享互斥闸**——
scenario / player / UTF 三类运行禁止在同一 Editor 并发驱动。

### pipeline 命令一览

| 命令 | 参数 | 说明 |
|---|---|---|
| `axu_scenario_list` | — | 枚举内建能力（echo/perf/loadtime/smoke + P1：soak/stress/monkey）+ Assets 下 `*.suite.json` / `*.scenario.json`（含 scenarioIds 摘要） |
| `axu_scenario_run` | `suite`（必填，工程相对/绝对）、`scenario`（可选，逗号分隔 id） | Editor PlayMode 内驱动 Agent Runner；**异步启动即返**，经 `axu_scenario_status` 轮询 `Temp/axu_scenario_status.json`（running/completed/error + reportPath/exitCode）；完成后看门狗自动趋势入库并退 Play |
| `axu_scenario_status` | — | 读运行状态文件原文（idle 时返回 idle 信封） |
| `axu_player_suite_build` | `output`（默认 `Builds/AxuScenarioPlayer/<产品名>.app`；**默认后缀为 macOS .app——Windows/Linux 构建请显式传 `output` 目录**） | Development Build；构建期临时确保 `AXU_TEST_AGENT` define 开启，`finally` 还原；场景 = Build Settings |
| `axu_player_suite_run` | `suite`（必填）、`player`（默认 `Builds/AxuScenarioPlayer`，支持 .app/目录/可执行）、`scenario`、`seed`、`warn_as_fail`、`timeout_ms`（默认 3600000，0=无限，超时 Kill） | 启动 Player 进程（`-axuSuite -axuQuitOnDone -axuReport …`）→ 等退出码 → 回拉报告到 `Library/AxuTestReports/scenario/player-{时间戳}-{suite}/` → 趋势入库 |
| `axu_trend_summary` | `scenario` / `suite`（可选过滤）、`last_n`（默认 10） | 读趋势 CSV，输出各指标最近 N 次序列与最近两次变化百分比（方向感知：**正值恒=变差**；fps 负值=改善，frameTime/memory 正值=劣化） |

退出码语义：0=全 pass；1=有 fail；2=仅 warn（warn_as_fail 可升级）。

### 趋势 CSV（schema `axu-trend/1`）

- 路径：`Library/AxuTestReports/trend/trend.csv`（追加式长表，不入库）。
- 列序：`timestampUtc,suite,scenario,capability,result,exitCode,durationMs,metric,value`；每条 metric 一行，
  另有 `metric` 为空串的 **run 级行**（`value` 空，携带整体 result/exitCode/durationMs）。
- metric 键与基线 diff 同源（`collector.stat` 扁平键，如 `fps.avg`、`scene.Demo_Scene.loadMs`）。
- 读写经 Core `TrendStore`（RFC 4180 引号/换行转义，跨平台一致）。

### Editor 侧配套

- **看板 Scenario 页**：扫描并列出 suite/scenario JSON（列表显示中文标题与能力清单、点击选中高亮、底部三步引导）→ Run Suite（含 define 预检）→ 运行后回读报告摘要 + Export JUnit/HTML；趋势自动入库。报告详情为逐场景 Foldout：一句话结论、指标表、预算表、基线对比、findings（人话翻译）、截图缩略图与工件链接；运行期轮询状态文件显示"第 x/N 个场景"进度条；三个打开按钮（报告 JSON / 工件目录 / HTML 摘要）。
- **看板 Trend 页**：按 scenario 分组查看各指标最近 10 次序列与环比（正=变差红 / 负=改善绿；run 级行按 pass/warn/fail 着色）。
- **导出器**：`JUnitExporter`（JUnit XML）与 `HtmlSummaryExporter`（单文件自包含 HTML 摘要）。
- **define 预检**（Phase 1 遗留收口）：运行入口检查 `AXU_TEST_AGENT` 未开时弹可跳过警告。
- **部署菜单**：`AxuTestFramework/Deploy Suite To StreamingAssets`（复制 suite JSON 到 `Assets/StreamingAssets/AxuSuites/`）与 `Remove Deployed Suites`。

## 场景测试能力库（Agent 层，`AXU_TEST_AGENT` 守护）

| 能力 id | 名称 | 一句话 | 文档 |
|---|---|---|---|
| `echo` | 链路自检 | 延时+日志最小闭环 | Agent/Capabilities/Echo/ |
| `perf` | 性能基准 | warmup→度量→预算→截图 | Agent/Capabilities/Perf/DOC.md |
| `loadtime` | 加载时长 | 场景矩阵加载计时 | Agent/Capabilities/LoadTime/DOC.md |
| `smoke` | 冒烟矩阵 | 全场景可进+无报错+截图 | Agent/Capabilities/Smoke/DOC.md |
| `soak` | 浸泡/泄漏 | 长跑内存斜率/FPS 衰减 + partial flush（崩溃不丢前段） | Agent/Capabilities/Soak/DOC.md |
| `stress` | 压力/承载 | 容量探测（目标帧率最大实体数）+ churn + 事件洪峰 | Agent/Capabilities/Stress/DOC.md |
| `monkey` | 猴子测试 | 随机点按（seed 可复现）+ 卡死检测 + JSONL 流水 | Agent/Capabilities/Monkey/DOC.md |
| `gamehook` | 游戏适配器 | 游戏侧显式注册自定义能力/代码态 scenario（`AxuTestAgent.RegisterCapability/RegisterScenario`） | Agent/Capabilities/GameHook/DOC.md |

示例 suite 见 `Samples~/Scenarios/`（每能力一份，Package Manager → Samples 导入）。

## Runtime 工具层速览

命名空间统一为 `AxuTestFramework`。

### AxuWait（等待）

```csharp
// 条件等待：每帧检查，5s 超时 → Assert.Fail（附清晰超时消息）
yield return AxuWait.Until(() => ui.isReady, 5f);
// 条件等待 + 按真实时间间隔检查（不逐帧）
yield return AxuWait.Until(() => done, 10f, 0.5f);
// 帧等待 / 秒等待
yield return AxuWait.ForFrames(3);
yield return AxuWait.ForSeconds(0.2f);
```

### AxuTestScene（场景）

```csharp
// 按路径加载（Editor 下支持 Assets/ 完整路径，无需加入 Build Settings）
yield return AxuTestScene.LoadAsync("Assets/Scenes/Demo.unity");
// 卸载 / 重载
yield return AxuTestScene.UnloadAsync("Demo");
yield return AxuTestScene.ReloadAsync("Assets/Scenes/Demo.unity");
```

### AxuCapture（截图，仅 PlayMode）

```csharp
// 截图到报告 artifacts 目录（默认 Library/AxuTestReports/artifacts/，文件名带时间戳）
yield return AxuCapture.ScreenshotCoroutine("game-ui-check");
Debug.Log(AxuCapture.LastScreenshotPath);   // 完整路径
```

EditMode 下调用会抛 `InvalidOperationException`（EditMode 无 GameView 渲染输出；替代方案见异常消息）。

### AxuLogWatch / AxuNoErrorsScope（日志监视）

```csharp
// 方式一：作用域——结束时若有 Error/Exception/Assert，Assert.Fail 汇总失败
using (AxuLogWatch.ExpectNoErrors())
{
    DoSomethingThatMustNotError();
}

// 方式二：手动监视
using (var watch = new AxuLogWatch())
{
    DoWork();
    Assert.IsFalse(watch.HasErrors, string.Join("\n", watch.Errors));
}
```

预期内的错误日志仍应先用 `LogAssert.Expect` 声明（AxuLogWatch 只做被动记录，不与 LogAssert 交互）。
内部只挂 `Application.logMessageReceivedThreaded`（全量、含非主线程），收集加锁，线程安全。

### AxuTestState（静态/单例重置）

```csharp
[UnitySetUp]
public IEnumerator SetUp()
{
    AxuTestState.ResetStatics();                                  // 执行已登记重置 + 清空登记表
    AxuTestState.Register("MySystem", MySystem.ResetForTest);     // 重新登记本轮所需重置项
    yield return AxuWait.ForFrames(1);
}
```

域重载关闭（Enter Play Mode Options 置 3）时静态/单例残留是主要风险面，**编写规范强制**在 `[UnitySetUp]`
中调用 `ResetStatics()`；`ResetAll()` 逐项捕获异常并返回失败汇总，不会因单项异常中断。

### Reports POCO（报告模型，schema `axu-test-report/1`）

`AxuTestReport` / `FilterInfo` / `ReportSummary` / `ReportResult` / `ReportConsole` / `ReportArtifact` /
`ReportEnvironment`——纯数据模型，由 M2 的报告器填充并序列化；`AxuCapture.CapturedPaths` 可作为
`Artifacts` 的数据源。

## 场景测试接入一页纸（任何游戏工程 15 分钟跑通首个 scenario）

接入模型 = **装包 + 开 define + 三选一入口**。以下步骤从零开始：

**① 装包（约 2 分钟）**：把本包放入工程的 `Packages/`（嵌入式）或经 Package Manager 从 tarball 安装。
依赖自动生效：`com.unity.test-framework` 与 `com.unity.nuget.newtonsoft-json`（必需）、
`com.unity.pipeline`（可选——只在需要 HTTP 命令驱动时安装）。

**② 开 define（1 分钟）**：菜单 `AxuTestFramework/Enable Test Agent` 勾选（Standalone 平台注入
`AXU_TEST_AGENT`，等价于 ProjectSettings 的 Scripting Define Symbols；切换后需等一次重编译）。

**③ 写第一个 suite（5 分钟）**：菜单 `AxuTestFramework/Create Suite JSON` 生成模板，或直接手写
（放 `Assets/` 下任意位置，扩展名 `.suite.json`）：

```json
{
  "schema": "axu-suite/1",
  "id": "my-first-suite",
  "scenarios": [
    {
      "schema": "axu-scenario/1",
      "id": "echo.first",
      "capability": "echo",
      "params": { "message": "hello", "delaySeconds": 1.0, "steps": 2 }
    }
  ]
}
```

**④ 三选一入口跑（2 分钟）**：

| 入口 | 适合谁 | 做法 |
|---|---|---|
| 看板（人） | 日常开发 | 菜单 `AxuFrameKit/AxuTestRunner` → Scenario 页 → 选中 suite → Run Suite（自动进/退 Play） |
| pipeline 命令（Agent/CI） | 无人值守 | `axu_scenario_run {suite}` → 轮询 `axu_scenario_status` → `axu_trend_summary` 看趋势 |
| Player/真机 | 打包验证 | 菜单 `Deploy Suite To StreamingAssets` → `axu_player_suite_build` + `axu_player_suite_run`（或 Player 命令行 `-axuSuite <路径> -axuQuitOnDone -axuReport <路径>`） |

**⑤ 看结果（1 分钟）**：报告 JSON 落 `Library/AxuTestReports/scenario/`（Player 侧运行自动回拉），
看板 Scenario 页显示摘要、可导出 JUnit/HTML；Trend 页看历次趋势。退出码：0=全过，1=有 fail，2=仅 warn。

**⑥ 进阶（可选）**：给 scenario 加 `budgets` 数组做绝对门槛判定、`baseline` 字段做跨次回归对比
（schema 与字段全集见 [`DOC/ScenarioSchema.md`](DOC/ScenarioSchema.md)；八种能力见上节表格，
各自 DOC.md 有参数说明）。自定义能力用 `gamehook` 显式注册 API（见 GameHook DOC）。

**已知边界**：性能类采集在 Editor 里受编辑器开销影响，正式判定以 Development Build（`axu_player_suite_*`）
为准；场景加载类能力要求目标场景在 Build Settings 列表内（Unity 6 Editor 与 Player 校验行为一致）。

## 各包接入（UTF 单元测试）

1. 在被测包内建 `Tests/Editor` 与/或 `Tests/Runtime`，asmdef 命名 `{被测程序集}.Tests.{Editor|Runtime}`；
2. references：被测程序集 + `AxuTestFramework` + `UnityEngine.TestRunner`（Editor 侧再加 `UnityEditor.TestRunner`）；
3. `overrideReferences: true` + `precompiledReferences: ["nunit.framework.dll"]`、
   `defineConstraints: ["UNITY_INCLUDE_TESTS"]`、`autoReferenced: false`；
4. 包 `package.json` **不声明**测试依赖（缺包时测试程序集不编译是特性）。

## 版本

见 [CHANGELOG.md](CHANGELOG.md)。
