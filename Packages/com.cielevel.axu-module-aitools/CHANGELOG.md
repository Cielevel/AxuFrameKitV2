# Changelog

## [0.1.0] - 2026-08-29

- 从 `Assets/AxuFrameKit/Modules/AITools` 迁入，改为独立嵌入式 UPM 包 `com.cielevel.axu-module-aitools`。
- UXML 加载改为脚本相对定位（原硬编码路径失效）。
- Editor-only 程序集隔离（原窗口未做编辑器隔离，存在 player build 报错风险，本次修复）。
- 含明文 API Key 的 AIConfig 配置资产属用户数据，不随包分发，已迁至 `Assets/TestAndSample/AITools_Config/`。
