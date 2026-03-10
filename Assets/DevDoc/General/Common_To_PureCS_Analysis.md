# Common 模块纯 C# 剥离可行性分析报告

本文档旨在分析 `Assets/AxuFrameKit/Common` 目录下的代码库，探讨哪些模块和组件可以完全剥离对 `UnityEngine` 的依赖，成为跨平台、跨引擎的纯净 C# (.NET Standard / .NET Core) 代码库。

## 一、 分析总览

`Common` 目录下包含了大量的框架层基础支持代码。其中：
- **强烈耦合于 Unity 的模块（不建议/无法剥离）**：`UI` 框架（高度依赖 UGUI）、`Editor` 扩展视图、`CustomAttribute` (依赖 PropertyDrawer 和协程编排)、`Extension` (大部分是对 Transform/GameObject/Vector 等的扩展)。
- **具备高潜力剥离的模块**：主要集中在 `Utility` (工具库)、`Advanced/ValueSystem` (数值系统) 目录下。其中很多模块的架构已经是抽象接口 + 具体实现的模式，非常利于剥离。

---

## 二、 核心可剥离模块详情

### 1. Save (存档与配置管理)
**当前状态**：核心类 `SaveUtil` 已经做了高度抽象 (`ISerializer`, `IStorage`, `ISymmetricCrypto`)。
**剥离方案**：
- **纯 C# 核心层**：保留所有的核心接口，以及 `LocalFileStorage` (基于 `System.IO`), `BinarySerializer` 等纯 .NET 实现。
- **剥离点**：所有的 `Debug.Log` 替换为自定义的 `ILogger` 接口或 `System.Diagnostics.Debug`。
- **Unity 桥接层**：将 `UnityJsonSerializer` (依赖 `JsonUtility`) 和 `PlayerPrefsStorage` 提炼到专有的 Unity 支持包中。

### 2. Net (网络通信)
**当前状态**：包含了 `Socket` 框架和基础 `HttpUtil`。
**剥离方案**：
- **Socket 层**：包体编解码 (`LengthPrefixedPacketCodec`, `ISocketPacketCodec`) 和 `SocketClient` 本就可以是纯 C# 的。目前可能夹杂了 `SocketUnityMainThreadDispatcher` (用于将回调切回 Unity 主线程) 和 `Debug.Log`。剥离后，可以将线程调度逻辑抽象化。
- **HTTP 层**：如果当前使用了 `UnityWebRequest`，可以编写基于 `System.Net.Http.HttpClient` 的统一实现供纯 C# 环境使用；提供接口让 Unity 端选择实现。

### 3. Event (事件中心)
**当前状态**：`EventCentral` 和 `EventChannel` (观察者/发布订阅模式)。
**剥离方案**：
- 目前仅在容错打印时涉及了 `UnityEngine.Debug`，部分对传入事件目标可能使用了 `UnityEngine.Object` 的 `null` 检查。
- 去除 `UnityEngine.Object` 特化检查，通过提炼接口和日志委托，即可成为完全独立的纯 C# 强类型事件总线。

### 4. Table / CSV (表格解析)
**当前状态**：`CsvParser`, `CsvTool`。
**剥离方案**：
- **纯 C# 核心**：所有的文本切割、CSV 读取、类型反射转换算法均是纯 C# 的，与环境无关。
- **Unity 桥接层**：`CsvContainerSo` 继承自 `ScriptableObject`，负责在 Unity 中序列化 CSV 数据，这部分需要留存在 Unity 专用层。

### 5. Math / Interpolation (数学与缓动动画)
**当前状态**：包含了贝塞尔曲线 (`BezierCore`), 缓动曲线与 `AxuTween` 核心状态机。
**剥离方案**：
- **核心计算层**：所有的 Easing 缓动公式（Sine, Quad 等）、时间归一化计算、贝塞尔计算都可以完全纯 C# 化。
- **驱动机分离**：目前的 `AxuTweenRunner` 可能挂载了 `MonoBehaviour` 以利用 `Update`。可以将其改为接收外部 `DeltaTime` 注入的纯数据驱动机。
- **表现终端**：具体改变 `Transform.position` 或 `CanvasGroup.alpha` 的 Tweener (如 `AxuTweenerMoveVec3`) 则强绑定 Unity，需剥离。

### 6. Timer / TaskQ (时间与异步队列)
**当前状态**：`CdTimer`, `TaskQ` 等时间流逝控制和串行化任务。
**剥离方案**：
- 目前常与 `Time.time` / `Time.deltaTime` 以及 `MonoBehaviour` 携程高度绑定。
- **改造方案**：将时间获取抽象为一个 `ITimeProvider` 接口，对于纯引擎外环境传递 `DateTime.Now` 差异，从而使得计时逻辑、冷却机制、队列排期全部可以在独立于 Unity 的线程中运行。

### 7. 数据结构与模式 (Singleton, Multiton, ObjectPool)
**剥离方案**：
- 非 `MonoBehaviour` 版本的单例 (`Singletons`)、多例 (`Multiton`)、纯对象池均只需移除对 `Debug` 命名空间的依赖即可完全剥离。
- 对象池需将原本混写的 `GameObjectPool` (绑定引擎实例化机制) 与 `ClassPool` (普通的 C# 引用池) 的物理路径划分开。

### 8. ValueSystem (数值修饰器系统)
**剥离方案**：
- 属性修饰器、基础增伤/减伤乘区逻辑是纯数学和集合运算配置，具备零耦合优势，修改其基类引用后即可完美纯 C# 化，甚至放于服务器端作为战斗数值裁决代码运行。

---

## 三、 演进建议与下一步目标

如果要正式实施 "核心代码下沉，完全剥离 Unity"，建议采用如下结构：

1. **`AxuFrameKit.Core` (纯 C# 库)**:
   - 脱离 *.csproj 对 UnityEngine.dll 的引用。
   - 内部不得包含任何 `using UnityEngine`。
   - 所有的日志全部调用 `AxuLogger.Log()` (通过接口让外界注入日志提供者)。
2. **`AxuFrameKit.Unity` (外壳/桥接层)**:
   - 包含 MonoBehaviour, ScriptableObject, UGUI 代码。
   - 实现 `AxuFrameKit.Core` 的接口（如提供 UnityJsonSerializer、PlayerPrefsStorage、UnityLogger、Unity时间提供器）。

通过这种依赖倒置倒流，可以将网络、配置、数值、算法等具有高度复用价值和极强服务器客户端共享价值的部分，完美打造为全平台的 NuGet C# 包。
