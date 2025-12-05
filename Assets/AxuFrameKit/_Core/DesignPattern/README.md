# 设计模式 (Design Patterns)

此目录包含了常用的游戏设计模式的通用实现。这些代码位于 `AxuFrameKit._Core.DesignPattern` 命名空间下，是纯 C# 实现，不依赖于 Unity API。

## 可用模式

### 1. 单例模式 (Singleton) - `Singleton.cs`
线程安全的懒加载单例基类。
- **用法**: 继承 `Singleton<T>`。
- **注意**: 这是针对纯 C# 类的单例。对于 Unity 的 `MonoBehaviour` 单例，请查找框架中位于 `_Core` 之外的 Unity 特定扩展 (`MonoSingleton`)。

### 2. 命令模式 (Command) - `Command.cs`
将请求封装为对象，从而使你可用不同的请求对客户进行参数化；对请求排队或记录请求日志，以及支持可撤销的操作。
- **组件**:
    - `ICommand`: 包含 `Execute()` 和 `Undo()` 的接口。
    - `CommandInvoker`: 管理 `Undo` (撤销) 和 `Redo` (重做) 栈。

### 3. 责任链模式 (Chain of Responsibility) - `ChainOfResponsibility.cs`
避免请求发送者与接收者耦合在一起，让多个对象都有可能接收请求，将这些对象连接成一条链，并且沿着这条链传递请求，直到有对象处理它为止。
- **组件**:
    - `IHandler<TRequest>`: 处理者接口。
    - `BaseHandler<TRequest>`: 简化处理者链接 (`SetNext`) 的抽象基类。

### 4. 备忘录模式 (Memento) - `Memento.cs`
在不破坏封装性的前提下，捕获一个对象的内部状态，并在该对象之外保存这个状态，以便以后当需要时能将该对象恢复到原先保存的状态。
- **组件**:
    - `Memento<TState>`: 保存状态快照。
    - `IOriginator<TState>`: 支持保存/加载状态的对象接口。
    - `Caretaker<TState>`: 管理备份历史记录。

---

## `_Core` 中的其他模式

由于某些模式较为复杂或代码量较大，它们被放置在 `_Core/Advanced` 目录下：

- **对象池 (Object Pool)**: `AxuFrameKit._Core.Advanced.ObjectPool`
- **有限状态机 (FSM)**: `AxuFrameKit._Core.Advanced.FSM`
- **事件系统 (Event System / Observer)**: `AxuFrameKit._Core.Advanced.EventSystem`
