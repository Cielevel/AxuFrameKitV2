# AxuTestFramework 扩展方案：从「UTF 执行壳」到「游戏级测试平台」

> **状态：已评审通过（2026-08-31），待实施（按 Phase 0–6 逐步推进）** ｜ 日期：2026-08-31 ｜ 前置阅读：包 README（`Packages/com.cielevel.axu-test-framework/README.md`）、`.zcode/08-unity-pipeline-api.md`
>
> **评审记录（2026-08-31，工程所有者）**：§2.2 决策表全部按"推荐"列执行；其中 **D7/D8 以推荐项为初始方案，实施推进中逐步确定**——执行代理到达相关决策点时向工程所有者提问、由其答复后定稿，不得自行变更。子代理 charter 已于 2026-08-31 对齐本方案（runtime-dev / editor-dev / tester / build / reviewer 五个扩充；`unity-device-tester` 留待 Phase 5 启动时新增）。
>
> **一句话**：现有 AxuTestFramework 解决的是"UTF 测试怎么跑、怎么报、怎么看"；本方案让它额外解决"**游戏本身怎么测**"——
> ① 提供任意游戏工程零成本接入的**运行时测试入口（Player Agent）**；② 提供性能/压力/浸泡/冒烟/猴子等**游戏级测试能力库**；
> ③ 配套**指标—预算—基线—趋势—门禁**数据链路。三件事都是 Unity Test Framework 不做的。

---

## 实施工作约定（2026-08-31 工程所有者制定，对所有 Phase 生效，派发时必须随任务转达）

1. **逐步推进**：严格按 Phase 0→6 顺序执行，一个 Phase 完成并经工程所有者确认后，才进入下一个 Phase；不跨阶段并行、不抢跑。
2. **阶段开工问答制**：每个 Phase 开工时，执行代理先通读本方案中该阶段的内容并勘察涉及现状，然后**一次性提出全部疑问**（含该阶段内可预见的 D7/D8 待定子决策）；工程所有者一次性作答，问答原文记录到 §7 进度台账对应 Phase 条目下，然后连续推进——**阶段中途不再提问**（除非出现约定之外的阻塞性破坏，可暂停上报）。
3. **零影响红线**：每个 Phase 结束时必须验证**不影响工程内其它模块的运行**，最低验证集：① 全工程编译零错误；② 既有 `axu_test_*` 链路与看板 Tests 页正常；③ 既有测试冒烟（Axu/Smoke 类别）全绿；④ Phase 0 起每期追加核对 §8 风险表第 10 条（现有 2 个 asmdef 行为零改动/可解释）。验证结果写入进度台账后方可收尾。
4. **进度记录与 git 同步**：每个 Phase 完成后：先更新 §7 进度台账（状态/日期/提交/问答记录），再做 git 提交（提交信息建议前缀 `axu-test-v2:`），必要时推送远端——保证其它电脑拉取后可无缝继续开发；中间成果可按子任务多次提交，但每个 Phase 必须以一次包含台账更新的收尾提交结束。
5. **跨平台双要求（Windows + macOS）**：
   - **持续开发能力**：本工程内的开发/验证流程在 Windows 与 macOS 均可执行——交付的命令模板、脚本、路径一律提供双平台形态（如 Player 启动验证：macOS 为 `.app/Contents/MacOS/<App>`，Windows 为 `.exe`），代码不得硬编码单平台路径；
   - **使用能力**：包导入其它工程（Windows/macOS 皆可）即用——已是 UPM 包天然支持，但必须坚持：零本工程特有资产/路径/包引用；平台差异 API（FrameTimingManager、ProfilerRecorder、persistentDataPath、输入注入等）一律做运行时探测与降级，并在能力文档中标注各平台可用性；
   - **实测忠实记录（2026-08-31 补充）**：验证结果只记录**实际执行平台**的实测——在 macOS 上实测就记 macOS 的结果，在 Windows 上实测就记 Windows 的结果，不做跨平台推定或"应当同样成立"的表述；未实测的平台在台账/文档中明确标注"未实测"。当前开发机为 macOS。

---

## 0. TL;DR

| 维度 | 现状（v1） | 扩展后（v2） |
|---|---|---|
| 测试形态 | NUnit/UTF 测试方法（测试程序集 + Attribute） | **场景测试（Scenario）**：数据驱动的 JSON/SO 情景定义，与测试程序集解耦 |
| 运行位置 | Editor（EditMode/PlayMode） | Editor + **真实 Player 构建**（桌面/真机 Development Build 内嵌 Agent） |
| 测什么 | 代码正确性（断言 pass/fail） | 性能/承载/内存泄漏/加载/冒烟/随机长跑（**指标 + 预算判定 + 趋势**） |
| 接入方式 | 装包 + 写测试程序集 | 装包 + 开一个 define 即可；**零场景、零代码也能跑**（命令行驱动） |
| 结果消费 | JSON 报告 + AxuTestRunner 看板 | 报告 v2 + 趋势 CSV + 基线对比 + 退出码/JUnit → **CI 门禁** |

**核心差异化（为什么不是 UTF 翻版）**：UTF 的测试跑在 Editor 测试沙箱里、按"测试方法"组织、结果是布尔值；
本方案的测试跑在**真实构建的 Player 里**（真机性能才有意义）、按"游戏情景"组织、结果是**带阈值的度量数据**。
两者互补而非替代——现有 `axu_test_*` 链路原样保留，新链路以 `scenario` 词汇族并行。

---

## 1. 背景与差距分析

### 1.1 现状资产盘点（v1 已有能力，全部保留复用）

| 资产 | 位置 | v2 中的归宿 |
|---|---|---|
| Runtime 工具层（AxuWait/AxuTestScene/AxuCapture/AxuLogWatch/AxuNoErrorsScope/AxuTestState） | `Runtime/`（asmdef `AxuTestFramework`，`UNITY_INCLUDE_TESTS` 守护） | **原样保留**，继续服务 UTF 形态；Agent 形态按需移植等价物（见 §2.2 决策 D5） |
| 报告 POCO（schema `axu-test-report/1`）+ `[TestRunCallback]` 落盘 | `Runtime/Reports/` + `Editor/Reports/` | 原样保留，服务 UTF；scenario 走新 schema（§6.1） |
| pipeline 命令 `axu_test_run/status/report`、`axu_window_screenshot` | `Editor/Commands/`（`[CliCommand]` + TypeCache 注册） | 原样保留；新增 `axu_scenario_*` / `axu_player_*` / `axu_trend_*`（§3.5） |
| AxuTestRunner 看板（UITK、套件树、三态勾选、彩色结果、分屏） | `Editor/Window/` | 加"Scenario / Trend"页签（§6.3） |
| 零框架依赖（不引用任何 `AxuFrameKit.*`） | 包级纪律 | **继续坚持**，是"任意游戏工程可接入"的前提 |

### 1.2 同质化问题

v1 的价值链是「pipeline 驱动 → UTF 执行 → 结构化报告 → 看板」，本质是 UTF 的工程化外壳。UTF 本身决定了天花板：

- 测试必须写在测试程序集里（策划/QA 无法参与；游戏代码与测试代码耦合度要求高）；
- PlayMode 测试在 Editor 里跑，**测不到真机性能、显存、热降频、真实施加载**；
- 结果只有 pass/fail + 耗时，没有 FPS/内存/DrawCall 等游戏指标，更没有阈值与趋势；
- 无法在打包后的游戏内触发、无法做长时浸泡（一跑几小时）与自动退出回传。

### 1.3 目标定位（三大支柱）

1. **通用接入层（Entry/Agent）**——任意游戏工程装包 + 开 define 即获得全套测试入口（命令行/编辑器/游戏内面板/API），Editor 与 Player 全场景。
2. **游戏级测试能力库（Capabilities）**——性能基准、承载压力、浸泡泄漏、场景冒烟矩阵、猴子测试等，开箱即用、可插拔扩展。
3. **数据与门禁（Data & Gates）**——统一度量 API、预算（Budget）判定、基线（Baseline）对比、趋势（Trend）留存、退出码/JUnit 供 CI 卡口。

### 1.4 非目标（明确不做，防蔓延）

- 不替代 UTF 单元测试（现有 `axu_test_*` 全保留，两套链路词汇不混用）；
- 不做云端设备农场编排（真机先走 adb/手工，HTTP 远程触发仅列远期）；
- 不做像素级图像回归（golden image diff，仅远期候选）；
- 不做网络协议级弱网仿真（系统级工具更合适，仅预留 Hook 位）；
- 不追求 Release 构建可测（性能类能力强制 Development Build，见 §9 风险 3）。

---

## 2. 总体架构

### 2.1 程序集拓扑（包内新增 2 个 asmdef，现有 2 个不动）

```
AxuTestFramework.Core      （新增）纯 C#，noEngineReferences:true，零 UnityEngine
AxuTestFramework.Agent     （新增）运行时 Agent：Runner/度量采集/Bootstrap/Overlay
     │ 仅依赖 Core（可引用 Newtonsoft dll）
AxuTestFramework           （现状）UTF 测试编写工具层，UNITY_INCLUDE_TESTS，原样保留
AxuTestFramework.Editor    （现状+）看板扩展 / scenario 编辑器 / define 开关 / CLI 工具
     │ 依赖 Agent + Core + 原 Runtime；pipeline 引用改为 versionDefines 可选化
AxuTestFramework.Editor.Pipeline（可选新增）axu_scenario_* 等依赖 pipeline 的命令，
     引用 Unity.Pipeline；缺失该包时整体不编译（versionDefines 守护）
```

各 asmdef 要点：

| asmdef | defineConstraints | 关键 references | 说明 |
|---|---|---|---|
| `AxuTestFramework.Core` | 无 | `Unity.Nuget.Newtonsoft-Json`（唯一例外，见决策 D1） | 场景模型/指标统计/预算/基线/趋势/报告 POCO/JSON 读写/种子工具 |
| `AxuTestFramework.Agent` | 无（内部用 `#if AXU_TEST_AGENT` 守护公开入口，见 D2） | Core + Newtonsoft | Player 可编译；`noEngineReferences:false`；IL2CPP 安全 |
| `AxuTestFramework`（现状） | `UNITY_INCLUDE_TESTS`（不变） | 现状不变 | 零回归风险 |
| `AxuTestFramework.Editor`（现状+） | Editor-only（不变） | + Agent、Core；pipeline 硬引用移除 | 0.2.0 起零 pipeline 引用；`AXU_PIPELINE_PRESENT` 由 Editor.Pipeline **自身** versionDefines 注入（versionDefines 为程序集级作用域，不能跨程序集供给 define） |
| `AxuTestFramework.Editor.Pipeline`（新增） | `AXU_PIPELINE_PRESENT` | Unity.Pipeline + Editor + Agent | 所有 `[CliCommand]` 新命令放这里；未装 pipeline 包则不编译 |

### 2.2 关键技术决策（先定盘，后实施）

| # | 决策点 | 选项 | **推荐** | 理由 |
|---|---|---|---|---|
| D1 | Core 的 JSON 方案 | ① System.Text.Json ② Newtonsoft ③ 自写迷你 JSON | **② Newtonsoft**（`com.unity.nuget.newtonsoft-json`，纯 .NET dll，Player 可用） | 与报告链路统一序列化栈；①在 Unity Player 支持不稳；③维护成本高。文档中注明"Core 零引擎依赖、允许 Newtonsoft" |
| D2 | Agent 进 Player 的编译守护 | ① `UNITY_INCLUDE_TESTS` ② 独立 define `AXU_TEST_AGENT` ③ 无守护常编译 | **② `AXU_TEST_AGENT`**，且 Editor 侧 scenario 运行也要求它 | ①在 Player 永不编译，否决；③测试代码进正式包不可接受；②一键开关、意图明确，defineConstraints 表达不了 OR 逻辑，故用包内 C# `#if` 而非 asmdef constraint（Agent asmdef 始终编译、入口被 `#if` 清空），保证"没开 define 时也不报缺失引用" |
| D3 | Scenario 载体 | ① 纯 ScriptableObject ② 纯 JSON ③ SO↔JSON 双向，**JSON 为权威** | **③** | JSON 是 CI/命令行/AI 的通用语；SO 是策划编辑器友好形态；双向转换器放 Editor 层 |
| D4 | 断言/判定体系 | ① 继续用 NUnit ② 自研轻量 Finding 模型 | **②** | Player 内没有 nunit.dll；scenario 判定本来就不是布尔断言而是"预算/条件 → Finding 列表"，自研更贴合 |
| D5 | Agent 与现有 Runtime 工具层的关系 | ① 共用现有 asmdef ② Agent 内移植等价小工具 | **② 移植**（`Agent/AxuTestWait`、`AgentLogWatch`、`AgentCapture`、复用思路重写 ResetStatics 登记表） | 现有 asmdef 被 `UNITY_INCLUDE_TESTS` 锁死且带 nunit 引用，强行共用会破坏现有零回归局面；先复制后收拢，在 CHANGELOG 注明来源 |
| D6 | 统计/性能测试基座 | ① Unity Performance Testing Package ② 自研 | **② 自研** | ① 深度绑定 UTF/Editor，Player 不可用；自研统计（分位数/斜率/回归）是纯 C#，落 Core 正好 |
| D7 | 场景发现/注册方式 | ① 运行时反射扫描 Attribute ② 显式 API 注册 ③ Editor 烘焙 Manifest，Player 只读 Manifest | **③ 为主 + ② 兜底** | IL2CPP stripping 下运行时反射不可靠；Editor 侧 TypeCache 扫描 `[AxuScenario]` 生成 `axu-scenarios.manifest.json`，Player 按 Manifest 加载；代码注册 API 供动态场景 |
| D8 | 真机报告回传 | ① HTTP ② adb/usb ③ 手动 | **② + ③（P1），① 远期** | persistentDataPath 落盘 + 文件名带 deviceModel + 提供 adb pull 命令模板；HTTP listener 是安全面与复杂度双重负担 |
| D9 | pipeline 依赖定位 | ① 必需依赖（现状） ② 可选化 | **② versionDefines 可选化**（§2.1） | "任意游戏工程接入"要求不强制携带 pipeline 包；CLI/看板/游戏内入口都不依赖它 |

> **决策状态（2026-08-31 评审定）**：**D1–D6、D9 已定稿**，按推荐列执行；**D7、D8 按推荐列作为初始方案**，实施推进中逐步确定——执行代理到达相关决策点时向工程所有者提问、答复后定稿，不得自行变更（已完成评审记录见文首）。

### 2.3 Capability 注册表（扩展点核心）

一切测试能力都实现同一接口，入口层只面向注册表编程（满足"提供各种测试的入口"）：

```csharp
// AxuTestFramework.Core
public interface IAxuTestCapability
{
    string Id { get; }                 // "perf" / "stress" / "soak" / "smoke" / "monkey" / "loadtime" / 自定义
    string DisplayName { get; }
    Type ParamType { get; }            // 该能力的参数 POCO（JSON 反序列化目标）
    IScenarioRunner CreateRunner(ScenarioContext ctx);   // Runner 工厂（由 Agent 层实现类注册）
}

public interface IScenarioRunner
{
    IEnumerator Run(ScenarioDef def, ScenarioRunHandle handle);  // 协程状态机，可被超时/取消打断
}
```

- 内置能力在 Agent 层静态注册；**游戏工程可注册自定义能力**（如"战斗 DPS 基准"）——这就是游戏侧扩展点；
- `ScenarioContext` 提供宿主服务：日志监视、截图、指标收集器工厂、等待原语、种子化随机源、Finding 收集器；
- 入口层（CLI/看板/Overlay/pipeline 命令）通过 `AxuCapabilityRegistry.List()` 枚举一切可用能力与场景 → **天然形成"测试入口大全"**。

### 2.4 数据流总览

```
              ┌──────────── Scenario/Suite 定义（JSON 权威 ↔ SO 编辑态）────────────┐
              │                                                                    │
   Editor 入口│                              Player 入口            游戏内入口      │
   ──────────┤│                            ──────────────          ──────────     │
   AxuTestRunner "Scenario"页                CLI: -axuSuite xxx.json      Overlay 热键面板
   pipeline: axu_scenario_run               -axuQuitOnDone -axuReport    （Dev Build）
        │                                        │                            │
        └────────────────┬───────────────────────┴────────────────────────────┘
                         ▼
              AxuTestAgent.RunSuite(suiteDef)          ← 按 Manifest/注册表分发
                         │ 逐 scenario 顺序执行（超时看门狗、进程级隔离建议）
                         ▼
              IAxuTestCapability → IScenarioRunner（协程）
                         │ 挂载
                         ▼
              MetricCollector 族（fps / frameTime / memory / rendering / gc / loadtime / 自定义）
                         │ 采样 → 环形缓冲 → 聚合统计（p50/p95/min/max/slope）
                         ▼
              ScenarioReport（schema axu-scenario-report/1）
                         │  ← BudgetEvaluator（阈值判定） ← BaselineDiff（基线对比）
         ┌───────────────┴────────────────┐
         ▼（Editor）                       ▼（Player）
  Library/AxuTestReports/         persistentDataPath/AxuTest/Reports/ → adb pull 回拉
         │
         ▼
  TrendStore（Library/AxuTrends/{suite}/{scenario}.{metric}.csv 追加）
         │
         ▼
  消费端：AxuTestRunner Trend 折线 ｜ axu_trend_summary ｜ 退出码 / JUnit / HTML → CI 门禁
```

---

## 3. 通用测试入口（对应需求 1）

### 3.1 接入模型：任何游戏工程 = 「装包 + 开 define + 三选一入口」

1. **零配置（推荐默认）**：开启 `AXU_TEST_AGENT` define 后，`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`
   自动挂起 `AgentBootstrap`，解析命令行参数（Editor 下解析为空即不激活）→ 打包出来的游戏带 `-axuSuite ...` 启动即测、测完退出。
   **游戏工程零场景改动、零代码接入**。
2. **场景入口**：`AgentBootstrap`（MonoBehaviour）拖进任意场景 / 做成 Prefab，适合想控制启动时序的工程。
3. **代码入口**：`AxuTestAgent.RunSuite(jsonPath)` / `AxuTestAgent.Run(scenarioJson)`，供游戏内控制台、调试面板或自定义触发器调用。

define 开关工具：包 Editor 层提供菜单 `AxuTestFramework/Enable Test Agent`（内部用 `BuildTargetGroup` 读写
scripting define symbols，不依赖 frame-kit 的 ScriptDefineSymbolSetter，保持零依赖）。

### 3.2 Scenario / Suite 数据模型

```jsonc
// 单个 scenario（capability 参数各区自定，budgets 全局统一语法）
{
  "schema": "axu-scenario/1",
  "id": "perf.combat.fhd",
  "capability": "perf",
  "title": "战斗场景 1080p 基准",
  "params": {
    "scene": "Assets/Scenes/Combat.unity",   // 由通用参数区约定：scene/timeScale/vsync/resolution/qualityLevel/seed/timeoutSec...
    "warmupSeconds": 10,
    "measureSeconds": 60,
    "collectors": ["fps", "frameTime", "memory", "rendering", "gc"],
    "sampleIntervalMs": 250
  },
  "budgets": [
    { "metric": "fps.avg",         "op": ">=", "value": 45 },
    { "metric": "frameTime.p95",   "op": "<=", "value": 25,  "unit": "ms", "severity": "warn" },
    { "metric": "memory.allocated.max", "op": "<=", "value": 2048, "unit": "MB" }
  ],
  "baseline": "baselines/combat-fhd.json"    // 可选；对比结果进 baselineDiff
}
```

```jsonc
// suite = scenario 列表 + 环境矩阵（profile 扇出：同一场景 × 多分辨率/画质档）
{
  "schema": "axu-suite/1",
  "id": "nightly-perf",
  "scenarios": ["perf.combat.fhd", "smoke.all-scenes", { "ref": "perf.combat.fhd", "profile": "low-end" }],
  "profiles": {
    "low-end": { "resolution": [1280, 720], "qualityLevel": 0 }
  },
  "settings": { "stopOnFailure": false, "warnAsFail": false, "scenarioTimeoutSec": 600 }
}
```

- 载体：`Assets/Tests/Scenarios/*.json` 为工程约定目录（Editor 扫描）；SO 包装（`AxuScenarioAsset`/`AxuSuiteAsset`）
  由 Editor 层提供双向导入导出（D3）。
- Player 路径：桌面直接 `-axuSuite 绝对路径`；真机把 suite 放 `StreamingAssets/AxuTest/`（构建时由 Editor 菜单一键拷贝）。

### 3.3 Player Agent 启动链与命令行参数

```
启动 → AgentBootstrap.Awake（#if AXU_TEST_AGENT）
     → 解析命令行 → 加载 suite → 逐 scenario 执行（DontDestroyOnLoad Runner 驱动）
     → 写报告（persistentDataPath 或 -axuReport 指定路径）→（-axuQuitOnDone）Application.Quit(退出码)
```

| 参数 | 默认 | 说明 |
|---|---|---|
| `-axuSuite <path>` | 空 | suite 或单 scenario JSON 路径；空则不激活（仅 Overlay 可用） |
| `-axuScenario <id>` | 空 | 只跑 suite 内指定 id（可多次） |
| `-axuReport <path>` | `persistentDataPath/AxuTest/Reports/{时间戳}.json` | 报告输出路径 |
| `-axuArtifacts <dir>` | 报告同目录 `artifacts/` | 截图/序列数据目录 |
| `-axuQuitOnDone` | 关 | 跑完自动退出；**退出码 = 判定**（§6.2） |
| `-axuOverlay` / `-axuNoOverlay` | Dev Build 显示 | 游戏内面板开关 |
| `-axuWarnAsFail` | 关 | warn 判定按 fail 计 |
| `-axuSeed <n>` | scenario 自带 | 覆盖随机种子（可复现） |

CI/无头补充：桌面 Player 支持 `-batchmode -nographics`——仅适用于不依赖渲染的能力（smoke 逻辑部分、monkey 逻辑流）；
**性能类能力必须带渲染窗口或真机**（风险 §9-5）。

### 3.4 游戏内 Overlay 面板（Dev Build 的"随身测试台"）

- `#if AXU_TEST_AGENT` 的即时模式 GUI（`OnGUI`，零依赖、零资产），热键（默认 `` F10 ``）呼出；
- 三个区：**能力/场景列表**（注册表枚举 + 点选运行）、**实时指标 HUD**（FPS/帧时间/内存迷你曲线，采样器常驻），
  **最近一次结果摘要**（Finding 列表 + 报告路径）；
- 不引入 UITK 资产依赖，保证任意工程挂上就能用；正式包（无 define）整段不编译。

### 3.5 Editor / pipeline 入口（新增命令，词汇族 `axu_scenario_*` / `axu_player_*` / `axu_trend_*`）

| 命令 | 形态 | 说明 |
|---|---|---|
| `axu_scenario_list` | Editor | 枚举注册表能力 + 工程内 scenario/suite 文件（含参数 schema 摘要，供 AI/人选用） |
| `axu_scenario_run` | Editor | PlayMode 内驱动 Agent Runner 跑指定 suite/scenario；异步 + `axu_scenario_status` 轮询，**复用 `axu_test_run` 的状态文件协议**（Temp 状态 JSON，域重载安全） |
| `axu_scenario_status` | Editor | 读运行状态文件（running/completed/error + reportPath） |
| `axu_player_suite_build` | Editor | 构建 Development Build（自动临时加 `AXU_TEST_AGENT` define，构完还原）；场景清单 = Build Settings 或 suite 引用的场景 |
| `axu_player_suite_run` | Editor | 启动 Player 进程（带 §3.3 参数）→ 等待退出码 → 回拉报告 → 追加趋势 CSV；桌面平台闭环 |
| `axu_trend_summary` | Editor | 读趋势 CSV，输出最近 N 次各指标序列与回归百分比 |

并发纪律沿用现状：与 `axu_test_run` 共享静态互斥闸，禁止并发驱动同一 Editor。

### 3.6 可移植性纪律（"任意工程"的硬约束）

- 包**零依赖**任何 `AxuFrameKit.*` 与本工程资产（现状保持，写进审查清单）；
- `com.unity.pipeline` 从 `dependencies` 降级为**可选**（D9）：Editor asmdef 加 versionDefines
  `{ "name": "com.unity.pipeline", "expression": "0.5.0", "define": "AXU_PIPELINE_PRESENT" }`，
  现有 pipeline 命令文件迁入 `Editor.Pipeline` 程序集或以 `#if AXU_PIPELINE_PRESENT` 包裹；
- 分发形态：UPM git URL / 本地 tarball / `*.unitypackage` 三种；README 增加"接入一页纸"（装包 → 菜单开 define → 写一个 JSON → 命令行跑）。

---

## 4. 测试能力库（对应需求 2）

### 4.1 能力总表（优先级 = 实施顺序依据）

| 能力 Id | 名称 | 解决什么游戏问题 | 优先级 |
|---|---|---|---|
| `perf` | 性能基准 | 场景/玩法帧率与帧时间达标、渲染与内存预算 | **P0** |
| `loadtime` | 加载时长 | 场景/资产加载矩阵，首帧时长，卡加载感知 | **P0** |
| `smoke` | 冒烟矩阵 | 全场景可进、无报错、可截图留证（每次出包跑一遍） | **P0** |
| `soak` | 浸泡/泄漏 | 数小时长跑内存斜率、FPS 衰减、错误累积 | **P1** |
| `stress` | 压力/承载 | 最大承载实体数（容量探测）、对象churn、事件洪峰 | **P1** |
| `monkey` | 猴子测试 | 随机输入长跑炸稳定性、卡死检测、出错留证 | **P1** |
| `gamehook` | 游戏适配器 | 游戏自定义能力的注册规范与宿主服务 | **P1**（基础设施） |
| `uiwalk` | UI 走查 | 声明式点击路径 + 断言 + 截图 | P2（远期） |
| `saveroundtrip` | 存档回环 | 依赖游戏 Hook，验证存/读一致性 | P2（远期） |
| `visualdiff` | 图像回归 | 截图 vs golden image | P3（远期候选） |

### 4.2 perf —— 性能基准

- **流程**：加载目标场景 → 应用 profile（分辨率/画质/vsync/targetFrameRate/timeScale/seed）→ warmup（丢弃采样）
  → 度量期采样 → 聚合 → 预算判定 →（可选）结束时截图；
- **采集器**（可按 `params.collectors` 裁剪，全部经 `MetricCollector` 抽象）：

| 采集器 | 数据源（Unity 6 API） | 产出指标 |
|---|---|---|
| `fps` | 自计帧 + `Time.unscaledDeltaTime` | fps.avg / p50 / p95 / min |
| `frameTime` | `FrameTimingManager`（cpuFrameTime/gpuFrameTime，Dev Build 可用） | cpuFrameTime.p95、gpuFrameTime.p95（ms） |
| `memory` | `Profiler.GetTotalAllocatedMemoryLong/GetTotalReservedMemoryLong/GetMonoUsedSizeLong` + `GC.GetTotalMemory` | allocated.max（MB）、monoUsed.max、reserved.max |
| `rendering` | `ProfilerRecorder`（ProfilerCategory.Render：Draw Calls / Triangles / SetPass Calls） | drawCalls.avg、setPass.avg、tris.avg |
| `gc` | `ProfilerRecorder`（Scripts: GC Allocated In Frame）+ GC.Collect 计数 | gcAllocPerFrame.avg（B）、gcCount |

- 采样纪律：采样协程按 `sampleIntervalMs`（默认 250ms）聚合入**固定容量环形缓冲**（Core 自实现，防长跑涨内存）；
  采集器在不可用环境（如 Release 构建调 ProfilerRecorder）自检并产出明确 Finding，**不静默给 0 值**。

### 4.3 loadtime —— 加载时长

- 场景矩阵模式：`scenes[]` 逐一 `LoadSceneAsync`，记录 **loadDurationMs / 首帧时长 / 稳定帧时长**（定义：连续 N 帧 ≥ 目标 fps）；
- 资产模式（可选）：首启预热 `Resources.Load`/AssetBundle/Addressables（检测到包存在才启用对应采集，versionDefines 思路同 D9）；
- 产出：每场景一行矩阵 + budgets（如 `loadtime.scene.total <= 5000ms`）。

### 4.4 stress —— 压力/承载

| 工具 | 说明 |
|---|---|
| `StressSpawner` | 批量实例化 prefab（阶梯/曲线 ramp），每档稳定后采样 → 承载曲线 |
| **容量探测**（亮点） | 自反馈加压：spawner 逐步加量直到 fps.avg < 目标阈值持续 T 秒，再二分收敛 → 输出**目标帧率下最大实体数**（版本间对比直接量化"优化了多少"） |
| `ChurnRunner` | 每秒 Create/Destroy N 个对象持续 M 秒：压对象池与 GC，配合 `memory`/`gc` 采集器看回收健康度 |
| `EventFlood` | 向事件中心每帧发布 N 事件 × M 秒（吞吐/耗时曲线；对框架自身 EventCenter 也是基准测试） |

- stress 是**组合能力**：params 里声明 `phases: [{tool:"spawner", ...}, {tool:"churn", ...}]`，Runner 顺序执行；
- 所有 stress 产物 prefab 由**游戏工程路径提供**（框架不内置任何游戏资产）。

### 4.5 soak —— 浸泡/泄漏

- 参数：`durationMinutes`（默认 60，支持 `-axuSuite` 长跑整夜）、`sampleIntervalSec`（默认 30）、`gcCollectBeforeSample: true`；
- **泄漏判定**：对 `memory.allocated` 序列做滑动窗口**线性回归斜率**（Core 统计模块），斜率 > 阈值（如 5 MB/min）→ Finding "泄漏嫌疑"，
  附回归曲线数据文件；
- FPS 衰减检测（后半程 p50 显著低于前半程）、错误计数曲线、每 N 分钟自动截图留证；
- 中间报告定期 flush（每 5 min 写 partial report），崩溃也不丢前段数据（§9-9）。

### 4.6 smoke —— 冒烟矩阵

- 场景清单来源（三选一）：Build Settings 全部场景 / 白名单列表 / 指定目录扫描（Editor 侧展开为清单）；
- 每场景：加载 → 稳定等待（N 秒无 Error + K 帧）→ **截图**（复用 `AgentCapture`）→ 记录 loadtime + 错误清单 → 卸载；
- 产出：场景 × {进得去?、耗时、错误数、截图} 矩阵；出包回归首选，也是 CI 门禁最便宜的一道闸。

### 4.7 monkey —— 猴子测试

- 输入合成：Input System 事件队列注入（键盘/手柄 `StateEvent`、触屏 `TouchStateEvent`）；UI 层可用
  `EventSystem` + `ExecuteEvents.Execute<IPointerClickHandler>` 精确点按（两条路径都封装进 `AgentInput`）；
- 参数：`durationSec`、`actionsPerSecond`、`actionSpace`（键位/区域白名单）、`seed`（**可复现**）；
- 安全网：错误即截图 + 可配置即停；**卡死检测**（M 秒无帧推进/无场景变化 → 采集堆栈快照 + Finding + 终止）；
- 产出：操作序列流水（JSONL，可回放分析）、错误/截图清单。

### 4.8 gamehook —— 游戏适配器（游戏侧扩展规范）

```csharp
// 游戏工程内：注册自定义能力/场景（IL2CPP 安全的显式注册路径，D7 兜底）
public static class MyGameTestRegistration
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        AxuTestAgent.RegisterCapability(new BossFightCapability());          // 自定义能力
        AxuTestAgent.RegisterScenario(ScenarioDef.FromCode(                  // 代码态场景
            id: "game.boss-dps", capability: "perf",
            run: RunBossFightSetup));                                        // IEnumerator 准备协程
    }
}
```

- 框架不依赖游戏、游戏按需注册——依赖方向永远向外；
- `[AxuScenario("id")]` Attribute 只是 **Editor 侧糖**（TypeCache 扫描烘焙 Manifest，D7），运行时一律走显式注册或 Manifest。

---

## 5. 度量与判定

### 5.1 Metrics API（Agent 层静态门面 + Core 统计）

```csharp
// 游戏代码/自定义能力内打点（#if AXU_TEST_AGENT 不强制——API 空实现壳保证 Release 可编译）
using (AxuMetrics.Scope("AI.Tick"))                       // 计时 scope
    ai.Tick();

AxuMetrics.Counter("Projectile.Spawned", 1);              // 累计
AxuMetrics.Gauge("Room.EnemyCount", enemies.Count);       // 瞬时值（采样期聚合 avg/max）
```

- 数据模型：`MetricSample { key, value, timestampMs }` → 环形缓冲 → `MetricStats`（avg/p50/p95/min/max/slope，纯 C# 单测覆盖）；
- 指标键命名空间约定：`<collector>.<metric>.<agg>`（如 `fps.avg`、`frameTime.p95`、`memory.allocated.max`）。

### 5.2 预算（Budget）判定

- `BudgetRule { metric, op(>=/<=/==/range), value, unit, severity(pass/warn/fail) }`；
- `BudgetEvaluator.Evaluate(stats[], rules[]) → BudgetVerdict[]`（纯 C#，Core 层单测覆盖）；
- scenario 级总判定 = 最严 budget 结果（fail > warn > pass）；**缺 budget 的 scenario 判 pass 但标记 `unbudgeted: true`**，
  防止"没人写阈值所以永远绿"。

### 5.3 基线与趋势

- **Baseline**：`-axuBaselineSave` 把本次指标存为基线 JSON（建议入库 `Assets/Tests/Baselines/`，随版本演进人工更新）；
  之后每次运行自动 diff，输出 `delta%` 与 verdict（阈值可配，如劣化 >10% → warn）；
- **Trend**：每次运行向 `Library/AxuTrends/{suiteId}/{scenarioId}.{metric}.csv` 追加一行（时间戳 + 值）；
  Editor 看板画折线（UITK Painter2D），`axu_trend_summary` 输出文本摘要——回答"这周优化是变好还是变坏"。

---

## 6. 报告与门禁

### 6.1 报告 schema `axu-scenario-report/1`（独立于现有 `axu-test-report/1`，零扰动）

```jsonc
{
  "schema": "axu-scenario-report/1",
  "suiteId": "nightly-perf",
  "startedAt": "2026-08-31T12:00:00Z", "endedAt": "...",
  "environment": {                       // Player 侧尤其重要：真机矩阵的坐标系
    "unityVersion": "...", "agentVersion": "0.2.0",
    "isEditor": false, "developmentBuild": true,
    "deviceModel": "iPhone 14 Pro", "os": "iOS 18.5",
    "graphicsDevice": "Apple A16", "graphicsApi": "Metal",
    "systemMemoryMB": 6144, "qualityLevel": 2, "resolution": [2556, 1179],
    "buildGuid": "..."
  },
  "summary": { "total": 3, "passed": 2, "warned": 1, "failed": 0 },
  "exitCode": 0,
  "results": [
    {
      "scenarioId": "perf.combat.fhd", "capability": "perf", "status": "warn",
      "durationMs": 71234, "unbudgeted": false,
      "metrics": {
        "fps": { "avg": 46.2, "p50": 47.9, "p95": 41.0, "min": 33.5 },
        "frameTime": { "cpuP95": 24.8, "gpuP95": 26.1 },
        "memory": { "allocatedMax": 1832.4, "monoUsedMax": 210.7 }
      },
      "budgets": [
        { "metric": "fps.avg", "op": ">=", "threshold": 45, "actual": 46.2, "verdict": "pass" },
        { "metric": "frameTime.p95", "op": "<=", "threshold": 25, "actual": 26.1, "unit": "ms", "verdict": "warn" }
      ],
      "baselineDiff": { "ref": "baselines/combat-fhd.json",
        "entries": [ { "metric": "fps.avg", "base": 48.1, "delta": "-3.9%", "verdict": "pass" } ] },
      "series": { "dir": "series/perf.combat.fhd/", "files": ["fps.csv", "memory.csv"] },
      "artifacts": [ "artifacts/perf.combat.fhd_end.png" ],
      "log": { "errorCount": 0, "warningCount": 3, "samples": [] },
      "findings": [ { "severity": "warn", "code": "BUDGET_WARN",
                      "message": "frameTime.p95=26.1ms > 25ms" } ]
    }
  ]
}
```

### 6.2 退出码与 CI 集成

| 退出码 | 语义 |
|---|---|
| 0 | 全部 pass |
| 1 | 存在 fail |
| 2 | 仅 warn（`-axuWarnAsFail` 时归入 1） |
| 3 | 基础设施错误（suite 解析失败/场景缺失/报告写盘失败） |

- 导出器（Editor 层）：`axu-scenario-junit.xslt` 思路改为直接 C# 导出 **JUnit XML**（scenario→testsuite、budget→testcase）；
  **HTML 摘要**（自包含单文件，内嵌迷你图）供邮件/群发；
- 典型 CI 流：构建（dev build + define）→ `-batchmode` 不可用时正常启动 → 等待进程退出 → 读退出码 → JUnit 上报 → 趋势入库。

### 6.3 AxuTestRunner 看板扩展

- 新页签 **"Scenario"**：能力/场景树（注册表驱动，含未写 budget 的灰色标记）→ 点选运行（Editor 内走 `axu_scenario_run` 同一服务）→ 结果树（budgets 逐条展示 pass/warn/fail）；
- 新页签 **"Trend"**：suite/scenario/指标三级选择 + 折线（Painter2D）+ 最近基线对比卡；
- 现有 "Tests" 页签（UTF 结果）不动，三页签并列。

---

## 7. 实施路线（Phase 0–6，每期有 DoD）

> 工作量为 AI agent 协作下的净工时预估，供排期参考；每期独立可交付、可停。
> **推进纪律（逐步推进 / 开工问答制 / 零影响红线 / git 同步 / 跨平台双要求）见文首「实施工作约定」，对所有 Phase 生效。**

### 进度台账（每 Phase 收尾更新；开工问答原文记入对应条目"问答记录"）

| Phase | 状态 | 完成日期 | 收尾提交 | 问答记录 |
|---|---|---|---|---|
| 0 架构基座 | **已完成（2026-08-31）** | 2026-08-31 | `axu-test-v2: Phase 0 架构基座`（本条目所在提交） | **开工问答（2026-08-31，一次性作答）**：① pipeline 解除方式 = v1 两个命令文件（axu_test_*、axu_window_screenshot，连 .meta）迁入 `Editor.Pipeline/`，Editor 主程序集零 pipeline 引用；② `AXU_TEST_AGENT` = 开发期 Standalone 常开，ProjectSettings 随本 Phase 收尾提交定版；③ 包版本 = Phase 0 即升 0.2.0；④ schema 草案 = 新建包根 `DOC/`。**实施记录**：新增 Core/Agent/Editor.Pipeline 三 asmdef + `SchemaNames.cs`/`AgentInfo.cs`（空程序集不产出 dll，以 Phase 1 反正需要的最小文件填充）；实施勘误：versionDefines 注入点为 Editor.Pipeline 自身（程序集级作用域，不能跨程序集供给）；expression `0.5.0-exp` 实证匹配 0.5.0-exp.1。**DoD 验证**：编译零 error；`Library/ScriptAssemblies/` 5 dll 齐全；`axu_test_run/status/report` 在册且归属 `AxuTestFramework.Editor.Pipeline`；回归 包测试 6/6 + Axu/Smoke 7/7 全绿；AXU_TEST_AGENT 开/关两形态在 Phase 0 因尚无 `#if` 守护代码而编译等价（按构造成立），真实验证移至 Phase 1 DoD。**零影响红线**：①②③④ 全过（Runtime asmdef 未动；Editor asmdef 仅按方案调整 references） |
| 1 Agent 骨架 | **已完成（2026-08-31/09-01 跨夜）** | 2026-09-01 | `axu-test-v2: Phase 1 Agent 骨架`（本条目所在提交） | **开工问答（2026-08-31，一次性作答）**：① backlog #1 Player 构建阻断 → 授权前置修复（已修：`OllamaScriptFillerWindow.cs` 包 `#if UNITY_EDITOR`，全树审计无同类；Player 构建 45s 首次通过即最终验证）；② echo 内建常驻；③ Editor 报告落 `Library/AxuTestReports/scenario/`。**实施记录**：Core 26 文件（schema POCO/SuiteLoader 扇出/CLI/报告模型）+ Agent 14 文件（Bootstrap/Runner 状态机/看门狗/Echo/报告写盘/退出码）+ Editor 4 文件（define 菜单/JSON 模板菜单/Run Suite 菜单/宿主清道夫）；Demo 套件 `Assets/Tests/Scenarios/echo-demo.suite.json`（内联场景 + `@fast` profile 扇出）。**集成修复 4 项**：① `NamedBuildTarget` 三轮（`UnityEditor.Build` 命名空间 → `.TargetName` 属性）；② RunSingle 裸 `yield return IEnumerator` 改 `StartCoroutine`；③ **环境异常实证：本机 Unity 6000.0.23f1 Editor 的 `WaitForSecondsRealtime` 等待契约失效（m_WaitUntilTime=-1/逐帧放行）** → `AgentWait.RealtimeSeconds` 改为 `Time.realtimeSinceStartup` 逐帧轮询（不再依赖 WFSRT，看门狗粒度升至 ≤1 帧）——⚠️ 工程内其他直接用 WFSRT 的代码同环境可能同样被吞；④ StartFromSources 守卫去掉 `_sourcesChecked`（防跨 Play 泄漏宿主吞启动）+ `AgentHostCleanup` 清道夫。**DoD 验证（macOS 实测，Windows 未实测）**：Editor PlayMode 两轮端到端全绿（2686/213ms、2170/201ms，第二次进 Play 激活正常）；macOS Player 构建 45s 首次通过 → CLI 运行 `-axuSuite -axuQuitOnDone -axuReport` 退出码 0、报告 2/2 pass（2289/201ms，isEditor=false）；`AXU_TEST_AGENT` 开/关两形态实测（dll 29184↔17920 B、守护串 1↔0、define 菜单 dogfood 通过，且发现 sed 直改 ProjectSettings 不会被编译管线感知——切换 define 必须走 API/菜单）；零影响回归 包测试 6/6 + Axu/Smoke 7/7、`axu_test_status` 探活正常、Console 零 error。**实现偏离（已接受）**：Unity 6 移除 `Application.exitCode` 属性 → 退出码经 `Application.Quit(int)` 携带；qualityLevel/resolution 按数值；finding severity 输出 "info/warn/error"；`RunSuite` 返回 bool。**遗留（记 Phase 4）**：Editor 层"桥文件存在但 define 关闭"的显式预检警告。 |
| 2 度量层 | **已完成（2026-09-01）** | 2026-09-01 | `axu-test-v2: Phase 2 度量层`（本条目所在提交） | **开工决策（2026-09-01，工程所有者预授权免问答——排队消息无法即时应答，主代理按推荐执行）**：① budget 键 = `<collector>.<stat>` 两段式；② DoD 载体 = `echo-demo.suite.json` 增补 budgets+baseline；③ `-axuBaselineSave` 本期落地（含 Editor 桥文件 `baselineSavePath` 通道）；④ 基线 schema `axu-baseline/1` 入 DOC §5；⑤ 采集器不可用 → Finding 不静默 0 值；⑥ 单测扩进 `AxuTestFramework.Tests.Editor`。**实施记录**：Core 10 文件（RingBuffer/MetricStats 百分位+最小二乘/BudgetEvaluator 两段键解析（末段未命中回退首段，兼容 custom 长键）/BaselineFile/Diff/Flatten/MetricDirection 方向元数据）+ Agent 11 文件（AxuMetrics 零分配 no-op 门面/MetricSession/5 采集器+注册表，宿主驱动采样）；契约测试先行（tester 与 runtime-dev 并行，实现按契约对齐）。**集成修复 5 项**：① ProfilerRecorder.Read()→LastValue、FrameTimingManager 改 GetLatestTimings(1,buf)（离线桩 API 失真 6 处）；② COLLECTOR_UNAVAILABLE/UNKNOWN 降 Warning（环境限制不影响判定，Memory._broken 留 Error）；③ FinalizeMetrics 收尾补采（短 scenario 也有 ≥1 样本，否则预算误 fail）；④ ResolveBaselineDirectory 非 .json 一律视为目录并自动创建；⑤ **扇出实例不再继承源场景 baseline**（@fast 曾拿 echo.demo 基线误判 fail）；⑥ **BaselineDiff 方向感知**（原定 Phase 3，提前：改善 4 倍曾被判"劣化"error——fps=HigherIsBetter、frameTime/memory/custom.scope=LowerIsBetter、未知 Symmetric）。**DoD 验证（macOS 实测，Windows 未实测）**：单测 57/57（含方向感知用例）；e2e 两轮全绿——首跑 metrics 5 键（rendering 本环境不可用属预期，warn finding）+ budgets 3 pass + baselines/ 2 文件，二跑 baselineDiff 17 项方向感知全 pass、@fast 无基线静默跳过；Axu/Smoke 7/7；Console 零 error。**已知环境项**：本 Editor 下 rendering 采集器 ProfilerRecorder 计数器无效（需 Dev Build/Profiler），已有 warn 级自检 finding。 |
| 3 首批能力 | 未开始 | — | — | — |
| 4 工具链 | 未开始 | — | — | — |
| 5 真机与进阶 | 未开始（按需） | — | — | — |
| 6 文档与收口 | 未开始 | — | — | — |

### Phase 0 —— 架构基座（0.5–1d）
- 落 4+1 个 asmdef（§2.1）；定稿 define 策略（D2）与 versionDefines 可选化（D9）；schema `axu-scenario/1`、`axu-suite/1`、`axu-scenario-report/1` 草案入包 DOC；
- **DoD**：新 asmdef 在 define 开/关两种形态下编译通过；现有 `axu_test_*` + 看板回归（跑一轮 Axu/Smoke）零变化；pipeline 可选化后本工程行为不回退。

### Phase 1 —— Agent 骨架（2–3d）
- Core：Scenario/Suite/Manifest/Budget/Finding POCO + JSON 读写 + 命令行解析器；
- Agent：`AgentBootstrap`（RuntimeInitializeOnLoad + 场景组件双形态）、`AxuTestAgent` 门面、Runner 状态机（顺序执行/超时看门狗/取消）、报告写盘（persistentDataPath / 指定路径）、退出码；
- Editor：`AxuTestFramework/Enable Test Agent` 菜单 + suite JSON 模板创建菜单；
- **DoD**：一个 demo capability（`echo`，仅延时+日志）的 suite 在 **Editor PlayMode 与 macOS Player（带 `-batchmode -axuQuitOnDone`）端到端跑通**，报告与退出码正确。

### Phase 2 —— 度量层（2–3d）
- Metrics API（Scope/Counter/Gauge）+ 采集器 5 件套（fps/frameTime/memory/rendering/gc，§4.2）+ 环形缓冲 + `MetricStats` 统计 + `BudgetEvaluator` + Baseline 存取/diff；
- **DoD**：对既有示例场景产出完整 metrics + budget 判定；二次运行 baseline diff 生效；统计与预算逻辑有 EditMode 单测（`AxuTestFramework.Tests.Editor` 扩充）。

### Phase 3 —— 首批能力（3–5d，可按 P0→P1 分两小批）
- P0：`perf` 完整 Runner、`loadtime` 场景矩阵、`smoke` 冒烟矩阵；
- P1：`soak`（斜率泄漏判定）、`stress`（Spawner/Churn/EventFlood + 容量探测）、`monkey` v1（输入注入 + 卡死检测）、`gamehook` 注册规范；
- **DoD**：每能力 ≥1 个示例 scenario 进 `Samples~/Scenarios/`；在本工程真实示例场景（`Assets/TestAndSample/`）跑通 perf/smoke/stress 并出报告；budget/无 budget/超时三条分支路径均验证。

### Phase 4 —— 工具链（2–3d）
- pipeline 新命令 6 个（§3.5，放 `Editor.Pipeline` 程序集）；趋势 CSV + 看板 Scenario/Trend 页签；JUnit/HTML 导出器；真机 suite 一键进 StreamingAssets 菜单；
- **DoD**：一条 `axu_scenario_run` 完成"跑 suite → 收报告 → 趋势入库 → 看板可见"；`axu_player_suite_build`+`run` 在 macOS 端到端闭环。

### Phase 5 —— 真机与进阶（按需，P1 内 3–5d）
- 设备 profile 矩阵扇出、`environment` 采集完善、adb pull 命令模板、Overlay 打磨（实时 HUD + 手动触发）；
- 远期池：HTTP 远程触发、`uiwalk`、`saveroundtrip`、`visualdiff`；
- **DoD**：一台 Android/iOS 真机跑 perf suite 并回拉报告，environment 字段完整。

### Phase 6 —— 文档与收口（1d）
- 包 README 重写（新增 Scenario 章 + "接入一页纸"）、CHANGELOG（0.2.0）、AGENTS.md 测试节同步、`.zcode` 知识库补条目、Samples 导入验证、`unity-reviewer` 交付审查；
- **DoD**：审查 P0/P1 清零；任意"干净工程"按接入一页纸 15 分钟内跑通首个 scenario（人工验收）。

---

## 8. 风险与对策

| # | 风险 | 对策 |
|---|---|---|
| 1 | Player 内无域重载，静态/单例残留污染后续 scenario | 每个 scenario 前强制 `ResetStatics` 等价物（Agent 层登记表，D5）；文档推荐 `-axuQuitOnDone` 一进程一 suite；跨 scenario 一律重载目标场景 |
| 2 | IL2CPP stripping 砍掉反射路径 | 显式注册 API 为主路径；Editor 烘焙 Manifest（D7）；包内附 `link.xml` 模板 |
| 3 | Release 构建下 Profiler API 缺失/失真 | 文档强制性能类跑 Development Build；采集器启动自检，不可用时产出明确 Finding（含原因），绝不静默 0 值 |
| 4 | 采集自身开销影响测量 | 采样间隔默认 250ms 聚合；环形缓冲固定容量；collectors 可裁剪；README 注明测量开销数量级 |
| 5 | `-batchmode -nographics` 无 GPU 数据 | 能力分级声明 `requiresGraphics`，无头模式遇到性能类 scenario 直接 Finding + 跳过，不误报 |
| 6 | 真机报告回取摩擦 | persistentDataPath + 文件名带 deviceModel；提供 adb pull 模板；StreamingAssets 一键部署菜单 |
| 7 | pipeline 包升级丢 macOS 补丁（既有已知问题） | 沿用 `.zcode/08` 第 8 节警示；可选化改造后至少"未装 pipeline"的工程完全不受影响 |
| 8 | 两套链路（UTF vs scenario）认知混乱 | 词汇隔离：`axu_test_*` 永远指 UTF；`axu_scenario_*`/`axu_player_*` 指 scenario；README 开篇画边界图 |
| 9 | 长时 soak 崩溃丢数据 | partial report 定期 flush（5 min）；崩溃后下次启动检测残留报告并标注 `truncated: true` |
| 10 | 现有使用方回归 | 现有 2 个 asmdef 行为零改动（仅 Editor asmdef 加 references + versionDefines）；每期 DoD 都含既有链路回归项 |

---

## 9. 命名与文件组织（实施时遵守）

```
com.cielevel.axu-test-framework/
├── Core/                                  # asmdef: AxuTestFramework.Core（noEngineReferences:true）
│   ├── Scenarios/   ScenarioDef.cs / SuiteDef.cs / ScenarioManifest.cs / ScenarioCli.cs
│   ├── Metrics/     MetricSample.cs / MetricStats.cs / RingBuffer.cs
│   ├── Budget/      BudgetRule.cs / BudgetEvaluator.cs / BudgetVerdict.cs
│   ├── Baseline/    BaselineFile.cs / BaselineDiff.cs
│   ├── Trend/       TrendPoint.cs
│   ├── Report/      ScenarioReport.cs / ReportEnvironment.cs / Finding.cs
│   └── Json/        AxuJson.cs（Newtonsoft 薄封装 + schema 校验）
├── Agent/                                 # asmdef: AxuTestFramework.Agent（#if AXU_TEST_AGENT 守护入口）
│   ├── Bootstrap/   AgentBootstrap.cs / AxuTestAgent.cs / AxuCapabilityRegistry.cs
│   ├── Runner/      ScenarioRunnerHost.cs / ScenarioRunHandle.cs / Watchdog.cs
│   ├── Capabilities/
│   │   ├── Perf/    PerfCapability.cs / PerfRunner.cs / PerfParams.cs
│   │   ├── LoadTime/Smoke/Soak/Stress/Monkey/…（各：Capability/Runner/Params 三件套）
│   ├── Collectors/  MetricCollector.cs / FpsCollector.cs / FrameTimeCollector.cs / …
│   ├── Tools/       AgentCapture.cs / AgentLogWatch.cs / AgentInput.cs / StressSpawner.cs / …
│   └── Overlay/     AgentOverlay.cs（即时模式 GUI）
├── Editor/                                # 现有 asmdef 扩展
│   ├── Scenarios/   AxuScenarioAsset.cs / AxuSuiteAsset.cs / ScenarioJsonMenu.cs / AgentDefineMenu.cs
│   ├── Export/      JUnitExporter.cs / HtmlSummaryExporter.cs
│   └── Window/      AxuTestRunnerWindow.cs（+Scenario/Trend 页签）
├── Editor.Pipeline/                       # 新 asmdef（AXU_PIPELINE_PRESENT 约束）
│   └── Commands/    AxuScenarioPipelineCommands.cs / AxuPlayerSuiteCommands.cs / AxuTrendCommand.cs
├── Tests/{Editor,Runtime}/                # 框架自测扩充（统计/预算/解析/注册表）
└── Samples~/Scenarios/                    # 各能力示例 suite/scenario JSON + 接入一页纸
```

- 命名空间：`AxuTestFramework.Core.{模块}`、`AxuTestFramework.Agent.{模块}`、`AxuTestFramework.Editor.{模块}`（与现有 `AxuTestFramework`、`AxuTestFramework.Editor` 并存，注意 using 别混）；
- 测试类别：新逻辑单测归 `[Category("Axu/TestFramework")]`；类名 `方法名_条件_期望结果` 纪律不变；
- **红线**：Core 不 using UnityEngine（审查项）；包不引用任何 `AxuFrameKit.*`（审查项）；Overlay/Bootstrap 全部 `#if AXU_TEST_AGENT`。

---

## 10. 整体验收清单（最终交付时逐项核对）

- [ ] 干净工程（无 pipeline 包）装本包 → 菜单开 define → 命令行 `-axuSuite` 跑通 smoke 示例，退出码正确；
- [ ] Editor 内经看板 Scenario 页与 pipeline `axu_scenario_run` 两条路跑同一 suite，报告一致；
- [ ] perf scenario 产出 §6.1 全部指标，budget fail 会置退出码 1，warn 单独 2；
- [ ] baseline diff 与趋势 CSV 在连续 3 次运行后可看折线与回归百分比；
- [ ] stress 容量探测输出"目标帧率下最大实体数"，两次运行可对比；
- [ ] soak 30 分钟长跑产出斜率判定 + partial report 机制验证（中途 kill 不丢前段）；
- [ ] monkey 以同 seed 复现同一操作序列；
- [ ] define 关闭时包对正式构建零影响（编译产物不含 Agent 代码，IL 检查）；
- [ ] 现有 `axu_test_*` / 看板 Tests 页 / 各包测试接入全部无回归（Axu/Smoke 全绿）；
- [ ] README"接入一页纸"照做 15 分钟内可跑通（人工）。
