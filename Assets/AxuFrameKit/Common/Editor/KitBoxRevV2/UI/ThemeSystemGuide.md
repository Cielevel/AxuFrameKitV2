# KitBoxRevV2 主题系统指南

## 🎨 主题概述

KitBoxRevV2 现在支持**多主题切换**，无需修改任何功能代码，只需更换 USS 样式表即可完全改变界面风格！

---

## 📦 可用主题

### 1. **默认主题** (`AxuKitBoxV2.uss`)
- 🎨 **风格**: 现代简约，深色主题
- 🎯 **特点**: 
  - 蓝色主题色 (#3498db)
  - 深灰色背景
  - 简洁的边框和圆角
  - 适中的间距和字体大小

### 2. **Unreal Engine 5 主题** (`AxuKitBoxV2_UE5Theme.uss`) ⭐ 新增
- 🎨 **风格**: 仿 UE5 编辑器，专业暗黑风格
- 🎯 **特点**:
  - UE5 标志性蓝色 (#0e7afe)
  - 更深的黑色背景
  - 橙色强调色
  - 更大的按钮和间距
  - 发光文字效果
  - 悬停动画效果
  - 专业的边框高亮

---

## 🔄 如何切换主题

### 方法：修改 `AxuKitBoxRevV2Window.cs`

打开文件：`Assets/AxuFrameKit/Common/Editor/KitBoxRevV2/AxuKitBoxRevV2Window.cs`

找到 `CreateGUI()` 方法中的主题选择部分：

#### 使用默认主题（当前）
```csharp
// Theme Selection - Switch between default and UE5 theme
// Default Theme:
var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/AxuFrameKit/Common/Editor/KitBoxRevV2/UI/AxuKitBoxV2.uss");

// UE5 Theme (uncomment to use):
// var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/AxuFrameKit/Common/Editor/KitBoxRevV2/UI/AxuKitBoxV2_UE5Theme.uss");
```

#### 切换到 UE5 主题
```csharp
// Theme Selection - Switch between default and UE5 theme
// Default Theme:
// var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/AxuFrameKit/Common/Editor/KitBoxRevV2/UI/AxuKitBoxV2.uss");

// UE5 Theme (uncomment to use):
var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/AxuFrameKit/Common/Editor/KitBoxRevV2/UI/AxuKitBoxV2_UE5Theme.uss");
```

**只需注释/取消注释对应的行即可！**

---

## 🎨 UE5 主题详细特性

### 配色方案

| 元素 | 颜色 | 说明 |
|------|------|------|
| 主背景 | `#0d0d0d` | 极深黑色，类似 UE5 |
| 面板背景 | `#212121` | 深灰色面板 |
| 主题色 | `#0e7afe` | UE5 标志性蓝色 |
| 强调色 | `#ff9800` | 橙色，用于高亮 |
| 成功色 | `#4caf50` | 绿色 |
| 危险色 | `#f44336` | 红色 |
| 主文本 | `#e0e0e0` | 浅灰色 |
| 次要文本 | `#9e9e9e` | 中灰色 |
| 静音文本 | `#616161` | 深灰色 |

### 视觉效果

#### 1. **发光文字效果**
```css
-unity-text-outline-width: 0.5px;
-unity-text-outline-color: rgba(14, 122, 254, 0.3);
```
- 标题和重要文本带有蓝色发光轮廓
- 模仿 UE5 的文字高亮效果

#### 2. **动画过渡**
```css
transition-property: background-color, scale, translate;
transition-duration: 0.15s;
```
- 按钮悬停时有缩放效果
- 导航项悬停时有平移效果
- 卡片悬停时有上浮效果

#### 3. **边框高亮**
- 选中的导航项有左侧橙色边框
- 页面标题有左侧蓝色边框
- 配置盒子有完整的边框

#### 4. **更大的尺寸**
- 按钮高度: 42-48px（默认主题 40-45px）
- 间距更宽松
- 字体略大

---

## 📊 主题对比

### 视觉对比

| 特性 | 默认主题 | UE5 主题 |
|------|---------|---------|
| **背景色** | `#2a2a2a` | `#0d0d0d` ⬇️ 更暗 |
| **主题色** | `#3498db` | `#0e7afe` 🔵 更亮 |
| **按钮高度** | 40-45px | 42-48px ⬆️ 更大 |
| **文字效果** | 无 | 发光轮廓 ✨ |
| **悬停动画** | 简单 | 缩放+平移 🎬 |
| **边框样式** | 简约 | 多层次 📐 |
| **整体风格** | 简约现代 | 专业工业 |

### 适用场景

#### 默认主题适合：
- ✅ 日常开发使用
- ✅ 追求简洁的用户
- ✅ 性能优先的场景

#### UE5 主题适合：
- ✅ UE 开发者
- ✅ 追求视觉效果
- ✅ 专业工具展示
- ✅ 暗黑环境工作

---

## 🛠️ 自定义主题

### 创建自己的主题

1. **复制现有主题文件**
   ```
   AxuKitBoxV2.uss → AxuKitBoxV2_MyTheme.uss
   ```

2. **修改 CSS 变量**
   ```css
   :root {
       --my-primary-color: #your-color;
       --my-bg-color: #your-bg;
       /* ... */
   }
   ```

3. **调整样式类**
   - 修改 `.btn-primary` 的颜色
   - 调整 `.page-header` 的样式
   - 自定义 `.nav-item` 的效果

4. **在代码中引用**
   ```csharp
   var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(
       "Assets/.../AxuKitBoxV2_MyTheme.uss"
   );
   ```

### 推荐的主题变体

#### 🌸 粉色主题
```css
:root {
    --accent-color: #e91e63;
    --accent-hover: #c2185b;
}
```

#### 🟢 绿色主题
```css
:root {
    --accent-color: #4caf50;
    --accent-hover: #388e3c;
}
```

#### 🟣 紫色主题
```css
:root {
    --accent-color: #9c27b0;
    --accent-hover: #7b1fa2;
}
```

#### ☀️ 亮色主题
```css
:root {
    --main-bg: #f5f5f5;
    --panel-bg: #ffffff;
    --text-color: #212121;
    --border-color: #e0e0e0;
}
```

---

## 🎯 主题系统的优势

### 1. **零代码修改**
- ✅ 不需要改动任何 C# 代码
- ✅ 不影响功能逻辑
- ✅ 不会引入 bug

### 2. **即时切换**
- ✅ 只需注释/取消注释一行
- ✅ 重新编译即可看到效果
- ✅ 可以快速对比不同主题

### 3. **完全分离**
- ✅ 样式与逻辑完全分离
- ✅ 易于维护和扩展
- ✅ 团队协作友好

### 4. **无限可能**
- ✅ 可以创建任意数量的主题
- ✅ 可以模仿任何编辑器风格
- ✅ 可以根据项目定制

---

## 📋 主题检查清单

创建新主题时，确保覆盖以下样式：

### 基础布局
- [ ] `:root` 变量定义
- [ ] `#root-container`
- [ ] `#sidebar`
- [ ] `#main-content`

### 导航
- [ ] `.nav-item`
- [ ] `.nav-item--selected`
- [ ] `.group-foldout`

### 按钮
- [ ] `.btn-primary`
- [ ] `.btn-success`
- [ ] `.btn-danger`
- [ ] `.btn-secondary`

### 容器
- [ ] `.page-container`
- [ ] `.config-box`
- [ ] `.card`
- [ ] `.slot-container`

### 文本
- [ ] `.page-header`
- [ ] `.text-secondary`
- [ ] `.text-muted`

### 列表
- [ ] `.asset-list`
- [ ] `.log-view`
- [ ] `.list-item-row`

---

## 🚀 未来扩展

### 计划中的功能

1. **运行时主题切换**
   - 添加主题选择下拉菜单
   - 无需重启即可切换

2. **主题预览**
   - 在设置中预览所有主题
   - 实时对比效果

3. **主题导入/导出**
   - 分享自定义主题
   - 导入社区主题

4. **主题编辑器**
   - 可视化编辑主题
   - 实时预览效果

---

## 📸 效果预览

### 默认主题
```
┌─────────────────────────────────────┐
│ AxuFrameKit V2      [蓝色标题栏]   │
├──────────┬──────────────────────────┤
│ 📁 通用  │ 材质球批量关联复制       │
│ 📁 材质  │ [蓝色按钮]               │
│ 📁 表格  │                          │
│ 📁 实用  │ [灰色背景面板]           │
└──────────┴──────────────────────────┘
```

### UE5 主题
```
┌─────────────────────────────────────┐
│ AxuFrameKit V2  [发光蓝色标题栏]   │
├──────────┬──────────────────────────┤
│ 📁 通用  │ 材质球批量关联复制       │
│ ▶ 材质   │ [亮蓝色按钮 + 阴影]      │
│ 📁 表格  │                          │
│ 📁 实用  │ [极深黑色背景 + 边框]    │
└──────────┴──────────────────────────┘
```

---

## ✨ 总结

通过样式分离架构：

✅ **实现了完全的主题化**  
✅ **零代码修改切换主题**  
✅ **可以模仿任何编辑器风格**  
✅ **无限的自定义可能性**

现在您可以：
1. 使用默认主题进行日常开发
2. 切换到 UE5 主题获得专业体验
3. 创建自己的主题来匹配项目风格

**只需修改 USS 文件，就能完全改变界面外观！** 🎨✨
