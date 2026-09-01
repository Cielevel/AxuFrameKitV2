# Changelog

All notable changes to this package will be documented in this file.

## [Unreleased]（Phase 3 P1 批次：soak/stress/monkey/gamehook；验收后随 Phase 3 收尾升 0.3.0）

- **四能力落地**（Agent 层，均 Player 可编译，各带 DOC.md 与 Samples~/Scenarios/ 示例）：
  - `soak`（浸泡/泄漏）：durationMinutes/sampleIntervalSec/gcCollectBeforeSample/screenshotEveryMinutes/leakSlopeMBPerMin/fpsDecayPercent/flushEveryMinutes；内存序列滑窗最小二乘斜率判"泄漏嫌疑"（warn+CSV 取证）、前后半程 p50 FPS 衰减判定、周期截图；**partial report 定期覆盖写**（`{报告名}.partial.json`，`truncated:true`，正常结束删除——崩溃/中断保留前段数据，ScenarioReport 增可空 `Truncated` 字段）。
  - `stress`（压力/承载，组合 phases）：`capacity` 容量探测（ramp + 60 帧滑窗 fps 判超载 + 二分收敛，输出 `capacity.maxEntities` 与承载曲线 CSV）、`spawner`（prefab 可空回落内置替身）、`churn`（每秒建毁 N × M 秒）、`flood`（自建托管事件洪峰吞吐基准，零框架依赖）；StressSpawner/StressCollector 配套。
  - `monkey` v1（猴子测试）：坐标随机点按（`AgentInput`：Input System `QueueStateEvent` 注入，`AXU_INPUTSYSTEM_PRESENT` versionDefines 守护；未装/无设备降级 UGUI ExecuteEvents + info finding）、seed 可复现、JSONL 操作流水、错误即截图可配即停、卡死检测（frameCount 停滞 → 截图 + error finding + 终止）。
  - `gamehook`（游戏适配器，显式注册 API）：`AxuTestAgent.RegisterCapability/RegisterScenario` + `ScenarioDef.FromCode`（Core 纯委托）/`FromCodeRoutine`（Agent 协程形态）+ `AxuScenarioRegistry`（进程内注册表，同 id 后者覆盖）；宿主 suite 解析推迟到 AfterSceneLoad 后首帧，保证游戏侧 RuntimeInitializeOnLoad 注册可见（时序契约见 DOC）。
- **宿主修复 2 项（DoD 实测暴露）**：① `AgentRunnerHost.Supervise` 改 Stack 驱动任意深度嵌套迭代器（旧实现只代步一层，capacity/churn 深层等待被整段吞掉致秒退零实体）；中断路径（超时/取消/异常）统一 Dispose 栈内迭代器（清场 finally 链生效，防 stress 实体泄漏）；② CapacityProbe FpsWindow 启动判据缺陷（`_filled` 自锁恒 NaN）修复。
- Agent asmdef：references 增 `Unity.InputSystem`（versionDefines 可选）与 `UnityEngine.UI`（降级路径）。
- 单测 155 → **209**（四能力参数/FromCode/SuiteLoader externalById 契约 +45 例，含 clamp/混排/覆盖语义）。
- macOS DoD：stress 容量探测（maxEntities=2000 下界/40 点曲线/churn 300 对/flood 295 万事件）、monkey 同 seed 双跑流水逐行一致、gamehook e2e pass、soak 10min（partial 每 2min 一刷×5、中断保留 truncated、结束删除）与 2min Player 抽验（isEditor=false）全过；零影响回归 209/209 + Axu/Smoke 7/7。

## [Unreleased]（Phase 4 工具链：axu_scenario_*/axu_player_*/axu_trend_* 六命令 + 趋势 + 看板页签 + 导出器）

- **Core/Trend**（纯 C#）：`TrendPoint` / `TrendStore`（schema `axu-trend/1` 追加式长表 CSV，RFC 4180 转义、
  引号内换行无损回读）/ `TrendSummary`（按指标序列 + 方向感知环比，正值恒=变差）；趋势路径
  `Library/AxuTestReports/trend/trend.csv`（`AxuTrendPaths`）。
- **Editor.Pipeline 六命令**：`axu_scenario_list/run/status`（Editor PlayMode 驱动 suite，异步 + Temp 状态文件协议）、
  `axu_player_suite_build/run`（macOS Development Build，define 临时开启 try/finally 还原；Player 进程运行 → 退出码 →
  回拉报告 → 趋势入库）、`axu_trend_summary`（最近 N 次序列与环比）。三类运行与 `axu_test_run` 共享互斥闸
  （`axu_test_run` 侧同步补上 scenario 在途拒绝，双向闭环）。
- **Editor 层**：`ScenarioRunService`（桥文件 + 看门狗 + 趋势自动入库 + 自动退 Play，菜单/命令/看板共用）、
  `JUnitExporter` / `HtmlSummaryExporter`、`ScenarioPrecheck`（`AXU_TEST_AGENT` 未开可跳过警告，Phase 1 遗留收口）、
  `PlayerSuiteDeployMenu`（StreamingAssets 一键部署/清理）。
- **看板**：新增 **Scenario / Trend** 页签（Tests 页零改动）——suite 列表与运行、报告摘要、JUnit/HTML 导出、
  趋势分组浏览与环比着色。
- **跨平台修复**：`AgentPaths` / `AxuCapture` 文件名清洗字符集改为平台无关固定并集（Windows 全集 + 控制字符），
  修复 macOS 上 `:` 等不被清洗导致 metric 键跨机不一致。
- 单测 130 → **155**（趋势存储/摘要契约 25 例）；macOS DoD：axu_scenario_run→status→趋势入库→trend_summary、
  player build(61.8s)+run 端到端回拉（isEditor=false）、包测试 155/155、Axu/Smoke 7/7。

## [Unreleased]（Phase 3 P0 批次：共享基建 + perf/loadtime/smoke；版本号待 Phase 3 收尾统一定）

- **三个 P0 能力落地**（三件套 + 注册，均 Player 可编译）：
  - `perf`（性能基准）：可选加载场景 → warmup 挂起采样丢弃 → 度量期 → 可选截图；指标由
    `collectors` 采集器产出、宿主预算判定。加载失败出 `PERF_SCENE_LOAD_FAILED`；
  - `loadtime`（加载时长）：场景矩阵逐个计时，产出 `loadtime.scene.<safe>.loadMs/.firstFrameMs/.stableMs`
    + `sceneCount/loadMs.max/loadMs.avg/unstableCount`；加载失败 `LOAD_FAILED`（最终 fail）、
    未达稳定 `SCENE_NOT_STABLE`（warn）；
  - `smoke`（冒烟矩阵）：清单三来源（`list`/`buildsettings`/`directory`[仅 Editor]）→ 逐场景日志监视
    加载 + 稳定窗错误计数 + 截图留证，产出 `smoke.scene.<safe>.loadMs/.errors` +
    `sceneCount/loadMs.max/errors.total/loadFailed.total`；清单错误 `SMOKE_NO_SCENES`/
    `SMOKE_DIRECTORY_UNAVAILABLE`；
  - 每能力目录附 `DOC.md`（参数表含 clamp、指标键表、budget 示例、平台备注）。
- **共享基建**（`Agent/Tools/`，Agent 层）：
  - `AgentEnvironment`（+ Overrides/Snapshot POCO）：DOC §4 通用参数保留键（timeScale/vsync/targetFrameRate/
    qualityLevel/resolution/seed）的解析（缺键/类型不符一律 null，绝不抛异常）、应用（越界 clamp；
    **resolution 仅 Player**）与恢复（宿主在 finally 中调用，超时中断也还原）；
  - `AgentScenes`：场景加载三级回退解析（原样路径 → 去 `.unity` → 纯文件名）+ 激活场景 isLoaded 确认；
  - `AgentCapture`：截图原语（**移植自 v1 `Runtime/Capture/AxuCapture` 思路**：延迟生效帧等待 + 文件校验，
    失败仅告警）+ artifacts 路径拼合；
  - `AgentLogWatch`：日志监视（**移植自 v1 `Runtime/Logging/AxuLogWatch` 思路**：Error/Exception/Assert 与
    Warning 分计数、最近 20 条 `"condition (asset:line)"` 快照、ResetCounts、线程安全、Dispose 退订）。
- **度量会话扩展**（加法式）：`MetricSession` 新增 `Suspend()/Resume()`（引用计数挂起，挂起期采样/打点/
  节拍全 no-op，perf warmup 用）与 `TryAttachCollector()`（runner 自有采集器挂入会话：loadtime/smoke 的
  结果采集器随宿主节拍一并驱动，id 冲突拒绝）。
- **Runner 契约扩展**（加法式）：`ScenarioContext` 新增 `ArtifactsDir`/`Seed`（构造参数带默认值，旧调用点
  不破坏）；`ScenarioRunHandle` 新增 `Artifacts` 列表（runner→宿主工件通道）；宿主在逐 scenario 外层应用/
  还原环境通用参数、支持 scenario 级 `timeoutSec` 覆盖、artifacts 目录默认落到报告同目录 `artifacts/`，
  runner 截图经 `Handle.Artifacts` 转相对报告目录路径写入报告 `results[].artifacts`。
- 内建能力注册追加三项（`BuiltInCapabilities.RegisterAll`，显式 API，无反射扫描）。
- **契约单测扩至 130 个**（原 57 + 新增 73，`Axu/TestFramework`）：三能力参数默认值/clamp/JSON 反序列化、
  `AgentEnvironmentOverrides.Parse` 纯解析、`MetricSession` 挂起/附加采集器语义、loadtime/smoke 采集器
  产出键与聚合、`AgentCapture`/`AgentLogWatch` 纯逻辑。
- **e2e 实测驱动的修复**（Windows，Editor PlayMode 桥文件链路实跑四套件）：
  - `AgentLogWatch` 快照改 FIFO 环形淘汰（原实现达上限后不追加，与"最近 20 条"注释不符；`DroppedCount`
    语义同步为"因淘汰未保留的总条数"）；
  - **全 0 聚合恒产出**：smoke 的 `errors.total`/`loadFailed.total`、loadtime 的 `unstableCount`/`sceneCount`
    在采集器 Start 播种 0 值采样——否则"零错误/零失败"这一合法状态会因指标缺失被预算按 fail 误判
    （观测类聚合如 `loadMs.max` 仍不播种，保持"缺失=fail"哲学）；
  - **Unity 6 场景清单校验实证与 `AgentScenes` 收口**：6000.0.23f1 实测 Editor 与 Player 一致强制校验
    构建场景清单（"Editor 任意 Assets 路径可载"不成立；运行时临时注册对同会话加载校验不可见）——
    收口为前置检查 + 可读报错指引，目标场景须加入 Build Settings/Build Profile，三能力 DOC 与
    `DOC/ScenarioSchema.md` §4 同步更正。
- **`Samples~` 目录补建（v1 悬空声明修复）**：根因为工程 `.gitignore` 的备份文件模式 `*~` 把 `Samples~`
  目录整体忽略（0.1.0 起示例从未真正入库）；`.gitignore` 追加 `!**/Samples~/` 反选后补齐
  `Tests/Editor`、`Tests/Runtime` 两示例与 package.json 声明对齐，新增 `Scenarios` 示例
  （echo/perf/loadtime/smoke 四 suite + 接入一页纸 README），package.json samples 增设第三项。

## [0.2.0] - 2026-08-31（场景测试平台建设启动，Phase 0/1/2 已落地）

- **Phase 2 度量层落地**：Core 层 `RingBuffer`/`MetricStats`（百分位线性插值 + 最小二乘斜率）/`BudgetEvaluator`
  （budget 键 `<collector>.<stat>`，缺失指标按 fail 并出 `BUDGET_METRIC_MISSING`）/`BaselineFile`（schema
  `axu-baseline/1`）+ `BaselineDiff`（**方向感知**：fps=越高越好、frameTime/memory/custom.scope=越低越好、
  未知 collector 对称；改善不计回归）；Agent 层 `AxuMetrics` 门面（Scope/Counter/Gauge，无会话零分配 no-op）、
  `MetricSession` + 5 个采集器（fps/frameTime/memory/rendering/gc，宿主逐帧驱动、250ms 窗口聚合、环境不可用出
  `COLLECTOR_UNAVAILABLE` 警告不静默 0 值、收尾补采保证短 scenario ≥1 样本）；宿主集成预算判定（结果 status
  只升不降）与基线 diff（`scenario.baseline` 相对 suite 目录；首跑缺基线静默跳过；`@profile` 扇出实例不继承
  源场景基线）；CLI/桥文件新增 `-axuBaselineSave`/`baselineSavePath`（按 scenarioId 写
  `<目录>/<scenarioId>.baseline.json`）。包单测扩至 57 个（`Axu/TestFramework`）。
- **Phase 1 Agent 骨架落地**：Core 层 scenario/suite 模型（内联场景、`ref+profile` 扇出 `@profile` 后缀、深合并）、
  `ScenarioCli` 命令行解析（`-axuSuite/-axuScenario/-axuQuitOnDone/-axuReport/...`）、Agent 层
  `AgentBootstrap`（RuntimeInitializeOnLoad + 场景组件双入口）+ `AgentRunnerHost`（顺序执行、真实时间看门狗、
  异常兜底）+ `ScenarioReportWriter`（schema `axu-scenario-report/1`）+ 状态文件 + 退出码（Player 经
  `Application.Quit(int)` 携带，Editor 永不退出）；内建 `echo` 能力（链路自检）；Editor 菜单
  `AxuTestFramework/Enable Test Agent`（define 开关）、`Create Scenario/Suite JSON`、`Run Suite JSON...`
  （Editor 桥文件 `Temp/axu_pending_suite.json` → Play 自动执行）。macOS Player 实测：构建后
  `-batchmode` 外的正常启动 + `-axuSuite ... -axuQuitOnDone` 退出码 0、报告 2/2 pass。
- **程序集拓扑扩展**：新增 `AxuTestFramework.Core`（纯 C# 基座，`noEngineReferences: true`，唯一外部依赖
  Newtonsoft.Json）、`AxuTestFramework.Agent`（Player 可编译的场景测试运行时，公开入口由 `AXU_TEST_AGENT`
  define 守护）、`AxuTestFramework.Editor.Pipeline`（Editor-only）。三者 0.2.0 先落程序集与契约，实现按
  场景测试平台方案（Phase 1–5）逐步填充。
- **pipeline 依赖可选化**：`com.unity.pipeline` 从 dependencies 移除；全部 `[CliCommand]` 命令
  （`axu_test_run` / `axu_test_report` / `axu_test_status` / `axu_window_screenshot`）迁入新程序集
  `AxuTestFramework.Editor.Pipeline`（`Editor.Pipeline/Commands/`，命名空间不变），该程序集以
  `defineConstraints: [AXU_PIPELINE_PRESENT]` + versionDefines（`com.unity.pipeline ≥0.5.0-exp`）守护——
  **未装 pipeline 包的工程整体不编译该程序集，包内其余功能不受影响**；`AxuTestFramework.Editor`
  主程序集自此零 pipeline 引用。
- 新增 `DOC/ScenarioSchema.md`：场景测试数据契约草案（`axu-scenario/1`、`axu-suite/1`、`axu-scenario-report/1`
  字段表、预算规则、通用 params 保留键）。
- 以下 v1 条目随 0.2.0 一并发布（0.1.0 后的未发版积累）：
- `AxuTestRunnerWindow` 右侧结果树行头按终态着色：**通过=浅绿**（`--ax-ok-light`）/ **未通过=浅红加粗**
  （`--ax-bad-light`）/ **跳过=浅蓝**（`--ax-skip-light`，取代原琥珀色）；着色选择器改为可命中的
  `.unity-foldout__toggle` 后代形式（旧 `>` 子选择器实际未命中）。`IsFailed` 拓宽为"非 Passed 且非 Skipped"，
  Error/Cancelled 等终态不再误标绿。

- `axu_test_run` 新增 `test_names` 参数：用例**精确全名**的 JSON 数组字符串（启用"选定测试"通道；
  不用逗号分隔——参数化用例全名含逗号会劈错），解析失败即返回错误信封不进入编排。README 同步更新
  参数表/示例/看板说明/filter schema。
- 新建包自身测试程序集 `AxuTestFramework.Tests.Editor`（类别 `Axu/TestFramework`）：
  ResolveTestNames 去空白/去重/全空白报错、BuildFilterInfo 的 testNames 优先级与回落、
  StartRunAsync 并发闸拒绝（状态文件制造/还原，不真正启动运行）。
- 测试运行新增"选定测试"通道：`AxuTestRunRequest.TestNames`（精确用例全名清单）经 UTF `Filter.testNames`
  执行，优先级高于 testName 子串过滤（给定名单时后者不参与）；与安全程序集清单保持 AND 收敛，
  预检 0 匹配即报错（含"勾选用例不属于当前模式"提示）。`FilterInfo` 新增类型 `testNames`（Value 为用例数描述）。
- `AxuTestRunnerWindow`：左侧套件树用例行带勾选框（集合按 FullName 键控，跨树重建/模式切换保留），
  程序集/命名空间/类各级 Foldout 头部带**三态勾选框**——点击批量勾选/取消其下当前可见用例
  （全选态点击=全不选，其余态点击=全选；部分勾选显示为填充态），勾选框拦截冒泡不连带折叠层级；
  顶栏 Run 拆为 **Run 全部** / **Run 选定（N）** 双按钮（无勾选时后者禁用），选定运行按当前模式执行。
- 新增 `axu_window_screenshot` pipeline 命令（**Windows 专属**，适用于**没有 CaptureToRenderTexture 能力的
  Unity 版本 < 6000.7**，6000.7+ 不参与编译、请改用 `capture_editor_element`）：经 `PrintWindow(PW_RENDERFULLCONTENT)`
  离屏渲染实现**后台无感窗口截图**——目标窗口可被遮挡/失焦，不抢焦点、不置顶、不干扰用户；支持 `window`/
  `menu`(自动开窗)/`output` 参数，`GetDIBits` 失败时自动回退 `GetBitmapBits` 并统一翻转行序。
  **macOS 无需对应实现**（2026-08-31 决策）：非 WindowsEditor 由运行时平台守卫先于任何 Win32 调用返回说明文本
  （不崩溃无副作用），Mac 截图由跨平台路径覆盖（`AxuCapture` + pipeline `capture_game_view`/`capture_scene_view`/
  `screenshot`）；不规划 CGWindowListCreateImage 变体，确需时优先升 6000.7+ 用 `capture_editor_element`。
- `AxuTestRunnerWindow`：左右分栏改为 `TwoPaneSplitView`（Horizontal），中间分隔条可拖拽调整两侧比例，
  经 viewDataKey 跨窗口重开保留；左右栏设 minWidth 下限防拖没，USS 补 dragline 悬停高亮。

## [0.1.0] - 2026-08-31

- 新建独立嵌入式包 `com.cielevel.axu-test-framework`：通用测试框架（EditMode + PlayMode），零 AxuFrameKit 依赖。
- Runtime 程序集 `AxuTestFramework`（`UNITY_INCLUDE_TESTS` 守护，全平台）：等待（AxuWait）、场景（AxuTestScene）、
  截图（AxuCapture）、日志监视（AxuLogWatch / AxuNoErrorsScope）、静态重置登记表（AxuTestState）、
  报告 POCO 模型（schema `axu-test-report/1`）。
- Samples~：EditMode / PlayMode 基础示例（经 Package Manager → Samples 导入）。
- Editor 程序集 `AxuTestFramework.Editor`（Editor-only，`autoReferenced: false`）：
  - `AxuTestReportWriter`（`[TestRunCallback]`）：运行结束落盘 `Library/AxuTestReports/{时间戳}-{mode}.json`
    （schema `axu-test-report/1`，含 results/console/artifacts/environment）；运行参数经
    `Temp/axu_test_run_context.json` 跨域重载传递；指定 out_path 时同步重定向 AxuCapture artifacts 目录。
  - `AxuConsoleRing`：运行期 Console 环形缓冲（Error/Exception/Assert/Warning，容量 200，线程安全）。
  - pipeline 自定义命令（TypeCache 自动注册，pipeline 包零改动）：`axu_test_run`（EditMode 同步 / PlayMode 强制异步）、
    `axu_test_report`（读报告原文，256KB 截断保护）、`axu_test_status`（读运行状态文件）。
  - **强制安全过滤**：`axu_test_run` 的 assemblyNames 恒为安全测试程序集清单并**无条件剔除 `Unity.Pipeline.Tests*`**
    （防其 SecurityTokenTests 清空 pipeline 本地 API 鉴权 token 的"token 中毒"事故），绝不发出无程序集范围的裸跑。
  - 看板窗口 `AxuTestRunnerWindow`（菜单 `AxuFrameKit/AxuTestRunner`，纯 UI Toolkit，独立自持 USS）：左侧套件树 /
    右侧结果树 / 顶栏模式切换 + Run + 过滤 + 报告 reveal，与命令共用 AxuTestRunService。
