# 02 · 嵌入式包 com.cielevel.axu-frame-kit 结构

> 包路径：`Packages/com.cielevel.axu-frame-kit/`。勘察时间：2026-08-29。

## 1. 包元信息（package.json）

| 项 | 值 |
|---|---|
| name | `com.cielevel.axu-frame-kit` |
| version | 0.1.0 |
| displayName | AxuFrameKit |
| description | AxuFrameKit by Cielevel |
| unity | 6000.0 |
| dependencies | com.unity.ugui: 1.0.0 · com.unity.textmeshpro: 3.0.0 · com.unity.nuget.newtonsoft-json: 3.2.2 |

包历史：06-12（c5eadc8）首建空壳 → 06-16（5277cdd）随迁移补齐依赖，版本号至今未变，无 CHANGELOG。

## 2. 程序集定义（3 个 .asmdef）

依赖链：**`AxuFrameKit.Core` ← `AxuFrameKit.Runtime` ← `AxuFrameKit.Editor`**

| asmdef | 路径 | rootNamespace | 引用 | 平台约束 | autoReferenced |
|---|---|---|---|---|---|
| AxuFrameKit.Core | `Runtime/_Core/AxuFrameKit.Core.asmdef` | `AxuFrameKit._Core` | 无（零依赖，纯 C#） | 全平台 | true |
| AxuFrameKit.Runtime | `Runtime/Common/AxuFrameKit.Runtime.asmdef` | `AxuFrameKit.Common` | AxuFrameKit.Core、UnityEngine.UI(ugui)、Unity.TextMeshPro | 全平台 | true |
| AxuFrameKit.Editor | `Editor/AxuFrameKit.Editor.asmdef` | `AxuFrameKit.Common.Editor` | AxuFrameKit.Core、AxuFrameKit.Runtime | **仅 Editor**（includePlatforms: Editor） | false（工具箱窗口，需手动引用/using） |

## 3. 顶层目录树（3 层深度）

```
com.cielevel.axu-frame-kit/
├── package.json
├── Editor/                          [31 cs] 编辑器专用（唯一 Editor asmdef）
│   └── Common/
│       ├── CustomAttribute/         CustomMonoBehaviour/CustomScriptableObject/Button 属性的 Inspector 绘制器
│       ├── Hierarchy/               Hierarchy 增强：Colorizer（规则着色）、Icon（规则图标）+ 各自 Config/Window
│       ├── KitBoxRevV2/             "AxuKitBox" 主工具箱窗口（纯 UI Toolkit），Core/Groups/UI
│       ├── UI/Kit/                  UIListView、UIBindParent 自定义 Inspector
│       ├── Utility/                 AutoSequence 编辑器、CSV 容器创建窗口、TerrainToMesh 转换器、CsvIOUtil、ShapeDrawer/TitleDrawer
│       └── Window/                  独立 EditorWindow：AssetFavorites、AvatarDownloader、ScriptableObjectCreator（SO 资产创建，含 Scanner）（SceneJumper/SpriteToAnimation 已拆为独立模块包）
└── Runtime/                         [231 cs] 运行时（两个程序集）
    ├── _Core/                       [80 cs] 纯 C# 核心库（详见 03-core-modules.md）
    │   ├── Advanced/                BetterFSM、FSM(泛型)、EventSystem、ObjectPool
    │   ├── DesignPattern/           Command、Memento、ChainOfResponsibility、Singleton
    │   ├── Enum/                    WorldAxis 等通用枚举+扩展
    │   ├── Extension/               String/Int/Float/DateTime/Collection/Enum/Task/Type/Reflection 扩展
    │   ├── Structure/               IndexedList、IndexedMultiList、BiDictionary、PriorityQueue、CircularBuffer、ReactiveProperty
    │   └── Utility/                 Common(Logger/Number)、Computed、Encrypt、Math(Interpolation/Ease)、Misc、Multiton、RandomX、TaskQ
    └── Common/                      [151 cs] Unity 运行时组件库（详见 04-common-modules.md）
        ├── CustomAttribute/         CustomMonoBehaviour、CustomScriptableObject 基类 + Inspector 属性（含 CreatorEntry：SO 创建窗口分组/显示名标注）
        ├── Extension/               Vector/Transform/Color/GameObject/RectTransform/AsyncOperation 扩展
        ├── UI/                      Framework(页面框架)、Kit(DataBind/ListView/Group/Scroll/Toast/ToggleBtnGroup/Typewriter/Video)
        └── Utility/                 Analysis、2D、Auto、Event、Math(Tween)、Misc、Multiton、Net、ObjectPool、Raycast、Res、Save、Serializer、Singleton、Static、Table(CSV)、TaskQ、Timer、Trigger、UseMono
```

## 4. 代码规模

| 范围 | .cs 文件数 |
|---|---|
| 包总计 | **262** |
| Editor（AxuFrameKit.Editor） | 29（2026-08-29 移除 2 个与独立包重复的窗口后） |
| Runtime/Common（AxuFrameKit.Runtime） | 151 |
| Runtime/_Core（AxuFrameKit.Core） | 80 |
| 包内 Tests | **0**（无任何测试） |

- 共 108 个目录；无第三方 DLL。2026-08-29 起包内含 27 个 .md（25 份模块文档自 Assets 迁入 + 包根 README/CHANGELOG），文档随代码放置，索引见包根 README.md。
- 注意：`Runtime/Common` 内存在少量 Editor 专属命名空间文件（如 `AxuFrameKit.Common.CustomAttribute.Editor`、`Common.UI.Kit.ListView.Editor`、`Common.UI.Kit.DataBind.Editor`、`Table.CSV.Editor`），**未单独 asmdef 隔离**，随 Runtime 程序集编译（依赖 `#if UNITY_EDITOR`）——新加编辑器代码应优先放包根 `Editor/` 目录。
- 项目侧测试/示例在包外：`Assets/TestAndSample/`（见 06 号文档）。

## 5. 与 Assets 的镜像关系（迁移后现状）

`Assets/AxuFrameKit/` 与包内 Runtime 结构**同名镜像**，但分工不同：

| 目录 | 内容 |
|---|---|
| `Assets/AxuFrameKit/`、`Assets/AxuIndieModules/` | **已于 2026-08-29 整体删除**——Modules 与独立模块拆为 8 个独立嵌入式包（见下） |

**模块包清单（2026-08-29 拆包）**：`com.cielevel.axu-module-{aitools,gizmos-util,livestream,noise}`（模块家族，仅 LiveStream 依赖框架包）+
`com.cielevel.axu-indie-{scene-jumper,selection-to-csv,sprite-to-animation,stat-pipeline}`（零依赖独立工具）；
各包含 package.json/README/CHANGELOG/独立 asmdef，示例在 `Samples~/`。

即当前完整分层：包内 `_Core`（纯 C#）→ 包内 `Common`（Unity 运行时/UI）→ 8 个模块包，Editor 工具层在框架包内 `Editor/`。
