# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-09-28

### Added

- 2026-09-28：**游戏内运行时控制台面板**（对标 `LiveStreamSimulatorWindow` 的运行时版本，可随 Player 构建发布）——
  预制体 `Runtime/Art/Prefab/PF UI LiveStream Console.prefab`（自带 Canvas，三栏布局：用户多选 / 礼物多选+每礼物独立数量 / 评论+点赞+赠送操作区，含收起按钮），拖入场景即用。
  - 新增 `LiveStreamConsoleManager`：三个配置（`UserConfigSo` / `GiftConfigSo` / `TirggerCollectionSo`）全部 **Inspector 手动挂载**（无自动搜索），Start 逐项校验缺失即 LogError 指名；发送语义与模拟器窗口一致（M 用户 × N 礼物逐条触发、空选择 LogWarning、评论发送后清空输入、数字输入过滤）。
  - 新增 `UIPanelLiveStreamConsole`（三栏面板）与 `UIConsoleUserItem` / `UIConsoleGiftItem`（条目）；礼物按 `GiftConfigSo` 列表下标填充 `GiftIndex`，与触发器按 index/name 两种匹配兼容。
  - `UserConfigSo` / `GiftConfigSo` 新增公开只读访问器 `Users` / `Gifts`（运行时面板读取，向后兼容）。
  - 纯 Runtime 代码零 Editor 依赖；字体用包内 SmileySans TMP 字体。
  - 演示：`Assets/TestAndSample/Sample_PackageDev/LiveStreamConsoleDemo/`（场景 `SC LiveStream Console Demo.unity` + 全套演示 SO + 三个 Debug.Log 触发器桩）；Editor Play 实测发送链路（评论精确词 "666"、点赞、礼物 M×N）与收起/展开均通过。
  - 新增 27 个 EditMode 测试（面板/条目/Manager 分发与校验/SO 访问器），包内合计 33/33 通过。

- 2026-09-02：新增 `LiveStreamPlayerWindow` 回放窗口（菜单 `AxuFrameKit/Modules/LiveStream/Operation Player`）——加载 `LiveStreamOpRecorder` 保存的录制 JSON，按录制时间戳回放礼物/评论/点赞操作。编辑器窗口无协程宿主，用 `EditorApplication.update` 自驱时间轴（相对首条记录时间偏移，兼容两种录制模式）；复用 `LiveStreamOpPlaybackHandler` 做类型分发；支持 加载/开始回放/停止/立即回放/清空日志，及「📌 使用选中文件」按钮（Project 窗口选中 .json 资产后一键校验解析并填入路径）。
- 2026-08-31：包内测试接入测试框架 AxuTestFramework——新增 `Tests/Editor/`（`AxuFrameKit.Modules.LiveStream.Tests.Editor`，6 个 EditMode 测试，类别 `Axu/Module/LiveStream`：操作录制/回放分发、评论触发器三路分发、礼物直传与按数量切割）。`UNITY_INCLUDE_TESTS` 守护不进构建；运行经 pipeline `axu_test_run`。

### Changed

- 2026-09-02：6 个可创建 SO（`UserConfigSo` / `GiftConfigSo` / `TriggerCommentConfigSo` / `TriggerLikeConfigSo` / `TriggerGiftConfigSo` / `TirggerCollectionSo`）移除 `[CreateAssetMenu]` 原生菜单头部，改标注框架包的 `[CreatorEntry]` 分组标签（分组：`直播互动/配置`、`直播互动/触发器`），创建入口统一为 `AxuFrameKit/Asset/ScriptableObject Creator` 窗口。既有资产不受影响（资产与脚本的关联由脚本 GUID 决定，与 attribute 无关）。

## [0.1.0] - 2026-08-29

### Added

- 从 `Assets/AxuFrameKit/Modules/LiveStream/` 迁入为独立嵌入式 UPM 包 `com.cielevel.axu-module-livestream`。
- Runtime：触发器 SO 体系（`LiveTriggerBase` 系 / `TirggerCollectionSo` / `UserConfigSo` / `GiftConfigSo` / `Trigger*ConfigSo`）、运行时 UI 面板（`UIPanelUser/Gift/Comment`）、操作录制器 `LiveStreamOpRecorder`、秋恋猫 HTTP 对接 `RequestLiveStream` 及 UI 辅助组件，附 Art（SmileySans 字体 / 材质 / ShaderGraph / 贴图）。
- Editor：`LiveStreamSimulatorWindow`（菜单 `AxuFrameKit/Modules/LiveStream/Simulator`）与 `LiveStreamRecorderWindow`（菜单 `AxuFrameKit/Modules/LiveStream/Operation Recorder`），及新增的桥接类 `LiveStreamEditorHooks`。

### Changed

- Runtime / Editor 程序集拆分：`LiveStreamEditorModeManager` 中 `#if UNITY_EDITOR` 块对 `LiveStreamSimulatorWindow` 的依赖解耦至 Editor 程序集（`LiveStreamEditorHooks`，`[InitializeOnLoadMethod]` 桥接模拟器事件），Runtime 程序集不再引用任何 Editor 类型。
- 示例（两个演示场景 / UI Unit Prefab / TestPanel 与 LiveTrigger 脚本 / SO 实例 / 头像与礼物图片）迁入 `Samples~/LiveStream/`，经 Package Manager → Samples 导入。

### Removed

- 原工程内路径 `Assets/AxuFrameKit/Modules/LiveStream/` 与 `Assets/TestAndSample/Sample_Modules/LiveStream/`。
