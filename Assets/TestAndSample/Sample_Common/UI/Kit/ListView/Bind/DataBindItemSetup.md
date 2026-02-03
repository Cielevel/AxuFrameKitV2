# UIListViewDataBindItem 使用指南

## 概述

`UIListViewDataBindItem` 是一种支持自动数据绑定的 ListView Item，无需继承和重写 `OnUpdateData` 方法，通过声明式配置即可完成数据与 UI 的绑定。

## 使用步骤

### 1. 准备预制体

1. 创建 Item 预制体
2. 添加 `UIListViewDataBindItem` 组件（替换掉原来的 UIListViewItem）
3. 添加 `UIBindParent` 组件
4. 设置绑定模式（ID 或 Index）

### 2. 配置数据绑定组件

在 Item 的子物体上添加具体的绑定组件：

| 组件 | 用途 | 数据类型 |
|------|------|----------|
| `UIBindTextStr` | 绑定 Text 组件 | string |
| `UIBindTextInt` | 绑定 Text 组件 | int |
| `UIBindTextFloat` | 绑定 Text 组件 | float |
| `UIBindTmpTextStr` | 绑定 TMP_Text 组件 | string |
| `UIBindImage` | 绑定 Image 组件的 Sprite | Sprite |
| `UIBindImageColor` | 绑定 Image 组件的颜色 | Color |
| `UIBindSlider` | 绑定 Slider 组件 | float |
| `UIBindActive` | 绑定物体激活状态 | bool |
| ... | ... | ... |

### 3. 设置绑定 ID 或 Index

为每个绑定组件设置唯一的 `ID`（字符串）或 `Index`（数字）：

```
ItemPrefab (UIListViewDataBindItem + UIBindParent)
├── TitleText (UIBindTextStr: ID="title")
├── DescText (UIBindTextStr: ID="description")
├── IconImage (UIBindImage: ID="icon")
├── CountText (UIBindTextInt: ID="count")
└── RareTag (UIBindActive: ID="isRare")
```

### 4. 代码中设置数据

```csharp
// 方式1：使用 ListViewDataBuilder（推荐）
var dataList = new List<IUIDataUnit[]>();
for (int i = 0; i < 100; i++)
{
    var data = new ListViewDataBuilder()
        .Set("title", $"物品 {i}")
        .Set("description", $"描述 {i}")
        .Set("icon", sprite)
        .Set("count", i * 10)
        .Set("isRare", i % 5 == 0)
        .Build();
    
    dataList.Add(data);
}

listView.SetBindData(dataList);

// 方式2：实现 IUIDataBindable 接口
public class MyItemData : IUIDataBindable
{
    public string Title;
    public int Count;
    
    public IUIDataUnit[] ToDataUnits()
    {
        return new ListViewDataBuilder()
            .Set("title", Title)
            .Set("count", Count)
            .Build();
    }
}

// 使用
var data = new MyItemData { Title = "物品", Count = 99 };
listView.SetDataSource(new List<object> { data });
```

## 绑定模式对比

### ID 绑定模式

- 通过字符串 ID 匹配组件
- 更具可读性
- 推荐用于复杂 Item

```csharp
.Set("playerName", "张三")
.Set("playerLevel", 99)
.Set("playerAvatar", sprite)
```

### Index 绑定模式

- 通过数字索引匹配组件
- 性能略好
- 推荐用于简单 Item

```csharp
.Set(0, "张三")      // 对应 Index=0 的组件
.Set(1, 99)         // 对应 Index=1 的组件
.Set(2, sprite)     // 对应 Index=2 的组件
```

## 混合使用两种模式

在同一个项目中，你可以混合使用两种模式：

```csharp
// 自定义模式（适用于复杂交互的 Item）
listViewCustom.SetDataSource(customDataList);

// DataBind 模式（适用于简单展示的 Item）
listViewDataBind.SetBindData(bindDataList);
```

## 完整示例

参见 `ListViewDemoController.cs` 中的完整使用示例。

## 注意事项

1. **预制体要求**：DataBind 模式的预制体必须包含 `UIListViewDataBindItem` 和 `UIBindParent` 组件
2. **ID 唯一性**：同一预制体内的绑定 ID 必须是唯一的
3. **类型匹配**：绑定的数据类型必须与组件期望的类型一致
4. **性能考虑**：对于超大量数据（>1000项），建议使用自定义模式以便进行更精细的优化
