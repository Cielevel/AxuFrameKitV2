# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
