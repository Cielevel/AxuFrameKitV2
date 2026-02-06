# Toast 使用指南

## 概述

`Toast` 是一个非侵入式的侧边弹出提示系统，支持从屏幕左侧或右侧弹出消息提示。它具有自动管理生命周期、纵向挤压、对象池优化等特性，适用于游戏中的各种提示场景。

## 核心组件

### 1. UISideToast<T>
Toast 管理器（抽象类），负责管理 Toast 单元的生命周期、动画和对象池。

### 2. UIToastUnit
Toast 单元基类，代表单个弹出的提示消息。

## 核心特性

- ✅ **自动生命周期管理**：进场 → 停留 → 退场 → 回收
- ✅ **纵向挤压**：新消息弹出时，旧消息自动向上/下移动
- ✅ **对象池优化**：自动复用 Toast 单元，减少 GC
- ✅ **平滑动画**：支持多种缓动函数
- ✅ **数量限制**：超出最大显示数量时，自动回收最旧的消息

## 快速开始

### 步骤 1：创建 Toast 单元预制体

1. 创建一个 GameObject，添加 `UIToastUnit` 组件
2. 添加 `CanvasGroup` 组件（用于淡入淡出）
3. 添加你的 UI 元素（Text、Image 等）
4. 保存为预制体

**示例结构：**
```
ToastUnit (UIToastUnit, CanvasGroup)
├── Background (Image)
├── Icon (Image)
└── Message (TextMeshProUGUI)
```

### 步骤 2：创建自定义 Toast 管理器

继承 `UISideToast<T>` 并实现 `OnToast` 方法：

```csharp
using AxuFrameKit.Common.UI.Kit.Toast;
using UnityEngine;

public class MyToastUnit : UIToastUnit
{
    [SerializeField] private TMPro.TextMeshProUGUI _messageText;
    [SerializeField] private Image _icon;

    public void SetMessage(string message, Sprite icon = null)
    {
        _messageText.text = message;
        if (icon != null)
            _icon.sprite = icon;
    }
}

public class MyToast : UISideToast<MyToastUnit>
{
    protected override void OnToast(MyToastUnit unit)
    {
        // 在这里设置 Toast 的内容
        unit.SetMessage("这是一条提示消息");
    }
}
```

### 步骤 3：配置 Toast 管理器

在场景中创建一个 GameObject，添加 `MyToast` 组件，并在 Inspector 中配置：

#### Unit 配置
```
Prefab Unit: 拖入 Toast 单元预制体
Unit Size: 设置单元大小（例如：300x80）
```

#### 位置配置
```
Parent Group: Toast 的父容器（通常是 Canvas 下的一个 RectTransform）
Start Anchored Pos: 进场起始位置（例如：(-400, -100) 从左侧进入）
End Anchored Pos: 进场最终位置（例如：(-50, -100) 停留位置）
```

#### 纵向挤压配置
```
Vertical Interval: 单元之间的间隔（例如：10）
Max Showing Count: 最多同时显示几个（例如：3）
Push Direction: 挤压方向
  - Up: 新消息在下，旧消息向上移动
  - Down: 新消息在上，旧消息向下移动
```

#### Tween 设置
```
Ease Type: 缓动函数（例如：OutQuad）
Tween In Duration: 进场动画时长（例如：0.3 秒）
Tween Out Duration: 退场动画时长（例如：0.3 秒）
Stay Duration: 停留时长（例如：2.0 秒）
```

## 代码使用

### 基础用法

```csharp
// 获取 Toast 管理器
MyToast toast = GetComponent<MyToast>();

// 弹出一条消息
toast.Toast();
```

### 带参数的 Toast

修改 `OnToast` 方法以支持参数：

```csharp
public class MyToast : UISideToast<MyToastUnit>
{
    private string _currentMessage;
    private Sprite _currentIcon;

    // 公开方法，供外部调用
    public void ShowMessage(string message, Sprite icon = null)
    {
        _currentMessage = message;
        _currentIcon = icon;
        Toast(); // 调用基类的 Toast 方法
    }

    protected override void OnToast(MyToastUnit unit)
    {
        unit.SetMessage(_currentMessage, _currentIcon);
    }
}
```

使用：
```csharp
// 显示简单消息
toast.ShowMessage("操作成功！");

// 显示带图标的消息
toast.ShowMessage("获得金币 +100", coinIcon);
```

### 高级用法：多种消息类型

```csharp
public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}

public class MyToast : UISideToast<MyToastUnit>
{
    [Header("消息类型配置")]
    [SerializeField] private Sprite _infoIcon;
    [SerializeField] private Sprite _successIcon;
    [SerializeField] private Sprite _warningIcon;
    [SerializeField] private Sprite _errorIcon;

    private string _currentMessage;
    private ToastType _currentType;

    public void ShowMessage(string message, ToastType type = ToastType.Info)
    {
        _currentMessage = message;
        _currentType = type;
        Toast();
    }

    protected override void OnToast(MyToastUnit unit)
    {
        Sprite icon = _currentType switch
        {
            ToastType.Info => _infoIcon,
            ToastType.Success => _successIcon,
            ToastType.Warning => _warningIcon,
            ToastType.Error => _errorIcon,
            _ => _infoIcon
        };

        unit.SetMessage(_currentMessage, icon);
    }
}
```

使用：
```csharp
toast.ShowMessage("操作成功！", ToastType.Success);
toast.ShowMessage("警告：库存不足", ToastType.Warning);
toast.ShowMessage("错误：网络连接失败", ToastType.Error);
```

## 自定义 Toast 单元

### 扩展 UIToastUnit

```csharp
public class RichToastUnit : UIToastUnit
{
    [SerializeField] private TMPro.TextMeshProUGUI _titleText;
    [SerializeField] private TMPro.TextMeshProUGUI _messageText;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Button _closeButton;

    private void Awake()
    {
        // 添加关闭按钮功能
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(() =>
            {
                ForceRecycle(); // 立即回收
            });
        }
    }

    public void SetContent(string title, string message, Color backgroundColor)
    {
        _titleText.text = title;
        _messageText.text = message;
        _backgroundImage.color = backgroundColor;
    }
}
```

## 动画自定义

Toast 系统使用 AxuTweener 进行动画控制，你可以在 `UIToastUnit` 中自定义动画：

```csharp
public class CustomToastUnit : UIToastUnit
{
    [SerializeField] private Transform _iconTransform;

    // 重写 OnGet 方法，添加自定义进场动画
    public new void OnGet(Vector2 startPos, Vector2 endPos)
    {
        base.OnGet(startPos, endPos);

        // 添加图标旋转动画
        if (_iconTransform != null)
        {
            _iconTransform.localRotation = Quaternion.Euler(0, 0, 360);
            _iconTransform.ToRotation(Quaternion.identity, TweenInDuration)
                         .SetEase(TweenEaseType)
                         .Start();
        }
    }
}
```

## 常见应用场景

### 1. 游戏提示消息
```csharp
// 任务完成
toast.ShowMessage("任务完成：击败 10 个敌人", ToastType.Success);

// 获得物品
toast.ShowMessage("获得物品：传说之剑", ToastType.Info);

// 成就解锁
toast.ShowMessage("成就解锁：初次胜利", ToastType.Success);
```

### 2. 系统通知
```csharp
// 好友上线
toast.ShowMessage("好友 [玩家名] 已上线");

// 邮件通知
toast.ShowMessage("您有 3 封新邮件");
```

### 3. 错误提示
```csharp
// 操作失败
toast.ShowMessage("操作失败：权限不足", ToastType.Error);

// 网络错误
toast.ShowMessage("网络连接失败，请检查网络设置", ToastType.Warning);
```

## 生命周期详解

每个 Toast 单元的生命周期如下：

```
1. 从对象池获取 (PoolOnGet)
   ↓
2. OnGet() - 设置初始位置和透明度
   ↓
3. 进场动画 (In) - 从 StartPos 移动到 EndPos
   ↓
4. 停留 (Stay) - 停留 StayDuration 秒
   ↓
5. 退场动画 (Out) - 淡出
   ↓
6. 回收到对象池 (PoolOnRelease)
   ↓
7. OnRelease() - 清理状态
```

### 强制回收

当新消息弹出且超过最大显示数量时，最旧的消息会被强制回收：

```csharp
// 内部调用流程
unit.ForceRecycle() 
  → 停止当前生命周期协程
  → 立即启动退场动画
  → 回收到对象池
```

## 注意事项

1. **对象池管理**：Toast 单元会自动复用，不要手动销毁
2. **生命周期**：不要在 `OnToast` 中执行耗时操作
3. **位置配置**：确保 `Start Anchored Pos` 和 `End Anchored Pos` 在屏幕可见范围内
4. **性能优化**：合理设置 `Max Showing Count`，避免同时显示过多消息
5. **Canvas 设置**：确保 Toast 的父容器在 Canvas 的最上层，避免被其他 UI 遮挡

## 调试技巧

### 查看当前状态
```csharp
// 在 UIToastUnit 中查看当前状态
Debug.Log($"Toast 状态: {unit.CurrentState}");
// 可能的值: In, Stay, Out, Recycled
```

### 调整动画速度
```csharp
// 在测试时可以加快动画速度
toast.TweenInDuration = 0.1f;
toast.TweenOutDuration = 0.1f;
toast.StayDuration = 0.5f;
```

## 性能优化建议

1. **预热对象池**：在游戏开始时预先创建一些 Toast 单元
2. **合理设置停留时间**：不要设置过长的停留时间
3. **限制显示数量**：根据屏幕大小合理设置 `Max Showing Count`
4. **避免频繁调用**：使用防抖机制，避免短时间内弹出大量消息

```csharp
// 防抖示例
private float _lastToastTime;
private const float ToastCooldown = 0.5f;

public void ShowMessageWithCooldown(string message)
{
    if (Time.time - _lastToastTime < ToastCooldown)
        return;

    _lastToastTime = Time.time;
    ShowMessage(message);
}
```

## 扩展阅读

- 了解 `AxuTweener` 动画系统以实现更复杂的动画效果
- 学习对象池模式以优化性能
- 参考 `UIBasePanel` 和 `UIBaseUnit` 了解 UI 框架的基础结构
