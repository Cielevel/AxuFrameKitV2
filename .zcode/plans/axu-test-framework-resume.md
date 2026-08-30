# AxuTestFramework 测试框架 — 暂停交接文档（Resume Guide)

> **状态：✅ 已完成（2026-08-31），本文档转为归档。**
> 全部里程碑 M0-M5 已交付：测试基建包 `Packages/com.cielevel.axu-test-framework/`（Runtime 工具层 + axu_test_* pipeline 命令 + AxuTestRunner 看板）、框架包 Tests/（29 个测试）、noise/livestream 模块包试点（各 6 个）、文档四处同步与审查修复。
> 现行使用方式见 `AGENTS.md` 测试节、`.zcode/08` 第 9 节、测试框架包 README。以下原始交接内容仅供过程回溯。

---

> **原始状态：暂停**（2026-08-31 凌晨，用户指示"暂时不做了"）。
> 暂停原因：M0 spike 执行中途子代理因模型请求失败中断（工程侧无异常，属临时性故障）。
> 本文档 = 恢复手册。批准方案全文在同目录 `plan-sess_2fd85ea0-d820-43ea-b1c1-d819e48ef6f3.md`（ZCode 自动存档，勿删）；两份配合阅读。
> 恢复时从本文"五、恢复执行手册"开始，无需重新勘察。

---

## 一、现场快照（暂停时点）

| 项 | 状态 |
|---|---|
| `ProjectSettings/EditorSettings.asset` | **干净**（git diff 为空，域重载设置 `Options=0` 未被 B 轮触碰） |
| `Assets/Tests/PlayModeSpike/` | 已创建，3 文件 + 3 meta（详见"四、M0 进度台账"；其中 2 个 .cs.meta 缺 MonoImporter 块，见遗留清理项） |
| `Temp/pipeline_spike_callback.json` | A 轮证据，**易失**（Temp 目录随 Editor 重启可能清空），关键数据已全文收录进本文第三节 |
| `Temp/pipeline_test_status.json` | 同上，summary 已收录 |
| Unity Editor | 暂停时疑似已关闭（进程仅见 Unity Hub）；恢复时需重新打开工程 |
| 未跟踪文件（git status） | `.zcode/plans/`（本文档 + 方案存档）、`Assets/Tests/PlayModeSpike.meta`、`Assets/Tests/PlayModeSpike/` |

## 二、项目一句话背景

为 AxuFrameKit（主框架，嵌入式包 `Packages/com.cielevel.axu-frame-kit`）建通用测试框架与工具：新独立嵌入式包 **`com.cielevel.axu-test-framework`**（命名空间 `AxuTestFramework.*`），覆盖 EditMode + PlayMode，经 Unity Pipeline 本地 HTTP API（`Packages/com.unity.pipeline`，端口 7800，文档 `.zcode/08`）实现"跑测试→收结构化 JSON 报告→看板"闭环，pipeline 包源码零改动（靠 TypeCache 自动发现工程侧 `[CliCommand]` 自定义命令）。方案已获用户批准（含 4 项决策，见第六节）。

## 三、M0 已取得的关键实证（恢复时的起点）

### ✅ A 轮（现状：域重载开启）核心链路已验证成功

- **`[TestRunCallback]` 触发实锤**：`Temp/pipeline_spike_callback.json`（2026-08-30T16:09:35Z 生成，运行 1.66s）：
  ```json
  {
    "runStartedUtc": "2026-08-30T16:09:33.6772400Z",
    "runFinishedUtc": "2026-08-30T16:09:35.3393500Z",
    "rootFullName": "AxuFrameKitV2",
    "resultState": "Skipped:Ignored",
    "total": 19, "passCount": 2, "failCount": 0, "skipCount": 17
  }
  ```
- **pipeline async PlayMode 全链路实锤**：`Temp/pipeline_test_status.json` `status=completed`，summary `{total:19, passed:2, failed:0, skipped:17, inconclusive:0}`——通过的 2 个即 spike 的 2 个 `[UnityTest]`；17 个 skipped 为 pipeline 包自带 PlayMode 测试（HotReloadCliIntegrationTests 等被 Ignore）。
- 结论：**"run_tests mode=playmode async_tests=true → 轮询 test_status → completed + 结构化结果 + TestRunCallback 落盘"在域重载开启设置下全链路可用**。这是 M2 报告落盘设计（`[TestRunCallback]` 支点）的最关键前提，已成立。

### ❌ 未完成部分

1. **B 轮（域重载关闭）完全未做**：临时切 `m_EnterPlayModeOptions` 0→3 → 重跑 → 复原 + git diff 核验。这是原命题场景，仍是 M0 闸门的必要条件。
2. A 轮收尾验证未记录：是否自动退出 Play、Editor.log ReloadAssembly 证据行、`list_tests` 清单核对、EditMode 基线（`run_tests mode=editor` 跑 6 个冒烟测试）是否执行过。
3. `.zcode/reports/` spike 报告未写。
4. **AGENTS.md 勘误未写入**（M3 才统一修，但勘误内容要先记进 spike 报告）：`m_EnterPlayModeOptionsEnabled:1 + m_EnterPlayModeOptions:0` 的实际语义是**进 Play 时域重载+场景重载照常发生**（0=None，非"不重载"）；AGENTS.md"不重新加载域/场景"是把 Enabled 标志误当选项值，git 历史显示自 Initial commit 即如此。

## 四、遗留文件与清理项

### `Assets/Tests/PlayModeSpike/`（保留至 M3 由正式测试替换后删除）

| 文件 | 状态 |
|---|---|
| `AxuFrameKitV2.Tests.PlayModeSpike.asmdef` + .meta | 完整。注意：实际为**全平台**（`includePlatforms` 空）+ `UNITY_INCLUDE_TESTS` + overrideReferences/nunit，与原计划 Editor-only 有偏差；因 `UNITY_INCLUDE_TESTS` 保证 player 构建不编译它，无实际风险。含 2 个 `[UnityTest]`（yield null 数帧、WaitForSeconds 0.2s） |
| `SpikeRunCallback.cs` | `[TestRunCallback]` 实现，RunFinished 写 `Temp/pipeline_spike_callback.json`（已验证可用，M2 AxuTestReportWriter 的原型） |
| `PlayModeSpikeTests.cs.meta` / `SpikeRunCallback.cs.meta` | ⚠️ **不完整**：仅 59 字节，缺 `MonoImporter:` 块（疑为手工写入）。Unity 实际已容忍并跑通测试，但**恢复后第一件事**：pipeline refresh 重编译，确认 Console 无 meta 告警、Unity 重写完整 meta，再 `git status` 复核 |

## 五、恢复执行手册（按序）

0. **启动 Unity Editor 打开本工程**，等 pipeline 服务自动拉起（端口描述符 `Library/Pipeline/.unity-pipeline-port`；API 用法见 `.zcode/08-unity-pipeline-api.md`）。
1. **补 M0 尾巴**（派 unity-tester，一次完成）：
   - refresh 重编译 → 处理 .meta 遗留项（第四节）→ Console 确认零错误；
   - EditMode 基线：`run_tests mode=editor`，预期 6/6；
   - A 轮复核：`run_tests mode=playmode filter="PlayModeSpike" async_tests=true` + `test_status` 轮询，确认 2/2 通过、自动退出 Play、Editor.log 无 ReloadAssembly（现状应**有**，域重载开启是预期行为——如实记录）；
   - **B 轮**：用 `eval` 或 settings 命令置 `EditorSettings.enterPlayModeOptions = 3`（**推荐代码切换而非直接改 asset 文件**，免重导入、残留风险最小；若直接改文件需触发重导入）→ 重跑同上 → 验证 TestRunCallback 仍触发（域重载关闭时无 ReloadAssembly 行）→ **复原为 0** → `git diff ProjectSettings/EditorSettings.asset` 必须为空；
   - 写报告到 `.zcode/reports/`（日期前缀命名，含 A/B 两轮判定 + AGENTS.md 勘误一节）。
   - **闸门判定**：A/B 均绿 → M0 通过；B 轮若断，只记录根因 + 退化方案建议（axu_test_run 内临时切域重载再复原），不得擅自实施。
2. **M1**（unity-runtime-dev）：建包 `Packages/com.cielevel.axu-test-framework/` + Runtime 工具层（AxuWait/AxuTestScene/AxuCapture/AxuLogWatch/AxuTestState/报告 POCO），asmdef 按方案第四节表（`UNITY_INCLUDE_TESTS` + autoReferenced:false）。验收：编译零警告、Samples 可跑。**注意：M1 的编译验证不得与任何测试运行并发**（域重载会污染 PlayMode 实测）。
3. **M2**（unity-editor-dev）：`AxuTestFramework.Editor`——`AxuTestReportWriter`（`[TestRunCallback]`，schema `axu-test-report/1`，落盘 `Library/AxuTestReports/{时间戳}-{mode}.json` + `artifacts/`）、`[CliCommand]` `axu_test_run`/`axu_test_report`、`AxuTestRunnerWindow`（UITK 看板，仿 KitBox 布局但包内自持精简 token USS；菜单 `AxuFrameKit/AxuTestRunner`）。验收：`/api/commands` 出现 axu_*；EditMode 一键出报告。
4. **M3**（unity-tester + unity-architect）：框架包 `Tests/Editor` + `Tests/Runtime`；`Assets/Tests/Editor/` **保留不动**；补首批 _Core/Common 测试；删除 PlayModeSpike；文档四处同步——AGENTS.md（测试节 + **域重载勘误**）、包 README 索引、`.zcode/08` 追加 axu_* 命令节、`.zcode/agents/unity-tester.md`。
5. **M4**（unity-tester）：`axu-module-noise`（零依赖）+ `axu-module-livestream`（依赖框架）两包试点建 Tests，类别过滤可跑 + 正常出包验证一次；总结接入模板。
6. **M5**（unity-reviewer）：审查（分层红线/.meta/asmdef/autoReferenced/命名）→ 版本 0.1.0 定稿 → `.zcode/README.md` 导航补条目（把本交接文档替换为正式知识库条目）。

## 六、关键决策记录（用户已拍板，恢复时不再讨论）

| 决策点 | 结论 |
|---|---|
| 包命名 | `com.cielevel.axu-test-framework` + 命名空间 `AxuTestFramework.*`（独立第三家族） |
| 旧测试 | `Assets/Tests/Editor/` 保留不动，作为工程级冒烟入口；新测试进各包 `Tests/` |
| 工具 UI | 独立 UITK 窗口，样式仿 KitBox 但完全独立；入口菜单与 KitBox 同级：`AxuFrameKit/AxuTestRunner` |
| 报告落盘 | 默认 `Library/AxuTestReports/`，`out_path` 可改写留档 |
| 实施顺序 | 严格 M0→M5，每阶段经 pipeline 实测验证后再进下一阶段 |

## 七、架构速记（详见同目录方案存档全文）

```
Agent ──HTTP──► Unity.Pipeline（内置 run_tests/test_status/console/... + TypeCache 自动发现工程侧
                [CliCommand]: axu_test_run / axu_test_report，来自 AxuTestFramework.Editor）
                     ▼ UTF TestRunnerApi
        [TestRunCallback] → AxuTestReportWriter（JSON + 截图落盘）
                     ▼ 被引用
框架包/模块包/indie 包 的 Tests/{Editor,Runtime}（UNITY_INCLUDE_TESTS 守护）
                     ▼ 引用
AxuTestFramework（Runtime 工具层：等待/场景/截图/日志监视/静态重置/报告模型）
```

依赖红线：`AxuTestFramework`（Runtime）不依赖任何框架程序集；`AxuTestFramework.Editor` 只依赖 `Unity.Pipeline` + `AxuTestFramework`，**不依赖** `AxuFrameKit.Editor`；无循环。

## 八、风险备忘（继承自方案第八节 + 执行新知）

| 风险 | 等级 | 缓解 |
|---|---|---|
| B 轮（域重载关闭）PlayMode 链路未实测 | 高 | M0 闸门未过，恢复后第一优先 |
| Unity.Pipeline 实验版 API 耦合 | 中 | 只依赖公开面；`SpikeRunCallback.cs` 已证明 `[TestRunCallback]` 通道可用 |
| spike .meta 不完整 | 低 | 恢复手册第 1 步处理 |
| 静态/单例残留（两种域重载形态下都存在） | 中 | `AxuTestState.ResetStatics` + 编写规范强制 |
| Temp/ 下证据文件易失 | — | 关键数据已收录本文第三节，不依赖 Temp 存活 |
