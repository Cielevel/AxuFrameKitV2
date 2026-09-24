# AxuTestFramework 可用性改造 · 阶段 2 交付与验收报告（首次使用路径）

> 对应方案：`.zcode/plans/axu-test-framework-usability-refactor.md`（阶段 2：首次使用路径）
> 实施日期：2026-09-24 · Unity 6000.0.23f1 · 包版本 `com.cielevel.axu-test-framework` 0.4.0 → **0.5.0**
> 实施方式：架构师代码级拆解 → Task A（逻辑层+契约+单测）∥ Task B（看板 FirstRun 卡）并行开发 → Task C（接线+空态+文档）→ 统一 E2E 验证 → 独立审查。基线 commit `bfb2854`（阶段 1 复评补修收尾）。

## 1. 变更摘要

| 方案要求 | 落地方式 | 关键文件 |
|---|---|---|
| 首页单一明显首次操作（自包含示例） | 总览页首屏第一元素为「开始使用」卡：自检大按钮"运行示例测试"+ 免责副标题。自检 suite 由包内代码内嵌规范 JSON（`FirstRunSuiteCatalog.SelfCheckSuiteJson`，suiteId=`axu-selfcheck-echo`，单 echo 内联场景，**无 budgets/baseline/场景依赖**）点按钮时写 `Temp/axu_selfcheck_echo.suite.json` 后经既有 `ScenarioRunMenu.TryRunSuite` 走阶段 1 启动-收尾契约；不引用 Assets demo（含预算，语义不纯）、不直读 Samples~（路径不稳） | `Editor/Scenarios/FirstRunSuiteCatalog.cs`、`Editor/Window/AxuTestRunnerWindow.FirstRun.cs`（新 partial） |
| "工具链可用 ≠ 项目全过"不误导 | 三处承载：①按钮副标题 `SelfCheckDisclaimer`；②结果映射 `DescribeSelfCheckOutcome`：completed 全过→"测试工具链可用…**不代表项目全部测试通过**"；有 fail/中断→不出现"可用/通过"字样；③`ScenarioRunStatusPresenter.ScopePrefix(suiteId)`：自检 suite 在总览徽章/详情与 Scenario 页摘要前缀"（示例自检）" | `Editor/Window/FirstRunPresenter.cs`（新）、`ScenarioRunStatusPresenter.cs`（加法：ScopePrefix） |
| 入口按用户目标组织，选目标后再出设置 | 「开始使用」卡四个目标行（点击展开设置行，互斥单开）：**检查代码功能(code)**=EditMode/PlayMode 二选+带模式跳转单元测试页（`SetPage("tests")+SetMode`，复用 Tests 页唯一 UTF 运行 UI，不造第二套入口）；**检查场景能否运行(scene)**=smoke(buildSettings) 代码内建 suite；**查看性能变化(perf)**=当前场景采样 suite（无预算，引导多次运行后看趋势页）；selfcheck 行不重复渲染。原快捷操作卡降级为次要按钮 | `FirstRunSuiteCatalog.BuildGoalSuiteJson`、`AxuTestRunnerWindow.FirstRun.cs`、`AxuTestRunnerWindow.Overview.cs` |
| 运行前提可操作预检 | `RunReadinessCheck.Evaluate`（纯函数，可注入单测）+ `GatherInput`（读真实状态：Play/编译/define/双在途/Build 场景数）。UI 横幅按 Issue 渲染修复按钮：`EnableAgentDefine`→新增幂等 `AgentDefineMenu.Enable()`；`ExitPlayMode`→退出 Play；双在途→"运行中，结束后可发起"等待文案（不伪装成修复）；Build 场景空→指引；编译中→禁用+提示。code 目标不需要 define（UTF 语义），按目标参数化门控 | `Editor/Scenarios/RunReadiness{,Issue,Input,Check}.cs`（新）、`AgentDefineMenu.cs`（加法：Enable） |
| 无 suite 文件时给"使用示例/创建最小配置" | Scenario 页 suite 列表空态两按钮："先用示例套件试一次"（复用 RunSelfCheck）；"创建最小配置"（`TryCreateGoalSuite("scene")`：AssetDatabase.CreateFolder 逐级+重名加序号+ImportAsset 落 `Assets/Tests/Scenarios/`，创建后自动刷新列表并选中，状态栏给下一步指引）。Scenario 页运行按钮 busy 判定扩为四条件（Play/运行记录/双在途），与总览一致 | `AxuTestRunnerWindow.ScenarioTrend.cs` |

文档与版本：`package.json` 0.5.0；CHANGELOG 新增 0.5.0 条目；README「看板窗口」新增"首次使用与工具链自检"小节、「场景测试接入一页纸」开头补零配置路径。根 `AGENTS.md` 的包条目同步 0.5.0。

## 2. 验收项逐条结果

环境：Windows · Unity 6000.0.23f1（运行中 Editor，经 Unity Pipeline 本地 API 驱动）· 包 0.5.0

| 验收项（方案原文） | 结果 | 证据 |
|---|---|---|
| 不了解项目的人打开窗口，无需读文档/编辑文件即可完成一次工具链自检 | ✅ | E2E S1–S3：define 关闭时按钮"尚未就绪"+横幅给"启用 AXU_TEST_AGENT"修复按钮（点击→重编译→就绪）；点"运行示例测试"→进 Play→终态自动退 Play→卡内回显结论，全程零 JSON/零文档（截图 01/01b/02/03） |
| 用户能看出示例测试通过 ≠ 项目全部测试通过 | ✅ | 成功结论行"示例自检完成：测试工具链可用。…不代表项目全部测试通过"；总览徽章"全部通过（示例自检）"；免责声明同时存在于按钮副标题与 README（截图 03） |
| 缺少前提条件时，按钮给出原因及可执行的处理方式 | ✅ | define 未开→"启用 AXU_TEST_AGENT"按钮（S1 实测点击修复闭环）；Play 中/运行在途→横幅"退出 Play 模式"按钮与等待文案（S2 截图 02 中三段式阻断横幅实拍）；编译中→禁用+提示 |
| 方案附加：无 suite 文件时的出路 | ✅ | E2E S5：移走 `Assets/Tests/Scenarios/` 后空态两按钮出现；"创建最小配置"重建目录+落 `scene.goal.suite.json`（含 .meta）+自动选中+状态栏指引；选中即跑通 completed 1/1（截图 05/06）；验证后资产原样还原（`git status -- Assets/` 零净变化） |

### 测试与静态检查

| 项目 | 命令 / 范围 | 结果 |
|---|---|---|
| 新增单测 | `axu_test_run {mode:editor, filter:FirstRun}` | **32/32**（RunReadinessCheckTests 11、FirstRunSuiteCatalogTests 8、FirstRunPresenterTests 13；console error=0） |
| 包类别回归 | `axu_test_run {mode:editor, category:"Axu/TestFramework"}` | **290/290**（阶段 1 基数 258 + 本次 32） |
| 全量 EditMode | `axu_test_run {mode:editor, category:"Axu"}` | **360/360**（报告 `Library/AxuTestReports/20260924-074657-editor.json`） |
| 全量 PlayMode | `axu_test_run {mode:playmode, category:"Axu/Play"}` | **14/14**（报告 `20260924-074721-playmode.json`） |
| 规范校验 | `axu_lint` | 4 pass / 1 warn / 0 fail；唯一 warn 为既有 24 处 `file.type_name`（改动前既有家族文件），**本次新增 20 文件零违规**（partial 拆分文件走既有锚点豁免） |
| 环境自检 | `axu_doctor` | 8/8 pass；`packageVersion=0.5.0` |
| 三入口等价 | `axu_scenario_run` 直跑 `Temp/axu_selfcheck_echo.suite.json` 绝对路径 | completed、exitCode 0、自动退 Play（E2E S6）——自检 suite 对看板/Pipeline 等价可用 |

### 截图（`.zcode/reports/axu-phase2-screenshots/`）

| 文件 | 状态 |
|---|---|
| `01-first-use-blocked.png` | define 关闭：首卡"开始使用"、按钮"尚未就绪"禁用、横幅含原因+"启用 AXU_TEST_AGENT"修复按钮 |
| `01b-first-use-ready.png` | 修复后：横幅消失、按钮"运行示例测试"可用 |
| `02-selfcheck-running.png` | 运行中：阻断横幅（Play 中/在途/等待）+修复按钮、结论行"自检运行中（第 0/1 项）"、徽章"正在运行（示例自检）" |
| `03-selfcheck-success.png` | 成功回显："测试工具链可用"+免责限定、徽章"全部通过（示例自检）" |
| `04-selfcheck-aborted.png` | 手动中断：`aborted/interrupted`、reportPath 空、"示例自检被中断…"、无报告冒充 |
| `05-empty-list.png` | Scenario 页空态：指引+两按钮 |
| `06-created-minimal.png` | 创建最小配置后：新 suite 选中、运行按钮点亮、状态栏指引 |

## 3. 兼容性与迁移影响

- **报告 schema 与 Pipeline 命令零变更**（审查确认 diff 无 schema/命令参数改动）。
- **包内 Editor API 纯加法**：`ScenarioRunStatusPresenter.ScopePrefix(string)`、`AgentDefineMenu.Enable()`；既有成员签名/行为零改动（`git diff` 核验）。
- 新增内部文件 `Temp/axu_selfcheck_echo.suite.json`（会话级，幂等重写，不入库不进 Assets）；"创建最小配置"按用户点击落 `Assets/Tests/Scenarios/*.goal.suite.json`（重名自动加序号，不覆盖既有文件）。
- 示例自检运行会进趋势 CSV（`axu-selfcheck-echo` 行）与 Scenario 报告目录，属设计行为；Trend 页行渲染未加前缀（阶段 3 信息分层范围）。
- perf 引导 suite 无预算：趋势页显示 unbudgeted 属预期（README 已写明，不误导）。

## 4. 已知限制与仍需人工确认

1. **审查遗留 P3（不阻断，留阶段 3）**：①首用卡 busy 判定三条件 vs Scenario 页四条件（缺 `ScenarioRunRecord.TryRead()`，残留陈旧记录时两卡可用性可能短暂不一致，最终被 `TryRunSuite` 预检兜住）；②横幅可见期间每 800ms 重建修复按钮（readiness 不变时也重建，存在每 tick 分配与吞点击的理论窗口）。
2. **code 目标复用 Tests 页**：目标行只做模式二选+带参跳转，不内嵌 UTF 运行 UI（避免第二套入口，方案评审决策 D3）。
3. **scene 目标前提**：Build Settings 无场景时被预检拦下（指引配置），但"场景存在但加载失败"属真实测试 fail，不是前提缺失，文案已区分；smoke 在场景清单非空时的完整行为由阶段 1 的 smoke 能力保证，本次未重复验证。
4. **perf 目标语义**：采样当前场景、无预算，首跑只有绝对值，环比需第二次运行；当前场景为空/未保存时数值无参考意义但不失败（tooltip 已写明）。
5. **`axu_doctor.packageVersion` 滞后为环境现象**：package.json 升版后 UPM 已解析版本不自动刷新（曾报 0.4.0），经 `package_resolve` 后恢复 0.5.0；非代码缺陷，未改命令。
6. **看板窗口验证期间被转为浮动**（E2E 截图需要，原为停靠）；无功能影响，可手动拖回。
7. **macOS 未实测**：本次全部在 Windows 验证；新增代码不含平台相关调用（Build Profiles 菜单打开为 try/catch 降级设计）。
8. **域重载兜底回显**：窗口重建后按"自检 suite 最近终态"回显一次；本阶段已加"（历史）"限定避免误导（见 §5 修复项）。

## 5. 评审修复（unity-reviewer 审查后、提交前）

审查结论为"修复后可交付"，2 条 P2 与 2 条同文件 P3 已在提交前修复（均在 `AxuTestRunnerWindow.FirstRun.cs`）：

| 级别 | 问题 | 处理 |
|---|---|---|
| P2 | 结论认领后 `_firstRunPendingRunId` 不清零：之后从场景套件页发起任意运行，轮询会把已回显的自检结论错误覆盖为"没等到结论" | 终态结论渲染交付后同步清零 pending |
| P2 | "保存为可编辑套件"tooltip 称"已存在则直接复用"，实际恒落新文件（重名加序号） | 文案对齐行为："重名自动加序号，不覆盖" |
| P3 | 认领作废分支要求运行记录存在；记录被看门狗清理后 pending 永不清除，结论行停在"自检已发起…" | record 缺失且状态不匹配时同样作废认领 |
| P3 | 域重载兜底把较早的自检终态当新鲜结论回显，无历史标注 | 兜底分支加"（历史）"限定 |

修复后复验：重编译 `completed`、零错误；`axu_test_run {mode:editor, filter:FirstRun}` 32/32 全过（报告 `Library/AxuTestReports/20260924-083013-editor.json`）；真实链路复验 P2-1：看板路径发起自检 → 终态正常回显（认领路径交付，无历史标注）→ 随后经 Pipeline 跑普通套件 echo-demo（completed，exitCode 1 为其自带预算 warnAsFail 的既有语义），期间卡内自检结论保持不被改写、未出现"没等到结论"误报，echo-demo 退出 Play 后窗口重建、结论行按设计复位（状态非自检 suite 时不渲染兜底）。另：package.json 升 0.5.0 后 `axu_doctor.packageVersion` 短暂滞后为 0.4.0，经 `package_resolve` 刷新后恢复 0.5.0（UPM 已解析版本缓存滞后，非代码缺陷）。

## 6. 与方案"实施边界与验证纪律"的对照

- ✅ 运行正确性先行：阶段 2 未改运行链路本体，三条入口仍共用阶段 1 启动-收尾契约；E2E S4 中断路径复验仍为 `aborted/interrupted`、无报告冒充。
- ✅ 报告 schema 与命令兼容（§3）。
- ✅ 未直接编辑场景/Prefab/资产 YAML；S5 的目录移动属临时验证动作，已原样还原（`git status -- Assets/` 零净变化）；"创建最小配置"为用户点击触发的编辑器内 C# 逻辑，.meta 由 Unity ImportAsset 生成。
- ✅ 未执行裸 `run_tests`（全部带 assembly/category/filter 过滤）；Play 在途期间未并发派发其他运行类命令（仅只读状态文件与截图）。
- ✅ 测试覆盖真实行为：自检/目标 suite JSON 过真实 `TryPrevalidateSuiteFile`、判定顺序断言、幂等落盘断言、呈现映射按关键限定语断言（非文案镜像）。
