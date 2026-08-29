# UIListView 快速入门教程

本教程将引导您快速上手UIListView，在5分钟内创建一个可用的列表。

## 教程1: 创建最简单的列表

### 第1步：创建数据类

```csharp
using System;

[Serializable]
public class PersonData
{
    public string Name;
    public int Age;
    public string Email;
}
```

### 第2步：创建Item脚本

创建文件`PersonItem.cs`：

```csharp
using AxuFrameKit.Common.UI.Kit.ListView;
using UnityEngine;
using TMPro;

public class PersonItem : UIListViewItem
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI ageText;
    [SerializeField] private TextMeshProUGUI emailText;

    protected override void OnUpdateData(object data, int index)
    {
        if (TryGetData<PersonData>(out var person))
        {
            nameText.text = person.Name;
            ageText.text = $"Age: {person.Age}";
            emailText.text = person.Email;
        }
    }
}
```

### 第3步：在Unity中创建Item预制体

1. 创建一个空GameObject，命名为`PersonItem`
2. 添加Image组件作为背景
3. 添加3个TextMeshProUGUI子对象：
   - `NameText`
   - `AgeText`
   - `EmailText`
4. 调整布局（建议使用RectTransform设置好大小和位置）
5. 添加`PersonItem`脚本到根对象
6. 将3个Text组件拖到脚本的对应字段
7. 保存为预制体

### 第4步：创建ListView

1. 创建一个空GameObject，命名为`PersonListView`
2. 添加`ScrollRect`组件
3. 创建子对象`Viewport`（添加Image和Mask组件）
4. 创建子对象`Content`在Viewport下
5. 配置ScrollRect：
   - Viewport → 拖入Viewport对象
   - Content → 拖入Content对象
   - Vertical scrollbar → 可选
6. 添加`UIListView`组件到根对象
7. 配置ListView：
   - Layout Mode → Vertical
   - Item Size → 80（根据您的Item预制体高度）
   - Item Prefab → 拖入PersonItem预制体

### 第5步：编写控制器

创建文件`PersonListController.cs`：

```csharp
using AxuFrameKit.Common.UI.Kit.ListView;
using System.Collections.Generic;
using UnityEngine;

public class PersonListController : MonoBehaviour
{
    [SerializeField] private UIListView listView;

    private void Start()
    {
        // 创建测试数据
        var people = new List<PersonData>
        {
            new PersonData { Name = "Alice", Age = 25, Email = "alice@example.com" },
            new PersonData { Name = "Bob", Age = 30, Email = "bob@example.com" },
            new PersonData { Name = "Charlie", Age = 28, Email = "charlie@example.com" },
            // ... 添加更多数据
        };

        // 绑定数据
        listView.SetDataSource(people);
    }
}
```

### 第6步：测试

1. 将`PersonListController`脚本添加到PersonListView对象
2. 拖入ListView组件引用
3. 运行场景
4. 您应该看到列表显示出来了！

---

## 教程2: 添加点击和选中功能

在上一个教程基础上，添加交互功能。

### 修改PersonItem

```csharp
public class PersonItem : UIListViewItem
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI ageText;
    [SerializeField] private TextMeshProUGUI emailText;
    [SerializeField] private Image background;

    protected override void OnUpdateData(object data, int index)
    {
        if (TryGetData<PersonData>(out var person))
        {
            nameText.text = person.Name;
            ageText.text = $"Age: {person.Age}";
            emailText.text = person.Email;
        }
    }

    // 监听选中状态变化
    protected override void OnSelectionChanged(bool isSelected)
    {
        background.color = isSelected 
            ? new Color(0.3f, 0.6f, 1f, 1f)  // 蓝色
            : Color.white;                    // 白色
    }

    // 监听点击
    protected override void OnItemClicked()
    {
        Debug.Log($"点击了: {GetData<PersonData>()?.Name}");
    }
}
```

### 修改控制器

```csharp
public class PersonListController : MonoBehaviour
{
    [SerializeField] private UIListView listView;

    private void Start()
    {
        var people = GenerateTestData();
        listView.SetDataSource(people);

        // 监听选中事件
        listView.OnSelectionChanged += OnSelectionChanged;
        
        // 监听点击事件
        listView.OnItemClicked += OnItemClicked;
    }

    private void OnItemClicked(UIListViewItem item, int index)
    {
        Debug.Log($"ListView: 点击了索引 {index}");
    }

    private void OnSelectionChanged(List<int> selectedIndices)
    {
        Debug.Log($"当前选中: {string.Join(", ", selectedIndices)}");
    }

    private List<PersonData> GenerateTestData()
    {
        var people = new List<PersonData>();
        for (int i = 0; i < 100; i++)
        {
            people.Add(new PersonData
            {
                Name = $"Person {i}",
                Age = Random.Range(20, 60),
                Email = $"person{i}@example.com"
            });
        }
        return people;
    }
}
```

---

## 教程3: 使用网格布局

创建一个图片网格。

### 第1步：数据类

```csharp
using UnityEngine;

[System.Serializable]
public class ImageData
{
    public Sprite Image;
    public string Title;
}
```

### 第2步：Item脚本

```csharp
using AxuFrameKit.Common.UI.Kit.ListView;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ImageGridItem : UIListViewItem
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI titleText;

    protected override void OnUpdateData(object data, int index)
    {
        if (TryGetData<ImageData>(out var imageData))
        {
            image.sprite = imageData.Image;
            titleText.text = imageData.Title;
        }
    }

    protected override void OnSelectionChanged(bool isSelected)
    {
        image.color = isSelected ? Color.yellow : Color.white;
    }
}
```

### 第3步：创建Item预制体

1. 创建正方形Item（例如150x150）
2. 添加Image组件填充整个区域
3. 添加Text在底部
4. 添加`ImageGridItem`脚本
5. 保存为预制体

### 第4步：配置ListView

在Inspector中：
- **Layout Mode** → Grid
- **Item Size** → 150
- **Spacing** → 10
- **Grid Constraint Count** → 3（每行3个）
- **Item Prefab** → 拖入ImageGridItem预制体

### 第5步：绑定数据

```csharp
public class ImageGridController : MonoBehaviour
{
    [SerializeField] private UIListView listView;
    [SerializeField] private Sprite[] testImages;

    private void Start()
    {
        var imageList = new List<ImageData>();
        
        for (int i = 0; i < testImages.Length; i++)
        {
            imageList.Add(new ImageData
            {
                Image = testImages[i],
                Title = $"Image {i + 1}"
            });
        }

        listView.SetDataSource(imageList);
    }
}
```

---

## 教程4: 动态添加和删除

实现动态列表操作。

```csharp
public class DynamicListController : MonoBehaviour
{
    [SerializeField] private UIListView listView;
    private List<PersonData> people = new List<PersonData>();

    private void Start()
    {
        listView.SetDataSource(people);
    }

    // 添加一个项目
    public void AddItem()
    {
        var newPerson = new PersonData
        {
            Name = $"New Person {people.Count}",
            Age = Random.Range(20, 60),
            Email = $"new{people.Count}@example.com"
        };

        people.Add(newPerson);
        listView.RefreshList(false); // false = 保持滚动位置
    }

    // 删除选中的项目
    public void RemoveSelected()
    {
        var selected = listView.GetSelectedIndices();
        
        // 从后往前删除，避免索引混乱
        selected.Sort();
        for (int i = selected.Count - 1; i >= 0; i--)
        {
            people.RemoveAt(selected[i]);
        }

        listView.RefreshList(false);
    }

    // 清空列表
    public void ClearAll()
    {
        people.Clear();
        listView.Clear();
    }

    // 批量添加
    public void AddBatch()
    {
        var newPeople = new List<PersonData>();
        for (int i = 0; i < 10; i++)
        {
            newPeople.Add(new PersonData
            {
                Name = $"Batch Person {people.Count + i}",
                Age = Random.Range(20, 60),
                Email = $"batch{i}@example.com"
            });
        }

        // 方式1: 手动添加再刷新
        people.AddRange(newPeople);
        listView.RefreshList(false);

        // 方式2: 使用AppendData（推荐）
        // listView.AppendData(newPeople);
    }
}
```

---

## 常见问题

### Q: Item显示不出来？

**A**: 检查以下几点：
1. Item预制体是否有UIListViewItem组件
2. ScrollRect的Content是否正确设置
3. Item Size是否大于0
4. 数据源是否非空

### Q: 滚动很卡顿？

**A**: 优化建议：
1. 检查OnUpdateData中是否有耗时操作
2. 启用对象池（usePooling = true）
3. 减少Item的UI复杂度
4. 使用TextMeshPro代替Text

### Q: 选中状态显示不正确？

**A**: 
1. 确保在Item预制体中设置了背景Image或选中指示器
2. 重写OnSelectionChanged方法处理视觉反馈
3. 检查SelectionSettings配置

### Q: 如何实现Item动画？

**A**: 
```csharp
public class AnimatedItem : UIListViewItem
{
    protected override void OnShow()
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }
}
```

### Q: 如何支持不同高度的Item？

**A**: 当前版本（v1.0）不支持动态高度，计划在v1.1添加。临时方案是将itemSize设为最大Item的高度。

---

## 下一步

恭喜！您已经掌握了UIListView的基本用法。接下来可以：

1. 阅读 **README.md** 了解完整API
2. 阅读 **ARCHITECTURE.md** 理解设计原理
3. 查看 **Examples** 文件夹的完整示例
4. 尝试自定义布局引擎扩展功能

祝开发愉快！
