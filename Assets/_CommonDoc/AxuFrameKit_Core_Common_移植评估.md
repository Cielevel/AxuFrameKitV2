# AxuFrameKit 移植评估：将 _Core 和 Common 迁移到 UPM Package

> **初始评估日期**: 2026-06-15
> **二次复核日期**: 2026-06-16
> **范围**: 将 `Assets/AxuFrameKit/_Core` 和 `Assets/AxuFrameKit/Common` 迁移到 `Packages/com.cielevel.axu-frame-kit/`
> **不涉及**: `Assets/AxuFrameKit/Modules` 和 `Assets/AxuFrameKit/KitDoc`

---

## 一、源与目标概况

### 源 (`Assets/AxuFrameKit/`)

| 目录 | .cs 文件数 | 特点 |
|---|---|---|
| `_Core/` | **80** | 纯 C#，仅 2 个文件引用 `UnityEngine`（`FloatExtension.cs` 使用 Mathf、`RandomNickname.cs` 使用 Debug/Random），零 MonoBehaviour/ScriptableObject，零 Editor 依赖 |
| `Common/` | **177** | **147 Runtime + 30 Editor**，大量 uGUI/TMP/Unity 依赖 |
| `Modules/` | **37** | 不迁移，但**依赖于 \_Core 和 Common**（引用全部集中在 LiveStream 子模块） |
| `KitDoc/` | 若干 | 不迁移（纯文档） |

- **程序集定义**: Assets/AxuFrameKit/ 内 **没有 .asmdef**，所有代码编译到 `Assembly-CSharp`
- **Assets/ 内其他目录也无 .asmdef**（含 TestAndSample、AxuExperimental 等）

### 目标 (`Packages/com.cielevel.axu-frame-kit/`)

当前是一个最小空壳：
```
Packages/com.cielevel.axu-frame-kit/
├── package.json                    # 版本 0.1.0，无依赖声明
├── Runtime/
│   ├── AxuFrameKit.Runtime.asmdef  # 空程序集定义（仅含 name 字段，无引用）
│   └── FrameKitTest.cs             # 占位测试文件（namespace Cielevel.AxuFrameKit）
```

---

## 二、依赖关系

### 依赖方向（严格单向）

```
_Core  ←  Common  ←  Modules
(无外部依赖)  (依赖_Core)  (依赖_Core和Common)
```

- **_Core → Common**: **0 处引用**，_Core 完全独立
- **Common → _Core**: **34 处 using 语句**，分布在 **30 个文件**中
- **_Core → Modules**: 0 处引用
- **Common → Modules**: 0 处引用
- **Modules → _Core**: **6 处 using 语句**，分布在 **5 个文件**中
- **Modules → Common**: **15 处 using 语句**，分布在 **10 个文件**中

> **注意**: Modules 中所有对 _Core/Common 的引用**全部来自 LiveStream 子模块**，AITools、GizmosUtil、Noise 无任何跨层引用。

### Common → _Core 的具体依赖

| _Core 类型 | Common 中使用次数 | 关键使用者 |
|---|---|---|
| `AxuFrameKit._Core.Utility.Math.Interpolation`（EaseType、IEase） | **15 个文件** | 所有 AxuTweener\* 子类、AxuTweenRunner、AxuTweenExtension、UISideToast、UIToastUnit |
| `AxuFrameKit._Core.Advanced.ObjectPool`（SimpleObjPool） | **8 个文件** | GameObjectPools 系列（3 个）、定时器管理（2 个）、MultitonAutoMonoR、AxuTweenRunner、UISideToast |
| `AxuFrameKit._Core.Extension`（扩展方法） | **4 个文件** | SimpleCsvTool、HttpUtil、SelectionToCsvRevPage、ScriptDefineSymbolSetter |
| `AxuFrameKit._Core.Enum`（WorldAxis） | **3 个文件** | TransformExtension、PositionTracker、SmoothPositionTracker |
| `AxuFrameKit._Core.Structure`（IndexedList） | **1 个文件** | UISideToast |
| `AxuFrameKit._Core.Utility.Encrypt`（加密） | **1 个文件** | SaveUtil（引用 Encrypt.Core + Encrypt.Symmetric 两个子命名空间） |
| `AxuFrameKit._Core.Utility.RandomX` | **1 个文件** | AvatarDownloaderWindow |

### 外部引用（Assets/ 中非 AxuFrameKit/ 的代码）

> ⚠️ **重要补充**：原评估未充分说明外部引用影响范围。

| 目录 | 引用文件数 | 引用目标 |
|---|---|---|
| `TestAndSample/` | **~41 个文件** | _Core 类型、Common 类型、Modules 类型 |
| `AxuExperimental/` | **2 个文件** | InGameConsoleManager → `Common.Utility.Singleton`；SubCellularGenerator → `Modules.Noise.FastNoiseLite` |
| `AxuIndieModules/` | **0 个文件** | 无直接 AxuFrameKit 引用（StatPipeline 有独立 .asmdef 但未引用 AxuFrameKit） |
| **合计** | **~43 个文件** | |

这些外部代码目前编译在 `Assembly-CSharp` 默认程序集中，迁移后需要为它们配置对包程序集的引用。

---

## 三、操作清单（9 项）

### 操作 1：创建包内目录结构

迁移后的包结构：

```
Packages/com.cielevel.axu-frame-kit/
├── package.json
├── Runtime/
│   ├── _Core/                        ← 80 个文件从 Assets/AxuFrameKit/_Core/ 移入
│   │   ├── Advanced/
│   │   ├── DesignPattern/
│   │   ├── Enum/
│   │   ├── Extension/
│   │   ├── Structure/
│   │   └── Utility/
│   └── Common/                       ← 147 个 Runtime 文件从 Assets/AxuFrameKit/Common/ 移入
│       ├── CustomAttribute/          （不含 Editor/ 子目录）
│       ├── Extension/
│       ├── UI/
│       └── Utility/                  （不含各 Editor/ 子目录）
└── Editor/
    └── Common/                       ← 30 个 Editor 文件移入
        ├── CustomAttribute/
        ├── Hierarchy/
        ├── KitBoxRevV2/
        ├── Utility/
        ├── Window/
        ├── UI/Kit/DataBind/
        ├── UI/Kit/ListView/
        ├── Utility/Auto/AutoSequence/
        └── Utility/Table/CSV/
```

**Editor 拆分规则**：所有位于 `Editor/` 子文件夹下的文件移入 `Editor/Common/`，保持相对路径后缀。

需拆分的 Editor 目录（经核实）：

| 源路径（Common/ 下） | 目标路径（Editor/Common/ 下） | .cs 文件数 | 非 .cs 文件 |
|---|---|---|---|
| `Editor/Hierarychy/Colorizer/` | `Hierarchy/Colorizer/` | 3 | 1 .uss + 1 .uxml |
| `Editor/Hierarychy/Icon/` | `Hierarchy/Icon/` | 3 | 1 .uss + 1 .uxml |
| `Editor/KitBoxRevV2/` | `KitBoxRevV2/` | 9 | 2 .uss + 1 .uxml（在 UI/ 子目录） |
| `Editor/Utility/` (+IO子目录) | `Utility/` | 4 | 0 |
| `Editor/Window/` | `Window/` | 4 | 1 .uss (CommonWindowStyles.uss) |
| `CustomAttribute/Editor/` | `CustomAttribute/` | 3 | 0 |
| `UI/Kit/DataBind/Editor/` | `UI/Kit/DataBind/` | 1 | 0 |
| `UI/Kit/ListView/Editor/` | `UI/Kit/ListView/` | 1 | 0 |
| `Utility/Auto/AutoSequence/Editor/` | `Utility/Auto/AutoSequence/` | 1 | 0 |
| `Utility/Table/CSV/Editor/` | `Utility/Table/CSV/` | 1 | 1 .uss |

### 操作 2：修正命名空间不一致（2 处，共 5 个文件）

**2a. Computed 文件 —— 命名空间缺少下划线**

4 个文件位于 `_Core/Utility/Computed/`，使用 `namespace AxuFrameKit.Core.Utility.Computed`（缺少下划线）：
- `IComputed.cs`、`IReadOnlyComputed.cs`、`Computed.cs`、`ComputedSource.cs`

**方案**：改为 `namespace AxuFrameKit._Core.Utility.Computed`，与所有其他 `_Core` 命名空间保持一致。同时更新所有引用此命名空间的地方（已知 `TestAndSample/Sample_Core/Utility/Computed/Sample_Computed.cs` 使用了旧命名空间）。

**2b. DefaultInputPreset.cs 命名空间**

位于 `Common/Utility/Static/DefaultInputPreset.cs`，命名空间为 `AxuFrameKit.Common.Static`（缺少 `Utility` 段）。

**方案**：改为 `namespace AxuFrameKit.Common.Utility.Static`，与同目录下 `Type2Name.cs`、`Type2DefaultStrValue.cs`、`EnumeratorWait.cs` 保持一致。

> **关于 TaskQ 的说明**：经核实，`_Core/Utility/TaskQ/` 下 6 个文件的命名空间实际上是 `AxuFrameKit._Core.Utility.TaskQ`（正确包含下划线），与其物理位置一致，**无需修改**。原评估中声称它们使用 `AxuFrameKit.Common.Utility.TaskQ` 是不准确的。

### 操作 3：修复命名空间问题文件（7 个文件）

**3a. 6 个全局命名空间文件**

以下文件**没有 `namespace` 声明**（位于全局命名空间），迁移到包后可能与用户代码产生命名冲突：

- `Common/Editor/Hierarychy/Colorizer/HierarchyColorizer.cs`
- `Common/Editor/Hierarychy/Colorizer/HierarchyColorizerConfig.cs`
- `Common/Editor/Hierarychy/Colorizer/HierarchyColorizerWindow.cs`
- `Common/Editor/Hierarychy/Icon/HierarchyIconConfig.cs`
- `Common/Editor/Hierarychy/Icon/HierarchyIconDrawer.cs`
- `Common/Editor/Hierarychy/Icon/HierarchyIconWindow.cs`

**方案**：添加命名空间 `namespace AxuFrameKit.Common.Editor.Hierarchy`。

**3b. 非标准命名空间文件**

`Common/Utility/2D/EdgeColliderShaped/EdgeColliderCircleShape.cs` 使用了非标准命名空间 `AxU_NextGen.Common._2D.EdgeColliderShaped`，不符合 `AxuFrameKit.Common.*` 的命名规范。

**方案**：改为 `namespace AxuFrameKit.Common.Utility._2D.EdgeColliderShaped`。如果该功能已废弃，也可考虑不迁移此文件。

### 操作 4：创建 3 个程序集定义

当前只有 1 个空的 `AxuFrameKit.Runtime.asmdef`，需拆分为：

| .asmdef | 位置 | 包含范围 | 依赖 |
|---|---|---|---|
| **AxuFrameKit.Core** | `Runtime/_Core/` | 所有 `_Core/` .cs 文件 | `UnityEngine`（仅 FloatExtension、RandomNickname 使用） |
| **AxuFrameKit.Runtime** | `Runtime/Common/` | 所有 Common Runtime .cs 文件 | `AxuFrameKit.Core`、`UnityEngine.UI`、`Unity.TextMeshPro`、`UnityEngine.Video`、`UnityEngine.EventSystems`、`UnityEngine.UnityWebRequestModule` |
| **AxuFrameKit.Editor** | `Editor/` | 所有 Editor .cs 文件 | `AxuFrameKit.Core`、`AxuFrameKit.Runtime`、`UnityEditor` |

**关键配置**：
- 三个 `.asmdef` 都设置 `"autoReferenced": false`，防止 `Assembly-CSharp` 自动引用（UPM 包默认行为）
- `AxuFrameKit.Core.asmdef` 的 `references` 数组为空（仅隐式引用 UnityEngine）
- `AxuFrameKit.Runtime.asmdef` 需要明确列出 `"references": ["AxuFrameKit.Core"]`
- `AxuFrameKit.Editor.asmdef` 的 `"references": ["AxuFrameKit.Core", "AxuFrameKit.Runtime"]`，并设置 `"includePlatforms": ["Editor"]`

> **注意**: `autoReferenced` 设为 `false` 意味着 `Assembly-CSharp` 不会自动引用这些程序集。需要使用包内类型的外部代码（如 Modules/、TestAndSample/）必须通过 `.asmdef` 或 `.asmref` 显式引用。

### 操作 5：更新 package.json

需要补充 Unity 包依赖声明：

```json
{
  "name": "com.cielevel.axu-frame-kit",
  "version": "0.1.0",
  "displayName": "AxuFrameKit",
  "description": "AxuFrameKit by Cielevel",
  "unity": "6000.0",
  "dependencies": {
    "com.unity.ugui": "1.0.0",
    "com.unity.textmeshpro": "3.0.0"
  }
}
```

**依赖分析依据**（经核实）：

| Unity 包 | Runtime 使用文件数 | Editor 使用文件数 | 典型用途 |
|---|---|---|---|
| `com.unity.ugui` | ~30+ | — | UIBind 系列、ListView、Scroll、ToggleBtnGroup、Toast |
| `com.unity.textmeshpro` | **6** | — | UIBindTmpText\* (3)、UIBindTmpInputField (1)、TmpTypewriter (1)、TmpTypewriterManager (1) |
| `com.unity.modules.video` | **1** | — | UIVideoKit |
| `com.unity.modules.unitywebrequest` | **4** | 1 | HttpUtil、HttpClient、HttpResult、Downloader (Runtime)；AvatarDownloaderWindow (Editor) |
| `com.unity.modules.animation` | — | 1 | SpriteToAnimationWindow |
| `com.unity.modules.uielements` | — | ~10+ | 所有使用 UI Toolkit（.uss/.uxml）的 EditorWindow |

> **注意**：Unity 内置模块（`com.unity.modules.*`）通常不需要显式声明在 dependencies 中，因为它们是 Unity 引擎的一部分。但如果包要发布到 UPM Registry，建议声明。

### 操作 6：关于 using 引用的兼容性

**好消息**：现有代码已使用完整的命名空间引用（如 `using AxuFrameKit._Core.Utility.Math.Interpolation`），只要程序集引用配置正确，**无需修改任何 `using` 语句**（操作 2/3 中修正的命名空间除外）。

文件物理位置的变化不影响命名空间解析——代码使用命名空间查找类型，而非文件路径。

### 操作 7：处理非 .cs 资源文件

Common/ 中包含以下需随包迁移的非代码文件（经完整核查）：

| 类型 | 文件数 | 具体文件 | 迁移目标 |
|---|---|---|---|
| `.uss`（UI Toolkit 样式） | **6** | `Editor/Hierarychy/Colorizer/HierarchyColorizerWindow.uss`、`Editor/Hierarychy/Icon/HierarchyIconWindow.uss`、`Editor/KitBoxRevV2/UI/AxuKitBoxV2.uss`、`Editor/KitBoxRevV2/UI/AxuKitBoxV2_UE5Theme.uss`、`Editor/Window/CommonWindowStyles.uss`、`Utility/Table/CSV/Editor/CsvContainerCreatorWindow.uss` | 移入 `Editor/Common/` 对应路径 |
| `.uxml`（UI Toolkit 布局） | **3** | `Editor/Hierarychy/Colorizer/HierarchyColorizerWindow.uxml`、`Editor/Hierarychy/Icon/HierarchyIconWindow.uxml`、`Editor/KitBoxRevV2/UI/AxuKitBoxV2.uxml` | 移入 `Editor/Common/` 对应路径 |
| `.md`（文档） | **~15** | 分布在 UI/Kit/DOC/、Utility/*/README.md 等 | **建议不迁移**到包中，保留在原位或归入 KitDoc/ |
| `.DS_Store` | **2** | macOS 系统文件 | **不迁移**，应在 .gitignore 中排除 |

> **注意**：相比原评估，非 .cs 资源文件的列表已经过完整扫描核实，不存在遗漏。无 .asset 配置文件、无 .png 图片文件。

### 操作 8：处理外部引用

> ⚠️ **新增操作**：原评估中此部分内容不充分。

迁移后，以下外部目录中的代码会编译失败（因为 _Core/Common 类型从 `Assembly-CSharp` 移走）：

**8a. `Assets/AxuFrameKit/Modules/`（37 个 .cs 文件）**
- 5 个文件引用 `_Core`，10 个文件引用 `Common`
- **方案**: 在 `Modules/` 根目录创建 `.asmdef`（如 `AxuFrameKit.Modules.asmdef`），references 中添加 `AxuFrameKit.Core` 和 `AxuFrameKit.Runtime`

**8b. `Assets/TestAndSample/`（~41 个文件）**
- 引用 `_Core`、`Common`、`Modules` 的类型
- **方案**: 在 `TestAndSample/` 根目录创建 `.asmdef`（如 `AxuFrameKit.TestAndSample.asmdef`），references 中添加 `AxuFrameKit.Core`、`AxuFrameKit.Runtime`、`AxuFrameKit.Modules`

**8c. `Assets/AxuExperimental/`（2 个文件）**
- `InGameConsoleManager.cs` → `AxuFrameKit.Common.Utility.Singleton`
- `SubCellularGenerator.cs` → `AxuFrameKit.Modules.Noise.FastNoiseLite`
- **方案**: 在 `AxuExperimental/` 创建 `.asmdef` 或为这两个文件单独添加 `.asmref`

### 操作 9：清理与验证

1. **删除** `Packages/com.cielevel.axu-frame-kit/Runtime/FrameKitTest.cs`（占位测试文件）
2. **保留** `Assets/AxuFrameKit/KitDoc/` 和 `Assets/AxuFrameKit/Modules/` 在原位置
3. **在 Unity 中验证编译通过**
4. **检查 UI Toolkit 资源加载路径**：迁移后 `.uss`/`.uxml` 路径变为 `Packages/com.cielevel.axu-frame-kit/...`，需更新 EditorWindow 中的资源加载代码

---

## 四、风险与注意事项

| # | 风险 | 等级 | 说明 | 缓解措施 |
|---|---|---|---|---|
| 1 | **外部代码引用断裂** | 🔴 高 | **43 个外部文件**（TestAndSample ~41、AxuExperimental 2）引用了 AxuFrameKit 类型。迁移后源文件夹清空，Assembly-CSharp 默认程序集中不再有这些类型 | 操作 8 中已规划：为每个外部目录创建 .asmdef 并配置引用 |
| 2 | **Modules 引用断裂** | 🔴 高 | Modules/ 中有 6 处引用 `_Core`，15 处引用 `Common`（全部在 LiveStream 子模块）。迁移后这些引用失效 | 操作 8a 中已规划：在 Modules/ 创建 .asmdef |
| 3 | **Assembly-CSharp 中断** | 🔴 高 | 项目中任何未使用 asmdef 的脚本，如果引用了 `_Core`/`Common` 的类型，移除源文件后会立即编译失败 | 迁移前先全局搜索所有引用 AxuFrameKit 类型的调用方，逐一处添加 asmdef |
| 4 | **全局命名空间冲突** | 🟡 中 | 6 个 Hierarchy 文件无命名空间，包中全局命名空间的类型可能与用户代码冲突 | 操作 3a 中已规划修复 |
| 5 | **非标准命名空间** | 🟡 中 | `EdgeColliderCircleShape.cs` 使用 `AxU_NextGen` 前缀命名空间，不符合 `AxuFrameKit` 规范 | 操作 3b 中已规划修复 |
| 6 | **ScriptableObject GUID 丢失** | 🟡 中 | Common 中存在继承 ScriptableObject 的类型（EventChannel、GameObjectPools、CsvContainerSo、CustomScriptableObject 等），关联的 .asset 实例通过 GUID 引用脚本，文件移动后 GUID 可能改变 | 迁移时使用 Unity 的 `AssetDatabase.MoveAsset` 以保留 .meta GUID |
| 7 | **Computed 命名空间变更的连锁修改** | 🟡 中 | 修改 4 个 Computed 文件的命名空间（添加下划线），所有引用方需同步更新。已知 `TestAndSample/Sample_Core/Utility/Computed/Sample_Computed.cs` 使用了旧命名空间 `AxuFrameKit.Core.Utility.Computed` | 全局搜索 `AxuFrameKit.Core.Utility.Computed`，逐一更新 |
| 8 | **package.json 依赖声明不完整** | 🟡 中 | `com.unity.modules.unitywebrequest`、`com.unity.modules.animation` 等如未声明，在特定 Unity 配置下可能不可用 | 操作 5 中已列出完整依赖列表 |
| 9 | **UI Toolkit 资源加载路径** | 🟡 中 | .uss/.uxml 在包内加载需通过包路径 `Packages/com.cielevel.axu-frame-kit/...`，当前代码可能使用相对路径或硬编码路径 | 检查所有 EditorWindow 中的资源加载代码（`AssetDatabase.LoadAssetAtPath` 等），替换路径前缀 |
| 10 | **.meta 文件关联** | 🟢 低 | 每个 .cs 文件的 .meta 中包含 GUID，移动时保留 .meta 可保持 GUID 不变 | 使用 Unity 的 `AssetDatabase.MoveAsset` 或在 Unity 编辑器内操作 |

---

## 五、工作量估算

| 操作 | 内容 | 文件数 | 预估耗时 | 自动化程度 |
|---|---|---|---|---|
| 操作 1 | 创建目录结构 + 移动文件 | ~266 个文件（80 Core + 177 Common + 9 资源） | 30 分钟 | 可脚本化（Unity 编辑器脚本） |
| 操作 2 | 修正命名空间不一致 | 5 个文件 | 10 分钟 | 手动 |
| 操作 3 | 添加/修复命名空间 | 7 个文件 | 15 分钟 | 手动 |
| 操作 4 | 创建 3 个 .asmdef + .meta | 6 个文件 | 15 分钟 | 手动 |
| 操作 5 | 更新 package.json | 1 个文件 | 10 分钟 | 手动 |
| 操作 6 | 验证 using 兼容性 | 全局 | 15 分钟 | 全局搜索验证 |
| 操作 7 | 迁移非代码资源 | ~9 个文件 | 10 分钟 | 可脚本化 |
| 操作 8 | 外部引用修复（Modules + TestAndSample + Experimental 的 .asmdef） | ~80+ 个文件受影响 | 45-60 分钟 | 半自动（创建 .asmdef + 全局验证） |
| 操作 9 | 清理占位文件 + UI Toolkit 路径修复 + 编译验证 | 待定 | 30-45 分钟 | 手动 |
| **合计** | | | **约 3-4 小时** | |

---

## 六、建议执行顺序

```
第 1 步: 资产备份 + 创建 git 分支
    ↓
第 2 步: 操作 2 + 操作 3（修复命名空间，在源位置修改 — 最低风险操作）
    ↓
第 3 步: 操作 5（更新 package.json 依赖声明）
    ↓
第 4 步: 操作 4（创建 .asmdef，先 Core → Runtime → Editor）
    ↓
第 5 步: 操作 1（移动文件到包目录，在 Unity 编辑器内操作以保持 .meta GUID）
    ↓
第 6 步: 操作 7（迁移非代码资源 .uss/.uxml）
    ↓
第 7 步: 操作 9 部分 — 修复 UI Toolkit 资源加载路径
    ↓
第 8 步: 操作 8（处理外部引用：Modules .asmdef → TestAndSample .asmdef → AxuExperimental 处理）
    ↓
第 9 步: 操作 6 + 操作 9 剩余 — 全量编译验证 + 运行测试 + 清理占位文件
```

> **推荐**：先迁移 `_Core`（最干净，依赖最少），编译通过后再迁移 `Common`，最后修复 `Modules` 和外部引用。分步进行，每一步都可独立验证。

---

## 七、迁移后目录对照

```
迁移前                                      迁移后
──────────────────────────────────────     ──────────────────────────────────────
Assets/AxuFrameKit/
├── _Core/           (80 files)      →     Packages/.../Runtime/_Core/
├── Common/                           →     Packages/.../Runtime/Common/ (147 files)
│   └── */Editor/                     →     Packages/.../Editor/Common/   (30 files)
├── Modules/                          →     保留（创建 .asmdef 引用包程序集）
├── KitDoc/                           →     保留
│
Assets/TestAndSample/                 →     保留（创建 .asmdef 引用包程序集）
Assets/AxuExperimental/               →     保留（创建 .asmdef 或 .asmref）
│
Packages/com.cielevel.axu-frame-kit/
├── Runtime/
│   ├── _Core/AxuFrameKit.Core.asmdef       新建
│   └── Common/AxuFrameKit.Runtime.asmdef   替换原空壳
├── Editor/
│   └── Common/AxuFrameKit.Editor.asmdef    新建
└── FrameKitTest.cs                   →     删除
```

---

## 八、二次评估变更记录

以下是相对于初始评估（2026-06-15）的主要修正：

| 项目 | 初始评估 | 修正后 | 说明 |
|---|---|---|---|
| _Core .cs 文件数 | 86 | **80** | 实际扫描核实 |
| Common 文件数 | ~170 (137R + 33E) | **177 (147R + 30E)** | 实际扫描核实 |
| Modules .cs 文件数 | 75 | **37** | 原数据严重偏高 |
| TaskQ 命名空间问题 | 声称使用 `Common.Utility.TaskQ` 需修复 | **实际已使用 `_Core.Utility.TaskQ`，无需修改** | 原评估描述有误，已删除此操作项 |
| Common→_Core 引用数 | 33 处 / 30+ 文件 | **34 处 / 30 文件** | 基本吻合，微调 |
| Modules→_Core | 6 处 | **6 处 / 5 文件** | 吻合 |
| Modules→Common | 15+ 处 | **15 处 / 10 文件** | 吻合 |
| TMP 引用文件数 | 8 | **6** | 实际核实 |
| Editor 目录文件数表 | 多处不准确 | **已全面修正** | 原评估中 Editor/Window 等目录的文件数不准确 |
| 非 .cs 资源 | 列举不完整 | **已完整列出** | 补充了 CommonWindowStyles.uss 等遗漏文件 |
| 外部引用影响范围 | 仅简要提及 | **新增操作 8** | 43 个外部文件需处理，原评估严重低估 |
| 非标准命名空间 | 未提及 | **新增操作 3b** | EdgeColliderCircleShape.cs 使用 `AxU_NextGen` 命名空间 |
| 总工作量估算 | 2.5-3.5 小时 | **3-4 小时** | 增加了外部引用处理的工时 |
