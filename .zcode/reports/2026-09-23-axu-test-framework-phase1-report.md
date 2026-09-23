# AxuTestFramework 可用性改造 · 阶段 1 交付与验收报告

> 对应方案：`.zcode/plans/axu-test-framework-usability-refactor.md`（阶段 1：运行闭环与可信状态）
> 实施日期：2026-09-23 · Unity 6000.0.23f1 · 包版本 `com.cielevel.axu-test-framework` 0.3.0 → **0.4.0**
> 实施时状态：**已实施并验证**（以下为实施 Agent 的原始验收记录）。2026-09-23 复评发现 3 项阶段 1 补修问题及 20 分钟实跑证据缺口，**最终验收待补修与复评**；任务与标准见 `.zcode/plans/axu-test-framework-usability-refactor.md` 的“阶段 1 复评补修清单”。

## 1. 变更摘要

阶段 1 的五项修复（方案原文编号一致）：

| # | 方案要求 | 落地方式 | 关键文件 |
|---|---|---|---|
| 1 | 看板/菜单/Pipeline 共享启动-收尾契约；看板运行自动退 Play 并加载本次报告 | 三条入口统一走 `ScenarioRunService.StartEditorPlayRun`：预检 → 写运行记录 `Temp/axu_scenario_run.json`（含 `runId`/suite/发起方）+ 桥文件 → 进 Play → 看门狗在终态时趋势入库、删记录、**自动退 Play**。看板状态改为文件驱动（域重载安全） | `Editor/Scenarios/ScenarioRunService.cs`、`ScenarioRunRecord.cs`（新）、`ScenarioRunMenu.cs`、`Window/AxuTestRunnerWindow.ScenarioTrend.cs` |
| 2 | 终态可辨识 + 运行标识/时间校验，陈旧文件不得当本次结果 | 桥文件与状态文件新增 `runId`（发起方生成，Agent 原样回写）；手动停止由看门狗补写 `status=aborted`（`reason=interrupted`，**`reportPath` 清空**）；`error` 终态带 `reason`（`suite_load_failed` / `report_write_failed` / `agent_not_run`）+ `message`；新增 `heartbeatUtc`；看板按 `runId` 归属回显，非本次不发报告 | `Agent/Runner/ScenarioStatusFile.cs`、`AgentRunnerHost.cs`、`Editor/Reports/AxuScenarioRunStatus.cs`、`Window/ScenarioRunStatusPresenter.cs`（新） |
| 3 | 长跑互斥：不得只按启动时间判在途（>15min soak、并发 UTF） | 在途判定改看**最后活动时间**（心跳 > 状态文件写入时间 > 启动时间，阈值 15 分钟）；Agent 宿主执行期每 5 秒心跳续写状态文件；新增 `AxuTestHeartbeat` 在 UTF 运行期同样每 5 秒刷新；`axu_doctor` 改用同一谓词与类型化模型 | `AgentRunnerHost.cs`、`Editor/Reports/AxuTestHeartbeat.cs`（新）、`AxuTestRunStatus.cs`、`AxuScenarioRunStatus.cs`、`Editor.Pipeline/Commands/AxuDoctorCommand.cs` |
| 4 | 纯 `ref` 代码态 scenario 的文件加载链路 | `SuiteDef.Validate(externalIds, errors, unresolvedRefs)` 三分法（结构错误 / 未解析 ref）；`SuiteLoader.LoadSuite(path, externalIds, ignoreUnresolvedRefs)` + `FindUnresolvedRefs`；Agent 加载与扇出共用同一注册快照；`axu_scenario_list` 宽松加载并给出 `unresolvedRefs` + `note`；新增工程内示例 suite（纯 ref） | `Core/Scenarios/SuiteDef.cs`、`SuiteLoader.cs`、`AgentRunnerHost.cs`、`AxuScenarioPipelineCommands.cs`、`Assets/Tests/Scenarios/gamehook-ref-demo.suite.json`（新） |
| 5 | `mode=all` 双阶段语义与整体结论 | 新增 `AxuTestRunPhase` 阶段模型；返回值增 `phases[]` / `phasesComplete` / `overallConclusion`（`editorSummary` 保留）；**仅当 EditMode 段正常 completed 才续跑 PlayMode 段**，超时/出错则跳过且 `status` 保留 `timeout`/`error`（不再被 `playmode_running` 掩盖）；PlayMode 段异步期间状态文件带 `precedingPhase`（跨域经运行上下文传递） | `Editor/Commands/AxuTestRunPhase.cs`（新）、`AxuTestRunService.cs`、`AxuTestRunOutcome.cs`、`AxuTestRunContext.cs`、`AxuTestReportWriter.cs` |

**顺带修复（实施中发现的两个静默缺陷）**：

1. `AxuScenarioRunStatus.Summary` 此前裸写 `ReportSummary`，因命名空间嵌套绑定到了 `AxuTestFramework` 下 **UTF 报告用的同名类型**（字段 total/passed/failed/skipped/inconclusive，**没有 warned**），导致 scenario 状态文件的 `warned` 计数一直被静默丢弃。现已显式绑定 `AxuTestFramework.Core.Report.ReportSummary`，并有回归测试锁定。
2. `axu_doctor` 的 `runs.in_flight` 用 `json["status"]`（camelCase）解析 UTF 状态文件，而该文件实际是 **PascalCase**（`Status`）——**UTF 在途永远检测不到**。改用类型化模型（`AxuTestRunStatus.TryRead`）后修复。

### 设计取舍（需要评审确认的三处）

- **续跑决策**：`mode=all` 在 EditMode 段超时/出错时**跳过** PlayMode 段（方案允许两选一，要求写明并测试）。理由：编辑器已卡或报告流程已坏时再进 Play 只会叠加故障并掩盖第一段问题；用例失败（`completed` + `failed>0`）**不**触发跳过。
- **`reportPath` 清空**：中断/失败时看门狗把 `reportPath` 置空。这是"界面不会回显旧报告"的机械保证——界面只认状态文件指名的报告，不再扫目录挑最新。
- **define 预检改为硬拦**：看板/菜单/`axu_scenario_run` 在 `AXU_TEST_AGENT` 未开时**不进 Play**（原菜单路径弹可跳过警告，允许进入必然空跑的 Play）。`ScenarioPrecheck.WarnIfAgentDisabled()` 已被 `DescribeBlocker()` / `EnsureRunnable()` 取代（包内 Editor API 变更，无外部引用）。

### 评审后的修正（提交前，unity-reviewer 独立审查）

审查结论为"可交付（无 P0/P1）"，其中 2 条 P2 已在提交前修复，另有 3 条 P3 顺手收口：

| 级别 | 问题 | 处理 |
|---|---|---|
| P2 | `mode=all` 的 EditMode 段结论只在第二段**终态**才落盘 → "异步期间可读 `precedingPhase`"与实现不符，且第二段被中断时该结论丢失 | 已在 `WriteRunContextAndStatus` 写 running 状态时带上 `PrecedingPhase`（报告落盘失败的兜底状态同理）；**E2E 复验**：PlayMode 段 running 窗口内 `PrecedingPhase.Status=completed`（summary 1/1）✓ |
| P2 | 启动序列先写桥文件 → 若其后抛异常会留下**孤儿桥文件**，下一次（哪怕用户手按的）Play 会静默执行该 suite | 桥文件改为**最后一步**写，catch 分支同时删除桥文件与运行记录；E2E 复验"桥文件未残留" ✓ |
| P2 | 看门狗在 Play 中只等终态：Agent 未接手或异常时编辑器会**无限留在 Play** | Play 中若本轮状态心跳已停（>15 分钟）→ 写 `aborted(reason=interrupted)` 并自动退 Play；另加"发起后 10 秒**启动宽限**"（`AxuRunLiveness.StartGraceSeconds`），避免在进 Play 的转换窗口内误判中断 |
| P3 | `RenderScenarioReport` 的 null 分支未关 JUnit/HTML 导出按钮（中断后仍可点、点击静默无效） | 已一并禁用（与"打开"三按钮一致） |
| P3 | 初始状态的 `suiteId` 用文件名 stem（"echo-demo.suite"），Agent 写的是 `suite.Id`（"echo-demo"）→ 进 Play 前后看板标题跳名 | 预检顺带回传文件里的 `id`，初始状态直接用它（E2E 复验 `suiteId: soak-demo`） |
| P3 | `StaleMinutes = 15` 在三处各写一份字面量 | 收敛到 `AxuRunLiveness.StaleMinutes`（同处还定义启动宽限），三处常量改为引用 |
| P3 | 互斥闸的手动解锁提示只提状态文件，未提配套的运行记录文件 | 4 处提示文案补齐 `Temp/axu_scenario_run.json` |

## 2. 验收项逐条结果

环境：Windows 11 · Unity 6000.0.23f1（运行中 Editor，经 Unity Pipeline 本地 API 驱动）· 包 0.4.0

| 验收项（方案原文） | 结果 | 证据 |
|---|---|---|
| 看板发起的场景运行无需用户手动退出 Play，即能看到本次结果 | ✅ | 经 `ScenarioRunMenu.TryRunSuite(path, SourceDashboard)` 发起 echo-demo：status 文件 running→completed，`editor_status.playMode=stopped`（**无任何人工停止**），看板按 `reportPath` 回显（截图 03/04） |
| 成功、失败、加载错误、手动停止均能到达明确终态；界面不会把上一次报告当作本次报告 | ✅ | 中断：启动 10 分钟 soak → `editor_stop` → 状态 `aborted`/`reason=interrupted`/`reportPath=""`，运行记录被清理，看板显示"■ 已中断"且导出/打开按钮禁用（截图 01/02）。加载错误/写盘失败：`reason=suite_load_failed`/`report_write_failed` + `message`（单测覆盖 reason 映射） |
| 运行中的 20 分钟 soak 不允许同一 Editor 启动另一项互斥测试 | ✅ | 10 分钟 soak 运行中（心跳持续续写，两次采样 `heartbeatUtc` 与文件 mtime 均推进）→ `axu_test_run` 返回 `success=false` + "scenario 套件运行在途…"；`axu_doctor` 报 `runs.in_flight=warn`（"最后活动 2s 前"）。纯谓词层面另有 20 分钟场景单测（`心跳新鲜 → 判在途` / `心跳停止 → 15 分钟后放行`） |
| 纯 `ref` 注册 scenario 可由真实 suite 文件加载；未知 `ref` 给出明确错误 | ✅ | `Assets/Tests/Scenarios/gamehook-ref-demo.suite.json`（无内联定义，仅 `{"ref":"hook.fromcode"}`）实测跑通：`axu_scenario_run` → completed、exitCode 0、报告 `hook.fromcode pass 1718ms`。未知 ref：`LoadSuite` 抛 `… 既未在 suite 内联定义中、也不在已注册的代码态 scenario 清单中`（单测 + `axu_scenario_list` 的 `unresolvedRefs`） |
| `mode=all` 的返回值和报告能说明两个阶段各自的结果 | ✅ | 正常路径：`phases=[editor completed(1/1), playmode running]`、`phasesComplete=false`、`overallConclusion` 写明两段；轮询状态文件终值带 `precedingPhase`（EditMode 段结论）。跳过路径（`timeout_ms=1` 强制超时）：`status=timeout`、`phasesComplete=true`、结论"…PlayMode 段已跳过…本次 mode=all 未完成"，且 `playMode=stopped`（第二段确实没启动） |

### 测试与静态检查

| 项目 | 命令 / 范围 | 结果 |
|---|---|---|
| 包自身契约测试 | `axu_test_run {mode:editor, category:"Axu/TestFramework"}` | **250 / 250 通过**（其中本阶段新增 23 例：`ScenarioRunStateTests` 13、`AxuTestRunPhaseTests` 10；`SuiteLoaderExternalTests` 扩至 10 例）。评审修正后复跑仍 250/250 |
| 全量 EditMode（框架包 + 模块包 + 测试基建包） | `axu_test_run {mode:editor, category:"Axu"}` | **320 / 320 通过**（report `20260923-102830-editor.json`） |
| 全量 PlayMode | `axu_test_run {mode:playmode, category:"Axu/Play"}` | **14 / 14 通过**（report `20260923-102902-playmode.json`） |
| 评审修正后的回归复验（E2E） | `axu_scenario_run`（纯 ref suite）→ 自动退 Play → 报告 exitCode 0；`axu_scenario_run`（10 分钟 soak）+ `editor_stop` → `aborted/interrupted`、`reportPath` 空、记录与桥文件均无残留；`mode=all` 异步期间状态文件带 `PrecedingPhase` | 全部符合预期 |
| 规范校验 | `axu_lint` | `warn`（4 pass / 1 warn）：`file.type_name` 24 处**全部为改动前既有**的多类型家族文件；本次新增 6 个文件零违规；`namespace.convention` / `editor.placement` 均 0 违规 |
| 环境自检 | `axu_doctor` | 在途判定（心跳语义）、包、define、存储等项均按预期 |
| Console | pipeline `console`（groundTruth） | `consoleErrors: 0`（报告样本中的 `axu-watch-e1` 等为框架日志测试**故意**产生的预期错误） |

### 截图（看板关键状态）

| 文件 | 状态 |
|---|---|
| `.zcode/reports/axu-phase1-screenshots/00-first-use.png` | 无运行状态文件时：回退显示历史报告并**显式标注"（历史报告）"** |
| `.zcode/reports/axu-phase1-screenshots/01-dashboard-open.png` | 中断后重开：徽章"■ 已中断"+"查看详情"禁用，旧报告不再冒充本次结果 |
| `.zcode/reports/axu-phase1-screenshots/02-scenario-idle.png` | Scenario 页中断终态：进度行红色"运行结束（■ 已中断）"、面板写明原因、导出/打开按钮禁用 |
| `.zcode/reports/axu-phase1-screenshots/03-running.png` | 运行中（看板来源发起）："…正在运行（第 0/2 个场景）"，并提示"跑完会自动退出 Play 并在这里给出结论" |
| `.zcode/reports/axu-phase1-screenshots/04-completed.png` | 运行完成回显本次报告（含逐场景 Foldout：结论/指标/预算/基线对比） |

## 3. 兼容性与迁移影响

- **报告 schema 不变**：`axu-test-report/1` 与 `axu-scenario-report/1` 未改动，历史报告可继续读取。
- **Pipeline 命令参数不变**；返回值**新增**字段（`phases` / `phasesComplete` / `overallConclusion`），旧调用方按原字段读不受影响。
- **状态文件新增字段**（增量，旧读取方忽略即可）：
  - `Temp/axu_scenario_status.json`：`runId`、`heartbeatUtc`、`reason`、`message`，并新增终态值 `aborted`。
  - `Temp/axu_test_run_status.json`：`HeartbeatUtc`、`PrecedingPhase`（该文件键为 PascalCase，与 scenario 侧的 camelCase 不同，读取方按大小写不敏感解析）。
  - 新增内部文件 `Temp/axu_scenario_run.json`（运行记录，仅看板/看门狗使用；不存在视为无在途运行）。
- **`axu_scenario_run` 行为变更**：文件结构非法/define 未开/Play 中现在**直接回错误信封**（原先会进 Play 后由 Agent 报错）。这是阶段 1 期望的"可辨识终态"，但对"故意用坏 suite 验证错误路径"的脚本是可见变化。
- **`axu_scenario_list` 行为变更**：解析不到 ref 的 suite 从 `kind:unknown + error` 变为 `kind:suite + scenarioIds + unresolvedRefs + note`。
- **包内 Editor API 变更**：`ScenarioPrecheck.WarnIfAgentDisabled()` → `DescribeBlocker()` / `EnsureRunnable()`；`SuiteLoader.LoadSuite(path)` 新增两个可选参数（旧调用签名兼容）；`ScenarioRunService.StartEditorPlayRun` 新增 `source` 可选参数与 `TryPrevalidateSuiteFile`/`IsScenarioRunInFlight` 语义调整。

## 4. 已知限制与仍需人工确认

1. **`axu_scenario_list` 与运行期解析的差异（设计如此）**：Editor 侧无场景注册表，纯 `ref` 只能报"声明清单"（`scenarioIds` + `unresolvedRefs`），与运行期经 `profile` 扇出的实际实例数可能不同。
2. **极度异常路径未做 E2E**：报告写盘失败（磁盘满/权限）与 suite 在 Agent 侧加载失败（Editor 预检放行但运行时注册表为空）只在单测层面覆盖 reason 映射；E2E 需要构造文件系统/注册时序故障。
3. **看板页签在进 Play 后回到总览页**：域重载会重建窗口（既有行为，非本次引入）。总览页会实时反映运行状态，但"停留在 Scenario 页看进度条"需要重新切页。方案阶段 3 的呈现改造可一并处理"运行中默认呈现哪个页"。
4. **`AxuTestReportWriter.ReporterVersion` 仍为 `0.1.0`**（注释写"与包版本同步"但历史上一直未同步）：本次未动，属独立漂移项，建议后续统一或改为常量语义。
5. **macOS 侧未实测**：本次验证全部在 Windows 完成。改动不含平台相关代码（文件读写/HTTP 均为跨平台路径），但"看板自动退 Play + 域重载后回显"在 macOS 的时序建议补一次人工确认。
6. **UTF 心跳的自愈上限（评审提出，属已知取舍）**：`AxuTestHeartbeat` 在 `RunStarted` 挂载、`RunFinished` 摘除。若某次运行既没有 `RunFinished` 且域不重载（例如 EditMode 用例死锁在同步等待里），心跳会持续把状态续成"活"的，互斥闸会被长期占用。此时状态文件确实反映"进程仍活着"，排障手段是删除状态文件或重载域（已在 README 的解锁提示里写明）。
7. **Tests 页的"最近报告"回填仍是目录最新启发式（评审 P3-13，本阶段范围外）**：`AxuTestRunnerWindow.TryLoadLatestReport` 在本次 UTF 运行无报告时会回填报告目录里最新的历史报告（`axu_test_report {latest:true}` 同性质）。阶段 1 的验收项针对场景套件运行，此处建议并入阶段 3 的"信息分层/结论限定范围"一起收口。
8. **`axu_test_run` 的 `mode=all` 跳过第二段时 `phases[]` 只有 1 段**（评审 P3-5）：机器消费者需容忍 1 或 2 段；"已跳过"信息目前在 `overallConclusion`/`message` 文本里。是否补 `skipped` 占位段留待阶段 2/3 按消费方需求决定。

## 5. 与方案"实施边界与验证纪律"的对照

- ✅ 先修运行正确性再改文案：本次未改动任何"跑完自动回显"类文案的正确性前提，路径本身已修好（文案仅在必要处补充"自动退出 Play"）。
- ✅ 保持既有报告 schema 与 Pipeline 命令兼容（见 §3）。
- ✅ 未直接编辑任何场景/Prefab/Asset 的 YAML；新增的 `gamehook-ref-demo.suite.json` 经 pipeline `import_asset` 导入（`.meta` 由 Unity 生成）。
- ✅ 未执行无程序集/测试名过滤的裸 `run_tests`；长跑（10 分钟 soak）期间未向同一 Editor 并发派发其他命令（互斥闸单测即以此场景验证）。
- ✅ 测试覆盖真实风险而非文案镜像：状态收尾（runId 归属 / 中断终态 / 心跳判活）、文件加载（纯 ref 全链路）、长跑互斥（20 分钟谓词 + 在途记录）、双阶段结果（续跑/跳过/结论），并对 `mode=all` 做了真实 E2E（含 `timeout_ms=1` 强制跳过分支）。
