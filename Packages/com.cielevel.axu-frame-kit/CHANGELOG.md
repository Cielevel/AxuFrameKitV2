# Changelog

本文件记录 com.cielevel.axu-frame-kit 的显著变更。0.1.0 为首个版本条目，其中 2026-08-29 之前的内容依据项目 git 提交历史整理。

## [0.1.0]

### 2026-09-03

- **新增**：AxuKitBox「实用工具」组两个效率小页面（注册于 `SetupGroups`，纯 Editor、零新依赖、互相独立）：
  - **缺脚本清扫器**（`KitBoxRevV2/Groups/UtilityGroup/` 下 `MissingScriptSweeperRevPage` + 纯逻辑类 `MissingScriptScanner`）：扫描 Assets 下 Prefab/Scene 资产中的 Missing Script——范围目录、预制体/场景开关、按 `AssetDatabase.GetAssetDependencyHash` 的会话级"干净缓存"（未变资产跳过）、进度条可取消；结果按资产分组列表、点击 Ping、嵌套 Prefab 实例上的缺失组件归属源资产（仅报告，不在实例清扫）；确认后一键移除（Prefab `LoadPrefabContents` → 官方移除 API → `SaveAsPrefabAsset` 落盘；场景就地或 Additive 打开保存后关闭）；`Packages/` 资产一律只读不扫。**重要口径修复**：Unity 6000.0.23f1 实测 `GetComponents<Component>()` 对缺失脚本槽位返回"真 null"（社区通行的 fake-null 判定 `== null && !ReferenceEquals(null)` 在本版本恒为 false），检测须按 `comp == null`，移除须走官方 `GameObjectUtility.RemoveMonoBehavioursWithMissingScript`（`DestroyImmediate` 对真 null 抛异常）；另实测该版本资产管线**拒绝保存含缺失脚本的 Prefab**（SaveAsPrefabAsset 引擎报错）且 prefab 根物体名强制=资产文件名。
  - **多场景组合**（`SceneSetPresetsRevPage` + 纯逻辑类 `SceneSetPresetStore`）：把当前多场景叠加组合（`GetSceneManagerSetup`，激活场景稳定排首）存为命名预设，JSON 落 `ProjectSettings/AxuSceneSets.json` 随仓库共享（读取侧损坏自动 `.bak` 后重建、永不抛异常；保存侧 IO 异常正常上抛）；一键恢复（`RestoreSceneManagerSetup`，有未保存场景先列名确认）、更新为当前、删除、缺失场景标注与逐条 Ping。与单场景快跳（SceneJumpRevPage / indie SceneJumper）定位互补不重叠。
- **测试**：`AxuFrameKit.Tests.Editor` 36 → 48 全绿（新增 `[Axu/Editor/MissingScript]` 5 个、`[Axu/Editor/SceneSets]` 7 个）。缺脚本正例采用**入库常驻夹具** `Tests/Editor/Fixtures/MissingScriptFixture.prefab`（stale-GUID 形态，测试复制副本后扫描/清扫）——运行时构造缺失槽位的三条路线（手写 YAML 注入、Unity 写档后文本注入、SerializedObject 置空 m_Script）经实验均被该版本资产管线剥离/拒绝，已留档于测试注释；截图留证 `artifacts/kitbox-sweeper-scenesets/`。

### 2026-09-02

- **新增**：通用 ScriptableObject 资产创建窗口 `ScriptableObjectCreatorWindow`（`Editor/Common/Window/`，菜单 `AxuFrameKit/Asset/ScriptableObject Creator`，另有 Project 右键 `Assets/Create/AxuFrameKit/ScriptableObject Creator` 入口、以选中文件夹为创建初始目录）。反射扫描全程序集非抽象 SO 子类（排除 Unity/TMPro/Microsoft/System/I18N 命名空间、EditorWindow/Editor 派生类、`*.Tests` 测试程序集与 Unity 官方程序集（程序集名以 Unity 开头），测试 fixture 与引擎内部 SO 不污染窗口），支持搜索过滤、"仅框架类型（`CustomScriptableObject` 派生）"开关、按 `CreatorEntry` 分组的折叠树（未标注类型按程序集归入"未分组"区，分组折叠状态经 EditorPrefs 跨开窗持久化、未记录默认展开）、`InspectorTitle`/`InspectorIntro` meta 渲染、序列化字段预览、同类型已有资产列表（点击 Ping）、`SaveFilePanelInProject` 创建后 Select+Ping。扫描逻辑抽为可测的 `ScriptableObjectCreatorScanner`，配套 EditMode 测试；`AxuFrameKit.Tests.Editor` asmdef 补 `AxuFrameKit.Editor` 引用。
- **新增**：`CreatorEntryAttribute`（`Runtime/Common/CustomAttribute/`，SO 子类可选标注）：路径串风格同 `MenuItem`，最后一段为创建窗口中的显示名（替代类名）、前序段为分组路径，如 `[CreatorEntry("配置/战斗/SkillData")]`。
- **变更**：包内可创建 SO 的创建入口从原生 `CreateAssetMenu` 右键菜单迁移至 `ScriptableObjectCreatorWindow`——移除 `GameObjectStrPools`/`GameObjectIntPools` 的 `[CreateAssetMenu]`，包内 3 个可创建 SO（含 `CustomScriptableObject` 标记基类）全部标注 `[CreatorEntry]`（分组：`框架/对象池`、`框架/基础`）。既有资产不受影响（资产与脚本的关联由脚本 GUID 决定，与 attribute 无关）。

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
