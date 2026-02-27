# BehaviorTree - 行为树系统

## 功能概述

这是一个完整的 Unity 行为树系统，支持可视化编辑。用于创建 AI 行为逻辑和游戏流程控制。

## 主要特性

### 运行时框架
- **节点基类系统**：`Node` 基类定义节点生命周期（Start/Update/Stop）
- **节点状态**：支持 Running、Success、Failure 三种状态
- **三种节点类型**：
  - `ActionNode` - 动作节点（叶节点），执行具体行为
  - `CompositeNode` - 组合节点，包含多个子节点
  - `DecoratorNode` - 装饰节点，包含单个子节点

### 内置节点
- `RootNode` - 根节点，行为树入口
- `SequencerNode` - 顺序节点，按顺序执行子节点
- `RepeaterNode` - 重复节点，持续执行子节点
- `WaitNode` - 等待节点，等待指定时间后返回成功
- `DebugLogNode` - 调试日志节点

### 编辑器扩展
- **可视化编辑器**：基于 GraphView 的图形化节点编辑
- **节点视图**：可视化节点展示和连接
- **自定义样式**：专门的编辑器样式配置

## 使用方式

1. 创建 `BehaviorTree` 资产
2. 在编辑器中可视化配置节点
3. 将 `BehaviorTreeRunner` 组件添加到 GameObject
4. 关联行为树资产并运行
