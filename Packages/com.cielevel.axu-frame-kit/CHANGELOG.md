# Changelog

本文件记录 com.cielevel.axu-frame-kit 的显著变更。0.1.0 为首个版本条目，其中 2026-08-29 之前的内容依据项目 git 提交历史整理。

## [0.1.0]

### 2026-08-31

- **新增**：包内测试体系落地（接入测试基建包 `com.cielevel.axu-test-framework`）。`Tests/Editor/`（`AxuFrameKit.Tests.Editor`，23 个 EditMode 测试，类别 `Axu/Smoke` / `Axu/Core/Util` / `Axu/Common/Util`）+ `Tests/Runtime/`（`AxuFrameKit.Tests.Runtime`，6 个 PlayMode 冒烟测试，类别 `Axu/Play`）；`UNITY_INCLUDE_TESTS` 守护不进构建，运行经 pipeline `axu_test_run` 命令或菜单 `AxuFrameKit/AxuTestRunner` 看板。`Assets/Tests/Editor/` 保留为工程级冒烟入口。
- **文档**：`Doc_MathUtility.md` 补充 AxuTween 完成判定语义（完成当帧 Runner 回收复用，勿轮询 `IsCompleted`，用 `OnComplete`）；`BetterFSM/ReadMe.md` 补充状态以 `GetType()` 为键、每状态类型仅一实例的约束；AGENTS.md 事件系统示例勘误（实际为 `EventCenter.Default` / `EventCentral.Default` + `Register/Send`）。

### 2026-08-30

- **修复**：KitBox 标题栏主题下拉框（`#theme-dropdown`）横向溢出窗口右缘、"UE5 经典"文字与下拉箭头被截断——Unity 默认主题给 BaseField label 预留的 `min-width: 120px` 挤占了固定宽 168px 中输入框的可用空间。USS 为字段补 `min-width: 0`/`flex-shrink`，label 改为自适应宽，input 填充剩余宽并 `overflow: hidden`，窗口变窄时随头部行收缩而非顶出边界。
- **修复**：主题变量表 `AxuKitTokens.uss` 改由 `AxuKitTheme` 集中加载（新增 `GetTokens`/`AttachTokens`：规范化工程相对路径 `Packages/.../Editor/Common/UI/AxuKitTokens.uss` + 缓存 + 失败告警）。原先 KitBoxV2、AssetFavorites、AvatarDownloader、CsvContainerCreator 四个窗口用脚本目录拼 `../` 相对路径调用 `AssetDatabase.LoadAssetAtPath`，该 API 对含 `..` 的路径恒返回 null 且被静默吞掉，token 表缺失导致组件样式表所有 `var(--kb-*)` 变量失效、窗口深底黑字不可见。
- **AxuKit 编辑器界面体系重构**：样式拆为 token 层（新增 `Editor/Common/UI/AxuKitTokens.uss`，语义化 `--kb-*` 变量 + 圆角/间距/字号 token，USS 能力边界内参考 macOS 视觉标准）与组件层（`AxuKitBoxV2.uss`/`CommonWindowStyles.uss`/`CsvContainerCreatorWindow.uss` 全部重写、只引用 token）。主题增至 5 套（UE5 经典 / **macOS Light（新增）** / Tokyo Night / Dracula / Nord），由新 `AxuKitTheme`（`Editor/Common/UI/AxuKitTheme.cs`）统一管理：全部 AxuKit 窗口共享主题选择（EditorPrefs `AxuKit.Theme`，兼容迁移旧键 `AxuKitBoxRevV2.Theme`）。
- **信息清理**：移除窗口内与标题栏重复的大标题、装饰性 emoji、英文开发用语（CURRENT/BUILD/Select 等）、调试信息（缺失资源 GUID 片段）；Welcome 页特性卡从纯装饰改为可点击导航。
- **去硬编码**：全部页面/窗口 C# 内联色值（橙色高亮 ×7、灰阶、hex 日志色等）改为主题感知 USS 类（`text-ok/warn/bad/accent`、`log-*`、`drag-hint`、`bare-btn`、`mode-toggle`、`col-divider`）；删除死文件 `AxuKitBoxV2_UE5Theme.uss`。

### 2026-08-29

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
