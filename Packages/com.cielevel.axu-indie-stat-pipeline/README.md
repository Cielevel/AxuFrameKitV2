# AxuIndie - StatPipeline

Roguelike 风格的数值管线系统，独立模块，无任何外部依赖。

## 核心概念

- **Stat**: 数值容器（`StatInt` / `StatFloat` / `StatDouble` / `StatLong`），基于 `StatCore`，按 `StatPipelineStages` 定义的阶段依次计算。
- **Modifier**: 数值修改器（`StatModifier` / `StatModifierCore` / `StatModifierLink`），挂载 Calculator 对数值进行加、减、乘、除、百分比、钳制等运算，支持属性间联动（Link）。
- **Calculator**: 计算器（`StatModifierCalculator` / `LinkStatModifierCalculator`），预置常用计算器资产，可用编辑器生成窗口批量创建。
- **Analysis**: 调试与分析（`StatDebugger`、Stat Analysis Window），用于运行时查看数值管线状态。

## 编辑器菜单

- `AxuIndieModules/StatPipeline/Calculator-Generator`：计算器生成窗口，可批量创建/清理标准命名的 Calculator 资产。
- `AxuIndieModules/StatPipeline/Stat Analysis Window`：数值分析窗口。

## 创建资产（CreateAssetMenu）

- `AxuIndieModules/StatPipeline/Stat/StatInt` / `StatFloat` / `StatDouble` / `StatLong`
- `AxuIndieModules/StatPipeline/Stages`
- `AxuIndieModules/StatPipeline/StatModifier`
- `AxuIndieModules/StatPipeline/StatModifierLink`

## 预置 Calculator 资产

预置计算器资产位于包内 `Runtime/Resources/`（`Calculator/` 与 `LinkCalculator/`），可通过 `Resources.Load` 跨包加载，例如：

```csharp
var calc = Resources.Load<StatModifierCalculator>("Calculator/Calculator-加法");
```

## 示例

示例（场景 + 示例脚本 + 示例资产）位于包的 `Samples~` 目录，通过 Package Manager 选中本包后点击 **Samples → Import** 导入使用。
