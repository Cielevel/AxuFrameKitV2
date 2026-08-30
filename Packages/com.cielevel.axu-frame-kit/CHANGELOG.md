# Changelog

本文件记录 com.cielevel.axu-frame-kit 的显著变更。0.1.0 为首个版本条目，其中 2026-08-29 之前的内容依据项目 git 提交历史整理。

## [0.1.0]

### 2026-08-30

- AxuKitBox（KitBoxRevV2）接入主题系统：`AxuKitBoxV2.uss` 颜色变量由 `:root` 重构为 `.theme-*` 主题作用域（`--ue5-*` 更名 `--kb-*`，并补齐滚动条滑块、按钮禁用态变量），内置 UE5 经典 / Tokyo Night / Dracula / Nord 四套主题；窗口标题栏新增主题下拉切换（EditorPrefs 持久化，键 `AxuKitBoxRevV2.Theme`）。`SymbolsRevPage` 状态文本由 C# 内联硬编码色改为主题感知 USS 类（`status-added` / `status-removed` / `text-secondary`）。

### 2026-08-29

- 补充包级 `README.md` 与本 `CHANGELOG.md`，package.json 增加 `documentationUrl`。
- 将原位于 `Assets/AxuFrameKit/{_Core,Common,KitDoc}` 下的 25 份模块文档迁入包内、随代码放置，并修正文中旧路径；`Assets/AxuFrameKit` 旧文档骨架随之清空。
- 移除与独立模块包重复的 `Editor/Common/Window/SceneJumperWindow.cs`、`SpriteToAnimationWindow.cs`（两者已拆为 `com.cielevel.axu-indie-scene-jumper` / `com.cielevel.axu-indie-sprite-to-animation` 包，独立包为唯一来源）；`CommonWindowStyles.uss` 保留（AssetFavorites/AvatarDownloader/KitBox SceneJumpRevPage 仍在使用）。
- 同日框架家族与独立模块整体拆包：`Assets/AxuFrameKit/Modules` 与 `Assets/AxuIndieModules` 拆为 8 个独立嵌入式包（`com.cielevel.axu-module-*` ×4、`com.cielevel.axu-indie-*` ×4），Assets 中不再保留框架/模块代码。

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
