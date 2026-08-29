---
name: Axu Editor UI Toolkit Style
description: Axu Editor工具的UI Toolkit开发规范。当为AxuFrameKit创建Editor工具并采用UI Toolkit时使用此Skill。包含UE5风格的配色、组件样式规范和C#代码模式。
allowed-tools: Read, Write, Glob
---

# Axu Editor UI Toolkit Style Guide

专门用于AxuFrameKit Editor工具开发的UI Toolkit样式规范。基于UE5编辑器风格设计。

## 何时使用此Skill

**使用此Skill当**:
- 为AxuFrameKit创建新的Editor窗口
- 使用UI Toolkit (UXML/USS/C#) 开发Editor工具
- 需要保持与现有KitBox、AvatarDownloader等工具一致的视觉风格

**不使用此Skill当**:
- 开发Runtime UI (使用标准UI Toolkit skill)
- 使用IMGUI开发Editor工具

---

## 核心样式文件

### 主要USS文件
1. **KitBox主题**: `Packages/com.cielevel.axu-frame-kit/Editor/Common/KitBoxRevV2/UI/AxuKitBoxV2.uss`
2. **通用窗口样式**: `Packages/com.cielevel.axu-frame-kit/Editor/Common/Window/CommonWindowStyles.uss`

### 引用方式
```csharp
// 在CreateGUI()或构造函数中加载样式
var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
    "Packages/com.cielevel.axu-frame-kit/Editor/Common/Window/CommonWindowStyles.uss");
rootVisualElement.styleSheets.Add(styleSheet);
```

---

## UE5 配色系统 (Color Palette)

### CSS变量定义
```css
:root {
    /* ===== 背景色 ===== */
    --ue5-bg-darkest: #141414;     /* 最深背景，用于根容器 */
    --ue5-bg-dark: #1a1a1a;        /* 深色背景，用于侧边栏/头部 */
    --ue5-bg-panel: #1e1e1e;       /* 面板背景 */
    --ue5-bg-element: #262626;     /* 元素背景，如卡片/区块 */
    --ue5-bg-hover: #333333;       /* 悬停背景 */
    --ue5-bg-active: #3a3a3a;      /* 激活/按下背景 */
    
    /* ===== 主题强调色 (UE5 Orange) ===== */
    --ue5-accent-primary: #ff9500;     /* 主强调色 */
    --ue5-accent-secondary: #e68a00;   /* 次强调色 */
    --ue5-accent-glow: rgba(255, 149, 0, 0.15);    /* 发光效果 */
    --ue5-accent-border: rgba(255, 149, 0, 0.4);   /* 强调边框 */
    
    /* ===== 边框色 ===== */
    --ue5-border-dark: #0d0d0d;    /* 深边框 */
    --ue5-border-subtle: #383838;  /* 细微边框 */
    --ue5-border-light: #454545;   /* 浅边框 */
    
    /* ===== 文本色 ===== */
    --ue5-text-primary: #e8e8e8;   /* 主文本 */
    --ue5-text-secondary: #a0a0a0; /* 次要文本 */
    --ue5-text-disabled: #666666;  /* 禁用文本 */
    --ue5-text-highlight: #ffffff; /* 高亮文本 */
    
    /* ===== 选中状态 ===== */
    --ue5-selected-bg: #ff9500;    /* 选中背景 (橙色) */
    --ue5-selected-text: #1a1a1a;  /* 选中文本 (深色) */
    --ue5-selected-hover: #ffaa33; /* 选中悬停 */
    
    /* ===== 按钮色 ===== */
    --ue5-btn-bg: #3d3d3d;
    --ue5-btn-hover: #4a4a4a;
    --ue5-btn-active: #555555;
    --ue5-btn-primary-bg: #ff9500;
    --ue5-btn-primary-hover: #ffaa33;
}
```

---

## 常用组件样式

### 1. 主容器 (Root Container)
```css
.ue5-root {
    background-color: #141414;
    color: var(--ue5-text-primary);
    font-size: 12px;
    padding: 4px;
}
```

### 2. 圆角面板 (Rounded Panel)
用于包裹内容区块的卡片式容器：
```css
.rounded-panel {
    background-color: #1e1e1e;
    border-radius: 8px;
    border-width: 1px;
    border-color: var(--ue5-accent-border);
    margin-bottom: 8px;
    overflow: hidden;
}
```

### 3. 区域头部 (Header Label)
```css
.header-label {
    height: 28px;
    background-color: var(--ue5-bg-dark);
    color: var(--ue5-text-secondary);
    font-size: 11px;
    -unity-font-style: bold;
    letter-spacing: 1px;
    padding-left: 12px;
    border-bottom-width: 1px;
    border-bottom-color: var(--ue5-border-dark);
    border-top-left-radius: 7px;
    border-top-right-radius: 7px;
}
```

### 4. Section Panel (带标题的内容区块)
```css
/* 外层容器 */
.section-panel {
    background-color: var(--ue5-bg-element);
    border-radius: 8px;
    border-width: 1px;
    border-color: var(--ue5-accent-border);
    margin-bottom: 16px;
    overflow: hidden;
}

/* 标题栏 - 带左侧橙色边条 */
.section-header {
    flex-direction: row;
    align-items: center;
    background-color: var(--ue5-bg-dark);
    padding: 10px 12px;
    border-bottom-width: 1px;
    border-bottom-color: var(--ue5-border-dark);
    border-left-width: 3px;
    border-left-color: var(--ue5-accent-primary);
}

.section-title {
    font-size: 13px;
    -unity-font-style: bold;
    color: var(--ue5-text-highlight);
    letter-spacing: 0.5px;
}

/* 内容区 */
.section-content {
    padding: 16px;
    background-color: var(--ue5-bg-element);
}
```

---

## 按钮样式

### Primary Button (主要操作)
```css
.btn-primary {
    height: 40px;
    font-size: 14px;
    -unity-font-style: bold;
    background-color: var(--ue5-btn-primary-bg);
    color: var(--ue5-selected-text);
    border-width: 1px;
    border-color: var(--ue5-accent-secondary);
    border-radius: 4px;
    transition-duration: 0.12s;
}

.btn-primary:hover {
    background-color: var(--ue5-btn-primary-hover);
}
```

### Secondary Button (次要操作)
```css
.btn-secondary {
    background-color: var(--ue5-btn-bg);
    color: var(--ue5-text-primary);
    border-width: 1px;
    border-color: var(--ue5-border-dark);
    border-radius: 3px;
}

.btn-secondary:hover {
    background-color: var(--ue5-btn-hover);
    color: var(--ue5-text-highlight);
}
```

### Action Button (小型操作按钮)
```css
.action-btn {
    height: 24px;
    min-width: 28px;
    font-size: 11px;
    background-color: var(--ue5-btn-bg);
    border-width: 1px;
    border-color: var(--ue5-border-dark);
    border-radius: 3px;
    color: var(--ue5-text-primary);
    padding: 0 6px;
}
```

### Toggle Button (切换按钮 - 胶囊型)
```css
.column-toggle-btn {
    margin-right: 6px;
    margin-bottom: 6px;
    height: 28px;
    border-radius: 14px;  /* 胶囊形状 */
    background-color: var(--ue5-btn-bg);
    border-width: 1px;
    border-color: var(--ue5-border-subtle);
    color: var(--ue5-text-secondary);
    padding: 0 12px;
    transition-duration: 0.12s;
}

.column-toggle-btn:hover {
    background-color: var(--ue5-btn-hover);
    color: var(--ue5-text-highlight);
}

/* 激活状态 - 橙色背景 */
.column-toggle-btn.active {
    background-color: var(--ue5-accent-primary);
    border-color: var(--ue5-accent-secondary);
    color: var(--ue5-selected-text);
    -unity-font-style: bold;
}
```

### Danger Button (危险操作)
```css
.btn-danger {
    height: 36px;
    background-color: #5a2a2a;
    color: #ffaaaa;
    border-width: 1px;
    border-color: #7a3a3a;
    border-radius: 4px;
}

.btn-danger:hover {
    background-color: #7a3a3a;
    color: #ffcccc;
}
```

### Success Button (成功操作)
```css
.btn-success {
    height: 36px;
    background-color: #2a5a2a;
    color: #aaffaa;
    border-width: 1px;
    border-color: #3a7a3a;
    border-radius: 4px;
}

.btn-success:hover {
    background-color: #3a7a3a;
    color: #ccffcc;
}
```

---

## 导航项样式 (用于侧边栏)

```css
.nav-item {
    margin: 2px 4px 2px 16px;
    padding: 6px 12px;
    border-radius: 3px;
    background-color: transparent;
    border-width: 0;
    -unity-text-align: middle-left;
    color: var(--ue5-text-secondary);
    font-size: 12px;
    transition-duration: 0.12s;
}

.nav-item:hover {
    background-color: var(--ue5-bg-hover);
    color: var(--ue5-text-highlight);
}

/* 选中状态 - 橙色背景 */
.nav-item--selected {
    background-color: var(--ue5-selected-bg);
    color: var(--ue5-selected-text);
    -unity-font-style: bold;
}

.nav-item--selected:hover {
    background-color: var(--ue5-selected-hover);
    color: var(--ue5-selected-text);
}
```

---

## 列表项样式

```css
.item-row {
    flex-direction: row;
    align-items: center;
    background-color: #242424;
    border-radius: 4px;
    padding: 4px 6px;
    margin-bottom: 4px;
    border-width: 1px;
    border-color: transparent;
}

.item-row:hover {
    background-color: #2e2e2e;
    border-color: var(--ue5-border-subtle);
}

.item-name {
    flex-grow: 1;
    font-size: 11px;
    color: var(--ue5-text-primary);
    -unity-text-align: middle-left;
}
```

---

## 信息提示框

### 标准Info Box
```css
.info-box {
    flex-direction: row;
    align-items: flex-start;
    background-color: rgba(255, 149, 0, 0.08);
    border-radius: 4px;
    border-width: 1px;
    border-left-width: 3px;
    border-color: var(--ue5-accent-border);
    border-left-color: var(--ue5-accent-primary);
    padding: 12px;
    margin-bottom: 12px;
}

.info-box-text {
    font-size: 12px;
    color: var(--ue5-text-secondary);
    white-space: normal;
}
```

### 变体 (Success/Warning/Error)
```css
/* 成功 */
.info-box--success {
    background-color: rgba(42, 90, 42, 0.2);
    border-color: #3a7a3a;
    border-left-color: #5cb85c;
}

/* 警告 */
.info-box--warning {
    background-color: rgba(255, 193, 7, 0.1);
    border-color: rgba(255, 193, 7, 0.3);
    border-left-color: #ffc107;
}

/* 错误 */
.info-box--error {
    background-color: rgba(220, 53, 69, 0.1);
    border-color: rgba(220, 53, 69, 0.3);
    border-left-color: #dc3545;
}
```

---

## 输入字段样式

```css
.unity-text-field > .unity-base-field__input {
    background-color: var(--ue5-bg-darkest);
    border-width: 1px;
    border-color: var(--ue5-border-subtle);
    border-radius: 3px;
    color: var(--ue5-text-primary);
    padding: 4px 8px;
}

.unity-text-field > .unity-base-field__input:focus {
    border-color: var(--ue5-accent-primary);
}

/* Toggle复选框 */
.unity-toggle:checked .unity-toggle__checkmark {
    background-color: var(--ue5-accent-primary);
    border-color: var(--ue5-accent-secondary);
}
```

---

## 滚动条样式 (UE5 Slim)

```css
.unity-scroller--vertical {
    width: 10px;
}

.unity-base-slider__dragger {
    background-color: #4a4a4a;
    border-radius: 2px;
    border-width: 0;
    margin: 2px;
}

.unity-base-slider__dragger:hover {
    background-color: #5a5a5a;
}

.unity-base-slider__dragger:active {
    background-color: var(--ue5-accent-primary);
}
```

---

## 布局辅助类

```css
/* Flexbox 行 */
.row { flex-direction: row; }
.row-space-between { flex-direction: row; justify-content: space-between; }
.row-center { flex-direction: row; align-items: center; }
.flex-grow { flex-grow: 1; }

/* 间距 */
.mb-5 { margin-bottom: 5px; }
.mb-10 { margin-bottom: 10px; }
.mb-15 { margin-bottom: 15px; }
.mb-20 { margin-bottom: 20px; }
.mt-5 { margin-top: 5px; }
.mt-10 { margin-top: 10px; }
.mt-20 { margin-top: 20px; }

/* 文本样式 */
.text-secondary { color: var(--ue5-text-secondary); }
.text-muted { color: var(--ue5-text-disabled); font-size: 11px; }
.text-bold { -unity-font-style: bold; }
```

---

## C# 代码模式

### Editor Window 基础模板
```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class MyEditorWindow : EditorWindow
{
    [MenuItem("AxuFrameKit/My Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<MyEditorWindow>();
        window.titleContent = new GUIContent("My Tool");
        window.minSize = new Vector2(400, 300);
    }

    public void CreateGUI()
    {
        // 加载通用样式
        var commonStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Packages/com.cielevel.axu-frame-kit/Editor/Common/Window/CommonWindowStyles.uss");
        rootVisualElement.styleSheets.Add(commonStyles);
        
        // 添加根容器样式
        rootVisualElement.AddToClassList("ue5-root");
        
        BuildUI();
    }

    private void BuildUI()
    {
        // 创建Section Panel
        var sectionPanel = new VisualElement();
        sectionPanel.AddToClassList("section-panel");
        
        // Section Header
        var header = new VisualElement();
        header.AddToClassList("section-header");
        header.Add(new Label("Settings") { name = "section-title" });
        header.Q<Label>().AddToClassList("section-title");
        sectionPanel.Add(header);
        
        // Section Content
        var content = new VisualElement();
        content.AddToClassList("section-content");
        
        // 添加Primary按钮
        var primaryBtn = new Button(() => Debug.Log("Clicked"));
        primaryBtn.text = "Execute";
        primaryBtn.AddToClassList("btn-primary");
        content.Add(primaryBtn);
        
        sectionPanel.Add(content);
        rootVisualElement.Add(sectionPanel);
    }
}
```

### Toggle Button 实现 (C#)
```csharp
// 创建胶囊型切换按钮
private Button CreateToggleButton(string text, bool initialState, Action<bool> onToggle)
{
    var button = new Button();
    button.text = text;
    button.AddToClassList("column-toggle-btn");
    
    bool isActive = initialState;
    if (isActive) button.AddToClassList("active");
    
    button.clicked += () =>
    {
        isActive = !isActive;
        button.ToggleInClassList("active");
        onToggle?.Invoke(isActive);
    };
    
    return button;
}

// 使用示例
var toggleBtn = CreateToggleButton("Option A", true, (active) => {
    Debug.Log($"Option A is now {(active ? "ON" : "OFF")}");
});
```

### 动态列表项创建
```csharp
private VisualElement CreateItemRow(string itemName, Action onRemove)
{
    var row = new VisualElement();
    row.AddToClassList("item-row");
    
    var nameLabel = new Label(itemName);
    nameLabel.AddToClassList("item-name");
    row.Add(nameLabel);
    
    var removeBtn = new Button(onRemove);
    removeBtn.text = "×";
    removeBtn.AddToClassList("remove-btn");
    row.Add(removeBtn);
    
    return row;
}
```

---

## 常见模式

### 1. 带标题的圆角组
UXML结构:
```xml
<ui:VisualElement class="rounded-panel">
    <ui:Label class="header-label" text="SECTION TITLE"/>
    <ui:VisualElement class="section-content">
        <!-- 内容 -->
    </ui:VisualElement>
</ui:VisualElement>
```

### 2. 表单行
```xml
<ui:VisualElement class="form-row">
    <ui:Label class="form-label" text="Name:"/>
    <ui:TextField class="form-value"/>
</ui:VisualElement>
```

### 3. 带帮助按钮的组
```xml
<ui:VisualElement class="helper-btn-row">
    <ui:Button class="helper-text-btn" text="Select All"/>
    <ui:Button class="helper-text-btn" text="Select None"/>
</ui:VisualElement>
```

---

## 设计原则

1. **深色主题优先**: 所有UI都应该在深色背景下工作良好
2. **橙色强调**: 使用 `#ff9500` 作为主要强调色，用于选中状态和重要操作
3. **圆角面板**: 使用 `border-radius: 8px` 的面板来组织内容
4. **细微边框**: 使用透明度边框 (`rgba(255, 149, 0, 0.4)`) 而不是实色
5. **状态反馈**: 所有交互元素都需要 hover 和 active 状态
6. **过渡动画**: 使用 `transition-duration: 0.12s` 提供流畅的交互感

---

## 相关文件参考

- `Packages/com.cielevel.axu-frame-kit/Editor/Common/KitBoxRevV2/UI/AxuKitBoxV2.uss` - 完整样式参考
- `Packages/com.cielevel.axu-frame-kit/Editor/Common/Window/CommonWindowStyles.uss` - 通用窗口样式
- `Packages/com.cielevel.axu-frame-kit/Editor/Common/KitBoxRevV2/` - KitBox实现参考

## 相关Skills

- **ui-tool-kits-skill**: 通用Unity UI Toolkit开发指南
