# Changelog

本文件记录 com.cielevel.axu-frame-kit 的显著变更。0.1.0 为首个版本条目，其中 2026-08-29 之前的内容依据项目 git 提交历史整理。

## [0.1.0]

### 2026-08-29

- 补充包级 `README.md` 与本 `CHANGELOG.md`，package.json 增加 `documentationUrl`。
- 将原位于 `Assets/AxuFrameKit/{_Core,Common,KitDoc}` 下的 25 份模块文档迁入包内、随代码放置，并修正文中旧路径；`Assets/AxuFrameKit` 旧文档骨架随之清空（仅余 Modules 待包化）。

### 2026-08-26

- 新增 Editor 工具 `TerrainToMeshConverter`（地形转网格，`Editor/Common/Utility/Converter/`）。

### 2026-08-02

- 存档（Save）相关改动。

### 2026-06-16

- **框架主体迁入本包**（提交 5277cdd）：`_Core`（80 个脚本）迁入 `Runtime/_Core`（新建 `AxuFrameKit.Core.asmdef`）；`Common`（147 个运行时 + 30 个编辑器脚本）迁入 `Runtime/Common`（`AxuFrameKit.Runtime.asmdef`）与 `Editor/Common`（`AxuFrameKit.Editor.asmdef`）；package.json 补齐依赖。

### 2026-06-10 ~ 06-12

- 包首次建立（空壳，version 0.1.0）。
- 新增 `MultitonManager`、TMP Typewriter 组件。

### 2026-05

- CSV 工具窗口系列优化；独立模块 SelectionToCsv。

### 2026-03 ~ 04

- 新增生产级 TCP Socket 客户端（长度前缀分包协议 `SocketClient`）。
- 加密模块强化（RSA padding、新增 XOR 对称加密）。
- `EventSystem` 由静态改为 Instance 模式；新增 `Computed<T>` 响应式派生值。

### 2026-02

- Save 存档工具完善；UI 框架修复；`_Core` 脚本补充 summary 注释。
