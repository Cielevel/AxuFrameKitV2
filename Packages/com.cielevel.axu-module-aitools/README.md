# AxuFrameKit Modules - AITools

AxuFrameKit 家族的可选独立模块：基于 UI Toolkit 的 AI 辅助编辑器窗口。无框架依赖，可单独使用。

## 功能

| 窗口 | 菜单路径 | 用途 |
| --- | --- | --- |
| Chat Window | `AxuFrameKit/Modules/AI Tools/Chat Window` | AI 对话窗口，支持流式响应与 Markdown 风格富文本渲染 |
| Script Generator | `AxuFrameKit/Modules/AI Tools/Script Generator` | 根据选中 GameObject 的组件字段，用 AI 生成对应脚本 |
| Script Filler | `AxuFrameKit/Modules/AI Tools/Script Filler` | 用 AI 为目标物体上的脚本字段自动填充赋值代码 |

## 配置（AIConfig）

每个窗口通过 `AIConfig`（ScriptableObject）保存模型名、API URL、API Key、temperature、system prompt 等设置。

- 创建路径：`Assets > Create > AxuFrameKit > AI-Tools > AI-Config`
- 窗口左上角可切换当前使用的 AIConfig，选择结果保存在 EditorPrefs 中

**重要提示：AIConfig 资产中包含明文 API Key，属于用户数据，不随包分发。** 请自行创建 AIConfig 并填入自己的 API Key，切勿将配置资产提交到版本库或打包分发。

## 示例

`Samples~` 目录下包含脚本生成器示例场景（SC Sample Modules AI Generators）。在 Package Manager 中选择本包，展开 Samples 并点击 Import 即可导入到 `Assets/Samples/` 下使用。
