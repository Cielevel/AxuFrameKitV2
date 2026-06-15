# AxuFrameKit 移植评估：将 _Core 和 Common 迁移到 UPM Package

> **评估日期**: 2026-06-15
> **范围**: 将 `Assets/AxuFrameKit/_Core` 和 `Assets/AxuFrameKit/Common` 迁移到 `Packages/com.cielevel.axu-frame-kit/`
> **不涉及**: `Assets/AxuFrameKit/Modules` 和 `Assets/AxuFrameKit/KitDoc`

---

## 一、源与目标概况

### 源 (`Assets/AxuFrameKit/`)

| 目录 | .cs 文件数 | 特点 |
|---|---|---|
| `_Core/` | 86 | 纯 C#，仅 2 个文件引用 `UnityEngine`（Mathf、Color），零 MonoBehaviour/ScriptableObject，零 Editor 依赖 |
| `Common/` | ~170 | 137 Runtime + 33 Editor，大量 uGUI/TMP/Unity 依赖 |
| `Modules/` | 75 | 不迁移，但**依赖于 \_Core 和 Common** |
| `KitDoc/` | 6 | 不迁移（纯文档） |

- **程序集定义**: Assets/AxuFrameKit/ 内 **没有 .asmdef**，所有代码编译到 `Assembly-CSharp`

### 目标 (`Packages/com.cielevel.axu-frame-kit/`)

当前是一个最小空壳：
```
Packages/com.cielevel.axu-frame-kit/
├── package.json                    # 版本 0.1.0，无依赖声明
├── Runtime/
│   ├── AxuFrameKit.Runtime.asmdef  # 空程序集定义（无引用）
│   └── FrameKitTest.cs             # 占位测试文件
```

---

## 二、依赖关系

### 依赖方向（严格单向）

```
_Core  ←  Common  ←  Modules
(无外部依赖)  (依赖_Core)  (依赖_Core和Common)
```

- **_Core → Common**: **0 处引用**，_Core 完全独立
- **Common → _Core**: **33 处引用**，分布在 30+ 个文件中
- **_Core → Modules**: 0 处引用
- **Common → Modules**: 0 处引用
- **Modules → _Core**: 6 处引用
- **Modules → Common**: 15+ 处引用

### Common → _Core 的具体依赖

| _Core 类型 | Common 中使用次数 | 关键使用者 |
|---|---|---|
| `AxuFrameKit._Core.Utility.Math.Interpolation`（AxuTweener、EaseType） | 14 个文件 | 所有 AxuTweener\* 子类、AxuTweenRunner、AxuTweenExtension |
| `AxuFrameKit._Core.Advanced.ObjectPool`（SimpleObjPool） | 7 个文件 | GameObjectPools、定时器管理、Toast UI |
| `AxuFrameKit._Core.Extension`（扩展方法） | 4 个文件 | CSV 工具、HTTP 工具、编辑器工具 |
| `AxuFrameKit._Core.Enum`（WorldAxis） | 3 个文件 | TransformExtension、位置追踪器 |
| `AxuFrameKit._Core.Structure`（IndexedList） | 1 个文件 | UISideToast |
| `AxuFrameKit._Core.Utility.Encrypt`（加密） | 1 个文件 | SaveUtil |
| `AxuFrameKit._Core.Utility.RandomX` | 1 个文件 | AvatarDownloaderWindow |

---

## 三、操作清单（8 项）

### 操作 1：创建包内目录结构

迁移后的包结构：

```
Packages/com.cielevel.axu-frame-kit/
├── package.json
├── Runtime/
│   ├── _Core/                        ← 86 个文件从 Assets/AxuFrameKit/_Core/ 移入
│   │   ├── Advanced/
│   │   ├── DesignPattern/
│   │   ├── Enum/
│   │   ├── Extension/
│   │   ├── Structure/
│   │   └── Utility/
│   └── Common/                       ← 137 个 Runtime 文件从 Assets/AxuFrameKit/Common/ 移入
│       ├── CustomAttribute/          （不含 Editor/ 子目录）
│       ├── Extension/
│       ├── UI/
│       └── Utility/                  （不含各 Editor/ 子目录）
└── Editor/
    └── Common/                       ← 33 个 Editor 文件移入
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

需拆分的 Editor 目录：

| 源路径（Common/ 下） | 目标路径（Editor/ 下） | 文件数 |
|---|---|---|
| `CustomAttribute/Editor/` | `Common/CustomAttribute/` | 3 |
| `Editor/Hierarychy/Colorizer/` | `Common/Hierarchy/Colorizer/` | 3 |
| `Editor/Hierarychy/Icon/` | `Common/Hierarchy/Icon/` | 3 |
| `Editor/KitBoxRevV2/` | `Common/KitBoxRevV2/` | 9 |
| `Editor/Utility/` | `Common/Utility/` | 4 |
| `Editor/Window/` | `Common/Window/` | 4 |
| `UI/Kit/DataBind/Editor/` | `Common/UI/Kit/DataBind/` | 1 |
| `UI/Kit/ListView/Editor/` | `Common/UI/Kit/ListView/` | 1 |
| `Utility/Auto/AutoSequence/Editor/` | `Common/Utility/Auto/AutoSequence/` | 1 |
| `Utility/Table/CSV/Editor/` | `Common/Utility/Table/CSV/` | 1 |

### 操作 2：修正命名空间不一致（3 处，共 11 个文件）

**2a. TaskQ 文件 —— 物理位置与命名空间不匹配**

6 个文件位于 `_Core/Utility/TaskQ/`，但声明了命名空间 `AxuFrameKit.Common.Utility.TaskQ`：
- `ITaskQ.cs`、`ITaskQc.cs`、`TaskQ.cs`、`TaskQAuto.cs`、`TaskQc.cs`、`TaskQcAuto.cs`

**建议方案**：将这些文件**物理移动到 `Runtime/Common/Utility/TaskQ/`**（`Common/Utility/TaskQ/` 下已有 `CoroutineQ.cs` 属于同一体系）。同时修改其内部引用的 `using AxuFrameKit._Core.Utility.Common` 为已有的 `using` 路径。

**2b. Computed 文件 —— 命名空间缺少下划线**

4 个文件使用 `namespace AxuFrameKit.Core.Utility.Computed`（缺少下划线）：
- `IComputed.cs`、`IReadOnlyComputed.cs`、`Computed.cs`、`ComputedSource.cs`

**方案**：改为 `namespace AxuFrameKit._Core.Utility.Computed`，与其他所有 `_Core` 命名空间命名一致。同时检查所有引用此命名空间的地方（如有）一并更新。

**2c. DefaultInputPreset.cs 命名空间**

位于 `Common/Utility/Static/DefaultInputPreset.cs`，命名空间为 `AxuFrameKit.Common.Static`（缺少 `Utility` 段）。

**方案**：改为 `namespace AxuFrameKit.Common.Utility.Static`，与同目录下 `Type2Name.cs`、`Type2DefaultStrValue.cs`、`EnumeratorWait.cs` 保持一致。

### 操作 3：修复 6 个全局命名空间文件

以下文件**没有 `namespace` 声明**（位于全局命名空间），迁移到包后可能与用户代码产生命名冲突：

- `Common/Editor/Hierarychy/Colorizer/HierarchyColorizer.cs`
- `Common/Editor/Hierarychy/Colorizer/HierarchyColorizerConfig.cs`
- `Common/Editor/Hierarychy/Colorizer/HierarchyColorizerWindow.cs`
- `Common/Editor/Hierarychy/Icon/HierarchyIconConfig.cs`
- `Common/Editor/Hierarychy/Icon/HierarchyIconDrawer.cs`
- `Common/Editor/Hierarychy/Icon/HierarchyIconWindow.cs`

**方案**：添加命名空间 `namespace AxuFrameKit.Common.Editor.Hierarchy`。

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

**依赖分析依据**：

| Unity 包 | 使用文件数 | 典型用途 |
|---|---|---|
| `com.unity.ugui` | 31 Runtime 文件 | UIBindImage、UIBindSlider、UIBindDropdown、ListView、Scroll、ToggleBtnGroup |
| `com.unity.textmeshpro` | 8 Runtime 文件 | UIBindTmpText\*、TmpTypewriter、Type2Name |
| `com.unity.modules.video` | 1 Runtime 文件 | UIVideoKit |
| `com.unity.modules.unitywebrequest` | 4 Runtime 文件 | HttpClient、HttpUtil、Downloader |
| `com.unity.modules.animation` | 1 Editor 文件 | SpriteToAnimationWindow |
| `com.unity.modules.uielements` | 17 Editor 文件 | 所有使用 UI Toolkit（.uss/.uxml）的 EditorWindow |

> **注意**：Unity 内置模块（`com.unity.modules.*`）通常不需要显式声明在 dependencies 中，因为它们是 Unity 引擎的一部分。但如果包要发布到 UPM Registry，建议声明。

### 操作 6：关于 using 引用的兼容性

**好消息**：现有代码已使用完整的命名空间引用（如 `using AxuFrameKit._Core.Utility.Math.Interpolation`），只要程序集引用配置正确，**无需修改任何 `using` 语句**。

文件物理位置的变化不影响命名空间解析——代码使用命名空间查找类型，而非文件路径。

### 操作 7：处理非 .cs 资源文件

Common/ 中包含以下需随包迁移的非代码文件：

| 类型 | 文件 | 说明 |
|---|---|---|
| `.uss`（UI Toolkit 样式） | `Common/Editor/KitBoxRevV2/UI/AxuKitBoxV2.uss`、`AxuKitBoxV2_UE5Theme.uss`、`Common/Editor/Hierarychy/Colorizer/HierarchyColorizerWindow.uss`、`Common/Editor/Hierarychy/Icon/HierarchyIconWindow.uss` | 需移入 `Editor/Common/` 对应路径 |
| `.uxml`（UI Toolkit 布局） | `Common/Editor/KitBoxRevV2/UI/AxuKitBoxV2.uxml`、`Common/Editor/Hierarychy/Colorizer/HierarchyColorizerWindow.uxml`、`Common/Editor/Hierarychy/Icon/HierarchyIconWindow.uxml` | 需移入 `Editor/Common/` 对应路径 |
| `.md`（文档） | Common/ 下约 10 个 `HOWTOUSE.md`、`README.md` 等 | **建议排除**，不迁移到包中 |
| `.asset`（ScriptableObject） | 检查是否有 .asset 配置文件 | 如有，需处理 GUID 变更 |

### 操作 8：清理与验证

1. **删除** `Packages/com.cielevel.axu-frame-kit/Runtime/FrameKitTest.cs`（占位测试文件）
2. **保留** `Assets/AxuFrameKit/KitDoc/` 和 `Assets/AxuFrameKit/Modules/` 在原位置
3. **为 Modules/ 添加 .asmref**：Modules 依赖 `_Core` 和 `Common`，迁移后需通过 `.asmref` 文件引用包中程序集
4. **检查 Assembly-CSharp 中是否有其他代码引用 \_Core/Common 类型**：如果项目其他脚本直接使用了这些类型，需为它们添加对 `AxuFrameKit.Core` / `AxuFrameKit.Runtime` 的程序集引用
5. **在 Unity 中验证编译通过**

---

## 四、风险与注意事项

| # | 风险 | 等级 | 说明 | 缓解措施 |
|---|---|---|---|---|
| 1 | **Modules 引用断裂** | 🔴 高 | Modules/ 中有 6 处引用 `_Core`，15+ 处引用 `Common`。迁移后源文件夹清空，Assembly-CSharp 默认程序集中不再有这些类型 | 在 `Assets/AxuFrameKit/Modules/` 中添加 `.asmref` 指向包程序集；或在 Modules 根创建带 references 的 .asmdef |
| 2 | **Assembly-CSharp 中断** | 🔴 高 | 项目中任何未使用 asmdef 的脚本，如果引用了 `_Core`/`Common` 的类型，移除源文件后会立即编译失败 | 迁移前先全局搜索所有引用 AxuFrameKit 类型的调用方，逐一处添加 asmdef 或 asmref |
| 3 | **全局命名空间冲突** | 🟡 中 | 6 个 Hierarchy 文件无命名空间，包中全局命名空间的类型可能与用户代码冲突 | 操作 3 中已规划修复 |
| 4 | **ScriptableObject GUID 丢失** | 🟡 中 | EventChannel、GameObjectPools 等 .asset 实例通过 GUID 引用脚本，文件移动后 GUID 可能改变 | 迁移前导出 .asset 的脚本引用映射；或使用 Unity 的 AssetDatabase 重新绑定 |
| 5 | **package.json 依赖声明不完整** | 🟡 中 | `com.unity.modules.unitywebrequest`、`com.unity.modules.animation` 等如未声明，在特定 Unity 配置下可能不可用 | 操作 5 中已列出完整依赖列表 |
| 6 | **UI Toolkit 资源加载路径** | 🟢 低 | .uss/.uxml 在包内加载需通过包路径 `Packages/com.cielevel.axu-frame-kit/...`，当前代码可能使用相对路径 | 检查 EditorWindow 中的资源加载代码，替换为 `AssetDatabase` 或 `Resources.Load` 兼容方式 |
| 7 | **命名空间变更导致的连锁修改** | 🟡 中 | 操作 2 中修改了 Computed、TaskQ、DefaultInputPreset 的命名空间，所有引用方需同步更新 | 每个变更影响范围小（1-2 个消费者），但需全局搜索确认 |
| 8 | **.meta 文件关联** | 🟢 低 | 每个 .cs 文件的 .meta 中包含 GUID，移动时保留 .meta 可保持 GUID 不变 | 使用 Unity 的 `AssetDatabase.MoveAsset` 或在 Unity 编辑器内操作 |

---

## 五、工作量估算

| 操作 | 内容 | 文件数 | 预估耗时 | 自动化程度 |
|---|---|---|---|---|
| 操作 1 | 创建目录结构 + 移动文件 | ~290 个文件 | 30 分钟 | 可脚本化（Unity 编辑器脚本） |
| 操作 2 | 修正命名空间不一致 | 11 个文件 | 20 分钟 | 手动 |
| 操作 3 | 添加缺失命名空间 | 6 个文件 | 15 分钟 | 手动 |
| 操作 4 | 创建 3 个 .asmdef + .meta | 6 个文件 | 15 分钟 | 手动 |
| 操作 5 | 更新 package.json | 1 个文件 | 10 分钟 | 手动 |
| 操作 6 | 验证 using 兼容性 | 全局 | 15 分钟 | 全局搜索验证 |
| 操作 7 | 迁移非代码资源 | ~10 个文件 | 15 分钟 | 可脚本化 |
| 操作 8 | Modules .asmref + 验证编译 | 待定 | 30-60 分钟 | 手动 |
| **合计** | | | **约 2.5-3.5 小时** | |

---

## 六、建议执行顺序

```
第 1 步: 资产备份 + 创建 git 分支
    ↓
第 2 步: 操作 5（更新 package.json 依赖声明）
    ↓
第 3 步: 操作 4（创建 .asmdef，先 Core → Runtime → Editor）
    ↓
第 4 步: 操作 2 + 操作 3（修复命名空间，在源位置修改）
    ↓
第 5 步: 操作 1（移动文件到包目录，在 Unity 编辑器内操作以保持 .meta GUID）
    ↓
第 6 步: 操作 7（迁移非代码资源）
    ↓
第 7 步: 操作 6（编译验证，修复任何编译错误）
    ↓
第 8 步: 操作 8（处理 Modules 引用，清理占位文件）
    ↓
第 9 步: 全量编译验证 + 运行测试
```

> **推荐**：先迁移 `_Core`（最干净，依赖最少），编译通过后再迁移 `Common`，最后修复 `Modules`。分步进行，每一步都可独立验证。

---

## 七、迁移后目录对照

```
迁移前                                      迁移后
──────────────────────────────────────     ──────────────────────────────────────
Assets/AxuFrameKit/
├── _Core/           (86 files)     →     Packages/.../Runtime/_Core/
├── Common/                           →     Packages/.../Runtime/Common/ (137 files)
│   └── */Editor/                     →     Packages/.../Editor/Common/   (33 files)
├── Modules/                          →     保留（通过 .asmref 引用包）
├── KitDoc/                           →     保留
│
Packages/com.cielevel.axu-frame-kit/
├── Runtime/
│   ├── AxuFrameKit.Runtime.asmdef    →     替换为 Core.asmdef（在 _Core/ 中）
│   └── FrameKitTest.cs               →     删除
```
