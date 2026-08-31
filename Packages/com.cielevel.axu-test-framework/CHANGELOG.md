# Changelog

All notable changes to this package will be documented in this file.

## [Unreleased]

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
  `menu`(自动开窗)/`output` 参数，`GetDIBits` 失败时自动回退 `GetBitmapBits` 并统一翻转行序。macOS 未实现
  （需 CGWindowListCreateImage 变体）。
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
