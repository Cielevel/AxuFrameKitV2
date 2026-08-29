# UIListView - 现代化列表视图组件

## 概述

UIListView是一个为Unity UGUI设计的高性能、功能丰富的列表视图组件。它参考了现代UI框架（如Unity UI Toolkit、RecyclerView等）的设计理念，提供了虚拟化渲染、对象池、多种布局模式等先进特性。

## 核心特性

### 1. 虚拟化渲染
- 只实例化可见区域的Item，大幅提升大数据量时的性能
- 智能缓冲区设计，确保滚动流畅

### 2. 对象池系统
- 自动复用Item实例，减少GC压力
- 可配置是否启用对象池
- 支持预热功能

### 3. 多种布局模式
- **垂直布局**：经典的垂直滚动列表
- **水平布局**：水平滚动列表
- **网格布局**：类Pinterest的瀑布流网格

### 4. 选中管理
- 支持单选/多选模式
- 可配置是否允许取消选中
- 完整的选中状态事件回调

### 5. 数据驱动
- 类型安全的数据绑定
- 支持任意数据类型
- 数据更新自动刷新UI

### 6. 完整的生命周期
- Item创建、更新、点击、选中等事件回调
- 子类可重写生命周期方法自定义行为

## 快速开始

### 步骤1: 创建Item预制体

1. 在场景中创建一个GameObject，添加必要的UI组件（如Text、Image等）
2. 添加`UIListViewItem`组件或继承自它的自定义组件
3. 将其保存为预制体

**推荐：创建自定义Item类**

```csharp
using AxuFrameKit.Common.UI.Kit.ListView;
using UnityEngine;
using TMPro;

public class MyCustomItem : UIListViewItem
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;

    protected override void OnUpdateData(object data, int index)
    {
        if (TryGetData<MyDataClass>(out var myData))
        {
            titleText.text = myData.Title;
            descText.text = myData.Description;
        }
    }

    protected override void OnSelectionChanged(bool isSelected)
    {
        titleText.color = isSelected ? Color.yellow : Color.white;
    }
}
```

### 步骤2: 设置ListView

1. 在场景中创建一个GameObject
2. 添加`ScrollRect`组件并配置好Content和Viewport
3. 添加`UIListView`组件
4. 在Inspector中配置：
   - 选择布局模式
   - 设置Item大小和间距
   - 拖入Item预制体
   - 配置其他选项

### 步骤3: 绑定数据

```csharp
using AxuFrameKit.Common.UI.Kit.ListView;
using System.Collections.Generic;
using UnityEngine;

public class MyController : MonoBehaviour
{
    [SerializeField] private UIListView listView;

    private void Start()
    {
        // 创建数据
        var data = new List<MyDataClass>();
        for (int i = 0; i < 1000; i++)
        {
            data.Add(new MyDataClass 
            { 
                Title = $"Item {i}",
                Description = $"Description for item {i}"
            });
        }

        // 绑定数据
        listView.SetDataSource(data);

        // 监听事件
        listView.OnItemClicked += (item, index) =>
        {
            Debug.Log($"Clicked item {index}");
        };

        listView.OnSelectionChanged += (selectedIndices) =>
        {
            Debug.Log($"Selected: {string.Join(", ", selectedIndices)}");
        };
    }
}

public class MyDataClass
{
    public string Title;
    public string Description;
}
```

## 详细配置

### 布局设置 (ListViewSettings)

| 参数 | 说明 | 默认值 |
|------|------|--------|
| layoutMode | 布局模式（Vertical/Horizontal/Grid） | Vertical |
| itemSize | Item大小（垂直=高度，水平=宽度） | 100 |
| spacing | Item间距 | 0 |
| gridConstraintCount | 网格模式下每行的Item数量 | 2 |
| padding | 四周边距 | (0,0,0,0) |
| bufferCount | 缓冲区额外Item数量 | 2 |
| usePooling | 是否启用对象池 | true |
| itemPrefab | Item预制体引用 | null |
| controlChildSize | 是否自动调整Item大小填充容器 | true |

### 选中设置 (SelectionSettings)

| 参数 | 说明 | 默认值 |
|------|------|--------|
| enableSelection | 是否启用选中功能 | true |
| allowMultipleSelection | 是否允许多选 | false |
| allowDeselect | 是否允许取消选中 | true |

## API参考

### 数据操作

```csharp
// 设置数据源
void SetDataSource<T>(IList<T> dataSource, bool resetPosition = true)

// 追加数据
void AppendData<T>(IList<T> newData)

// 刷新列表
void RefreshList(bool resetPosition = true)

// 清空列表
void Clear()
```

### 选中操作

```csharp
// 选中指定索引的Item
void SelectItem(int index)

// 取消选中
void DeselectItem(int index)

// 清空所有选中
void ClearSelection()

// 获取选中的索引列表
List<int> GetSelectedIndices()
```

### 导航操作

```csharp
// 滚动到指定Item
void ScrollToItem(int index, bool animated = true)
```

## 事件回调

```csharp
// Item被创建时
event Action<UIListViewItem, int> OnItemCreated;

// Item需要更新数据时
event Action<UIListViewItem, object, int> OnItemUpdate;

// Item被点击时
event Action<UIListViewItem, int> OnItemClicked;

// Item选中状态变化时
event Action<UIListViewItem, bool> OnItemSelectionChanged;

// 选中列表变化时
event Action<List<int>> OnSelectionChanged;
```

## 高级用法

### 自定义Item生命周期

```csharp
public class AdvancedItem : UIListViewItem
{
    protected override void OnInitialize()
    {
        // Item首次创建时调用（只调用一次）
    }

    protected override void OnShow()
    {
        // Item从对象池取出或显示时调用
    }

    protected override void OnHide()
    {
        // Item回收到对象池或隐藏时调用
    }

    protected override void OnItemClicked()
    {
        // Item被点击时调用
    }
}
```

### 动态修改配置

虽然大部分配置应该在编辑器中设置，但某些场景下需要运行时修改：

```csharp
// 注意：运行时修改配置后需要调用RefreshList()
listView.RefreshList(false);
```

## 性能优化建议

1. **合理设置bufferCount**：值越大滚动越流畅，但占用内存越多，建议2-4
2. **启用对象池**：除非特殊需求，建议始终启用
3. **避免在OnUpdateData中执行耗时操作**：数据绑定应尽量简单快速
4. **使用TextMeshPro**：比传统Text性能更好
5. **图片异步加载**：如果Item包含网络图片，使用异步加载避免卡顿

## 与现有组件的对比

| 特性 | UIListView | UILoopVerticalScrollRect | Unity ListView (UI Toolkit) |
|------|-----------|--------------------------|----------------------------|
| 虚拟化渲染 | ✅ | ✅ | ✅ |
| 对象池 | ✅ | ✅ | ✅ |
| 多种布局 | ✅ (3种) | ❌ (仅垂直) | ✅ |
| 选中管理 | ✅ 完整 | ❌ | ✅ |
| UGUI兼容 | ✅ | ✅ | ❌ (UI Toolkit) |
| 事件系统 | ✅ 丰富 | ⚠️ 基础 | ✅ |
| 编辑器友好 | ✅ | ⚠️ | ✅ |

## 已知限制

1. 目前不支持动态Item高度/宽度（计划中）
2. 网格布局仅支持固定列数，不支持自适应（计划中）
3. 滚动动画功能待完善

## 示例场景

在`Examples`文件夹中提供了完整的示例：
- `SimpleListViewItem.cs` - 基础Item实现示例
- `ListViewDemo.cs` - 使用示例和测试工具

## 技术支持

如遇到问题或有功能建议，请参考项目文档或联系开发团队。

---

**版本**: 1.0.0  
**作者**: AxuFrameKit Team  
**最后更新**: 2026-02-03
