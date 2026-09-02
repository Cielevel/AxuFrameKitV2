# 05 · Editor 层模块清单（AxuFrameKit.Editor 程序集，29 个 .cs）

> 路径：`Packages/com.cielevel.axu-frame-kit/Editor/`。命名空间：`AxuFrameKit.Common.Editor.*`。
> asmdef 限 Editor 平台（includePlatforms: Editor），`autoReferenced: false`（使用方需手动引用程序集/using）。

## 模块总表

| 子模块 | 路径 | 关键类 | 职责 |
|---|---|---|---|
| **KitBoxRevV2** | `Editor/Common/KitBoxRevV2/` | `AxuKitBoxRevV2Window`（主窗口，纯 UI Toolkit）、`IKitBoxRevV2/IKitPageRevV2/IKitGroupRevV2`；页面：`WelcomeRevPage`、`SymbolsRevPage`（宏符号表）、`MaterialCopierRevPage`、`CsvEditViewLiteRevPage`、`SelectionToCsvRevPage`、`BatchRenameRevPage`、`SceneJumpRevPage` | 框架自带多页签"工具箱"编辑器窗口，**插件化页面体系**（实现 IKitPageRevV2 即可挂入新页） |
| Hierarchy 增强 | `Editor/Common/Hierarchy/{Colorizer,Icon}/` | `HierarchyColorizer`（+Config/Window）、`HierarchyIconDrawer`（+Config/Window） | 按规则给 Hierarchy 条目着色 / 画图标 |
| CustomAttribute 绘制 | `Editor/Common/CustomAttribute/` | `CustomMonoBehaviourAttributeEditor`、`CustomScriptableObjectAttributeEditor`、`ButtonAttributeEditor` | 运行时 `InsButton` 等属性对应的 Inspector 绘制器 |
| 独立窗口 | `Editor/Common/Window/` | `AssetFavoritesWindow`（资源收藏夹）、`AvatarDownloaderWindow`（头像下载）、`ScriptableObjectCreatorWindow` + `ScriptableObjectCreatorScanner`（SO 资产创建窗口：全程序集扫描非抽象 SO 子类，`CreatorEntryAttribute` 标注分组/显示名、未标注类型按程序集归入未分组、默认排除 `*.Tests` 测试程序集、Unity 官方程序集与 Editor/EditorWindow 派生，搜索/仅框架开关/已有资产 Ping/创建后 Select+Ping） | 独立工作流窗口（SceneJumper/SpriteToAnimation 已拆为独立模块包 `com.cielevel.axu-indie-*`） |
| Utility | `Editor/Common/Utility/` | `TerrainToMeshConverter`（466 行，最新功能）、`CsvIOUtil/CsvRowUnit`、`CsvContainerCreatorWindow`、`SequenceEditor`（AutoSequence 编辑器）、`ShapeDrawerEditor`、`TitleDrawerEditor`、`ScriptDefineSymbolSetter`（宏定义批量设置） | 转换器与批量工具 |
| UI Kit 绘制 | `Editor/Common/UI/Kit/` | `UIListViewEditor`、`UIBindParentEditor` | ListView / DataBind 的自定义 Inspector |

## 相关资源

- 与 KitBox / Editor 工具配套的 AI 技能文档在根目录 `.agent/skills/axu-ui-tool-kits-editor-skill/`（Axu Editor 工具的 UI Toolkit 样式规范：UE5 风格配色、组件样式、C# 代码模式）与 `.agent/skills/ui-tool-kits-skill/`（Unity UI Toolkit 通用参考）。
- 注意：`Runtime/Common` 内还有少量 Editor 专属命名空间文件（CustomAttribute.Editor、ListView.Editor、DataBind.Editor、Table.CSV.Editor），未单独 asmdef，随 Runtime 程序集编译——新增编辑器代码应优先放包根 `Editor/`。
- 独立编辑器小工具（SceneJumper / SelectionToCsv / SpriteToAnimation）已于 2026-08-29 拆为 `com.cielevel.axu-indie-*` 独立包（框架包内重复副本同日移除）；SelectionToCsv 在 KitBox 内仍有重写版页面（`TableGroup/SelectionToCsvRevPage`），二者并存、菜单不同。
