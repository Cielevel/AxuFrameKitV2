# ToggleBtnGroup 使用指南

## 概述

`ToggleBtnGroup` 是一个功能强大的 Toggle 组管理系统，支持单选、多选和防空选等多种模式。它通过集中管理状态逻辑，提供了运行时动态注册和灵活的配置选项。

## 核心组件

### 1. UIToggleParent
Toggle 组的控制器，负责管理所有子 Toggle 的状态和行为。

### 2. UIToggleChild
Toggle 子单元，代表单个可点击的按钮。

### 3. UIToggleBaseUnit
表现层基类，用于定义 Toggle 的视觉效果（如颜色、图片、GameObject 激活状态等）。

**内置实现：**
- `UIUnitToggleColor` - 颜色切换
- `UIUnitToggleSprite` - 图片切换
- `UIUnitToggleGo` - GameObject 显示/隐藏

## 快速开始

### 步骤 1：创建 Toggle 组结构

1. 创建一个父 GameObject，添加 `UIToggleParent` 组件
2. 在父对象下创建多个子 GameObject，每个添加 `UIToggleChild` 组件
3. 为每个 `UIToggleChild` 添加 `Button` 组件（会自动添加）

### 步骤 2：配置 UIToggleParent

在 Inspector 中配置以下参数：

```
Toggle Mode (选择模式):
- Single: 单选模式（只能选中一个）
- Multiple: 多选模式（可以选中多个）

Allow Empty (允许空选):
- true: 允许所有按钮都不选中
- false: 至少保持一个按钮选中

Toggle Child Config (子按钮配置):
- Child: 拖入对应的 UIToggleChild 对象
- ID: 设置唯一标识符（字符串）
- Default State: 设置初始选中状态
```

### 步骤 3：添加视觉效果

在每个 `UIToggleChild` 上添加表现层组件：

#### 示例 1：颜色切换
```csharp
// 添加 UIUnitToggleColor 组件
// 配置：
- Target Image: 需要改变颜色的 Image 组件
- On Color: 选中时的颜色
- Off Color: 未选中时的颜色
```

#### 示例 2：图片切换
```csharp
// 添加 UIUnitToggleSprite 组件
// 配置：
- Target Image: 需要改变图片的 Image 组件
- On Sprite: 选中时的图片
- Off Sprite: 未选中时的图片
```

#### 示例 3：GameObject 显示/隐藏
```csharp
// 添加 UIUnitToggleGo 组件
// 配置：
- Target Go: 需要控制显示/隐藏的 GameObject
```

## 代码使用

### 通过索引选中

```csharp
// 获取 UIToggleParent 组件
UIToggleParent toggleParent = GetComponent<UIToggleParent>();

// 选中索引为 0 的按钮
toggleParent.SelectIndex(0);

// 选中索引为 1 的按钮（不触发事件）
toggleParent.SelectIndex(1, triggerEvent: false);
```

### 通过 ID 选中

```csharp
// 选中 ID 为 "tab1" 的按钮
toggleParent.SelectID("tab1");

// 选中 ID 为 "tab2" 的按钮（不触发事件）
toggleParent.SelectID("tab2", triggerEvent: false);
```

### 监听状态变化

#### 单选模式
```csharp
// 在 UIToggleParent 上监听单选变化
toggleParent.OnSingleChange += (selectedChild) =>
{
    Debug.Log($"选中了: {selectedChild.ID}");
};
```

#### 多选模式
```csharp
// 在 UIToggleParent 上监听多选变化
toggleParent.OnMultipleChange += (selectedChildren) =>
{
    Debug.Log($"当前选中数量: {selectedChildren.Count}");
    foreach (var child in selectedChildren)
    {
        Debug.Log($"选中: {child.ID}");
    }
};
```

#### 监听单个按钮状态变化
```csharp
// 在 UIToggleChild 上监听状态变化
UIToggleChild toggleChild = GetComponent<UIToggleChild>();
toggleChild.OnStateChanged += (isOn) =>
{
    Debug.Log($"按钮 {toggleChild.ID} 状态变为: {isOn}");
};
```

### 运行时动态初始化

如果按钮是动态实例化的，可以手动调用初始化：

```csharp
// 在动态创建按钮后调用
toggleParent.InitializeGroup();
```

## 高级用法

### 自定义表现层单元

继承 `UIToggleBaseUnit` 创建自定义视觉效果：

```csharp
using AxuFrameKit.Common.UI.Kit.ToggleBtnGroup;
using UnityEngine;

public class UIUnitToggleScale : UIToggleBaseUnit
{
    [SerializeField] private Transform _targetTransform;
    [SerializeField] private Vector3 _onScale = Vector3.one * 1.2f;
    [SerializeField] private Vector3 _offScale = Vector3.one;

    public override void SetState(bool isOn)
    {
        if (_targetTransform != null)
        {
            _targetTransform.localScale = isOn ? _onScale : _offScale;
        }
    }
}
```

### 编辑器自动填充

在 `UIToggleParent` 的 Inspector 中：
1. 点击右键菜单
2. 选择 "Auto Populate Children"
3. 自动扫描并填充所有子对象中的 `UIToggleChild`

## 常见应用场景

### 1. 标签页切换
```csharp
// 配置为单选模式，不允许空选
Toggle Mode: Single
Allow Empty: false

// 监听切换事件，显示对应的内容面板
toggleParent.OnSingleChange += (selectedChild) =>
{
    ShowPanel(selectedChild.Index);
};
```

### 2. 多选过滤器
```csharp
// 配置为多选模式，允许空选
Toggle Mode: Multiple
Allow Empty: true

// 监听变化，应用过滤条件
toggleParent.OnMultipleChange += (selectedChildren) =>
{
    List<string> filters = selectedChildren.Select(c => c.ID).ToList();
    ApplyFilters(filters);
};
```

### 3. 设置选项
```csharp
// 配置为多选模式
Toggle Mode: Multiple

// 保存设置
void SaveSettings()
{
    foreach (var child in toggleParent.GetSelectedChildren())
    {
        PlayerPrefs.SetInt(child.ID, child.IsOn ? 1 : 0);
    }
}
```

## 注意事项

1. **ID 唯一性**：确保每个 Toggle 的 ID 在组内是唯一的
2. **初始化时机**：如果使用动态创建，确保在使用前调用 `InitializeGroup()`
3. **事件触发**：某些操作可以选择是否触发事件（`triggerEvent` 参数）
4. **防空选逻辑**：在单选模式下，如果 `Allow Empty` 为 false，点击已选中的按钮不会取消选中

## 特殊功能

### UIToggleParentForPanel
专门用于面板切换的扩展类，继承自 `UIToggleParent`，提供了更便捷的面板管理功能。

```csharp
// 使用 UIToggleParentForPanel 可以直接关联面板 GameObject
// 选中按钮时自动显示/隐藏对应的面板
```

## 调试技巧

1. **查看当前状态**：在运行时查看 `UIToggleChild.IsOn` 属性
2. **验证配置**：`OnValidate` 方法会在编辑器中自动验证配置
3. **日志输出**：在事件回调中添加日志，追踪状态变化

## 性能优化建议

1. 避免在事件回调中执行耗时操作
2. 使用对象池管理动态创建的 Toggle
3. 合理使用 `triggerEvent` 参数，避免不必要的事件触发
