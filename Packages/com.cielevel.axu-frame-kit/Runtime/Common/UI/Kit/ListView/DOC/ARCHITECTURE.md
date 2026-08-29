# UIListView 架构设计文档

## 设计理念

UIListView的设计参考了以下现代框架的优秀实践：

1. **Unity UI Toolkit ListView** - 虚拟化渲染、数据绑定模式
2. **Android RecyclerView** - ViewHolder模式、多种LayoutManager
3. **iOS UICollectionView** - 灵活的布局系统、重用机制
4. **React虚拟列表** - 高性能渲染策略

## 架构概览

```
UIListView (核心控制器)
├── UIListViewItemPool (对象池管理)
├── IListViewLayoutEngine (布局引擎接口)
│   ├── VerticalLayoutEngine (垂直布局)
│   ├── HorizontalLayoutEngine (水平布局)
│   └── GridLayoutEngine (网格布局)
└── UIListViewItem (Item基类)
    └── CustomItem (用户自定义Item)
```

## 核心组件详解

### 1. UIListView - 核心控制器

**职责**：
- 管理整个列表的生命周期
- 协调布局引擎、对象池、数据源
- 处理滚动事件并触发虚拟化更新
- 管理选中状态
- 提供公开API

**关键设计**：
- 使用策略模式管理不同布局引擎
- 事件驱动的数据更新机制
- 分离配置（Settings）和运行时状态

### 2. UIListViewItem - Item抽象

**职责**：
- 定义Item的标准接口
- 处理点击事件
- 管理选中状态的视觉反馈
- 提供数据绑定抽象方法

**设计模式**：
- **模板方法模式**：定义OnUpdateData等抽象方法供子类实现
- **观察者模式**：内部通知ListView点击事件

**关键特性**：
- 泛型数据获取辅助方法
- 生命周期钩子（OnShow/OnHide）
- 可选的视觉反馈组件

### 3. UIListViewItemPool - 对象池

**职责**：
- 管理Item实例的创建和回收
- 减少Instantiate和Destroy的开销
- 支持预热功能

**实现细节**：
- 使用Stack<T>作为池存储结构（LIFO）
- 记录所有创建的实例以便清理
- Dispose模式确保资源释放

**性能优化**：
- 首次Get时创建，后续从池中取出
- 避免频繁的GameObject激活/失活

### 4. 布局引擎系统

#### IListViewLayoutEngine接口

定义了布局引擎必须实现的核心方法：
- `CalculateContentSize` - 计算content总大小
- `CalculateVisibleItemCount` - 计算可见Item数量
- `GetVisibleItemRange` - 获取当前应显示的Item范围
- `PositionItem` - 设置Item的位置和大小
- `GetItemPosition` - 获取Item位置（用于滚动定位）

#### VerticalLayoutEngine - 垂直布局

**算法**：
```
contentHeight = paddingTop + paddingBottom + 
                (itemCount * itemSize) + 
                ((itemCount - 1) * spacing)

startIndex = floor((scrollY - paddingTop) / (itemSize + spacing))
endIndex = startIndex + visibleCount

itemY = -paddingTop - (index * (itemSize + spacing))
```

**特点**：
- 从上到下排列
- Content锚点在顶部
- 支持填充宽度或固定宽度

#### HorizontalLayoutEngine - 水平布局

**算法**：
```
contentWidth = paddingLeft + paddingRight + 
               (itemCount * itemSize) + 
               ((itemCount - 1) * spacing)

startIndex = floor((-scrollX - paddingLeft) / (itemSize + spacing))
endIndex = startIndex + visibleCount

itemX = paddingLeft + (index * (itemSize + spacing))
```

**特点**：
- 从左到右排列
- Content锚点在左侧
- 支持填充高度或固定高度

#### GridLayoutEngine - 网格布局

**算法**：
```
rowCount = ceil(itemCount / columnCount)
contentHeight = paddingTop + paddingBottom + 
                (rowCount * itemSize) + 
                ((rowCount - 1) * spacing)

row = index / columnCount
col = index % columnCount

itemX = paddingLeft + col * (itemWidth + spacing)
itemY = -paddingTop - row * (itemSize + spacing)
```

**特点**：
- 固定列数，行数自适应
- 每列宽度均分可用空间
- 垂直滚动，网格分布

## 虚拟化渲染原理

### 核心思想

只渲染屏幕可见区域及缓冲区的Item，大幅减少GameObject数量。

### 工作流程

```
1. 计算可见范围
   ↓
2. 检查当前activeItems
   ↓
3. 回收超出范围的Item
   ↓
4. 创建/更新可见范围内的Item
   ↓
5. 监听滚动事件，重复1-4
```

### 关键算法

```csharp
private void RefreshVisibleItems()
{
    // 1. 根据滚动位置计算可见范围
    var range = _layoutEngine.GetVisibleItemRange(_visibleItemCount);
    
    // 2. 回收超出范围的Item
    foreach (var item in _activeItems.ToList())
    {
        if (item.DataIndex < range.start || item.DataIndex >= range.end)
        {
            RecycleItem(item);
            _activeItems.Remove(item);
        }
    }
    
    // 3. 创建可见范围内的Item
    for (int i = range.start; i < range.end; i++)
    {
        if (!IsItemActive(i))
        {
            var item = GetOrCreateItem();
            item.DataIndex = i;
            UpdateItemData(item, i);
            _layoutEngine.PositionItem(item, i);
            _activeItems.Add(item);
        }
    }
}
```

### 性能分析

假设列表有1000个Item，每个Item 100像素高，屏幕高度1920像素：

- **传统方式**：实例化1000个GameObject
- **虚拟化方式**：实例化约 1920/100 + bufferCount ≈ 22个GameObject

**性能提升**：约45倍减少GameObject数量！

## 数据绑定机制

### 类型安全设计

```csharp
// 在UIListView中，数据源是泛型的
public void SetDataSource<T>(IList<T> dataSource)

// 在UIListViewItem中，提供类型安全的获取方法
protected T GetData<T>()
protected bool TryGetData<T>(out T result)
```

### 数据流

```
User Data (List<MyData>)
    ↓
ListView.SetDataSource() → 转换为 IList<object>
    ↓
OnScrollChanged → UpdateItemData()
    ↓
Item.UpdateData() → OnUpdateData()
    ↓
用户在OnUpdateData中通过GetData<T>()获取强类型数据
    ↓
更新UI组件 (Text, Image, etc.)
```

## 选中管理系统

### 状态维护

使用`HashSet<UIListViewItem>`存储选中的Item，提供O(1)的查找性能。

### 选中逻辑

```csharp
OnItemClicked(item)
    ↓
enableSelection检查
    ↓
单选模式? → 清空其他选中
    ↓
allowDeselect检查
    ↓
SetItemSelected(item, newState)
    ↓
触发事件回调
```

### 与虚拟化的配合

**挑战**：Item会被回收和复用，如何保持选中状态？

**解决方案**：
- 选中状态存储在UIListView中，而非Item上
- Item回收时自动取消选中状态
- Item创建/更新时，从ListView恢复选中状态（如需要）

## 事件系统

### 分层事件设计

```
用户操作 (点击Item)
    ↓
UIListViewItem.OnPointerClick
    ↓
Item内部处理 (OnItemClicked)
    ↓
通知ListView (OnItemClickedInternal)
    ↓
ListView处理选中逻辑
    ↓
触发公开事件 (OnItemClicked, OnSelectionChanged)
    ↓
用户代码处理
```

### 事件类型

1. **Item级事件**：在Item内部处理，子类可重写
2. **ListView级事件**：通过C# event暴露给用户代码

## 扩展性设计

### 1. 自定义布局引擎

实现`IListViewLayoutEngine`接口即可添加新布局：

```csharp
public class WaterfallLayoutEngine : BaseLayoutEngine
{
    // 实现瀑布流布局
}

// 在UIListView中注册
private IListViewLayoutEngine CreateLayoutEngine()
{
    switch (settings.layoutMode)
    {
        case LayoutMode.Waterfall:
            return new WaterfallLayoutEngine(...);
        // ...
    }
}
```

### 2. 自定义Item行为

继承`UIListViewItem`并重写虚方法：

```csharp
public class AnimatedItem : UIListViewItem
{
    protected override void OnShow()
    {
        PlayEnterAnimation();
    }
    
    protected override void OnSelectionChanged(bool isSelected)
    {
        PlaySelectionAnimation(isSelected);
    }
}
```

### 3. 自定义对象池策略

虽然目前使用内置对象池，但可轻松替换：

```csharp
public interface IItemPool
{
    UIListViewItem Get();
    void Return(UIListViewItem item);
}

// 可实现更复杂的池策略，如分级池、预热策略等
```

## 优化技术

### 1. 对象池预热

```csharp
_itemPool.Prewarm(visibleItemCount);
```

在数据绑定前预创建Item，避免首次滚动时的卡顿。

### 2. 延迟刷新

滚动时不立即刷新，通过`_currentStartIndex`检测，只在范围变化时刷新。

### 3. 批量操作

```csharp
RecycleOutOfRangeItems(); // 先回收
UpdateVisibleItems();     // 再创建
```

分离回收和创建操作，减少中间状态。

### 4. 缓冲区设计

bufferCount参数增加上下缓冲，减少滚动时的频繁创建/销毁。

## 已知问题与未来改进

### 当前版本限制

1. **固定Item大小**：目前只支持统一大小的Item
2. **网格布局限制**：只支持固定列数
3. **滚动动画**：ScrollToItem的平滑动画未实现

### 计划改进

#### v1.1
- [ ] 支持动态Item高度/宽度
- [ ] 平滑滚动动画
- [ ] 更多布局模式（瀑布流、交错网格）

#### v1.2
- [ ] 拖拽排序支持
- [ ] 分组头部支持
- [ ] 下拉刷新/上拉加载

#### v2.0
- [ ] 与UIDataBind系统集成
- [ ] 支持UI Toolkit版本
- [ ] 编辑器内实时预览

## 性能基准测试

（待测试数据补充）

| 数据量 | GameObject数 | 内存占用 | 滚动FPS |
|--------|--------------|----------|---------|
| 100 | ~20 | TBD | TBD |
| 1,000 | ~20 | TBD | TBD |
| 10,000 | ~20 | TBD | TBD |
| 100,000 | ~20 | TBD | TBD |

## 总结

UIListView通过虚拟化渲染、对象池、灵活的布局引擎等技术，提供了一个高性能、易扩展的列表解决方案。它的设计充分考虑了Unity UGUI的特点，同时借鉴了现代UI框架的最佳实践，为Unity开发者提供了一个与主流框架同等水平的列表组件。
