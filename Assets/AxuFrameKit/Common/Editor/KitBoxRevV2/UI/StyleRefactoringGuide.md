# KitBoxRevV2 样式重构指南

本文档说明如何使用 `AxuKitBoxV2.uss` 中定义的样式类来替换 C# 代码中的内联样式。

## 样式类使用示例

### 1. 页面容器

**之前（内联样式）：**
```csharp
var root = new VisualElement();
root.style.paddingTop = 20;
root.style.paddingBottom = 20;
root.style.paddingLeft = 20;
root.style.paddingRight = 20;
```

**之后（使用 USS 类）：**
```csharp
var root = new VisualElement();
root.AddToClassList("page-container");
```

---

### 2. 页面标题

**之前：**
```csharp
var header = new Label("标题文本");
header.style.fontSize = 20;
header.style.unityFontStyleAndWeight = FontStyle.Bold;
header.style.marginBottom = 10;
```

**之后：**
```csharp
var header = new Label("标题文本");
header.AddToClassList("page-header");
```

---

### 3. 提示文本

**之前：**
```csharp
var hint = new Label("提示文本");
hint.style.color = new Color(0.6f, 0.6f, 0.6f);
hint.style.marginBottom = 20;
hint.style.whiteSpace = WhiteSpace.Normal;
```

**之后：**
```csharp
var hint = new Label("提示文本");
hint.AddToClassList("page-hint");
```

---

### 4. 主要操作按钮

**之前：**
```csharp
var button = new Button(OnClick);
button.text = "执行操作";
button.style.height = 45;
button.style.fontSize = 15;
button.style.unityFontStyleAndWeight = FontStyle.Bold;
button.style.backgroundColor = new Color(0.2f, 0.5f, 0.8f);
button.style.color = Color.white;
button.style.borderLeftWidth = 0;
button.style.borderRightWidth = 0;
button.style.borderTopWidth = 0;
button.style.borderBottomWidth = 0;
button.style.borderTopLeftRadius = 6;
button.style.borderTopRightRadius = 6;
button.style.borderBottomLeftRadius = 6;
button.style.borderBottomRightRadius = 6;
```

**之后：**
```csharp
var button = new Button(OnClick);
button.text = "执行操作";
button.AddToClassList("btn-primary");
```

---

### 5. 成功/危险按钮

```csharp
// 成功按钮（绿色）
exportButton.AddToClassList("btn-success");

// 危险按钮（红色）
deleteButton.AddToClassList("btn-danger");

// 次要按钮（透明背景）
clearButton.AddToClassList("btn-secondary");

// 小按钮（24x24）
iconButton.AddToClassList("btn-small");
```

---

### 6. 配置盒子

**之前：**
```csharp
var configBox = new VisualElement();
configBox.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
configBox.style.paddingTop = 15;
configBox.style.paddingBottom = 15;
configBox.style.paddingLeft = 15;
configBox.style.paddingRight = 15;
configBox.style.borderTopLeftRadius = 8;
configBox.style.borderTopRightRadius = 8;
configBox.style.borderBottomLeftRadius = 8;
configBox.style.borderBottomRightRadius = 8;
configBox.style.marginBottom = 20;
```

**之后：**
```csharp
var configBox = new VisualElement();
configBox.AddToClassList("config-box");
```

---

### 7. 对象槽容器

**之前：**
```csharp
var box = new VisualElement();
box.style.width = new Length(44, LengthUnit.Percent);
box.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
box.style.paddingTop = 12;
box.style.paddingBottom = 12;
box.style.paddingLeft = 12;
box.style.paddingRight = 12;
box.style.borderTopLeftRadius = 6;
box.style.borderTopRightRadius = 6;
box.style.borderBottomLeftRadius = 6;
box.style.borderBottomRightRadius = 6;

var title = new Label(label);
title.style.unityFontStyleAndWeight = FontStyle.Bold;
title.style.marginBottom = 8;
```

**之后：**
```csharp
var box = new VisualElement();
box.AddToClassList("slot-container");

var title = new Label(label);
title.AddToClassList("slot-title");
```

---

### 8. 日志视图

**之前：**
```csharp
var logView = new ScrollView();
logView.style.flexGrow = 1;
logView.style.minHeight = 150;
logView.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
logView.style.paddingTop = 8;
logView.style.paddingBottom = 8;
logView.style.paddingLeft = 8;
logView.style.paddingRight = 8;
logView.style.borderLeftWidth = 1;
logView.style.borderRightWidth = 1;
logView.style.borderTopWidth = 1;
logView.style.borderBottomWidth = 1;
logView.style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);
logView.style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);
logView.style.borderTopColor = new Color(0.1f, 0.1f, 0.1f);
logView.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);
```

**之后：**
```csharp
var logView = new ScrollView();
logView.AddToClassList("log-view");
```

---

### 9. 资产列表

```csharp
var assetList = new ListView(...);
assetList.AddToClassList("asset-list");
```

---

### 10. 布局辅助类

```csharp
// 水平布局
var row = new VisualElement();
row.AddToClassList("row");

// 水平布局 + 两端对齐
var header = new VisualElement();
header.AddToClassList("row-space-between");

// 水平布局 + 居中对齐
var centerRow = new VisualElement();
centerRow.AddToClassList("row-center");

// 填充剩余空间
textField.AddToClassList("flex-grow");
```

---

### 11. 间距工具类

```csharp
// Margin Bottom
element.AddToClassList("mb-5");   // margin-bottom: 5px
element.AddToClassList("mb-10");  // margin-bottom: 10px
element.AddToClassList("mb-15");  // margin-bottom: 15px
element.AddToClassList("mb-20");  // margin-bottom: 20px
element.AddToClassList("mb-25");  // margin-bottom: 25px
element.AddToClassList("mb-30");  // margin-bottom: 30px

// Margin Top
element.AddToClassList("mt-5");   // margin-top: 5px
element.AddToClassList("mt-10");  // margin-top: 10px
element.AddToClassList("mt-20");  // margin-top: 20px
element.AddToClassList("mt-30");  // margin-top: 30px
```

---

### 12. 文本样式

```csharp
// 次要文本（灰色）
label.AddToClassList("text-secondary");

// 静音文本（更浅的灰色，小字体）
label.AddToClassList("text-muted");

// 粗体
label.AddToClassList("text-bold");
```

---

### 13. 章节标题

```csharp
var sectionHeader = new Label("操作日志");
sectionHeader.AddToClassList("section-header");
```

---

## 完整示例：MaterialCopierRevPage 重构

### 重构前（部分代码）：

```csharp
public VisualElement CreateContent()
{
    var root = new VisualElement();
    root.style.paddingTop = 20;
    root.style.paddingBottom = 20;
    root.style.paddingLeft = 20;
    root.style.paddingRight = 20;

    var header = new Label("材质球批量关联复制");
    header.style.fontSize = 20;
    header.style.unityFontStyleAndWeight = FontStyle.Bold;
    header.style.marginBottom = 10;
    root.Add(header);

    var hint = new Label("根据子物体名称一一对应，将源模型的材质球同步至目标模型。");
    hint.style.color = new Color(0.6f, 0.6f, 0.6f);
    hint.style.marginBottom = 20;
    hint.style.whiteSpace = WhiteSpace.Normal;
    root.Add(hint);

    // ... 更多代码
}
```

### 重构后：

```csharp
public VisualElement CreateContent()
{
    var root = new VisualElement();
    root.AddToClassList("page-container");

    var header = new Label("材质球批量关联复制");
    header.AddToClassList("page-header");
    root.Add(header);

    var hint = new Label("根据子物体名称一一对应，将源模型的材质球同步至目标模型。");
    hint.AddToClassList("page-hint");
    root.Add(hint);

    // ... 更多代码
}
```

---

## 可用的 USS 类完整列表

### 页面结构
- `.page-container` - 页面容器（padding: 20px）
- `.page-header` - 页面主标题
- `.page-subheader` - 页面副标题
- `.page-hint` - 提示文本
- `.section-header` - 章节标题

### 按钮
- `.btn-primary` - 主要按钮（蓝色）
- `.btn-success` - 成功按钮（绿色）
- `.btn-danger` - 危险按钮（红色）
- `.btn-secondary` - 次要按钮（透明）
- `.btn-small` - 小按钮（24x24）

### 容器
- `.config-box` - 配置盒子
- `.card` - 卡片容器
- `.slot-container` - 对象槽容器
- `.slot-title` - 槽标题

### 工具栏
- `.toolbar` - 工具栏
- `.toolbar-spacer` - 工具栏间隔

### 列表和视图
- `.log-view` - 日志视图
- `.asset-list` - 资产列表
- `.list-item-row` - 列表项行

### 布局
- `.row` - 水平布局
- `.row-space-between` - 水平布局 + 两端对齐
- `.row-center` - 水平布局 + 居中
- `.flex-grow` - 填充剩余空间

### 文本
- `.text-secondary` - 次要文本
- `.text-muted` - 静音文本
- `.text-bold` - 粗体

### 间距
- `.mb-5` ~ `.mb-30` - Margin Bottom
- `.mt-5` ~ `.mt-30` - Margin Top

### 特定页面
- `.welcome-*` - 欢迎页样式
- `.feature-card-*` - 功能卡片样式
- `.scene-row` - 场景行样式
- `.symbol-*` - 符号页样式

---

## 重构建议

1. **优先重构高频使用的样式**：如页面容器、标题、按钮等
2. **保留特殊样式的内联定义**：如动态颜色、特殊尺寸等
3. **组合使用多个类**：`element.AddToClassList("btn-primary"); element.AddToClassList("mt-20");`
4. **逐步迁移**：不需要一次性全部重构，可以在维护时逐步替换

---

## 优势

✅ **代码更简洁**：减少 80% 的样式代码  
✅ **易于维护**：统一修改 USS 文件即可  
✅ **样式复用**：避免重复定义相同样式  
✅ **主题切换**：未来可以轻松支持多主题  
✅ **可读性强**：类名语义化，一目了然
