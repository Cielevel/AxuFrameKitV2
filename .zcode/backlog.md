# 待办事项（Backlog）

> 记录已识别但暂缓处理的问题。每项注明发现来源与建议方案，处理完成后移入下方"已完成"或直接删除。
> （2026-08-31 由 AxuTestFramework 交付审查与出包验证产生）

## 1. ~~OllamaModelCall 窗口脚本阻断 Player 构建~~ 【已修复 2026-08-31，Phase 1 前置授权修复】

- **修复**：`Assets/AxuExperimental/OllamaModelCall/OllamaScriptFillerWindow.cs` 整文件 `#if UNITY_EDITOR` 包裹（未移动文件、GUID 不变）。
- **审计**：全 Assets 树扫描 `using UnityEditor`/`EditorWindow`/`MenuItem`/`AssetDatabase`/`EditorPrefs` 等无保护用例，除该文件外其余命中全部位于 `Editor/` 子目录（Player 不编译），无同类残留。
- **验证**：以 Phase 1 DoD 的 macOS Player 构建实测为准（见 `.zcode/plans/axu-test-framework-game-test-platform-plan.md` Phase 1 台账）。

## 2. `_Core`"纯 C# 零依赖"表述与实际不符 【P3 级，既有问题】

- **现象**：`Runtime/_Core/Extension/FloatExtension.cs:4`、`Runtime/_Core/Utility/RandomX/RandomNickname.cs:3` 存在 `using UnityEngine`（Mathf 等）；`AxuFrameKit.Core.asmdef` 为 `noEngineReferences:false`。与 AGENTS.md、`.zcode/02` 中"_Core 纯 C# 零 Unity 依赖"的表述矛盾。
- **来源**：2026-08-31 unity-reviewer 交付审查 P3（非 AxuTestFramework 交付引入）。
- **建议方案（二选一）**：
  - (a) 清理代码：将依赖 UnityEngine 的辅助移到 Common 层或以纯 BCL 实现（需全面审计 `_Core` 全部文件的 `using UnityEngine`，上述两处仅是审查抽样发现）；
  - (b) 修订文档：表述改为"Core 基本无 Unity 依赖（个别文件使用 UnityEngine 数学工具），可在非渲染场景使用"。
- 建议与 `_Core` 后续演进一起处理；在处理前，涉及 `_Core` 可移植性的新代码应避免引入新的 UnityEngine 依赖。

## 3. 构建/导入会反复产生设置资产漂移 【P3 级，反复出现】

- **现象**：Player 构建（有时含域重载导入）后，以下文件会反复出现非交付性改动，需在提交前甄别还原：
  - `ProjectSettings/ProjectSettings.asset`：`preloadedAssets` 被自动加回一项（guid `052faaac586de48259a63d0c4782560b`，疑为 InputSystem 的默认 Actions 资产，Unity 会自动重挂）；
  - `Assets/URP/Settings/PC_RPAsset.asset` + `UniversalRenderPipelineGlobalSettings.asset`：URP schema 重序列化（含字段增删与枚举值位移，如 `m_PrefilteringModeAdditionalLight` 4→0）；
  - `ProjectSettings/GraphicsSettings.asset`：纯 YAML 换行格式漂移。
- **来源**：2026-08-31 M0 事故与出包验证两次实证；均已还原保持提交干净，但**下次构建大概率复发**。
- **建议**：要么刻意提交一次这些资产的当前序列化状态（之后稳定），要么在构建工作流中固定"构建后检查 git status"步骤；同时排查该 guid 资产来源决定 preloadedAssets 的期望值。
