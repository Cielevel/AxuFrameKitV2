# Design Patterns

This directory contains generic, Unity-agnostic implementations of common Game Design Patterns. These are pure C# implementations located in the `AxuFrameKit._Core.DesignPattern` namespace.

## Available Patterns

### 1. Singleton (`Singleton.cs`)
A thread-safe, lazy-loaded Singleton base class.
- **Usage**: Inherit from `Singleton<T>`.
- **Note**: This is for pure C# classes. For Unity `MonoBehaviour` singletons, look for a `MonoSingleton` in the Unity-specific extensions of the framework (outside `_Core`).

### 2. Command (`Command.cs`)
Encapsulates a request as an object, thereby letting you parameterize clients with different requests, queue or log requests, and support undoable operations.
- **Components**:
    - `ICommand`: Interface with `Execute()` and `Undo()`.
    - `CommandInvoker`: Manages `Undo` and `Redo` stacks.

### 3. Chain of Responsibility (`ChainOfResponsibility.cs`)
Avoids coupling the sender of a request to its receiver by giving more than one object a chance to handle the request.
- **Components**:
    - `IHandler<TRequest>`: Interface for handlers.
    - `BaseHandler<TRequest>`: Abstract base class that simplifies linking handlers (`SetNext`).

### 4. Memento (`Memento.cs`)
Captures and externalizes an object's internal state so that the object can be restored to this state later, without violating encapsulation.
- **Components**:
    - `Memento<TState>`: Holds the state snapshot.
    - `IOriginator<TState>`: Interface for objects that support saving/loading state.
    - `Caretaker<TState>`: Manages the history of backups.

---

## Other Patterns in `_Core`

Some complex patterns are located in `_Core/Advanced` due to their size or complexity:

- **Object Pool**: `AxuFrameKit._Core.Advanced.ObjectPool`
- **Finite State Machine (FSM)**: `AxuFrameKit._Core.Advanced.FSM`
- **Event System (Observer)**: `AxuFrameKit._Core.Advanced.EventSystem`
