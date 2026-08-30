# 数学与动画模块文档 (Math & Interpolation)

## 1. 简介
本模块位于 `Packages/com.cielevel.axu-frame-kit/Runtime/Common/Utility/Math`，提供了一些通用的数学计算与动画插值工具。主要包含两个部分：
1.  **Interpolation**: 基于扩展方法的轻量级 Tween 动画库。
2.  **Curve**: 独立的贝塞尔曲线计算工具。

---

## 2. 插值动画 (Interpolation)

核心类为 `AxuTweenExtension`，它为 Unity 的常用组件提供了类似 DOTween 的链式扩展方法。
**注意**：调用扩展方法创建动画后，**必须调用 `.Start()`** 才能开始播放。
**注意（完成判定）**：补间完成当帧 Runner 会立即回收复用该 Tweener（`Reset()` 后 `IsCompleted` 复位并移出列表），因此**不要事后轮询 `IsCompleted`**（永远读到 false）；完成回调请使用 `OnComplete`。

### 常用接口一览表

| 扩展对象 | 方法名 | 参数 | 描述 |
| :--- | :--- | :--- | :--- |
| **Transform** | `ToPos` / `ToLocalPos` | `(Vector3 target, float duration)` | 移动到指定的世界或本地位置 |
| **Transform** | `ToPosX`, `ToPosY`, `ToPosZ` | `(float value, float duration)` | 移动世界坐标的单一轴向 |
| **Transform** | `ToLocalPosX`, `ToLocalPosY`, `ToLocalPosZ` | `(float value, float duration)` | 移动本地坐标的单一轴向 |
| **Transform** | `ToLocalScale` | `(Vector3 target, float duration)` | 缩放变换 |
| **Transform** | `RotateQua` / `LocalRotateQua` | `(Quaternion target, float duration)` | 旋转 (基于四元数) |
| **Transform** | `RotateEuler` / `LocalRotateEuler` | `(Vector3 target, float duration)` | 旋转 (基于欧拉角) |
| **RectTransform** | `ToAnchoredPos` | `(Vector2 target, float duration)` | UI 锚点位置移动 |
| **RectTransform** | `ToAnchoredPosX`, `ToAnchoredPosY` | `(float value, float duration)` | UI 锚点位置单一轴向移动 |
| **Graphic** | `ToColor` | `(Color target, float duration)` | UI 颜色渐变 (Image, Text 等) |
| **Graphic** | `ToAlpha` | `(float alpha, float duration)` | UI 透明度渐变 |
| **CanvasGroup** | `ToAlpha` | `(float alpha, float duration)` | CanvasGroup 透明度渐变 |
| **(Tweener)** | `Start` | `()` | **启动动画 (链式调用的最后一步)** |
| **(Tweener)** | `SetEase` | `(EaseType type)` | 设置缓动曲线 (需引用 Core 命名空间) |
| **(Tweener)** | `OnComplete` | `(Action callback)` | 动画完成回调 |

### 使用示例

```csharp
using AxuFrameKit.Common.Utility.Math.Interpolation;

// 1. 移动 Transform
transform.ToPos(new Vector3(10, 0, 0), 1f)
         .SetEase(EaseType.OutBack) // 设置缓动
         .OnComplete(() => Debug.Log("Arrived")) // 设置回调
         .Start(); // <--- 必须调用 Start

// 2. UI 透明度渐变
myImage.ToAlpha(0f, 0.5f).Start();
```

---

## 3. 曲线计算 (Curve)

核心类为 `BezierCore`，提供静态方法进行贝塞尔曲线的数学计算。该类不依赖 MonoBehaviour，纯数学实现。

### 接口一览表

| 方法名 | 功能 | 参数说明 | 返回值 |
| :--- | :--- | :--- | :--- |
| `GetLinearPoint` | 线性插值 (Lerp) | `p0` (起点), `p1` (终点), `t` (0-1) | `Vector3` (当前点) |
| `GetQuadraticPoint` | 二次贝塞尔曲线 | `p0`, `p1` (控制点), `p2`, `t` | `Vector3` (当前点) |
| `GetCubicPoint` | 三次贝塞尔曲线 | `p0`, `p1` (控制点1), `p2` (控制点2), `p3`, `t` | `Vector3` (当前点) |
| `GetCubicTangent` | 计算三次曲线切线 | 同上 | `Vector3` (切线方向，未归一化) |
| `GetCubicPoints` | 离散化曲线取点 | 同上 + `resolution` (采样点数量) | `Vector3[]` (路径点数组) |

### 使用示例

```csharp
using AxuFrameKit.Common.Utility.Math.Curve;

// 计算三次贝塞尔曲线在 t=0.5 处的位置
Vector3 pos = BezierCore.GetCubicPoint(start, p1, p2, end, 0.5f);

// 获取整条曲线的 100 个采样点 (用于 LineRenderer 绘制等)
Vector3[] path = BezierCore.GetCubicPoints(start, p1, p2, end, 100);
lineRenderer.positionCount = path.Length;
lineRenderer.SetPositions(path);
```
