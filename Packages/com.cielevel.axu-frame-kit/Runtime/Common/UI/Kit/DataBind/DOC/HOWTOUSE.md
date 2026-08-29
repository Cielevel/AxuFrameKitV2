# DataBind 使用指南

## 概述

`DataBind` 是一个强大的数据绑定系统，用于将数据自动应用到 UI 组件上。它支持通过 ID 或索引绑定，提供了类型安全的数据传递机制，大大简化了 UI 数据更新的代码。

## 核心概念

### 1. UIBindParent
数据绑定的管理器，负责收集和管理所有绑定组件，并将数据分发到对应的组件。

### 2. UIBindComponent
绑定组件的基类，代表一个可以接收数据的 UI 组件。

### 3. UIDataUnit
数据单元，封装了要传递的数据及其目标（ID 或索引）。

## 核心特性

- ✅ **类型安全**：编译时类型检查，避免运行时错误
- ✅ **灵活绑定**：支持 ID 和索引两种绑定模式
- ✅ **自动收集**：自动扫描子对象中的绑定组件
- ✅ **多组件支持**：内置多种常用 UI 组件的绑定实现
- ✅ **易于扩展**：可以轻松创建自定义绑定组件

## 绑定模式

### ID 模式
通过字符串 ID 绑定组件，适合需要明确标识的场景。

**优点：**
- 可读性强
- 易于维护
- 不受组件顺序影响

**缺点：**
- 需要手动配置 ID
- 字符串比较略慢

### Index 模式
通过数字索引绑定组件，适合有固定顺序的场景。

**优点：**
- 配置简单
- 性能更好

**缺点：**
- 可读性较差
- 受组件顺序影响

## 快速开始

### 步骤 1：创建 UI 结构

创建一个包含多个 UI 元素的面板：

```
PlayerInfoPanel (UIBindParent)
├── NameText (TextMeshProUGUI + UIBindTmpTextStr)
├── LevelText (TextMeshProUGUI + UIBindTmpTextInt)
├── HPText (TextMeshProUGUI + UIBindTmpTextFloat)
├── AvatarImage (Image + UIBindImage)
└── OnlineIcon (GameObject + UIBindActive)
```

### 步骤 2：添加绑定组件

为每个 UI 元素添加对应的绑定组件：

| UI 元素 | 组件类型 | 绑定组件 |
|---------|---------|----------|
| TextMeshProUGUI (字符串) | UIBindTmpTextStr | 绑定文本内容 |
| TextMeshProUGUI (整数) | UIBindTmpTextInt | 绑定整数并转为文本 |
| TextMeshProUGUI (浮点数) | UIBindTmpTextFloat | 绑定浮点数并转为文本 |
| Image | UIBindImage | 绑定 Sprite |
| GameObject | UIBindActive | 绑定激活状态 |

### 步骤 3：配置 UIBindParent

在 `UIBindParent` 组件的 Inspector 中：

#### ID 模式配置
```
Bind Type: ID

UI Bind Component List:
  - UIBindTmpTextStr (ID: "name")
  - UIBindTmpTextInt (ID: "level")
  - UIBindTmpTextFloat (ID: "hp")
  - UIBindImage (ID: "avatar")
  - UIBindActive (ID: "online")
```

#### Index 模式配置
```
Bind Type: Index

UI Bind Component List:
  - UIBindTmpTextStr (Index: 0)
  - UIBindTmpTextInt (Index: 1)
  - UIBindTmpTextFloat (Index: 2)
  - UIBindImage (Index: 3)
  - UIBindActive (Index: 4)
```

### 步骤 4：绑定数据

#### ID 模式绑定

```csharp
using AxuFrameKit.Common.UI.Kit.DataBind;

public class PlayerInfoPanel : MonoBehaviour
{
    [SerializeField] private UIBindParent _bindParent;

    public void UpdatePlayerInfo(PlayerData data)
    {
        _bindParent.SetData(
            UIData.Id("name", data.playerName),
            UIData.Id("level", data.level),
            UIData.Id("hp", data.currentHP),
            UIData.Id("avatar", data.avatarSprite),
            UIData.Id("online", data.isOnline)
        );
    }
}
```

#### Index 模式绑定

```csharp
public class PlayerInfoPanel : MonoBehaviour
{
    [SerializeField] private UIBindParent _bindParent;

    public void UpdatePlayerInfo(PlayerData data)
    {
        _bindParent.SetData(
            UIData.Index(0, data.playerName),
            UIData.Index(1, data.level),
            UIData.Index(2, data.currentHP),
            UIData.Index(3, data.avatarSprite),
            UIData.Index(4, data.isOnline)
        );
    }
}
```

## 内置绑定组件

### 文本组件

#### UIBindTmpTextStr
绑定字符串到 TextMeshProUGUI。

```csharp
UIData.Id("message", "Hello World")
```

#### UIBindTmpTextInt
绑定整数到 TextMeshProUGUI。

```csharp
UIData.Id("score", 12345)
```

#### UIBindTmpTextFloat
绑定浮点数到 TextMeshProUGUI，支持格式化。

```csharp
UIData.Id("health", 99.5f)
// 在组件上可以设置格式，如 "F2" 显示为 "99.50"
```

#### UIBindTextStr / UIBindTextInt / UIBindTextFloat
与上述类似，但用于 Unity 的 Text 组件（非 TextMeshPro）。

### 图片组件

#### UIBindImage
绑定 Sprite 到 Image 组件。

```csharp
UIData.Id("icon", mySprite)
```

#### UIBindImageColor
绑定颜色到 Image 组件。

```csharp
UIData.Id("color", Color.red)
```

#### UIBindImageMaterial
绑定材质到 Image 组件。

```csharp
UIData.Id("material", myMaterial)
```

#### UIBindRawImage
绑定 Texture 到 RawImage 组件。

```csharp
UIData.Id("texture", myTexture)
```

#### UIBindRawImageColor / UIBindRawImageMaterial / UIBindRawImageTexture
用于 RawImage 的颜色、材质、纹理绑定。

### 交互组件

#### UIBindInputField
绑定字符串到 InputField。

```csharp
UIData.Id("input", "默认文本")
```

#### UIBindTmpInputField
绑定字符串到 TMP_InputField。

```csharp
UIData.Id("input", "默认文本")
```

#### UIBindSlider
绑定浮点数到 Slider。

```csharp
UIData.Id("volume", 0.75f)
```

#### UIBindDropdown
绑定整数到 Dropdown（选中的索引）。

```csharp
UIData.Id("quality", 2) // 选中索引为 2 的选项
```

### 其他组件

#### UIBindActive
绑定布尔值到 GameObject 的激活状态。

```csharp
UIData.Id("visible", true)
```

## 实际应用示例

### 示例 1：角色信息面板

```csharp
public class CharacterPanel : MonoBehaviour
{
    [SerializeField] private UIBindParent _bindParent;

    public void ShowCharacter(Character character)
    {
        _bindParent.SetData(
            UIData.Id("name", character.Name),
            UIData.Id("level", character.Level),
            UIData.Id("hp", character.CurrentHP),
            UIData.Id("maxHp", character.MaxHP),
            UIData.Id("mp", character.CurrentMP),
            UIData.Id("maxMp", character.MaxMP),
            UIData.Id("avatar", character.AvatarSprite),
            UIData.Id("classIcon", character.ClassIconSprite)
        );
    }
}
```

### 示例 2：物品详情面板

```csharp
public class ItemDetailPanel : MonoBehaviour
{
    [SerializeField] private UIBindParent _bindParent;

    public void ShowItem(Item item)
    {
        _bindParent.SetData(
            UIData.Id("itemName", item.Name),
            UIData.Id("description", item.Description),
            UIData.Id("price", item.Price),
            UIData.Id("quantity", item.Quantity),
            UIData.Id("icon", item.IconSprite),
            UIData.Id("rarity", item.RarityColor),
            UIData.Id("isEquipped", item.IsEquipped)
        );
    }
}
```

### 示例 3：列表项绑定

```csharp
public class InventoryItem : MonoBehaviour
{
    [SerializeField] private UIBindParent _bindParent;

    public void SetData(int index, ItemData data)
    {
        _bindParent.SetData(
            UIData.Index(0, data.icon),
            UIData.Index(1, data.name),
            UIData.Index(2, data.quantity),
            UIData.Index(3, data.isNew)
        );
    }
}
```

### 示例 4：动态表单

```csharp
public class DynamicForm : MonoBehaviour
{
    [SerializeField] private UIBindParent _bindParent;

    public void LoadFormData(Dictionary<string, object> formData)
    {
        List<IUIDataUnit> dataUnits = new List<IUIDataUnit>();

        foreach (var kvp in formData)
        {
            // 根据数据类型创建对应的 UIDataUnit
            if (kvp.Value is string strValue)
                dataUnits.Add(UIData.Id(kvp.Key, strValue));
            else if (kvp.Value is int intValue)
                dataUnits.Add(UIData.Id(kvp.Key, intValue));
            else if (kvp.Value is float floatValue)
                dataUnits.Add(UIData.Id(kvp.Key, floatValue));
            else if (kvp.Value is bool boolValue)
                dataUnits.Add(UIData.Id(kvp.Key, boolValue));
        }

        _bindParent.SetData(dataUnits.ToArray());
    }
}
```

## 高级用法

### 创建自定义绑定组件

```csharp
using AxuFrameKit.Common.UI.Kit.DataBind.Component;
using UnityEngine;
using UnityEngine.UI;

// 自定义绑定组件：绑定进度条
public class UIBindProgressBar : UIBindComponentGeneric<float>
{
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _fillImage;
    [SerializeField] private Gradient _colorGradient;

    public override void SetData(float value)
    {
        if (_slider != null)
        {
            _slider.value = value;
        }

        if (_fillImage != null && _colorGradient != null)
        {
            _fillImage.color = _colorGradient.Evaluate(value);
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        if (_slider == null)
            _slider = GetComponent<Slider>();
    }
}
```

使用自定义绑定组件：

```csharp
_bindParent.SetData(
    UIData.Id("health", 0.75f) // 75% 血量
);
```

### 批量更新优化

```csharp
public class OptimizedPanel : MonoBehaviour
{
    [SerializeField] private UIBindParent _bindParent;
    private List<IUIDataUnit> _dataCache = new List<IUIDataUnit>();

    public void UpdateMultipleFields(PlayerData data)
    {
        _dataCache.Clear();

        // 批量准备数据
        _dataCache.Add(UIData.Id("name", data.name));
        _dataCache.Add(UIData.Id("level", data.level));
        _dataCache.Add(UIData.Id("hp", data.hp));
        
        // 一次性更新
        _bindParent.SetData(_dataCache.ToArray());
    }
}
```

### 条件绑定

```csharp
public class ConditionalBinding : MonoBehaviour
{
    [SerializeField] private UIBindParent _bindParent;

    public void UpdateUI(PlayerData data, bool showDetails)
    {
        var dataList = new List<IUIDataUnit>
        {
            UIData.Id("name", data.name),
            UIData.Id("level", data.level)
        };

        // 根据条件添加额外数据
        if (showDetails)
        {
            dataList.Add(UIData.Id("hp", data.hp));
            dataList.Add(UIData.Id("mp", data.mp));
            dataList.Add(UIData.Id("exp", data.exp));
        }

        _bindParent.SetData(dataList.ToArray());
    }
}
```

### 数据清空

```csharp
// 清空所有绑定的数据（重置为默认值）
_bindParent.ClearData();
```

## 编辑器辅助功能

### 自动收集组件

在 `UIBindParent` 组件上：
1. 右键点击组件
2. 选择 "Auto Collect Components"
3. 自动扫描并添加所有子对象中的 `UIBindComponent`

### OnValidate 自动填充

在编辑器中，`UIBindParent` 会在 `OnValidate` 时自动收集子组件，无需手动配置。

## 类型安全机制

DataBind 系统使用泛型确保类型安全：

```csharp
// ✅ 正确：类型匹配
UIData.Id("name", "John")  // string → UIBindTmpTextStr

// ❌ 错误：类型不匹配（会在运行时警告）
UIData.Id("name", 123)     // int → UIBindTmpTextStr (期望 string)
```

运行时会输出警告：
```
类型不匹配: 组件 NameText 期望 System.String 但并未实现对应泛型类。
```

## 注意事项

1. **绑定模式选择**：
   - 一旦选择了绑定模式（ID 或 Index），不要在运行时切换
   - ID 模式更适合复杂 UI，Index 模式更适合简单列表

2. **ID 唯一性**：
   - 在 ID 模式下，确保每个组件的 ID 是唯一的
   - 重复的 ID 会导致只有一个组件接收数据

3. **类型匹配**：
   - 确保传递的数据类型与绑定组件的类型匹配
   - 类型不匹配会在运行时产生警告

4. **性能考虑**：
   - 避免每帧调用 `SetData`
   - 只在数据实际变化时更新
   - 对于频繁更新的数据，考虑直接操作组件

5. **初始化时机**：
   - `UIBindParent` 在 `Awake` 时自动初始化
   - 如果组件是动态添加的，可能需要手动调用 `Initialize()`

## 调试技巧

### 查看绑定模式

```csharp
UIBindParent bindParent = GetComponent<UIBindParent>();
Debug.Log($"当前绑定模式: {bindParent.CurrentBindMode}");
```

### 检查组件列表

在编辑器中查看 `UI Bind Component List`，确保所有组件都已正确添加。

### 运行时验证

```csharp
[ContextMenu("验证绑定")]
void ValidateBindings()
{
    UIBindParent bindParent = GetComponent<UIBindParent>();
    bindParent.AutoCollectComponents();
    Debug.Log("组件收集完成");
}
```

## 性能优化建议

1. **缓存 UIBindParent 引用**：
```csharp
private UIBindParent _bindParent;

void Awake()
{
    _bindParent = GetComponent<UIBindParent>();
}
```

2. **批量更新**：
```csharp
// ✅ 好：一次性更新多个字段
_bindParent.SetData(
    UIData.Id("name", name),
    UIData.Id("level", level),
    UIData.Id("hp", hp)
);

// ❌ 差：多次调用
_bindParent.SetData(UIData.Id("name", name));
_bindParent.SetData(UIData.Id("level", level));
_bindParent.SetData(UIData.Id("hp", hp));
```

3. **避免不必要的更新**：
```csharp
private string _lastPlayerName;

void UpdatePlayerName(string newName)
{
    if (_lastPlayerName == newName)
        return;

    _lastPlayerName = newName;
    _bindParent.SetData(UIData.Id("name", newName));
}
```

## 最佳实践

### 1. 使用数据类封装

```csharp
public class PlayerUIData
{
    public string Name { get; set; }
    public int Level { get; set; }
    public float HP { get; set; }
    public Sprite Avatar { get; set; }

    public IUIDataUnit[] ToDataUnits()
    {
        return new IUIDataUnit[]
        {
            UIData.Id("name", Name),
            UIData.Id("level", Level),
            UIData.Id("hp", HP),
            UIData.Id("avatar", Avatar)
        };
    }
}

// 使用
PlayerUIData uiData = new PlayerUIData { /* ... */ };
_bindParent.SetData(uiData.ToDataUnits());
```

### 2. 创建绑定辅助类

```csharp
public static class UIBindHelper
{
    public static void BindPlayerData(UIBindParent parent, PlayerData data)
    {
        parent.SetData(
            UIData.Id("name", data.name),
            UIData.Id("level", data.level),
            UIData.Id("hp", data.hp)
        );
    }

    public static void BindItemData(UIBindParent parent, ItemData data)
    {
        parent.SetData(
            UIData.Id("itemName", data.name),
            UIData.Id("icon", data.icon),
            UIData.Id("quantity", data.quantity)
        );
    }
}
```

### 3. 使用构建器模式

```csharp
public class UIDataBuilder
{
    private List<IUIDataUnit> _dataUnits = new List<IUIDataUnit>();

    public UIDataBuilder Add<T>(string id, T value)
    {
        _dataUnits.Add(UIData.Id(id, value));
        return this;
    }

    public UIDataBuilder Add<T>(int index, T value)
    {
        _dataUnits.Add(UIData.Index(index, value));
        return this;
    }

    public IUIDataUnit[] Build()
    {
        return _dataUnits.ToArray();
    }
}

// 使用
var data = new UIDataBuilder()
    .Add("name", "John")
    .Add("level", 10)
    .Add("hp", 100f)
    .Build();

_bindParent.SetData(data);
```

## 常见问题

**Q: 如何在运行时切换绑定模式？**
A: 不建议在运行时切换。绑定模式应该在设计时确定。

**Q: 可以绑定自定义类型吗？**
A: 可以，创建继承自 `UIBindComponentGeneric<T>` 的自定义绑定组件即可。

**Q: 如何处理可选字段？**
A: 使用条件绑定，只在需要时添加对应的 `UIDataUnit`。

**Q: 性能如何？**
A: 对于大多数场景，性能足够好。如果有极高性能要求，考虑直接操作组件或使用缓存。

## 扩展阅读

- 参考 `ListView` 组件了解如何在列表中使用 DataBind
- 学习 `UIBaseComponent` 了解 UI 框架的基础结构
- 查看 Editor 文件夹中的 `UIBindParentEditor.cs` 了解编辑器扩展
