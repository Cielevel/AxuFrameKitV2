# AxuFrameKit Modules - GizmosUtil

Scene 视图 Gizmos 绘制库（AxuFrameKit 家族的可选模块，无任何框架依赖）。

## 用途

在任意 GameObject 上挂载 `GizmosDrawer` 组件，即可在 Scene 视图中绘制各类辅助图形（直线、贝塞尔曲线、形状、路径、射线等）：

- 核心组件 `GizmosDrawer`（`MonoBehaviour`）通过 `[SerializeReference]` 维护一个 `BaseDrawer` 抽象列表，`OnDrawGizmos` 时逐个调用各抽屉的 `Draw()`。
- 每个抽屉均为 `GizmosDrawer` 的嵌套可序列化类，可自由组合、按需配置颜色与参数。
- 自带 `GizmosDrawerEditor` 自定义检视器，使用 `ReorderableList` 对抽屉列表进行增删、排序与编辑。

## 目录结构

```
com.cielevel.axu-module-gizmos-util/
├── Runtime/   # GizmosDrawer.cs / .Line / .Shape / .Advanced（partial 类）
├── Editor/    # GizmosDrawerEditor.cs（ReorderableList 检视器）
└── Samples~/  # 示例场景
```

## 示例导入

通过 Package Manager 选中本包（Packages: In Project → AxuFrameKit Modules - GizmosUtil），在 Samples 区点击 Import 即可将示例场景 `SC Sample Module Gizmos` 导入到 `Assets/Samples/` 下。
