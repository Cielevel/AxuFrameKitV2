# 03 · _Core 层模块清单（AxuFrameKit.Core 程序集，80 个 .cs）

> 路径：`Packages/com.cielevel.axu-frame-kit/Runtime/_Core/`。命名空间：`AxuFrameKit._Core.{子模块}`。
> 特点：**纯 C#，零 Unity 引擎依赖**，可在非 Unity 环境使用。

## 模块总表

| 子模块 | 路径 | 命名空间 | 关键类 | 职责 |
|---|---|---|---|---|
| BetterFSM | `_Core/Advanced/BetterFSM/` | `AxuFrameKit._Core.Advanced.BetterFSM` | `IState`、`ITransition/IPredicate/FuncPredicate`、`Transition`、`StateMachine` | 委托/谓词驱动的分层状态机（推荐用这套） |
| FSM（泛型） | `_Core/Advanced/FSM/FSM.cs` | `AxuFrameKit._Core.Advanced.FSM` | `IState<T>`、`StateBase<T>`、`StateMachine<T>` | 另一套泛型状态机实现 |
| EventSystem | `_Core/Advanced/EventSystem/EventCenter.cs` | `AxuFrameKit._Core.Advanced.EventSystem` | `EventCenter` | 类型安全的中心化事件中心（Subscribe/Publish/Unsubscribe） |
| ObjectPool | `_Core/Advanced/ObjectPool/` | `AxuFrameKit._Core.Advanced.ObjectPool` | `TypeObjPools`、`IPools/IPoolable/Poolable`、`SimpleObjPool<T>` | 按类型管理的 C# 对象池 |
| DesignPattern | `_Core/DesignPattern/` | `AxuFrameKit._Core.DesignPattern` | `Singleton<T>`、`Command/CommandInvoker`、`Memento/Caretaker/IOriginator`、`BaseHandler`（责任链） | 经典设计模式泛型实现 |
| Structure | `_Core/Structure/` | `AxuFrameKit._Core.Structure` | `IndexedList<T>`、`IndexedMultiList<T>`、`BiDictionary<K,V>`、`PriorityQueue<T>`、`CircularBuffer<T>`、`ReactiveProperty<T>` | 高性能/带索引数据结构 + 响应式属性 |
| Extension | `_Core/Extension/` | `AxuFrameKit._Core.Extension` | `StringExtension`、`IntExtension`、`FloatExtension`、`DateTimeExtension`、`CollectionExtension`、`EnumExtensions`、`TaskExtension`、`TypeExtension`、`ReflectionExtension` | 基础类型扩展方法（9 个文件） |
| Computed | `_Core/Utility/Computed/` | `AxuFrameKit._Core.Utility.Computed` | `IComputed<T>/IReadOnlyComputed<T>`、`Computed<T>`、`ComputedSource<T>` | 惰性求值 + 依赖追踪 + 连锁失效的响应式派生值（类 Vue computed） |
| Encrypt | `_Core/Utility/Encrypt/` | `AxuFrameKit._Core.Utility.Encrypt` | 见下表 | 完整加密工具集，接口化分层 |
| Math/Interpolation | `_Core/Utility/Math/Interpolation/` | `AxuFrameKit._Core.Utility.Math.Interpolation`（含 `.Ease`） | `IAxuTweener`、`AxuTweener<TTarget>`、`IEase` + 全套 Ease（Linear/Sine/Quad/Cubic/Quart/Quint/Expo/Circ/Back/Elastic/Bounce 的 In/Out/InOut）、`EaseFactory`、`EaseType` | 引擎无关的缓动/插值基座（Common 层 Tween 构建于此之上） |
| TaskQ | `_Core/Utility/TaskQ/` | `AxuFrameKit._Core.Utility.TaskQ` | `ITaskQ/ITaskQc`、`TaskQ`、`TaskQAuto`、`TaskQc`、`TaskQcAuto` | 无 Mono 的异步任务队列（Multiton 实例，Auto=自驱动） |
| Multiton | `_Core/Utility/Multiton/` | `AxuFrameKit._Core.Utility.Multiton` | `Multiton<T>`、`MultitonAuto<T>` | 多例基类（按键索引的实例管理，IDisposable） |
| RandomX | `_Core/Utility/RandomX/` | `AxuFrameKit._Core.Utility.RandomX` | `RandomX`、`AliasSampler<T>/AliasMethod`、`RandomId`、`RandomNickname`、`RandomAvatarUrl` | 随机数封装、别名法加权抽样、随机 ID/昵称/头像 |
| Common/Misc | `_Core/Utility/Common/`、`_Core/Utility/Misc/` | `AxuFrameKit._Core.Utility.Common` / `.Misc` | `Logger`、`Number`、`PingPongNum` | 日志、数值工具、乒乓计数器 |
| Enum | `_Core/Enum/` | `AxuFrameKit._Core.Enum` | `WorldAxis` + `WorldAxisExtension` | 世界坐标轴枚举 |

## Encrypt 模块细分（`AxuFrameKit._Core.Utility.Encrypt`）

| 类 | 类别 | 说明 |
|---|---|---|
| `EncryptHelper` | 门面 | 统一入口 |
| `AesCrypto`、`XorCrypto`（`ISymmetricCrypto`） | 对称加密 | AES、XOR |
| `RsaCrypto`（`IAsymmetricCrypto`） | 非对称加密 | RSA（含 padding 配置） |
| `MD5Hasher/SHA256Hasher/SHA512Hasher`（`IHasher`） | 哈希 | 摘要计算 |
| `Pbkdf2KeyDerivation` | 密钥派生 | PBKDF2 |
| `HmacSha256` | MAC | HMAC 签名 |
| `Base64Encoder/HexEncoder`（`IEncoder`） | 编码 | Base64/Hex |
| `EncryptException` | 异常 | 统一异常类型 |

> 历史注意：03-10 曾有 SaveUtil 迁至 core encrypt 模块的改动；`build_error.log` 记录过 Pbkdf2KeyDerivation 的 CS0283/CS0133 与 SaveUtil 缺 System.Security.Cryptography 的历史报错，现已随包迁移解决。

## 事件系统对照（避免混用）

| 系统 | 层 | 类型 | 用法 |
|---|---|---|---|
| `EventCenter` | _Core | 类型安全（泛型事件类） | `EventCenter.Subscribe<T>(cb)` / `Publish(new T())` / `Unsubscribe` |
| `EventCentral` | Common | 字符串事件 | `EventCentral.Subscribe("name", cb)` / `Publish("name", data)` |
| `EventChannel<T>` | Common | ScriptableObject 通道 | SO 资产式事件，Inspector 可连线 |

## 单例族对照

| 类 | 层 | 说明 |
|---|---|---|
| `Singleton<T>` | _Core | 纯 C# 单例 |
| `SingletonLazy<T>` | Common | 延迟初始化，支持编辑器自动清理（**优先推荐**） |
| `SingletonMono<T>` | Common | MonoBehaviour 单例，Awake 初始化 |
| `SingletonCustomMono<T>` | Common | 自定义挂点单例 |
| `SingletonMonoCr<T>` | Common | Instance 自动创建 GameObject |
| `SingletonMonoCrDs<T>` | Common | 自动创建 + DontDestroyOnLoad 跨场景不销毁 |
