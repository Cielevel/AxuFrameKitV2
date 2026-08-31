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

## 3. ~~构建/导入会反复产生设置资产漂移~~ 【已按选项 1 处理 2026-09-01：一次性刻意提交为基线】

- **处理**：工程所有者选定方案 1——把三份资产的当前序列化状态**刻意提交**为基线（URP schema 翻新 + YAML 格式），使磁盘形态与 Unity 产出对齐，此后构建/导入的 diff 应稳定为干净（见"设置资产基线"提交）。
- **判读**：`PC_RPAsset`/`GlobalSettings` 的新增字段（`m_AllowPostProcessAlphaOutput`/`m_PrefilterAlphaOutput` 等）与枚举值位移是 URP 17.0.3 把资产翻新到当前 schema，语义不变；`GraphicsSettings` 为纯 YAML 换行格式。
- **遗留观察点**：① 若此后构建仍反复出现同类漂移，说明 Unity 每次都会重写（届时改用方案 2：构建后检查还原）；② `ProjectSettings.asset` 的 `preloadedAssets` 自动重挂（guid `052faaac586de48259a63d0c4782560b`，疑为 InputSystem 默认 Actions）未随本次基线入库（当前工作区该文件干净），复发时单独决策。
