# Changelog

本文件记录 com.cielevel.axu-frame-kit 的显著变更。0.1.0 为首个版本条目，其中 2026-08-29 之前的内容依据项目 git 提交历史整理。

## [Unreleased]

### 2026-09-20

`axu_lint` 规范校验首跑漂移清单收尾（仅命名空间归位 / 文件重命名，类型名、公共 API 与序列化引用零变更）。

- **Editor 命名空间归位（7 文件）**：`Editor/Common/` 下 7 个 Editor 类从 `AxuFrameKit.Common.{区域}.Editor` 形态迁到规范前缀 `AxuFrameKit.Common.Editor.{区域}`（CustomAttribute×3、UI.Kit.ListView、UI.Kit.DataBind、Utility.Auto.AutoSequence、Utility.Table.CSV）；均为属性注册型 CustomEditor / 编辑器窗口，仓库内无外部 `using` 引用，随迁补显式 using。
- **文件名与类名对齐（6 文件，GUID 不变）**：`GameObjectExtension.cs`→`GameObjectExtensions.cs`、`RectTransformExtension.cs`→`RectTransformExtensions.cs`、`_Core/Extension/EnumExtension.cs`→`EnumExtensions.cs`、`Timer/CountDownManager.cs`→`CountdownManager.cs`（大小写对齐）、`Tweener/AxuTweenerFade.cs`→`AxuTweenerCanvasGroupAlpha.cs` 与 `Tweener/AxuTweenerCanvasGroupAlpha.cs`→`AxuTweenerCustomFloat.cs`（两文件内容与旧名交叉错位，按"文件随类名"归位）。
- **验证**：无头编译零 error；`axu_lint` 复跑 `namespace.convention` 7→0、`file.type_name` 37→24（余下均为既有家族文件形态：Ease 一族、Singletons、FSM、DesignPattern 等）。

### 2026-09-18

补齐 2026-09-17 加密批次留下的告警尾巴：`AesCrypto` 旧口令接口标记过时时，`EncryptHelper` 的便捷转发被漏掉，编译报 CS0618。

- **修复（编译告警 / 加密外观）**：`EncryptHelper.AesEncrypt/AesDecrypt` 的旧口令转发补 `#pragma warning disable CS0618` 并在注释中标注"仅为兼容既有密文"——`EncryptHelper` 是旧密文的对外入口，不随 `AesCrypto` 同名方法一起废弃；新增 `AesEncryptWithPbkdf2/AesDecryptWithPbkdf2` 便捷入口（`iterations` 可配，默认 10000），补上此前只能经 `EncryptHelper.AES.EncryptStringWithPbkdf2` 才能走到的口令路径。
- **测试**：`CoreEncryptTests` 新增 `EncryptHelper_便捷接口_旧口令与Pbkdf2均可往返`（旧路径可用性 + 新接口往返 + 与 `AesCrypto` 密文格式互通）；旧口令用例加**有意**抑制与用途说明。
- **文档**：`EncryptUtility.md` / `QuickReference.md` / `APIReference.md` 三份同步——PBKDF2 便捷接口入表，旧口令接口统一标注"仅用于解既有密文"。

### 2026-09-17

全框架体检后的集中修复批次（分层红线、域重载、UI、网络/存档/加密、任务队列、Editor 工具）。测试：`AxuFrameKit.Tests.Editor` 48 → 49、`AxuFrameKit.Tests.Runtime` 6 → 14，两侧全绿；`LiveStream`/`Noise` 模块测试与工程级冒烟同步复跑通过。

- **修复（_Core 分层红线）**：`AxuFrameKit.Core.asmdef` 置 `noEngineReferences: true`（编译器强制零引擎依赖），并清除最后三处引擎引用——`Logger` 重写为纯 C# 可注入 Sink（公共 API 不变；新增 `LoggerUnityWire`/`LoggerWire` 分别把运行时与编辑器输出接线到 `UnityEngine.Debug`）、`FloatExtension` 改用 `System.Math`、`RandomNickname` 改走 `Logger`。
- **修复（域重载静态状态）**：新增 `MultitonRegistry`（全框架静态重置登记中心）与 `AxuStaticResetGuard`（每次进入 Play 最早期统一清理）；登记覆盖 Multiton / MultitonAuto / MultitonMono 族、Singleton 全家族（Lazy / Mono / CustomMono / MonoCr / DesignPattern `Singleton<T>`）、`EventChannelCore` 实例订阅、UseMono 双 handler。同步修复：`MultitonAuto.DisposeAll` 边枚举边删必抛、`SingletonMono(Cr)` 重复实例不销毁与静态引用残留、UseMono Separate 模式 `_destroyedObjects` 强引用泄漏（改 `ConditionalWeakTable` 弱标记）。
- **修复（UI 框架层）**：`UIBaseManager<T>` 重构为统一查找核心 + 惰性收集——修复 Awake 前调用 NRE、已销毁组件残留抛 `MissingReferenceException`、动态实例化组件查找不到三类缺陷；新增 `RegisterPage/UnregisterPage/RegisterArea/UnregisterArea`；28 个公开 API 签名零变更。`IsPageShow/IsAreaShow` 补 `Content` 判空，id 重载补类型过滤。`UIBaseComponent` 修复：`Start` 不再覆盖此前的显隐交互、`OnValidate` 不再覆写手工填充的 Name/ID/Content、空参数回落无参 `OnShow/OnHide` 回调（此前走 Manager 的 `ShowPage<T>()` 不会触发只重写了无参回调的子类）。
- **修复（UI Kit）**：`UIListView` 追加数据后列表空白（刷新早退）、非池模式克隆堆积、活跃项 O(n²) 查询、可见数量不随视口重算、Grid 布局忽略 `controlChildSize`、适配器 lambda 订阅叠加与悬垂；`UIBindParent` 重复 ID/Index 抛异常、Index 自动编号、跨模式误用守卫；`UIVideoKit` RenderTexture/材质泄漏、`playbackSpeed` 未生效、淡出回调无防护（新增 `Stop()`）；`TmpTypewriter` 富文本 alpha 破坏与播放中文本变更失效、Manager 版空文本永不完成与 label 销毁后每帧异常；`UIToastUnit` 回收不停用、`UISideToast` 池初始化时序 NRE；`UIToggleParent(ForPanel)` 订阅泄漏、id 恒空与 Index 口径不一致；`UIImageGroupID/UISpriteGroupIndex` 重复 ID 与空引用健壮性；删除 `UILoopVerticalScrollRect` 的占位 `GetVisibleRange`。
- **修复（网络 / 存档 / 加密 / 定时器）**：`HttpUtil` 六方法补 30s 超时 + 保留头集合去重、`Downloader` 三处补超时、`SocketClient` 四处吞异常补日志；`SaveUtil` 改为写盘成功后再提交缓存、键名白名单校验（防路径逃逸）；`AesCrypto` 字节接口严格校验密钥长度（16/24/32）、修正解密路径的操作枚举、新增 `EncryptStringWithPbkdf2/DecryptStringWithPbkdf2` 并把旧口令接口标记过时（**旧字符串接口保持原有密文格式，既有存档/旧密文仍可解**）；`RsaCrypto` 新增可选填充方式参数（默认维持平台历史行为）与 PKCS1 明文长度预检；`HmacSha256` 字符串校验改常量时间；`Pbkdf2KeyDerivation` 迭代次数可配；倒计时/秒表句柄加版本号，归还池后写入不再串到新计时器。
- **修复（任务队列）**：新增 `TaskQueueEngine` 公共引擎（信号量等待替代空转轮询、generation 代数杜绝 Stop→立即 Run 的双消费者竞态、任务级异常隔离），`TaskQ / TaskQAuto / TaskQc / TaskQcAuto` 全部收敛其上且公共签名不变，修复 `TaskQc` 系 CTS 重复 Run 泄漏与单任务异常中止整队；`CoroutineQ` 子协程异常不再导致队列永久假死。
- **修复（Editor 工具）**：KitBox——CSV 编辑器改为引号语义解析/转义 + 写入 `.bak` 备份 + UTF8 无 BOM、缺脚本清扫器不再静默保存已打开场景（改为标记脏 + 提示手动保存）、页面新增 `OnEnter/OnExit` 生命周期并治理全局订阅（修复 `selectionChanged` 进页叠加订阅、`projectChanged` 全工程重建、`hierarchyChanged` 无注销）、当前页 EditorPrefs 持久化、场景组合存储改原子写 + 损坏弹窗告知；独立工具——`ButtonAttributeEditor` 反射缓存 + 支持多选、`AvatarDownloader` 补超时并修正工程路径判定、`ScriptDefineSymbolSetter` 改精确匹配宏、`UIBindParentEditor` 补 Undo/SetDirty、Hierarchy Colorizer 补 Repaint 早退与层号缓存、Icon 修复 null 缓存失效、四处每帧 `GUIStyle/GUIContent` 分配改缓存、`CsvIOUtil` 无扩展名路径越界与编码口径统一。
- **修复（其他 Runtime）**：`Computed` 在 Dispose/重算时正确解绑上游订阅；`AutoEventChannel*` 补反注册与空校验、类名与文件名对齐；`ResourceLoader` 失败改为告警并新增失败回调重载；`CsvContainerSo` 统一加载口径、补空引用防护、移除 ScriptableObject finalizer；`Structure` 系列补 `TryDequeue`/`IComparer` 重载、统一重复元素策略、去掉 `BiDictionary` 双重上报；`CollectionExtension.GetRandomUniqueItems` 修复含重复元素时的死循环并复用共享随机源；`GameObjectPools`/`SimpleObjPool` 增加已销毁对象与重复归还防护；`TransformExtension.ClearChildren` 修复编辑器模式漏删；删除字典重复键的静态死雷 `Type2Name`/`Type2DefaultStrValue`。
- **测试**：新增 `Tests/Runtime/UIFrameworkPlayModeTests.cs`（8 个用例：惰性收集、按类型与 id 查找、已销毁过滤、`Content` 为空不抛、`Start` 竞争回归、动态注册/注销）；`CoreEncryptTests` 新增 PBKDF2 口令接口往返与字节接口严格校验用例。
- **文档**：`UI框架.md` 重写为与代码一致的职责说明（Manager 只管 Page/Area；Panel/Unit 为组件层），并补回调/`Content`/`Name`/`ID` 约定。

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
