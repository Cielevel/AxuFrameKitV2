# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- 2026-08-31：包内测试接入测试框架 AxuTestFramework——新增 `Tests/Editor/`（`AxuFrameKit.Modules.Noise.Tests.Editor`，6 个 EditMode 测试，类别 `Axu/Module/Noise`：Perlin 确定性/值域、FastNoiseLite 六种 NoiseType 同种子逐点确定与异种子差异）。`UNITY_INCLUDE_TESTS` 守护不进构建；运行经 pipeline `axu_test_run`。

## [0.1.0] - 2026-08-29

### Added

- 从 `Assets/AxuFrameKit/Modules/Noise/` 迁入为独立嵌入式 UPM 包 `com.cielevel.axu-module-noise`，代码零改动。
- Runtime：`FastNoiseLite`（Auburn/FastNoiseLite v1.1.1 封装）与 `SimpleNoise`（纯 C# Perlin/FBM/白噪声）。
- Editor：`Noise Generator`（`AxuFrameKit/Modules/Noise/Noise Generator`）与 `FastNoiseLite Previewer`（`AxuFrameKit/Modules/Noise/FastNoiseLite Previewer`）两个窗口，窗口样式表 `UI/NoiseWindowStyles.uss`。

### Third Party Notices

本包包含第三方软件组件，其授权条款如下：

**FastNoiseLite** (`Runtime/FastNoiseLite/FastNoiseLite.cs`)

Copyright (c) 2023 Jordan Peck (jordan.me2@gmail.com) and Contributors (Auburn/FastNoiseLite)

以 MIT License 授权，完整许可文本见本包根目录的 `Third Party Notices.md` 及源文件头部注释。
