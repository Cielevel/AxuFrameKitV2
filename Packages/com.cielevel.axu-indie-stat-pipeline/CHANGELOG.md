# Changelog

## [Unreleased]

### 2026-09-20

- **文件名与类名对齐（3 文件，GUID 不变）**：`Calculator/CommonCalculator/_StatModifierCalculator.cs`→`StatModifierCalculator.cs`、`Calculator/LinkCalculator/_LinkStatModifierCalculator.cs`→`LinkStatModifierCalculator.cs`（去掉排序用 `_` 前缀）、`Stat/StatPipelineStages.cs`→`StatStages.cs`。仅文件重命名，类型名与资产引用不变。

## [0.1.0] - 2026-08-29

- 自 Assets/AxuIndieModules/StatPipeline 迁入为独立嵌入式包，新增 Runtime/Editor 程序集定义，示例迁入 Samples~。
