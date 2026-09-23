# Changelog

All notable changes to this package will be documented in this file.

## [Unreleased]

## [0.4.0] - 2026-09-23

- **可用性改造 阶段 1：运行闭环与可信状态（2026-09-23）**。让用户发起一次运行后必定得到明确终态，
  界面不会停在"似乎一直在运行"，也不会把上一次的报告当成本次结果。五项修复：

  1. **统一启动-收尾契约（看板 / 菜单 / pipeline 共用 `ScenarioRunService`）**：一次运行 = 预检 →
     写运行记录 `Temp/axu_scenario_run.json`（新文件，含 `runId`/suite 路径/发起方）+ 桥文件 →
     进 Play → Agent 执行 → 看门狗在**终态**（completed/error）时趋势入库、删记录、**自动退出 Play**。
     看板发起的运行不再需要用户手动退 Play；此前"跑完自动回显结果"文案与实际行为不符（菜单路径停在 Play、
     窗口按"退出 Play + 时间戳挑最新报告"回读，域重载还会清掉静态挂起标志）。
  2. **运行标识与可信终态**：桥文件与状态文件新增 `runId`（发起方生成，Agent 原样回写），消费端按它确认归属；
     用户手动停止 → 看门狗补写 `status=aborted`（`reason=interrupted`，**`reportPath` 清空**），
     界面显示"已中断"而不回显旧报告；suite 加载失败 / 报告写盘失败分别带 `reason=suite_load_failed` /
     `report_write_failed` + `message`（终态可辨识）。状态文件新增 `heartbeatUtc`。
  3. **长跑互斥（心跳判活）**：在途判定不再看 `startedAtUtc`，改看**最后活动时间**（心跳 > 状态文件写入时间 >
     启动时间，阈值 15 分钟）。Agent 宿主在 scenario 执行期间每 5 秒心跳续写状态文件（`HeartbeatIntervalSec`），
     新增 `AxuTestHeartbeat` 在 UTF 运行期同样每 5 秒刷新心跳——**20 分钟的 soak 期间发起 UTF 测试会被正确拒绝**
     （旧实现 15 分钟后误判陈旧放行）。`axu_doctor` 的 `runs.in_flight` 改用同一谓词与类型化模型
     （顺带修复：此前用 camelCase 键读 PascalCase 的 UTF 状态文件，UTF 在途永远检测不到）。
  4. **纯 `ref` 代码态 scenario 的文件加载链路**：`SuiteDef.Validate` 支持传入"运行时已注册 id"集合
     （`SuiteLoader.LoadSuite(path, externalIds, ignoreUnresolvedRefs)` + `FindUnresolvedRefs`），
     Agent 侧加载/扇出共用同一注册快照——只含纯 `ref` 的 suite 文件（无内联定义）不再被校验拦下；
     未解析的 `ref` 仍报明确错误。`axu_scenario_list` 走宽松加载：照常列出并给出 `unresolvedRefs` + `note`。
     新增工程内示例 `Assets/Tests/Scenarios/gamehook-ref-demo.suite.json`（实测跑通）。
  5. **`mode=all` 双阶段语义**：新增 `AxuTestRunPhase` 阶段模型与返回值 `phases[]` / `phasesComplete` /
     `overallConclusion` / `editorSummary`；**仅当 EditMode 段正常 `completed` 才异步启动 PlayMode 段**
     （超时/出错则跳过并在结论里写明，`status` 保留 `timeout`/`error`，不再被 `playmode_running` 掩盖）；
     PlayMode 段异步运行期间，`axu_test_status` 的状态文件带 `precedingPhase`（EditMode 段结论，
     跨域经运行上下文文件传递）。
  
  另修一处静默 bug：`AxuScenarioRunStatus.Summary` 此前绑定到命名空间链上同名的 UTF 汇总类型
  （无 `warned` 字段），导致 scenario 状态文件的 `warned` 计数一直丢；已显式绑定
  `AxuTestFramework.Core.Report.ReportSummary`。
  看板 Scenario 页改为文件驱动状态（运行记录 + 状态文件，跨域重载成立），运行记录存在时禁用再次发起。
  Pipeline 命令参数与报告 schema（`axu-test-report/1` / `axu-scenario-report/1`）**不变**；
  状态文件新增字段为增量（`axu_scenario_status` 新增终态值 `aborted` 与 `reason`/`message`/`runId`/`heartbeatUtc`，
  `axu_test_status` 新增 `HeartbeatUtc`/`PrecedingPhase`）。

- **新增 pipeline 命令 `axu_lint`（2026-09-20，项目规范校验）**：把 AGENTS.md 软规范机器化——5 条规则：
  `namespace.convention`（文件命名空间 vs asmdef rootNamespace 前缀）、`editor.placement`（fail 级：
  未受 #if 保护的 UnityEditor 引用出现在 Player 面向程序集/非 Editor 路径）、`file.type_name`
  （文件名 vs 类型名，拆分文件首段锚点豁免）、`runtime.editor_ns`（/Runtime/ 下的 Editor 命名空间回归哨兵）、
  `tests.asmdef_guard`（Player 面向测试程序集缺 UNITY_INCLUDE_TESTS=构建泄漏）。参数 `include`（支持
  目录名前缀展开，默认 Assets + com.cielevel 一族）/ `exclude`（默认 /FastNoiseLite/）/ `max_issues`。
  纯只读、不进互斥闸；regex 启发式扫描（非 Roslyn），输出定位"规范漂移清单"。
  本工程首跑基线：656 文件 / 34 程序集，namespace 7 处（框架包 Editor 历史漂移）、file.type_name 37 处、
  其余 3 条零违规；同日收尾清理（框架包 Editor 命名空间归位 ×7 + 真错名文件重命名 ×13，详见各包 CHANGELOG），
  复跑降为 namespace 0 / file.type_name 24（余下均为多类型家族文件形态，如 Ease 一族）。实现于 `Editor.Pipeline/Commands/AxuLintCommand.cs`。

- **新增 pipeline 命令 `axu_doctor`（2026-09-20，环境自检）**：一键体检信封
  `{status(ok/warn/error), summary, environment, checks[{id,status,message,hint?}]}`，覆盖 8 项——
  pipeline 描述符（port/evalToken 存在性，token 值永不回显）、必需包（test-framework/newtonsoft-json）、
  可选包（pipeline/InputSystem/uGUI，双缺=monkey 降级 warn）、`AXU_TEST_AGENT` define、Editor 忙闲、
  在途运行（新鲜 running 预判互斥闸占用，>15 分钟陈旧不判）、报告存储可写探针（自清理）+ 存量盘点、
  suite 盘点。纯诊断、**不进 RunGate 互斥闸**；单项异常只降级该项。实现于
  `Editor.Pipeline/Commands/AxuDoctorCommand.cs`（`AXU_PIPELINE_PRESENT` 守护，TypeCache 自动注册）。

- **Agent 输入后端正规化（2026-09-20，backlog #5）**：主 `AxuTestFramework.Agent` 程序集移除对
  `Unity.InputSystem` / `UnityEngine.UI` 的硬引用（只依赖 Core + Newtonsoft.Json），monkey 输入能力拆为
  两个可选守护程序集——`AxuTestFramework.Agent.Input`（versionDefines `com.unity.inputsystem ≥1.0` →
  `AXU_INPUTSYSTEM_PRESENT` + defineConstraints，InputSystem 注入主路径）与 `AxuTestFramework.Agent.UGUI`
  （`AXU_UGUI_PRESENT`，UGUI 命中检测/降级点按）；未装对应包的工程该程序集整体不编译，主程序集与包其余
  功能零影响。后端经 `AgentInputBackends` 反射发现（优先级 InputSystem > UGUI；两者都缺时 monkey 仅记录
  坐标并出一次性 info 提示，消息分三态）；主程序集新增 `IAgentTapInjector` / `IAgentUguiProbe` 接口。
  新增 4 例回归测试（主程序集引用红线 + 可选后端解析/Reset），修复前先在 backlog #5 记录的现象（干净工程
  编译失败风险）不再存在。

- **看板 Scenario 页报告详情与运行进度 P0（2026-09-20，backlog #4）**：
  - 场景结果行改 **Foldout 详情**：一句话结论（findings 人话翻译 + 预算/基线/错误/警告计数）、
    指标表（按 collector 分组，`collector.stat` → 聚合值）、预算表（实际 vs 门槛 + pass/warn/fail 着色、
    指标缺失标注）、基线对比表（base → current + 变化率，劣化按判定着色）、findings 列表
    （严重级 + 翻译，原始 code/message 进 tooltip）、截图缩略图（PNG/JPG 解码为 160×90 预览）
    与其它工件链接（均可点击用默认程序打开）；
  - **运行进度**：轮询 `Temp/axu_scenario_status.json` 渲染"第 x/N 个场景"进度条
    （running/completed/error 三态；>15 分钟未更新标陈旧；pipeline 驱动的运行同样可见；
    本窗口发起运行时忽略上一轮残留状态）；
  - **打开按钮**：打开报告 JSON / 打开工件目录 / 打开 HTML 摘要（无则即时生成到报告旁）；
  - 新增 `ScenarioReportPresenter`（工件路径解析 / 结论生成 / finding 翻译 / 色调映射，纯函数）与
    `AxuScenarioRunStatus`（状态文件模型）；新增单测 10 例（`ScenarioReportPresenterTests`）；
  - `AxuTestTokens.uss` 新增详情/色调/缩略图/进度样式（全部沿用既有 token，无新色值）。

- **看板面板可读化改造（2026-09-02，面向非技术使用者）**：
  - 新增 **「总览」页并设为默认页**（`AxuTestRunnerWindow.Overview.cs`，partial）：
    最近一次 suite 运行的大结果徽章（✓全部通过 / ⚠通过但有警告 / ✗有失败 / —尚无记录，颜色绿/黄/红/灰）、
    一行中文摘要（套件名、运行时间、N 项·通过·警告·失败·用时、退出码）、趋势速览
    （各场景最近 8 次运行彩色圆点序列，点击直达该场景趋势页）、三个快捷大按钮
    （运行场景套件 / 查看趋势 / 刷新）；truncated（soak 中断）报告显示"运行中 / 中断"与提醒；
    读取逻辑与 Scenario 页同源（排除 `*.partial.json`）；suite 运行完成回读报告后总览自动刷新。
  - **Scenario 页可读化**：suite 列表不再只显示文件路径——主行显示 suite 中文 `title`（无 title 回退 id）、
    副行显示 id 与能力清单（Inline.Capability / Ref，最多 6 项），完整路径进 tooltip；选中行高亮
    （`ax-suite-selected` 主题橙）；空状态文案与错别字修正（"（尚无报告——点击左侧套件选中后运行…）"）；
    Run 按钮更名「▶ 运行选中的套件」并在未选中时提示；列表底部加三步流程引导小字。
  - **Trend 页可读化**：页头与空态改中文引导（"在「场景套件」页运行一次套件后自动记录"）；
    下拉标签改「场景」；run 级整体结果行按 pass/warn/fail 着色（`ax-trend-run-*`）；
    环比 tooltip 明确"正值=变差（红）/负值=改善（绿）"；滚动区上方加读取方式说明行。
  - **Tab 栏**：Tests / Scenario / Trend 三页签改为「总览 / 单元测试 / 场景套件 / 趋势」四页签（中文标签）。
  - 样式：`AxuTestTokens.uss` 新增 overview 卡片/大徽章/大按钮/趋势圆点/suite 行选中/帮助小字等类
    （全部沿用既有 token，无新色值）。

## [0.3.0] - 2026-09-02（场景测试平台 v2 完成：八能力 + 工具链，Phase 3/4 交付）

- **能力库扩至八能力**（Agent 层，均 Player 可编译，各带 DOC.md 与 Samples~/Scenarios/ 示例）：
  - `perf`（性能基准）：可选加载场景 → warmup 挂起采样丢弃 → 度量期 → 可选截图；指标由
    `collectors` 采集器产出、宿主预算判定。加载失败出 `PERF_SCENE_LOAD_FAILED`；
  - `loadtime`（加载时长）：场景矩阵逐个计时，产出 `loadtime.scene.<safe>.loadMs/.firstFrameMs/.stableMs`
    + `sceneCount/loadMs.max/loadMs.avg/unstableCount`；加载失败 `LOAD_FAILED`（最终 fail）、
    未达稳定 `SCENE_NOT_STABLE`（warn）；
  - `smoke`（冒烟矩阵）：清单三来源（`list`/`buildsettings`/`directory`[仅 Editor]）→ 逐场景日志监视
    加载 + 稳定窗错误计数 + 截图留证，产出 `smoke.scene.<safe>.loadMs/.errors` +
    `sceneCount/loadMs.max/errors.total/loadFailed.total`；清单错误 `SMOKE_NO_SCENES`/
    `SMOKE_DIRECTORY_UNAVAILABLE`；
  - `soak`（浸泡/泄漏）：durationMinutes/sampleIntervalSec/gcCollectBeforeSample/screenshotEveryMinutes/
    leakSlopeMBPerMin/fpsDecayPercent/flushEveryMinutes；内存序列滑窗最小二乘斜率判"泄漏嫌疑"（warn+CSV
    取证）、前后半程 p50 FPS 衰减判定、周期截图；**partial report 定期覆盖写**（`{报告名}.partial.json`，
    `truncated:true`，正常结束删除——崩溃/中断保留前段数据，ScenarioReport 增可空 `Truncated` 字段）；
  - `stress`（压力/承载，组合 phases）：`capacity` 容量探测（ramp + 60 帧滑窗 fps 判超载 + 二分收敛，
    输出 `capacity.maxEntities` 与承载曲线 CSV）、`spawner`（prefab 可空回落内置替身）、`churn`
    （每秒建毁 N × M 秒）、`flood`（自建托管事件洪峰吞吐基准，零框架依赖）；StressSpawner/StressCollector 配套；
  - `monkey` v1（猴子测试）：坐标随机点按（`AgentInput`：Input System `QueueStateEvent` 注入，
    `AXU_INPUTSYSTEM_PRESENT` versionDefines 守护；未装/无设备降级 UGUI ExecuteEvents + info finding）、
    seed 可复现、JSONL 操作流水、错误即截图可配即停、卡死检测（frameCount 停滞 → 截图 + error finding + 终止）；
  - `gamehook`（游戏适配器，显式注册 API）：`AxuTestAgent.RegisterCapability/RegisterScenario` +
    `ScenarioDef.FromCode`（Core 纯委托）/`FromCodeRoutine`（Agent 协程形态）+ `AxuScenarioRegistry`
    （进程内注册表，同 id 后者覆盖）；宿主 suite 解析推迟到 AfterSceneLoad 后首帧，保证游戏侧
    RuntimeInitializeOnLoad 注册可见（时序契约见 DOC）。
  - 每能力目录附 `DOC.md`（参数表含 clamp、指标键表、budget 示例、平台备注）；内建能力注册经
    `BuiltInCapabilities.RegisterAll`（显式 API，无反射扫描）。
- **共享基建**（`Agent/Tools/`）：
  - `AgentEnvironment`（+ Overrides/Snapshot POCO）：DOC §4 通用参数保留键（timeScale/vsync/targetFrameRate/
    qualityLevel/resolution/seed）的解析（缺键/类型不符一律 null，绝不抛异常）、应用（越界 clamp；
    **resolution 仅 Player**）与恢复（宿主在 finally 中调用，超时中断也还原）；
  - `AgentScenes`：场景加载三级回退解析（原样路径 → 去 `.unity` → 纯文件名）+ 激活场景 isLoaded 确认；
  - `AgentCapture`：截图原语（延迟生效帧等待 + 文件校验，失败仅告警）+ artifacts 路径拼合；
  - `AgentLogWatch`：日志监视（Error/Exception/Assert 与 Warning 分计数、最近 20 条快照 FIFO 环形淘汰、
    ResetCounts、线程安全、Dispose 退订）。
- **度量会话与 Runner 契约扩展**（加法式）：`MetricSession` 新增 `Suspend()/Resume()`（引用计数挂起，perf
  warmup 用）与 `TryAttachCollector()`（runner 自有采集器挂入会话）；`ScenarioContext` 新增 `ArtifactsDir`/
  `Seed`；`ScenarioRunHandle` 新增 `Artifacts` 列表；宿主逐 scenario 应用/还原环境通用参数、支持 scenario 级
  `timeoutSec` 覆盖；artifacts 目录默认落到报告同目录 `artifacts/`，截图路径写进报告 `results[].artifacts`。
- **工具链**（Phase 4）：
  - **Core/Trend**（纯 C#）：`TrendPoint` / `TrendStore`（schema `axu-trend/1` 追加式长表 CSV，RFC 4180
    转义、引号内换行无损回读）/ `TrendSummary`（按指标序列 + 方向感知环比，正值恒=变差）；趋势路径
    `Library/AxuTestReports/trend/trend.csv`（`AxuTrendPaths`）。
  - **Editor.Pipeline 六命令**：`axu_scenario_list/run/status`（Editor PlayMode 驱动 suite，异步 + Temp
    状态文件协议）、`axu_player_suite_build/run`（Development Build，define 临时开启 try/finally 还原；
    Player 进程运行 → 退出码 → 回拉报告 → 趋势入库）、`axu_trend_summary`（最近 N 次序列与环比）。
    三类运行与 `axu_test_run` 共享互斥闸（双向闭环）。
  - **Editor 层**：`ScenarioRunService`（桥文件 + 看门狗 + 趋势自动入库 + 自动退 Play，菜单/命令/看板共用）、
    `JUnitExporter` / `HtmlSummaryExporter`、`ScenarioPrecheck`（`AXU_TEST_AGENT` 未开可跳过警告）、
    `PlayerSuiteDeployMenu`（StreamingAssets 一键部署/清理）。
  - **看板**：新增 **Scenario / Trend** 页签（Tests 页零改动）——suite 列表与运行、报告摘要、JUnit/HTML
    导出、趋势分组浏览与环比着色。
- **实测驱动修复 6 项**：
  - `AgentRunnerHost.Supervise` 改 Stack 驱动任意深度嵌套迭代器（旧实现只代步一层，capacity/churn 深层
    等待被整段吞掉致秒退零实体）；中断路径统一 Dispose 栈内迭代器（防 stress 实体泄漏）；
  - `CapacityProbe` FpsWindow 启动判据缺陷（`_filled` 自锁恒 NaN → 每窗误判超载收敛 0）修复；
  - **gamehook 注册时序**：宿主 BeforeSceneLoad 即解析 suite 早于游戏侧 AfterSceneLoad 注册 →
    CAPABILITY_MISSING 假阴性；RunAll 首步 yield null 推迟到 AfterSceneLoad 后首帧；
  - **全 0 聚合恒产出**：smoke/loadtime 的 total 类指标在采集器 Start 播种 0 值采样——"零错误"这一合法
    状态不再被预算按指标缺失误判 fail（观测类聚合保持"缺失=fail"哲学）；
  - **Unity 6 场景清单校验收口**：6000.0.23f1 实测 Editor 与 Player 一致强制校验构建场景清单——收口为
    前置检查 + 可读报错指引，目标场景须加入 Build Settings/Build Profile；
  - **跨平台文件名清洗**：`AgentPaths`/`AxuCapture` 清洗字符集改平台无关固定并集（Windows 全集 + 控制
    字符），修复 macOS 上 `:` 等不被清洗导致 metric 键跨机不一致。
- **`Samples~` 目录补建（v1 悬空声明修复）**：根因为工程 `.gitignore` 的备份文件模式 `*~` 把 `Samples~`
  整体忽略；`.gitignore` 追加 `!**/Samples~/` 反选后补齐 `Tests/{Editor,Runtime}` 两示例，新增 `Scenarios`
  示例（八能力各一份 example suite + 接入 README），package.json samples 增设第三项。
- Agent asmdef：references 增 `Unity.InputSystem`（versionDefines 可选）与 `UnityEngine.UI`（降级路径）。
- 契约单测 57 → **209**（三能力 73 例、趋势 25 例、四能力 45 例 + 既有）。
- **DoD 验证**：macOS——perf/loadtime/smoke 四套件 PlayMode 桥文件链路、budget/unbudgeted/timeout 三分支
  路径、stress 容量探测（maxEntities=2000 下界/40 点曲线/churn 300 对/flood 295 万事件）、monkey 同 seed
  双跑流水逐行一致、gamehook e2e pass、soak 10min（partial 每 2min 一刷×5、中断保留 truncated、结束删除）
  与 2min Player 抽验（isEditor=false）、player build+run 端到端回拉、axu_scenario_run→status→趋势入库→
  trend_summary、包测试 209/209、Axu/Smoke 7/7；Windows——P0 三能力四套件 e2e + 验收重放（见下）。
- **验收重放（2026-09-02，Windows）**：echo/branches/perf/stress 四套件 e2e 串行重放全符合预期——
  echo 1p/1f（BASELINE_REGRESSION 为已知跨机基线项）、branches 三分支闭环（budget pass/unbudgeted
  pass+`unbudgeted:true`/timeout `RUNTIME_TIMEOUT` error）、perf pass（fps.avg=103.02、cpuP95=29.04ms、
  截图 artifact 落盘）、stress 2 pass（capacity.maxEntities=2000 打满配置上限/40 点曲线、churn 300 对、
  flood 126.8 万事件）；回归 包测试 209/209 + Axu/Smoke 7/7、Console 零 error、四场运行趋势入库
  （trend.csv 135 数据点/7 scenario）。工程所有者实机试用通过（反馈的看板可视化深度项记 backlog #4，
  不阻塞本版）。
- **交付审查修复（2026-09-02，Phase 6 unity-reviewer）**：
  - 【P1】`axu_scenario_list` 内建能力静态清单补齐 soak/stress/monkey（gamehook 注明"注册制"）——
    原清单停留在四能力时代，误导自动化调用方的能力发现；
  - 【P2】Samples 编辑器示例 asmdef 引用名 `UnityEngine.Editor.TestRunner` 更正为 `UnityEditor.TestRunner`
    （原引用不存在，导入 SampleEditMode 后无法编译）；
  - 【读侧闭环】看板回读报告排除 `*.partial.json`（soak 中断残留不再被误选为最新报告）并在发现残留时
    LogWarning 提示（含"目录只有 partial"盲区：残留告警先于"未发现报告"判空）；在途互斥报错文案
    （axu_test_run / scenario / player 三处命令 + 看板路径）统一补"running 超 15 分钟视为陈旧自动放行、
    可删状态文件手动解锁"提示；
  - 【文档】Echo 补 `DOC.md`（八能力各带 DOC 达成）；README 目录树对齐实际文件名
    （AgentRunnerHost/ScenarioRunHandle 等）；`axu_player_suite_build` 参数行补 Windows/Linux 需显式
    `output` 的平台备注；Agent asmdef 对 InputSystem/UGUI 硬引用约束如实注明（正规化拆分记 backlog #5）。


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
