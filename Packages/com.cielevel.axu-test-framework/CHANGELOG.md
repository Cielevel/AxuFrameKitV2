# Changelog

All notable changes to this package will be documented in this file.

## [0.2.0] - 2026-08-31（场景测试平台建设启动，Phase 0/1 已落地）

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
