# AxuTestFramework — 基于 Pipeline 的通用测试框架与工具方案（已批准稿，补入 M0 前勘发现）

## 零、M0 前勘重要发现（更新）

- 实测 `ProjectSettings/EditorSettings.asset`：`m_EnterPlayModeOptionsEnabled: 1` + `m_EnterPlayModeOptions: 0`。Unity 语义下 Options=0（None）= **进 Play 时域重载+场景重载照常发生**；AGENTS.md"不重新加载域/场景"是把 Enabled 标志误当选项值（git 历史显示自 Initial commit 即为 0，非近期误改）。
- 因此 M0 改为 **A/B 两轮对照**：A 轮现状（域重载开启，pipeline collector 重挂机制的设计路径）；B 轮临时切 `Options` 0→3（域重载关闭，原命题场景），跑完**复原为 0 并 git diff 核验**。两种形态都实测，框架才能两种设置都支持。AGENTS.md 勘误记入 spike 报告，M3 阶段统一修正文档。

## 一、目标

为主框架建立**通用型**测试框架与工具集：覆盖 EditMode 与 PlayMode，深度利用 Unity Pipeline 本地 HTTP API（`Packages/com.unity.pipeline` 0.5.0-exp.1）形成"跑测试 → 收结构化结果 → 看板可视化"闭环，人和 AI 代理都能驱动；8 个模块包均可低成本接入。

## 二、已核实的承重事实

1. pipeline 支持工程侧免改包注册自定义命令：`TypeCacheCommandDiscovery.cs` 全程序集扫描 `[CliCommand]`——测试工具以此挂入 pipeline，pipeline 包源码零改动。
2. `run_tests` 原生支持 PlayMode：`mode=editor/playmode/all`、`filter`/`filter_type`、`async_tests`；PlayMode 必须走 async，async 下有 collector 重挂机制 + `Temp/pipeline_test_status.json` 结果落盘。
3. UTF 1.4.6 提供 `[TestRunCallback]`（跨域重载回调通道），作为报告落盘支点。
4. 配套能力齐全：`editor_play/stop`、`set_autotick`（失焦保障）、`console`/`get_console_logs`、`capture_game_view`、`eval` 逃生舱。
5. 现有测试极少：`Assets/Tests/Editor/`（6 个 EditMode 冒烟测试）；框架包与 8 个模块包均无 Tests。
6. KitBox 页面在 `SetupGroups()` 硬编码；菜单 `AxuFrameKit/AxuEditor工具集V2-Rev`；主题 `AxuKitTokens.uss` + `AxuKitTheme.cs`。

## 三、总体架构

```
      Agent（unity-tester / unity-pipeline）              人（AxuTestRunner 窗口，UITK）
                │ POST /api/exec                                ▲
                ▼                                               │
  ┌──────── Unity.Pipeline HTTP Server (127.0.0.1) ─────────┐   │
  │ 内置: run_tests / test_status / editor_play/stop /      │   │
  │       console / capture_game_view / set_autotick        │   │
  │ TypeCache 自动发现（零改包源码）▼                        │   │
  │ 自定义: axu_test_run / axu_test_report                  │   │
  │         ← AxuTestFramework.Editor 的 [CliCommand]       │   │
  └──────────────────────┬─────────────────────────────────┘   │
                         ▼ UTF TestRunnerApi                    │
              [TestRunCallback] → AxuTestReportWriter           │
                         │ JSON + 截图落盘 ──────────────────────┘
   框架包 Tests/ · 模块包 Tests/ · indie 包 Tests/  （UNITY_INCLUDE_TESTS 守护）
                         │ 引用
                         ▼
   AxuTestFramework（Runtime 工具层：等待/场景/截图/日志监视/静态重置/报告模型）
```

依赖红线：`AxuTestFramework`（Runtime）不依赖任何框架程序集；`AxuTestFramework.Editor` 依赖 `Unity.Pipeline` + `AxuTestFramework`，**不依赖** `AxuFrameKit.Editor`；各包 `Tests.*` 依赖"被测程序集 + AxuTestFramework"。无循环。

## 四、落点与规划（用户已拍板）

| 决策点 | 结论 |
|---|---|
| 包命名 | 新建独立嵌入式包 `Packages/com.cielevel.axu-test-framework/`，命名空间 `AxuTestFramework.*` |
| 旧测试 | `Assets/Tests/Editor/` **保留不动**，作为工程级冒烟入口；新测试进各包 `Tests/` |
| 工具 UI | 独立 UITK 窗口，布局/样式语言仿 KitBox 但完全独立（包内自持精简 token USS）；入口菜单与 KitBox 同级：`AxuFrameKit/AxuTestRunner`（仅菜单字符串同前缀，无代码依赖） |
| 报告落盘 | 默认 `Library/AxuTestReports/{时间戳}-{mode}.json` + `artifacts/*.png`；`out_path` 参数可改写留档 |

### 新包结构
```
com.cielevel.axu-test-framework/          # package.json 0.1.0, deps: test-framework 1.4.6 + newtonsoft-json
├── README.md / CHANGELOG.md
├── Runtime/                              # asmdef: AxuTestFramework（全平台）
│   ├── Wait/AxuWait.cs                   #   帧/条件/秒等待
│   ├── Scene/AxuTestScene.cs             #   场景加载/卸载助手
│   ├── Capture/AxuCapture.cs             #   PlayMode 内截图到 artifacts/
│   ├── Logging/AxuLogWatch.cs            #   日志监视 + ExpectNoErrors scope
│   ├── State/AxuTestState.cs             #   单例/静态重置登记表
│   └── Reports/                          #   报告 POCO（schema: axu-test-report/1）
├── Editor/                               # asmdef: AxuTestFramework.Editor（Editor-only）
│   ├── Commands/AxuTestPipelineCommands.cs   # [CliCommand] axu_test_run / axu_test_report
│   ├── Reports/AxuTestReportWriter.cs        # [TestRunCallback] JSON 落盘 + console 快照
│   └── Window/AxuTestRunnerWindow.cs         # UITK 看板（仿 KitBox 布局，独立 token USS）
└── Samples~/                             # Edit 基础示例 + PlayMode 场景冒烟示例
```

### asmdef 关键设置
| asmdef | 引用 | 设置 |
|---|---|---|
| `AxuTestFramework` | UnityEngine.TestRunner + precompiled nunit（overrideReferences） | `defineConstraints:["UNITY_INCLUDE_TESTS"]`、`autoReferenced:false`、全平台 |
| `AxuTestFramework.Editor` | Unity.Pipeline、Newtonsoft-Json、AxuTestFramework、UnityEngine.TestRunner、UnityEditor.TestRunner | `includePlatforms:["Editor"]`、`autoReferenced:false` |

`UNITY_INCLUDE_TESTS` 保证正常出包时测试基建不编译——运行时零影响。

### 各包接入约定
- 框架包：`Tests/Editor/`（`AxuFrameKit.Tests.Editor`）+ `Tests/Runtime/`（`AxuFrameKit.Tests.Runtime`，PlayMode 冒烟起步）。
- 模块/indie 包：`Tests/{Editor,Runtime}` 按需建，asmdef 命名 `{被测程序集}.Tests.{Editor|Runtime}`；包 package.json 不声明测试依赖（缺包时测试程序集不编译是特性）。

## 五、pipeline 集成点

- `axu_test_run`：包装 UTF TestRunnerApi。参数 `mode/filter/filter_type/category/clear_console/report/out_path/timeout`；EditMode 同步返回，PlayMode 强制 async。
- 报告落盘：`[TestRunCallback]` RunFinished 写 JSON（summary/results含堆栈/console快照/artifacts/environment）。
- console 断言：run 前清 Console，报告时快照 Error 级；测试内用 AxuLogWatch。
- PlayMode 编排：`set_autotick` → `axu_test_run(mode=playmode, async)` → 轮询 `test_status` → 收报告；`editor_play/stop`、`capture_game_view`、`eval` 为逃生舱。
- 视觉断言：测试内 `AxuCapture.Screenshot()` 写 artifacts 并记入报告。

## 六、测试编写规范

- 类别即套件：`[Category("Axu/Smoke")]`、`[Category("Axu/Play")]`、`[Category("Axu/Module/Noise")]`。
- 命名 `方法名_条件_期望结果`；预期错误 `LogAssert.Expect`，意外 Error 判失败。
- 域重载关闭纪律：`[UnitySetUp]` 中 `AxuTestState.ResetStatics()`；禁止测试间隐式依赖。

## 七、实施里程碑

| 阶段 | 内容 | 派发 | 验收 |
|---|---|---|---|
| **M0 spike（闸门，进行中）** | A/B 两轮对照验证 PlayMode 全链路（域重载开/关）；结论写 `.zcode/reports/2026-08-30-playmode-spike.md` | unity-tester（已派发，等待批准继续） | 两轮均拿到结构化结果 + TestRunCallback 触发，或给退化方案 |
| **M1 建包 + Runtime 工具层** | 包骨架 + AxuWait/AxuTestScene/AxuCapture/AxuLogWatch/AxuTestState/报告 POCO | unity-runtime-dev | asmdef 符合规划；编译零警告；Samples 可跑 |
| **M2 Editor 层** | AxuTestReportWriter、axu_test_run/axu_test_report、AxuTestRunnerWindow | unity-editor-dev | `/api/commands` 出现 axu_*；EditMode 一键出报告；窗口可列套件/跑/看结果 |
| **M3 框架包测试落地** | 框架包 Tests/Editor + Tests/Runtime；Assets/Tests 保留；补首批 _Core/Common 测试；文档四处同步（AGENTS.md 含域重载勘误、包 README、.zcode/08、unity-tester 代理定义） | unity-tester + unity-architect | `axu_test_run(mode=editor, category="Axu/Smoke")` 全绿；文档同步 |
| **M4 模块包试点** | noise（零依赖）+ livestream（依赖框架）各建 Tests；出包验证一次 | unity-tester | 类别过滤可跑；出包不受影响 |
| **M5 收尾** | 版本 0.1.0 定稿；unity-reviewer 审查；.zcode/README 导航补条目 | unity-reviewer | 审查单通过 |

## 八、风险与缓解

| 风险 | 等级 | 缓解 |
|---|---|---|
| PlayMode 全链路 × 两种域重载形态未实测 | 高 | M0 A/B 双轮闸门，含退化方案 |
| 对 Unity.Pipeline（实验版）的依赖 | 中 | 只依赖公开面（[CliCommand]/[CliArg]/响应模型）；内部类复用集中单文件 |
| 静态/单例残留 | 中 | AxuTestState.ResetStatics + 编写规范强制 |
| 新手包接入漏配 asmdef | 中 | README"接入一页纸" + 模板 + reviewer 检查项 |
| 报告落 Library/ 不可入库 | 低 | out_path 参数改写留档 |
| 对 8 个模块包影响 | 零 | 不强制、不进构建、不改主程序集 |