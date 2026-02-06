# Group 使用指南

## 概述

`Group` 是一组用于管理和切换多个 UI 元素（图片、精灵）的组件。它提供了通过 ID 或索引来显示/隐藏特定元素的功能，适用于状态切换、图标显示等场景。

## 核心组件

### 1. UIImageGroupID
通过字符串 ID 管理 Image 组件的显示/隐藏。

### 2. UIImageGroupIndex
通过数字索引管理 Image 组件的显示/隐藏。

### 3. UISpriteGroupID
通过字符串 ID 管理 Sprite（精灵）的显示/隐藏。

### 4. UISpriteGroupIndex
通过数字索引管理 Sprite（精灵）的显示/隐藏。

## 使用场景

- 🎯 **状态图标切换**：根据不同状态显示不同图标
- 🎯 **多语言图片**：根据语言切换不同的图片
- 🎯 **角色头像**：根据角色 ID 显示对应头像
- 🎯 **装备图标**：根据装备类型显示对应图标
- 🎯 **UI 状态指示器**：显示不同的状态图标（在线/离线/忙碌等）

## UIImageGroupID 使用指南

### 快速开始

#### 步骤 1：设置组件

1. 在需要切换图片的 GameObject 上添加 `UIImageGroupID` 组件
2. 在子对象中创建多个 Image 组件

**示例结构：**
```
StatusIcon (UIImageGroupID)
├── OnlineIcon (Image)
├── OfflineIcon (Image)
├── BusyIcon (Image)
└── AwayIcon (Image)
```

#### 步骤 2：配置 Inspector

在 `UIImageGroupID` 组件的 Inspector 中配置图片列表：

```
Images:
  - Image: OnlineIcon (Image 组件)
    ID: "online"
  
  - Image: OfflineIcon (Image 组件)
    ID: "offline"
  
  - Image: BusyIcon (Image 组件)
    ID: "busy"
  
  - Image: AwayIcon (Image 组件)
    ID: "away"
```

### 代码使用

#### 显示单个图片

```csharp
UIImageGroupID imageGroup = GetComponent<UIImageGroupID>();

// 显示 ID 为 "online" 的图片，隐藏其他所有图片
imageGroup.ShowOne("online");

// 显示 ID 为 "offline" 的图片
imageGroup.ShowOne("offline");
```

#### 显示多个图片

```csharp
// 同时显示多个图片，隐藏其他图片
imageGroup.ShowMulti("online", "busy");

// 显示三个图片
imageGroup.ShowMulti("online", "busy", "away");
```

### 实际应用示例

#### 示例 1：玩家状态指示器

```csharp
public class PlayerStatusIndicator : MonoBehaviour
{
    [SerializeField] private UIImageGroupID _statusIcon;

    public void SetStatus(PlayerStatus status)
    {
        string statusId = status switch
        {
            PlayerStatus.Online => "online",
            PlayerStatus.Offline => "offline",
            PlayerStatus.Busy => "busy",
            PlayerStatus.Away => "away",
            _ => "offline"
        };

        _statusIcon.ShowOne(statusId);
    }
}
```

#### 示例 2：多语言图片切换

```csharp
public class LanguageImage : MonoBehaviour
{
    [SerializeField] private UIImageGroupID _languageImage;

    public void SetLanguage(string languageCode)
    {
        // languageCode: "en", "zh", "ja", "ko" 等
        _languageImage.ShowOne(languageCode);
    }
}
```

#### 示例 3：装备槽位图标

```csharp
public class EquipmentSlot : MonoBehaviour
{
    [SerializeField] private UIImageGroupID _equipmentIcon;

    public void SetEquipment(string equipmentType)
    {
        // equipmentType: "weapon", "armor", "accessory" 等
        _equipmentIcon.ShowOne(equipmentType);
    }

    public void ClearEquipment()
    {
        // 显示空槽位图标
        _equipmentIcon.ShowOne("empty");
    }
}
```

## UIImageGroupIndex 使用指南

### 快速开始

`UIImageGroupIndex` 与 `UIImageGroupID` 类似，但使用数字索引而不是字符串 ID。

#### 配置 Inspector

```
Images:
  - Image: Icon0 (Image 组件)
  - Image: Icon1 (Image 组件)
  - Image: Icon2 (Image 组件)
  - Image: Icon3 (Image 组件)
```

### 代码使用

```csharp
UIImageGroupIndex imageGroup = GetComponent<UIImageGroupIndex>();

// 显示索引为 0 的图片
imageGroup.ShowOne(0);

// 显示索引为 1 的图片
imageGroup.ShowOne(1);

// 同时显示索引为 0 和 2 的图片
imageGroup.ShowMulti(0, 2);
```

### 实际应用示例

#### 示例：星级评分

```csharp
public class StarRating : MonoBehaviour
{
    [SerializeField] private UIImageGroupIndex _starGroup;

    // 显示 1-5 星评分
    public void SetRating(int rating)
    {
        if (rating < 1 || rating > 5)
        {
            Debug.LogError("评分必须在 1-5 之间");
            return;
        }

        // 显示对应数量的星星
        int[] indices = new int[rating];
        for (int i = 0; i < rating; i++)
        {
            indices[i] = i;
        }

        _starGroup.ShowMulti(indices);
    }
}
```

## UISpriteGroupID 使用指南

### 快速开始

`UISpriteGroupID` 用于管理 Sprite（精灵）而不是 Image 组件。

#### 配置 Inspector

```
Sprites:
  - Sprite: NormalSprite (Sprite 资源)
    ID: "normal"
  
  - Sprite: HoverSprite (Sprite 资源)
    ID: "hover"
  
  - Sprite: PressedSprite (Sprite 资源)
    ID: "pressed"
  
  - Sprite: DisabledSprite (Sprite 资源)
    ID: "disabled"
```

### 代码使用

```csharp
UISpriteGroupID spriteGroup = GetComponent<UISpriteGroupID>();

// 显示 ID 为 "normal" 的精灵
spriteGroup.ShowOne("normal");

// 显示 ID 为 "hover" 的精灵
spriteGroup.ShowOne("hover");
```

### 实际应用示例

#### 示例：按钮状态切换

```csharp
public class CustomButton : MonoBehaviour
{
    [SerializeField] private UISpriteGroupID _buttonSprite;

    public void OnPointerEnter()
    {
        _buttonSprite.ShowOne("hover");
    }

    public void OnPointerExit()
    {
        _buttonSprite.ShowOne("normal");
    }

    public void OnPointerDown()
    {
        _buttonSprite.ShowOne("pressed");
    }

    public void SetEnabled(bool enabled)
    {
        _buttonSprite.ShowOne(enabled ? "normal" : "disabled");
    }
}
```

## UISpriteGroupIndex 使用指南

### 快速开始

`UISpriteGroupIndex` 使用数字索引管理 Sprite。

### 代码使用

```csharp
UISpriteGroupIndex spriteGroup = GetComponent<UISpriteGroupIndex>();

// 显示索引为 0 的精灵
spriteGroup.ShowOne(0);

// 同时显示索引为 0 和 1 的精灵
spriteGroup.ShowMulti(0, 1);
```

## 高级用法

### 动态添加图片

虽然 Group 组件主要通过 Inspector 配置，但你也可以在运行时动态管理：

```csharp
// 注意：直接修改内部字典需要反射或扩展组件
// 推荐在编辑器中预先配置好所有可能的图片
```

### 组合使用

```csharp
public class ComplexStatusDisplay : MonoBehaviour
{
    [SerializeField] private UIImageGroupID _statusIcon;
    [SerializeField] private UIImageGroupID _badgeIcon;

    public void SetPlayerStatus(string status, bool hasNotification)
    {
        // 显示状态图标
        _statusIcon.ShowOne(status);

        // 根据是否有通知显示徽章
        if (hasNotification)
            _badgeIcon.ShowOne("notification");
        else
            _badgeIcon.ShowOne("none");
    }
}
```

### 错误处理

```csharp
public class SafeImageGroup : MonoBehaviour
{
    [SerializeField] private UIImageGroupID _imageGroup;
    [SerializeField] private string _defaultId = "default";

    public void ShowSafe(string id)
    {
        // 尝试显示指定 ID，如果不存在则显示默认图片
        try
        {
            _imageGroup.ShowOne(id);
        }
        catch
        {
            Debug.LogWarning($"ID '{id}' 不存在，显示默认图片");
            _imageGroup.ShowOne(_defaultId);
        }
    }
}
```

## 注意事项

1. **ID 唯一性**：
   - 在 `UIImageGroupID` 和 `UISpriteGroupID` 中，每个 ID 必须是唯一的
   - 编辑器会在 `OnValidate` 时检查并报错重复的 ID

2. **索引范围**：
   - 在 `UIImageGroupIndex` 和 `UISpriteGroupIndex` 中，确保索引在有效范围内
   - 超出范围会导致错误

3. **性能考虑**：
   - `ShowOne` 会隐藏所有其他图片，只显示指定的一个
   - `ShowMulti` 会隐藏所有图片，然后只显示指定的多个
   - 避免频繁调用，特别是在 Update 中

4. **初始状态**：
   - 默认情况下，所有图片都是显示的
   - 建议在 Start 中调用一次 `ShowOne` 设置初始状态

## 调试技巧

### 查看当前配置

```csharp
// 在编辑器中查看 OnValidate 的错误提示
// 如果有重复的 ID，会在 Console 中显示错误
```

### 运行时检查

```csharp
public class ImageGroupDebugger : MonoBehaviour
{
    [SerializeField] private UIImageGroupID _imageGroup;

    [ContextMenu("显示所有图片状态")]
    void ShowAllStates()
    {
        // 需要扩展 UIImageGroupID 以暴露内部字典
        // 或者通过反射访问
    }
}
```

## 性能优化建议

1. **预先配置**：在编辑器中预先配置好所有图片，避免运行时动态添加
2. **合理分组**：将相关的图片放在同一个 Group 中
3. **避免过度使用**：如果只有 2-3 个图片需要切换，考虑直接使用 `SetActive` 或 `enabled`
4. **缓存引用**：在频繁调用的情况下，缓存 Group 组件的引用

## 最佳实践

### 1. 使用枚举提高可读性

```csharp
public enum PlayerStatus
{
    Online,
    Offline,
    Busy,
    Away
}

public class PlayerStatusDisplay : MonoBehaviour
{
    [SerializeField] private UIImageGroupID _statusIcon;

    public void SetStatus(PlayerStatus status)
    {
        string id = status.ToString().ToLower();
        _statusIcon.ShowOne(id);
    }
}
```

### 2. 创建配置文件

```csharp
[CreateAssetMenu(fileName = "IconConfig", menuName = "UI/Icon Config")]
public class IconConfig : ScriptableObject
{
    [System.Serializable]
    public class IconMapping
    {
        public string key;
        public string iconId;
    }

    public List<IconMapping> mappings;

    public string GetIconId(string key)
    {
        return mappings.Find(m => m.key == key)?.iconId ?? "default";
    }
}
```

### 3. 扩展组件功能

```csharp
public class ExtendedImageGroup : UIImageGroupID
{
    [SerializeField] private string _defaultId;

    public void ShowOneOrDefault(string id)
    {
        try
        {
            ShowOne(id);
        }
        catch
        {
            if (!string.IsNullOrEmpty(_defaultId))
                ShowOne(_defaultId);
        }
    }
}
```

## 常见问题

**Q: 如何在运行时添加新的图片？**
A: Group 组件主要设计用于编辑器配置。如果需要运行时动态添加，建议扩展组件或使用其他方案。

**Q: ShowMulti 可以传入空数组吗？**
A: 可以，但会隐藏所有图片。

**Q: ID 和 Index 哪个更好？**
A: 取决于使用场景。ID 更具可读性，Index 更高效。如果图片数量固定且有明确的顺序，使用 Index；如果需要更好的可维护性，使用 ID。

**Q: 如何实现淡入淡出效果？**
A: Group 组件只负责显示/隐藏，不包含动画。如果需要淡入淡出，建议结合 CanvasGroup 和 Tween 系统。
