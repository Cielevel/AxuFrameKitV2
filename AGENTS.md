# AxuFrameKit Project Guide

> 本文档供 AI Coding Agent 阅读，用于快速了解本项目架构与开发规范。

## 项目概览

**AxuFrameKit** (阿修框架工具包) 是一个 Unity 应用程序框架，旨在提供清晰的架构和可复用的组件。项目采用分层架构设计，确保模块间的低耦合与高内聚。

- **项目类型**: Unity 应用程序框架
- **Unity 版本**: 6000.0.23f1 (Unity 6)
- **渲染管线**: URP (Universal Render Pipeline)
- **主要语言**: C# (LangVersion 9.0)
- **目标框架**: .NET Framework 4.7.1

---

## 架构设计

项目采用**三层架构**，依赖关系如下：

```
Modules (功能模块) ──────► Common (通用模块) ──────► _Core (核心模块)
     │                        │                        │
     └────────────────────────┴────────────────────────┘
                          均依赖
```

### 1. _Core (核心层)

**定位**: 框架的核心基础模块

**特点**: 
- 纯 C# 实现，**无任何 Unity 引擎依赖**
- 包含通用的工具类、数据结构、设计模式实现以及高级架构组件
- 可在非 Unity 环境中使用

**目录结构**:
```
Assets/AxuFrameKit/_Core/
├── Advanced/           # 高级架构组件
│   ├── BetterFSM/     # 改进版有限状态机
│   ├── EventSystem/   # 类型安全的事件中心
│   ├── FSM/           # 有限状态机
│   ├── ObjectPool/    # 通用对象池
│   └── ValueSystem/   # 数值系统
├── DesignPattern/     # 设计模式实现
│   ├── ChainOfResponsibility.cs
│   ├── Command.cs
│   ├── Memento.cs
│   └── Singleton.cs
├── Enum/              # 通用枚举
├── Extension/         # 扩展方法
│   ├── CollectionExtension.cs
│   ├── EnumExtension.cs
│   ├── StringExtension.cs
│   └── TypeExtension.cs
├── Structure/         # 数据结构
│   ├── BiDictionary.cs      # 双向字典
│   ├── CircularBuffer.cs    # 环形缓冲区
│   ├── IndexedList.cs       # 索引列表
│   ├── PriorityQueue.cs     # 优先级队列
│   └── IntBound.cs          # 整数范围
└── Utility/           # 实用工具
    ├── Common/        # 通用工具 (Logger, Number)
    ├── Math/          # 数学工具 (插值、缓动)
    ├── Misc/          # 杂项工具
    └── RandomX/       # 随机数工具增强
```

**命名空间规范**: `AxuFrameKit._Core.{子模块名}`

### 2. Common (通用层)

**定位**: 基于 Unity 引擎的通用组件与工具

**特点**:
- 依赖 Unity API
- 依赖 `_Core` 层

**目录结构**:
```
Assets/AxuFrameKit/Common/
├── Advanced/          # 高级组件 (ValueSystem)
├── CustomAttribute/   # 自定义 Inspector 属性
│   ├── Editor/        # 属性绘制器
│   └── *.cs           # ButtonAttribute, InspectorTitle 等
├── Editor/            # 编辑器扩展
│   ├── KitBoxV2/      # AxuKitBoxV2 工具箱窗口
│   ├── KitBoxRevV2/   # 新版工具箱
│   ├── Hierarychy/    # Hierarchy 窗口增强
│   └── Utility/       # 编辑器工具
├── Extension/         # Unity 类型扩展
│   ├── TransformExtension.cs
│   ├── VectorExtension.cs
│   └── GameObjectExtension.cs
├── UI/                # UI 框架
│   ├── Framework/     # UI 基础框架 (UIBaseManager, UIBasePanel 等)
│   └── Kit/           # UI 组件套件
│       ├── DataBind/  # 数据绑定组件
│       ├── Group/     # 图片/精灵分组
│       ├── Scroll/    # 循环滚动视图
│       ├── Toast/     # 消息提示
│       ├── ToggleBtnGroup/  # 切换按钮组
│       └── Video/     # 视频播放套件
└── Utility/           # 通用工具
    ├── 2D/            # 2D 工具
    ├── Analysis/      # 分析工具 (调试器、游戏内控制台)
    ├── Auto/          # 自动化组件
    │   ├── AutoCamera/     # 相机面向
    │   ├── AutoDestroy/    # 自动销毁
    │   ├── AutoSequence/   # 序列播放器
    │   ├── AutoSnapshot/   # 快照录制/播放
    │   └── AutoTracker/    # 位置追踪
    ├── Event/         # 事件中心 (EventCentral)
    ├── Math/          # Unity 数学扩展 (Tween)
    ├── Multiton/      # 多例模式
    ├── Net/           # 网络工具
    ├── ObjectPool/    # GameObject 对象池
    ├── Raycast/       # 射线检测
    ├── Res/           # 资源加载
    ├── Save/          # 存档工具
    ├── Singleton/     # Unity 单例实现
    ├── Static/        # 静态工具类
    ├── Table/CSV/     # CSV 表格处理
    ├── TaskQ/         # 任务队列 (协程/异步)
    ├── Timer/         # 倒计时与定时器
    └── UseMono/       # MonoBehaviour 使用模式接口
```

**命名空间规范**: `AxuFrameKit.Common.{子模块名}`

### 3. Modules (模块层)

**定位**: 独立的功能模块，即插即用

**特点**:
- 具体业务功能实现
- 依赖 `_Core` 和 `Common` 层

**目录结构**:
```
Assets/AxuFrameKit/Modules/
├── AITools/           # AI 辅助工具
│   ├── Chat/          # AI 对话窗口
│   ├── Filler/        # 脚本填充工具
│   └── Generator/     # 脚本生成工具
├── BehaviorTree/      # 行为树系统
│   ├── Editor/        # 行为树编辑器
│   └── Runtime/       # 运行时节点
├── GizmosUtil/        # Gizmos 辅助工具
│   ├── Editor/
│   └── GizmosDrawer.*.cs
├── LiveStream/        # 直播/交互功能模块
│   ├── Script/
│   │   ├── EditorWindow/
│   │   ├── System/    # 核心系统
│   │   ├── UI/        # UI 面板
│   │   └── Utility/   # 工具类
└── Noise/             # 噪声生成
    ├── FastNoiseLite/
    └── SimpleNoise/
```

**命名空间规范**: `AxuFrameKit.Modules.{模块名}`

---

## 其他目录说明

```
Assets/
├── AxuIndieModules/       # 独立模块 (StatPipeline - 数值管道系统)
├── Doc/                   # 文档资源
├── PreTestFunc/           # 预测试功能 (实验性代码)
├── Scripts/Generated/     # 自动生成的脚本
├── TestAndSample/         # 测试和示例场景
│   ├── Sample_Core/       # _Core 层示例
│   ├── Sample_Common/     # Common 层示例
│   └── Sample_Modules/    # Modules 层示例
├── TextMesh Pro/          # TMP 资源
└── URP/                   # URP 模板资源
```

---

## 命名规范

### 命名空间
- **Core 层**: `AxuFrameKit._Core.{子模块}`
- **Common 层**: `AxuFrameKit.Common.{子模块}`
- **Modules 层**: `AxuFrameKit.Modules.{模块名}`
- **独立模块**: `AxuIndieModules.{模块名}`
- **测试代码**: `TestAndSample.{路径}`

### 类命名
- **单例基类**: `Singleton<T>`, `SingletonMono<T>`, `SingletonLazy<T>`
- **UI 基类**: `UIBaseManager`, `UIBasePanel`, `UIBaseUnit`, `UIBasePage`, `UIBaseArea`, `UIBaseComponent`
- **数据绑定**: `UIBind{组件名}` (如 `UIBindTextStr`, `UIBindImage`)
- **编辑器窗口**: 以 `Window` 结尾 (如 `AxuKitBoxV2Window`)

### 文件组织
- 每个类单独一个文件
- 文件名与类名一致
- Editor 代码必须放在 `Editor/` 子目录下
- 运行时和编辑器代码严格分离

---

## 依赖包 (Packages)

```json
{
  "com.termway.ide.antigravity": "Git 包 - IDE 增强",
  "com.unity.ai.navigation": "2.0.4",
  "com.unity.ide.rider": "3.0.38",
  "com.unity.ide.visualstudio": "2.0.22",
  "com.unity.inputsystem": "1.11.1",
  "com.unity.render-pipelines.universal": "17.0.3",
  "com.unity.test-framework": "1.4.5",
  "com.unity.timeline": "1.8.7",
  "com.unity.ugui": "2.0.0",
  "com.unity.visualscripting": "1.9.4"
}
```

---

## 构建与开发

### 脚本定义符号
- `UNITY_TEST`: 在 Standalone 平台启用

### 编辑器设置
- **序列化模式**: Force Text (m_SerializationMode: 2)
- **行尾符**: OS Native (m_LineEndingsForNewScripts: 0)
- **进入 Play 模式选项**: 已启用，不重新加载域/场景 (EnterPlayModeOptionsEnabled: 1)

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

**Core 层 - 类型安全事件中心**:
```csharp
// 订阅
EventCenter.Subscribe<PlayerDiedEvent>(OnPlayerDied);

// 发布
EventCenter.Publish(new PlayerDiedEvent { PlayerId = 1 });

// 取消订阅
EventCenter.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
```

**Common 层 - EventCentral (字符串事件)**:
```csharp
EventCentral.Subscribe("EventName", callback);
EventCentral.Publish("EventName", data);
```

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

- **KitDoc**: `Assets/AxuFrameKit/KitDoc/`
  - `MathUtilityDoc.md` - 数学与动画插值文档
  - `SaveUtilityDoc.md` - 存档工具文档
  - `TableCsvUtilityDoc.md` - CSV 表格处理文档
- **README**: 项目根目录 `README.md`

---

## 开发注意事项

1. **分层依赖**: 
   - `_Core` 层**绝对不能**引用 Unity API
   - `Common` 层只能依赖 `_Core`
   - `Modules` 层可以依赖 `_Core` 和 `Common`

2. **Editor 代码**:
   - 必须放在 `Editor/` 子目录下
   - 使用 `#if UNITY_EDITOR` 包裹 Editor 专用代码
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
   - 遵循 `AxuFrameKit.{层}.{模块}` 规范

---

## 测试

- 使用 Unity Test Framework (com.unity.test-framework 1.4.5)
- 示例代码位于 `Assets/TestAndSample/`
- 每个主要功能都应有对应的 Sample 场景

---

## 版本控制

- **Git**: 使用标准 Unity .gitignore
- **忽略目录**: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Build/`
- **忽略文件**: `*.csproj`, `*.sln`, `*.unitypackage`
