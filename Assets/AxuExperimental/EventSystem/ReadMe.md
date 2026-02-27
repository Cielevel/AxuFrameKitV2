# EventSystem - 事件系统

## 功能概述

一个灵活的事件系统，基于 ScriptableObject 的事件通道模式，支持类型安全的事件传递。

## 主要特性

### 两种事件模式
- **全局 EventBus**：单例模式，支持订阅/发布、过滤器和异步等待
- **事件通道 (EventChannel)**：基于 ScriptableObject，可配置独立的事件通道

### 事件通道
- `GameEventChannel<T>` - 泛型事件通道基类
- `GameEventUnityEvent<T>` - 可序列化的 UnityEvent 泛型类
- 支持自定义事件类型（如 `PlayerDiedEventChannel`）

### 监听方式
- **C# 回调**：通过代码订阅事件
- **UnityEvent**：在 Inspector 中配置响应
- **EventListener 组件**：MonoBehaviour 形式的事件监听器

### 高级功能
- **一次性订阅**：自动取消订阅的单次监听
- **事件过滤器**：基于条件过滤事件
- **异步等待**：支持 async/await 等待事件触发

### 编辑器工具
- **创建菜单**：快速创建事件通道资产
- **自定义编辑器**：事件通道专用 Inspector 界面
- **自动注册**：`AutoRegisterEventChannels` 自动加载事件通道

## 使用场景

- 解耦游戏系统之间的通信
- UI 与游戏逻辑的交互
- 跨场景事件传递
