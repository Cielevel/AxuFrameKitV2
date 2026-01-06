# CSV 编辑页面拖拽功能说明

## ✨ 功能概述

`CsvEditViewLiteRevPage` 现在支持完整的 **拖拽加载 CSV 文件** 功能，并且使用 UI Toolkit 实现了更好的视觉反馈！

---

## 🎯 功能特性

### 1. **拖拽加载 CSV 文件**
- ✅ 从文件浏览器拖拽 `.csv` 文件到窗口
- ✅ 自动识别 CSV 文件格式
- ✅ 拒绝非 CSV 文件的拖拽

### 2. **视觉反馈**
- ✅ **拖拽提示文本** - 工具栏显示 "可拖拽 CSV 文件到此处"
- ✅ **拖拽覆盖层** - 拖拽文件时显示半透明蓝色遮罩
- ✅ **释放提示** - 显示 "📄 释放以加载 CSV 文件"
- ✅ **边框高亮** - 蓝色发光边框指示拖拽区域
- ✅ **拒绝反馈** - 非 CSV 文件显示禁止图标

---

## 🎨 视觉效果

### 正常状态
```
┌────────────────────────────────────────┐
│ 📂 打开  💾 保存  +行  +列            │
│ 未加载文件 - 可拖拽 CSV 文件到此处    │
├────────────────────────────────────────┤
│                                        │
│         (空白编辑区域)                 │
│                                        │
└────────────────────────────────────────┘
```

### 拖拽 CSV 文件时
```
┌────────────────────────────────────────┐
│ 📂 打开  💾 保存  +行  +列            │
│ 未加载文件 - 可拖拽 CSV 文件到此处    │
├────────────────────────────────────────┤
│ ╔══════════════════════════════════╗  │
│ ║                                  ║  │
│ ║   📄 释放以加载 CSV 文件         ║  │
│ ║                                  ║  │
│ ╚══════════════════════════════════╝  │
└────────────────────────────────────────┘
    ↑ 半透明蓝色遮罩 + 发光边框
```

### 加载文件后
```
┌────────────────────────────────────────┐
│ 📂 打开  💾 保存  +行  +列            │
│ data.csv                              │
├────────────────────────────────────────┤
│ Name      │ Age  │ City              │
│ Alice     │ 25   │ Beijing           │
│ Bob       │ 30   │ Shanghai          │
└────────────────────────────────────────┘
```

---

## 🔧 技术实现

### UI Toolkit 拖拽事件

```csharp
// 注册拖拽事件
root.RegisterCallback<DragEnterEvent>(OnDragEnter);    // 进入拖拽区域
root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated); // 拖拽中
root.RegisterCallback<DragLeaveEvent>(OnDragLeave);     // 离开拖拽区域
root.RegisterCallback<DragPerformEvent>(OnDragPerform); // 释放文件
```

### 事件处理流程

#### 1. **DragEnterEvent** - 进入拖拽区域
```csharp
private void OnDragEnter(DragEnterEvent evt)
{
    if (DragAndDrop.paths.Length > 0 && IsCsvFile(DragAndDrop.paths[0]))
    {
        _dragOverlay.style.display = DisplayStyle.Flex; // 显示覆盖层
    }
}
```

#### 2. **DragUpdatedEvent** - 拖拽中
```csharp
private void OnDragUpdated(DragUpdatedEvent evt)
{
    if (IsCsvFile(DragAndDrop.paths[0]))
    {
        DragAndDrop.visualMode = DragAndDropVisualMode.Copy; // 显示复制图标
    }
    else
    {
        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected; // 显示禁止图标
    }
}
```

#### 3. **DragLeaveEvent** - 离开拖拽区域
```csharp
private void OnDragLeave(DragLeaveEvent evt)
{
    _dragOverlay.style.display = DisplayStyle.None; // 隐藏覆盖层
}
```

#### 4. **DragPerformEvent** - 释放文件
```csharp
private void OnDragPerform(DragPerformEvent evt)
{
    _dragOverlay.style.display = DisplayStyle.None;
    
    if (IsCsvFile(DragAndDrop.paths[0]))
    {
        DragAndDrop.AcceptDrag();
        LoadCsvFromPath(DragAndDrop.paths[0]); // 加载 CSV
    }
}
```

---

## 🎨 拖拽覆盖层样式

### 覆盖层设计
```csharp
_dragOverlay.style.position = Position.Absolute;
_dragOverlay.style.backgroundColor = new Color(0.2f, 0.5f, 0.8f, 0.3f); // 半透明蓝色
_dragOverlay.style.borderWidth = 3;
_dragOverlay.style.borderColor = new Color(0.3f, 0.7f, 1.0f); // 亮蓝色边框
```

### 提示文本样式
```csharp
dragHint.style.fontSize = 24;
dragHint.style.unityFontStyleAndWeight = FontStyle.Bold;
dragHint.style.color = Color.white;
dragHint.style.unityTextOutlineWidth = 1; // 文字轮廓
dragHint.style.unityTextOutlineColor = new Color(0, 0, 0, 0.5f); // 黑色阴影
```

---

## 📋 使用方法

### 方法 1：拖拽加载（推荐）
1. 从文件浏览器中找到 CSV 文件
2. 拖拽文件到 CSV 编辑窗口
3. 看到蓝色覆盖层时释放鼠标
4. CSV 文件自动加载并显示

### 方法 2：按钮加载
1. 点击工具栏的 "📂 打开" 按钮
2. 在文件选择对话框中选择 CSV 文件
3. 点击确定加载

---

## ✅ 支持的文件格式

### ✅ 接受的文件
- `.csv` - 标准 CSV 文件
- `.CSV` - 大写扩展名也支持

### ❌ 拒绝的文件
- `.txt` - 文本文件
- `.xlsx` - Excel 文件
- `.json` - JSON 文件
- 其他非 CSV 格式

---

## 🔍 文件验证

### 扩展名检查
```csharp
private bool IsCsvFile(string path)
{
    return Path.GetExtension(path)
        .Equals(".csv", StringComparison.OrdinalIgnoreCase);
}
```

- ✅ 不区分大小写
- ✅ 精确匹配 `.csv` 扩展名
- ✅ 拒绝其他格式

---

## 🎯 用户体验优化

### 1. **清晰的提示**
- 工具栏始终显示 "可拖拽 CSV 文件到此处"
- 拖拽时显示大字体提示

### 2. **即时反馈**
- 进入拖拽区域立即显示覆盖层
- 离开拖拽区域立即隐藏覆盖层
- 非 CSV 文件显示禁止图标

### 3. **视觉吸引**
- 半透明蓝色遮罩
- 发光边框效果
- 大字体白色文字 + 黑色轮廓

### 4. **防误操作**
- 只接受 CSV 文件
- 自动拒绝其他格式
- 事件传播控制

---

## 🆚 与 IMGUI 版本对比

### IMGUI 版本（旧）
```csharp
// 简单的拖拽检测
if (Event.current.type == EventType.DragUpdated)
{
    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
}
```

### UI Toolkit 版本（新）
```csharp
// 完整的事件系统
root.RegisterCallback<DragEnterEvent>(OnDragEnter);
root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
root.RegisterCallback<DragLeaveEvent>(OnDragLeave);
root.RegisterCallback<DragPerformEvent>(OnDragPerform);
```

### 优势对比

| 特性 | IMGUI | UI Toolkit |
|------|-------|------------|
| **事件系统** | 简单 | 完整 ✨ |
| **视觉反馈** | 无 | 覆盖层 + 边框 ✨ |
| **提示文本** | 无 | 大字体提示 ✨ |
| **文件验证** | 基础 | 完善 ✨ |
| **用户体验** | 一般 | 优秀 ✨ |

---

## 🚀 未来增强

### 计划中的功能

1. **多文件拖拽**
   - 支持同时拖拽多个 CSV 文件
   - 显示文件列表供选择

2. **拖拽预览**
   - 悬停时显示文件信息
   - 预览前几行数据

3. **拖拽动画**
   - 添加淡入淡出效果
   - 边框脉冲动画

4. **拖拽历史**
   - 记录最近拖拽的文件
   - 快速重新加载

---

## 📝 代码示例

### 完整的拖拽处理代码

```csharp
// 1. 创建覆盖层
_dragOverlay = new VisualElement();
_dragOverlay.style.position = Position.Absolute;
_dragOverlay.style.backgroundColor = new Color(0.2f, 0.5f, 0.8f, 0.3f);
_dragOverlay.style.display = DisplayStyle.None;

var dragHint = new Label("📄 释放以加载 CSV 文件");
dragHint.style.fontSize = 24;
_dragOverlay.Add(dragHint);

root.Add(_dragOverlay);

// 2. 注册事件
root.RegisterCallback<DragEnterEvent>(evt => {
    if (IsCsvFile(DragAndDrop.paths[0]))
        _dragOverlay.style.display = DisplayStyle.Flex;
});

root.RegisterCallback<DragLeaveEvent>(evt => {
    _dragOverlay.style.display = DisplayStyle.None;
});

root.RegisterCallback<DragPerformEvent>(evt => {
    _dragOverlay.style.display = DisplayStyle.None;
    if (IsCsvFile(DragAndDrop.paths[0]))
    {
        DragAndDrop.AcceptDrag();
        LoadCsvFromPath(DragAndDrop.paths[0]);
    }
});
```

---

## ✨ 总结

UI Toolkit 的拖拽功能：

✅ **更强大** - 完整的事件系统  
✅ **更直观** - 丰富的视觉反馈  
✅ **更友好** - 清晰的用户提示  
✅ **更可靠** - 完善的文件验证  
✅ **更现代** - 符合现代 UI 设计规范  

**拖拽功能已经完全实现，并且体验比 IMGUI 版本更好！** 🎉
