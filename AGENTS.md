# AxuFrameKit Project Guide

> 本文档供 AI Coding Agent 阅读，用于快速了解本项目架构与开发规范。
> （2026-08-31 更新：新增 AxuTestFramework 通用测试体系与 axu_test_* pipeline 命令；2026-08-29 已同步框架迁移到 UPM 嵌入式包后的现状；详细模块清单见 `.zcode/` 知识库）

## 项目概览

**AxuFrameKit** (阿修框架工具包) 是一个 Unity 应用程序框架，旨在提供清晰的架构和可复用的组件。项目采用分层架构设计，确保模块间的低耦合与高内聚。

- **项目类型**: Unity 应用程序框架（框架本体以嵌入式 UPM 包 `com.cielevel.axu-frame-kit` 随项目开发）
- **Unity 版本**: 6000.0.23f1 (Unity 6)
- **渲染管线**: URP (Universal Render Pipeline 17.0.3，全局管线资产 `Assets/URP/Settings/PC_RPAsset.asset`)
- **主要语言**: C# (LangVersion 9.0)
- **API 兼容级别**: .NET Standard

---

## 架构设计

项目采用**三层架构**，依赖关系如下：

```
Modules (功能模块) ──────► Common (通用模块) ──────► _Core (核心模块)
     │                        │                        │
     └────────────────────────┴────────────────────────┘
                          均依赖
```

**重要**：`_Core` 与 `Common`（含 Editor 工具层）已于 2026-06 迁入嵌入式包 `Packages/com.cielevel.axu-frame-kit/`；
模块层也已全部拆分为独立嵌入式包（见第 4 节），Assets 中不再有框架代码。

### 程序集定义 (asmdef)

| 程序集 | 路径 | 命名空间根 | 依赖 | 说明 |
|---|---|---|---|---|
| `AxuFrameKit.Core` | `Runtime/_Core/AxuFrameKit.Core.asmdef` | `AxuFrameKit._Core` | 无（纯 C# 零依赖） | 全平台 |
| `AxuFrameKit.Runtime` | `Runtime/Common/AxuFrameKit.Runtime.asmdef` | `AxuFrameKit.Common` | AxuFrameKit.Core、UnityEngine.UI、Unity.TextMeshPro | 全平台 |
| `AxuFrameKit.Editor` | `Editor/AxuFrameKit.Editor.asmdef` | `AxuFrameKit.Common.Editor` | AxuFrameKit.Core、AxuFrameKit.Runtime | 仅 Editor，`autoReferenced: false` |

依赖链：`AxuFrameKit.Core` ← `AxuFrameKit.Runtime` ← `AxuFrameKit.Editor`。
新写的 Editor 代码放包根 `Editor/` 目录（勿放进 Runtime，Runtime 内仅历史遗留少量 `#if UNITY_EDITOR` 保护的 Editor 命名空间文件）。

### 1. _Core (核心层)

**定位**: 框架的核心基础模块

**特点**: 
- 纯 C# 实现，**无任何 Unity 引擎依赖**
- 包含通用的工具类、数据结构、设计模式实现以及高级架构组件
- 可在非 Unity 环境中使用

**目录结构**:
```
Packages/com.cielevel.axu-frame-kit/Runtime/_Core/
├── Advanced/           # 高级架构组件
│   ├── BetterFSM/     # 改进版有限状态机（委托/谓词驱动）
│   ├── FSM/           # 泛型有限状态机（另一套实现）
│   ├── EventSystem/   # 类型安全的事件中心 (EventCenter)
│   └── ObjectPool/    # 通用 C# 对象池 (SimpleObjPool, TypeObjPools)
├── DesignPattern/     # 设计模式实现
│   ├── ChainOfResponsibility.cs
│   ├── Command.cs
│   ├── Memento.cs
│   └── Singleton.cs
├── Enum/              # 通用枚举 (WorldAxis 等)
├── Extension/         # 扩展方法（9 个文件）
│   ├── StringExtension.cs
│   ├── IntExtension.cs / FloatExtension.cs / DateTimeExtension.cs
│   ├── CollectionExtension.cs
│   ├── EnumExtensions.cs / TaskExtension.cs
│   └── TypeExtension.cs / ReflectionExtension.cs
├── Structure/         # 数据结构
│   ├── BiDictionary.cs      # 双向字典
│   ├── CircularBuffer.cs    # 环形缓冲区
│   ├── IndexedList.cs / IndexedMultiList.cs  # 索引列表
│   ├── PriorityQueue.cs     # 优先级队列
│   └── ReactiveProperty.cs  # 响应式属性
└── Utility/           # 实用工具
    ├── Common/        # 通用工具 (Logger, Number)
    ├── Computed/      # 响应式派生值 (Computed<T>，类 Vue computed)
    ├── Encrypt/       # 完整加密工具集 (AES/RSA/MD5/SHA/PBKDF2/HMAC/Base64/Hex)
    ├── Math/          # 数学工具 (Interpolation/Ease 全套，引擎无关缓动基座)
    ├── Misc/          # 杂项工具 (PingPongNum)
    ├── Multiton/      # 多例基类
    ├── RandomX/       # 随机数工具增强 (AliasSampler, RandomId 等)
    └── TaskQ/         # 无 Mono 异步任务队列 (TaskQ/TaskQAuto/TaskQc)
```

**命名空间规范**: `AxuFrameKit._Core.{子模块名}`

### 2. Common (通用层)

**定位**: 基于 Unity 引擎的通用组件与工具

**特点**:
- 依赖 Unity API 与 `_Core` 层（AxuFrameKit.Core）

**目录结构**:
```
Packages/com.cielevel.axu-frame-kit/Runtime/Common/
├── CustomAttribute/   # 自定义基类与 Inspector 属性
│                      #   (CustomMonoBehaviour, CustomScriptableObject, InsButton/InspectorIntro/InspectorTitle/CreatorEntry)
├── Extension/         # Unity 类型扩展
│                      #   (Transform/Vector/Color/GameObject/RectTransform/AsyncOperation 扩展)
├── UI/                # UI 框架
│   ├── Framework/     # UI 基础框架 (UIBaseManager → Area → Panel → Page)
│   └── Kit/           # UI 组件套件
│       ├── DataBind/       # 数据绑定组件 (UIBind* 系列，约 19 个)
│       ├── ListView/       # 虚拟化列表 (UIListView + 可插拔布局引擎)
│       ├── Group/          # 图片/精灵分组
│       ├── Scroll/         # 循环滚动视图
│       ├── Toast/          # 消息提示
│       ├── ToggleBtnGroup/ # 切换按钮组
│       ├── Typewriter/     # TMP 打字机
│       └── Video/          # 视频播放套件
└── Utility/           # 通用工具
    ├── 2D/            # 2D 工具
    ├── Analysis/      # 分析工具 (PerformanceMonitor, Debugger)
    ├── Auto/          # 自动化组件（零代码组件集）
    │   ├── AutoCamera/     # 相机面向
    │   ├── AutoDestroy/    # 自动销毁
    │   ├── AutoSequence/   # 序列播放器
    │   ├── AutoSnapshot/   # 快照录制/播放
    │   ├── AutoTracker/    # 位置追踪
    │   └── AutoEventChannel/ # 事件通道触发
    ├── Event/         # 事件中心 (EventCentral + ScriptableObject 事件通道 EventChannel<T>)
    ├── Math/          # Unity 数学扩展 (AxuTween 补间系统，基于 _Core Ease)
    ├── Multiton/      # 多例模式 (Mono 侧)
    ├── Net/           # 网络工具 (HttpUtil/Downloader/HttpClient/SocketClient)
    ├── ObjectPool/    # GameObject 对象池 (GameObjectPools, SO 配置)
    ├── Raycast/       # 射线检测 (SimpleCamClicker/Hover)
    ├── Res/           # 资源加载
    ├── Save/          # 存档系统 (SaveUtil + 策略化 IStorage 后端)
    ├── Serializer/    # 可替换序列化后端 (BinarySerializer, UnityJsonSerializer)
    ├── Singleton/     # Unity 单例族 (SingletonLazy/SingletonMono/SingletonMonoCr/CrDs)
    ├── Static/        # 静态工具类
    ├── Table/CSV/     # CSV 表格处理 (CsvParser, CsvContainerSo<T>)
    ├── TaskQ/         # 任务队列 (CoroutineQ)
    ├── Timer/         # 倒计时与定时器 (CountdownManager, StopwatchManager)
    ├── Trigger/       # 触发器抽象
    └── UseMono/       # 非 Mono 类挂接 Update 等生命周期的代理机制
```

**命名空间规范**: `AxuFrameKit.Common.{子模块名}`

### 3. Editor (编辑器工具层)

**定位**: 框架自带的编辑器扩展与工作流工具（`AxuFrameKit.Common.Editor.*`）

```
Packages/com.cielevel.axu-frame-kit/Editor/Common/
├── KitBoxRevV2/       # "AxuKitBox" 多页签工具箱主窗口（纯 UI Toolkit，页面插件化 IKitPageRevV2）
├── Hierarchy/         # Hierarchy 增强 (Colorizer 规则着色 / Icon 规则图标，各带 Config/Window)
├── CustomAttribute/   # CustomMonoBehaviour/CustomScriptableObject/InsButton 的 Inspector 绘制器
├── Window/            # 独立窗口 (AssetFavorites, AvatarDownloader, ScriptableObjectCreator)
├── Utility/           # 转换与批量工具 (TerrainToMeshConverter, CsvIOUtil, CsvContainerCreatorWindow,
│                      #   SequenceEditor, ScriptDefineSymbolSetter, ShapeDrawer/TitleDrawer)
└── UI/Kit/            # UIListView / UIBindParent 自定义 Inspector
```

### 4. Modules (模块层，独立 UPM 包)

**定位**: 独立的功能模块，即插即用。**2026-08-29 起已全部拆分为独立嵌入式包**（各含 package.json + README/CHANGELOG + 独立 asmdef，示例在各自 `Samples~/`，经 Package Manager → Samples 导入）：

| 包 | 内容 | asmdef | 框架依赖 |
|---|---|---|---|
| `com.cielevel.axu-module-aitools` | AI 对话/脚本填充/脚本生成窗口 | `AxuFrameKit.Modules.AITools` + `.Editor` | 无 |
| `com.cielevel.axu-module-gizmos-util` | Gizmos 绘制库 | `AxuFrameKit.Modules.GizmosUtil` + `.Editor` | 无 |
| `com.cielevel.axu-module-livestream` | 直播互动模拟 | `AxuFrameKit.Modules.LiveStream` + `.Editor` | **有**（axu-frame-kit + Newtonsoft + TMP/ugui，asmdef 显式引用 Unity.TextMeshPro/UnityEngine.UI） |
| `com.cielevel.axu-module-noise` | 噪声生成与预览 | `AxuFrameKit.Modules.Noise` + `.Editor` | 无（含 FastNoiseLite MIT 声明） |

独立工具包（零依赖、互相独立）：`com.cielevel.axu-indie-scene-jumper`、`-selection-to-csv`、`-sprite-to-animation`、`-stat-pipeline`（各自 Editor-only 或 Runtime+Editor asmdef）。

**命名空间规范**: 模块家族 `AxuFrameKit.Modules.{模块名}`、独立模块 `AxuIndieModules.{模块名}`（均保持不变）。

---

## 其他目录说明

```
Assets/
├── AxuExperimental/       # 实验性模块集 (BehaviorTree, EventSystem, InGameConsole 等 9 个，各带 ReadMe；未包化)
├── DevDoc/                # 开发过程文档 (Socket 技术设计、纯 C# 剥离分析)
├── TestAndSample/         # 测试和示例场景
│   ├── Sample_Core/           # _Core 层示例
│   ├── Sample_Common/         # Common 层示例
│   ├── Sample_Experimental/   # 实验模块示例
│   ├── Sample_PackageDev/     # 包开发验证示例
│   ├── _Sample_Inner/         # 内部示例
│   └── AITools_Config/        # AIChatConfig 用户配置资产（含 API key，属用户数据不入包）
├── TEMP/                  # 临时测试数据 (CSV)
├── Tests/                 # 工程级测试入口 (AxuFrameKitV2.Tests.Editor：_Core 冒烟，EditMode；框架包测试在包内 Tests/)
├── TextMesh Pro/          # TMP Essential 资源
└── URP/                   # URP 管线资产 (Settings/PC_RPAsset 为全局管线)
```

> 原 `Assets/AxuFrameKit/`（Modules）与 `Assets/AxuIndieModules/` 已于 2026-08-29 全部拆包移除；
> 原 `Sample_Modules/` 的模块示例在各模块包的 `Samples~/` 内。

另：`Assets/_CommonDoc/`（包迁移评估文档）、`Assets/_DevDoc/`（TaskQ 改进备忘）、根目录 `docs/`（docfx 文档站）。

---

## 命名规范

### 命名空间
- **Core 层**: `AxuFrameKit._Core.{子模块}`
- **Common 层**: `AxuFrameKit.Common.{子模块}`
- **Editor 层**: `AxuFrameKit.Common.Editor.{区域}`
- **Modules 层**: `AxuFrameKit.Modules.{模块名}`
- **独立模块**: `AxuIndieModules.{模块名}`
- **测试代码**: `AxuFrameKit.Tests.{Editor|Runtime}`（框架包）、`TestAndSample.{路径}`（Assets 工程级入口）、`AxuTestFramework(.Editor).*`（测试基建包自身）、模块包 `{被测程序集}.Tests.*`

### 类命名
- **单例基类**: `Singleton<T>`, `SingletonMono<T>`, `SingletonLazy<T>`, `SingletonMonoCr<T>`, `SingletonMonoCrDs<T>`
- **UI 基类**: `UIBaseManager`, `UIBasePanel`, `UIBaseUnit`, `UIBasePage`, `UIBaseArea`, `UIBaseComponent`
- **数据绑定**: `UIBind{组件名}` (如 `UIBindTextStr`, `UIBindImage`)
- **编辑器窗口**: 以 `Window` 结尾 (如 `AxuKitBoxRevV2Window`)

### 文件组织
- 每个类单独一个文件
- 文件名与类名一致
- Editor 代码必须放在 `Editor/` 子目录下（包内即包根 `Editor/`）
- 运行时和编辑器代码严格分离

---

## 依赖包 (Packages)

框架本体为**嵌入式包**（`Packages/com.cielevel.axu-frame-kit`，不在 manifest.json 声明，由 Packages 目录自动识别）：

```json
{
  "com.cielevel.axu-frame-kit": "嵌入式包 (embedded) - 框架本体，version 0.1.0",
  "com.cielevel.axu-module-aitools": "嵌入式包 - AI 工具模块",
  "com.cielevel.axu-module-gizmos-util": "嵌入式包 - Gizmos 绘制模块",
  "com.cielevel.axu-module-livestream": "嵌入式包 - 直播互动模块（依赖框架包）",
  "com.cielevel.axu-module-noise": "嵌入式包 - 噪声模块",
  "com.cielevel.axu-indie-scene-jumper": "嵌入式包 - 场景快跳（零依赖）",
  "com.cielevel.axu-indie-selection-to-csv": "嵌入式包 - 选中导出 CSV（零依赖）",
  "com.cielevel.axu-indie-sprite-to-animation": "嵌入式包 - 序列帧转动画（零依赖）",
  "com.cielevel.axu-indie-stat-pipeline": "嵌入式包 - 数值管线（零依赖）",
  "com.cielevel.axu-test-framework": "嵌入式包 - 通用测试框架 AxuTestFramework 0.3.0（UTF 工具层 + 场景测试平台：八能力 Agent 运行时、axu_test_* / axu_scenario_* / axu_player_* / axu_trend_* pipeline 命令、AxuTestRunner 看板（总览/Tests/Scenario/Trend 四页签，总览为默认页））",
  "com.termway.ide.antigravity": "Git 包 - IDE 增强",
  "com.unity.ai.navigation": "2.0.4",
  "com.unity.collab-proxy": "2.9.3",
  "com.unity.ide.rider": "3.0.38",
  "com.unity.ide.visualstudio": "2.0.22",
  "com.unity.inputsystem": "1.11.1",
  "com.unity.multiplayer.center": "1.0.0",
  "com.unity.nuget.newtonsoft-json": "3.2.2",
  "com.unity.pipeline": "嵌入式包 (embedded, 0.5.0-exp.1) - Unity CLI agent 集成（已修补 macOS HttpListener 端口探测，见 .zcode/08 第 8 节）",
  "com.unity.render-pipelines.universal": "17.0.3",
  "com.unity.test-framework": "1.4.6",
  "com.unity.timeline": "1.8.7",
  "com.unity.ugui": "2.0.0",
  "com.unity.visualscripting": "1.9.4"
}
```

包自身依赖：`com.unity.ugui`、`com.unity.textmeshpro`、`com.unity.nuget.newtonsoft-json`。

---

## 构建与开发

### 脚本定义符号
- `UNITY_TEST`: 在 Standalone 平台启用

### 编辑器设置
- **序列化模式**: Force Text (m_SerializationMode: 2)
- **行尾符**: OS Native (m_LineEndingsForNewScripts: 0)
- **进入 Play 模式选项**: Enabled 标志开启但 Options=0（None）——**进 Play 时域重载与场景重载照常发生**。
  （2026-08-31 勘误：此前误记为"不重新加载域/场景"，是把 `m_EnterPlayModeOptionsEnabled` 误当选项值；实测 `m_EnterPlayModeOptions: 0`。无论哪种形态，PlayMode 测试都应在 `[UnitySetUp]` 调 `AxuTestState.ResetStatics()`）
- **色彩空间**: Linear

### 构建输出
- 构建输出目录: `Build/`, `Builds/`
- Unity Package 导出: `*.unitypackage`

---

## 核心设计模式

### 1. 单例模式 (Singleton)

提供多种单例实现以适应不同场景：

```csharp
// 纯 C# 单例 (Core 层)
public class MyManager : Singleton<MyManager> { }

// Unity MonoBehaviour 单例 (Common 层)
public class MyMonoManager : SingletonMono<MyMonoManager> { }

// 自动创建 GameObject 的单例
public class AutoManager : SingletonMonoCr<AutoManager> { }

// 跨场景不销毁的单例
public class PersistentManager : SingletonMonoCrDs<PersistentManager> { }

// 延迟初始化单例 (支持编辑器清理)
public class LazyManager : SingletonLazy<LazyManager> { }
```

### 2. 事件系统

**Core 层 - 类型安全事件中心** (`AxuFrameKit._Core.Advanced.EventSystem`，静态实例 `EventCenter.Default`)：
```csharp
// 订阅（返回 IDisposable，Dispose 即退订）
IDisposable sub = EventCenter.Default.Subscribe<PlayerDiedEvent>(OnPlayerDied);

// 发布
EventCenter.Default.Publish(new PlayerDiedEvent { PlayerId = 1 });

// 取消订阅
EventCenter.Default.Unsubscribe(OnPlayerDied);   // 或 sub.Dispose()
```

**Common 层 - EventCentral (泛型事件，静态实例 `EventCentral.Default`)**：
```csharp
// 订阅（返回 IUnRegister 句柄）
var reg = EventCentral.Default.Register<PlayerDiedEvent>(OnPlayerDied);

// 发布
EventCentral.Default.Send(new PlayerDiedEvent { PlayerId = 1 });

// 退订：reg.UnRegister(); 或随生命周期自动退订 reg.UnRegisterWhenGameObjectDestroyed(gameObject);
```

**Common 层 - ScriptableObject 事件通道**: `EventChannel<T>`（SO 资产式，可 Inspector 连线，带生命周期自动注销）。

### 3. 对象池

**Core 层 - 通用对象池**:
```csharp
var pool = new SimpleObjPool<MyClass>(() => new MyClass());
var obj = pool.Get();
pool.Release(obj);
```

**Common 层 - GameObject 池**:
```csharp
GameObjectStrPools.Instance.PreWarm("PrefabName", 10);
var go = GameObjectStrPools.Instance.Get("PrefabName");
GameObjectStrPools.Instance.Release("PrefabName", go);
```

### 4. 数据绑定 (UI)

```csharp
// 在 UI 组件上添加绑定组件
UIBindTextStr.Bind(myTextComponent, () => model.PlayerName);
```

---

## 文档位置

- **AI 知识库**: 根目录 `.zcode/`（2026-08-29 勘察生成的详细模块清单、包结构、文档清单与演进史，**查模块/类时优先检索**；`08` 为 Unity Pipeline 本地 API、`09` 为 Unity 全流程子代理体系）
- **框架文档**（2026-08-29 已迁入包内、随代码放置）: `Packages/com.cielevel.axu-frame-kit/`
  - 包根 `README.md`（包结构与**全量文档索引表**）、`CHANGELOG.md`（变更记录）
  - 各模块 ReadMe/HOWTOUSE/ARCHITECTURE 位于对应模块目录旁，如 `Runtime/_Core/Advanced/BetterFSM/ReadMe.md`、
    `Runtime/_Core/Utility/Encrypt/`（3 份）、`Runtime/Common/UI/Kit/ListView/DOC/`（5 份）、`Runtime/Common/Utility/Save/`、
    `Runtime/Common/Utility/{Math,Table/CSV}/Doc_*.md`、`Runtime/Common/UI/Framework/UI框架.md` 等——**查文档先看包 README 的索引**
- **模块包文档**: 8 个模块包（`Packages/com.cielevel.axu-module-*`、`com.cielevel.axu-indie-*`）各自带 README.md / CHANGELOG.md
- **测试框架文档**: `Packages/com.cielevel.axu-test-framework/README.md`（UTF 工具层 API、axu_test_* / axu_scenario_* / axu_player_* / axu_trend_* 命令参数表、两套报告 schema、场景测试接入一页纸、各包接入约定；schema 细节在包内 `DOC/ScenarioSchema.md`）
- **开发文档**: `Assets/DevDoc/`（Socket 技术设计等）、`Assets/_CommonDoc/`（包迁移评估）

---

## 开发注意事项

1. **分层依赖**: 
   - `_Core` 层（AxuFrameKit.Core）**绝对不能**引用 Unity API
   - `Common` 层（AxuFrameKit.Runtime）只能依赖 `_Core`
   - `Modules` 层可以依赖 `_Core` 和 `Common`
   - Editor 层（AxuFrameKit.Editor）依赖前两者，且仅限编辑器

2. **Editor 代码**:
   - 框架包内的 Editor 代码放包根 `Editor/` 目录（AxuFrameKit.Editor 程序集）
   - 使用 `#if UNITY_EDITOR` 包裹散落的 Editor 专用代码
   - 编辑器代码命名空间通常以 `.Editor` 结尾

3. **单例使用**:
   - 优先使用 `SingletonLazy<T>` (支持编辑器自动清理)
   - MonoBehaviour 单例注意在 `Awake` 中初始化
   - 使用 `SingletonMonoCr<T>` 系列可直接通过 Instance 获取，自动创建

4. **扩展方法**:
   - 统一放在 `Extension/` 目录下
   - 使用 `this` 关键字作为第一个参数
   - 遵循"单一职责"原则

5. **命名空间**:
   - 所有代码必须使用命名空间
   - 遵循 `AxuFrameKit.{层}.{模块}` 规范（Core 层为 `AxuFrameKit._Core.{模块}`）

6. **文件移动**:
   - 在 Unity 内移动/重命名资源（连同 .meta 文件），避免破坏 GUID 引用

---

## 测试

- 使用 Unity Test Framework (com.unity.test-framework 1.4.6) + 自建测试基建 **AxuTestFramework**（嵌入式包 `Packages/com.cielevel.axu-test-framework`，命名空间 `AxuTestFramework.*`，Runtime 工具层 `UNITY_INCLUDE_TESTS` 守护不进构建）
- **测试分布**（2026-08-31 起）：
  - 框架包 `Packages/com.cielevel.axu-frame-kit/Tests/Editor/`（`AxuFrameKit.Tests.Editor`）与 `Tests/Runtime/`（`AxuFrameKit.Tests.Runtime`，PlayMode）
  - `Assets/Tests/Editor/`（`AxuFrameKitV2.Tests.Editor`）**保留**为工程级冒烟入口
  - 模块/indie 包在各自 `Tests/{Editor,Runtime}/` 建测试：asmdef 命名 `{被测程序集}.Tests.{Editor|Runtime}`，引用 AxuTestFramework；包 `package.json` 无需声明测试依赖（缺包时测试程序集因 `UNITY_INCLUDE_TESTS` 不编译，是特性非错误）
- **运行方式（agent 优先）**：Unity Pipeline 自定义命令 `axu_test_run` / `axu_test_status` / `axu_test_report`（用法见 `.zcode/08`）；报告 JSON 落盘 `Library/AxuTestReports/{时间戳}-{mode}.json`，截图在 `artifacts/`。人用看板：菜单 `AxuFrameKit/AxuTestRunner`
- **场景测试平台（v2，2026-09 起，与 UTF 并行、词汇隔离）**：`axu_test_*` 永远指 UTF 测试；`axu_scenario_*` / `axu_player_*` / `axu_trend_*` 指 scenario 套件运行（Editor PlayMode / Player 真机 / 趋势）。入口 = suite JSON（工程内 demo 在 `Assets/Tests/Scenarios/*.suite.json`，schema `axu-suite/1`）+ `AXU_TEST_AGENT` define（菜单 `AxuTestFramework/Enable Test Agent`，本工程已开）。八能力：echo/perf/loadtime/smoke/soak/stress/monkey/gamehook（各带 DOC.md 与 Samples 示例）。报告落 `Library/AxuTestReports/scenario/`（schema `axu-scenario-report/1`；退出码 0=全过/1=有 fail/2=仅 warn），趋势 CSV 在 `Library/AxuTestReports/trend/`；看板 Scenario / Trend 页签；接入步骤见包 README「场景测试接入一页纸」。⚠️ 六命令与 `axu_test_run` 共享互斥闸禁止并发；**Play 长跑（soak 等）期间严禁在同一 Editor 并行派发其他 pipeline 命令**（2026-09-02 事故实证）
- ⚠️ **禁止无程序集范围的裸 `run_tests`**：会实跑 pipeline 包自带测试，其 SecurityTokenTests 毒化会话 token 导致后续请求 401 死锁（2026-08-31 事故实证）；`axu_test_run` 已内置排除，直用 pipeline `run_tests` 时必须带 assembly 过滤
- **编写规范**：类别即套件（`[Category("Axu/Smoke")]`、`[Category("Axu/Play")]`、`[Category("Axu/Core/Util")]`、`[Category("Axu/Module/{名}")]`，fixture 级标注）；命名 `方法名_条件_期望结果`；预期错误用 `LogAssert.Expect`，意外 Error 判失败（`AxuLogWatch`/`AxuNoErrorsScope`）；PlayMode 测试 `[UnitySetUp]` 先 `AxuTestState.ResetStatics()`（域重载开/关都要做，见下方勘误）
- 示例场景位于 `Assets/TestAndSample/`
- 每个主要功能都应有对应的 Sample 场景

---

## 版本控制

- **Git**: 使用标准 Unity .gitignore
- **忽略目录**: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Build/`
- **忽略文件**: `*.csproj`, `*.sln`, `*.unitypackage`
