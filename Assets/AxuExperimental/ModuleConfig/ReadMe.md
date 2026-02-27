# ModuleConfig - 模块化配置系统

## 功能概述

一个模块化实体配置系统，使用 `SerializeReference` 特性实现多态序列化，支持在 Inspector 中灵活配置组件模块。

## 主要特性

### 模块化架构
- **Module 基类**：抽象基类，所有模块继承实现
- **ModuleEntity 组件**：MonoBehaviour 容器，管理多个模块
- **多态序列化**：使用 `[SerializeReference]` 支持接口/抽象类序列化

### 内置模块示例
- `HealthModule` - 生命值模块
- `MovementModule` - 移动模块
- `AttackModule` - 攻击模块
- `ShieldModule` - 护盾模块

### 编辑器扩展
- **自定义 Inspector**：友好的模块配置界面
- **折叠显示**：每个模块独立折叠
- **下拉菜单**：通过下拉菜单添加新模块
- **类型显示**：清晰展示每个模块的类型信息

## 使用场景

- RPG 角色属性配置
- 敌人 AI 行为参数配置
- 可升级系统的基础架构
- 需要灵活组合功能的实体设计

## 技术亮点

- 利用 Unity 2020.3+ 的 `SerializeReference` 特性
- 完全的编辑器友好，无需代码即可配置
- 易于扩展，添加新模块只需继承基类
