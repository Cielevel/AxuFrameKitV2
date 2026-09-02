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

## 4. 看板 Scenario 页可视化深度不足（报告详情 + 运行进度）【验收反馈，Phase 6 决策点】

- **来源**：2026-09-02 工程所有者首次实机试用（echo-demo 两次运行）反馈，属 Phase 3 验收产出。
- **现象**：① 运行中看板仅一句"Suite 运行中"状态字，Play 期间无进度/当前场景/实时指标（黑盒期）；② 运行后看板仅 1 行摘要 + 每场景 1 行文字（`AxuTestRunnerWindow.ScenarioTrend.cs` `RenderScenarioReport`），报告 JSON 里的指标表/预算判定/基线对比/findings/截图 artifact 全部无处展示，用户须自行打开 JSON 才知道发生了什么。
- **建议方案**（按投入产出排序，前两项即可根治）：
  - P0（Editor 层纯 UI，0.5–1d）：场景行 Foldout 展开——指标表 / 预算表（门槛 vs 实际着色）/ 基线对比表（base→current，劣化标红）/ findings 列表 / 截图缩略图；一句话结论（findings 人话翻译）；窗口轮询 `Temp/axu_scenario_status.json`（协议已有，pipeline 命令在用）渲染"第 x/N 个场景"进度条；打开 JSON/目录/HTML 三按钮。
  - P1（Agent 层，1d）：游戏内 Overlay HUD——即方案 §3.4/Phase 5 的 `AgentOverlay.cs`（`#if AXU_TEST_AGENT` 守护），Play 时 Game 视图显示当前 suite/scenario、进度、实时 fps/内存。**原计划已排 Phase 5（按需），维持不动，不因本条提前。**
  - P2：Trend 页自绘折线图（UITK generateVisualContent）+ 任意两份报告 A/B 对比。
- **决策约定**：不因此打乱既定 Phase 顺序；Phase 6 收口时由工程所有者决定"顺手做掉 P0"或"留作 v2 后独立迭代"。

## 5. Agent asmdef 对 InputSystem/UGUI 的硬引用正规化【P2 级，Phase 6 审查发现】

- **现象**：`Packages/com.cielevel.axu-test-framework/Agent/AxuTestFramework.Agent.asmdef` 硬引用 `Unity.InputSystem` 与 `UnityEngine.UI`（monkey 输入注入与 UGUI 降级路径用），但包 package.json 未声明这两个依赖；versionDefines（AXU_INPUTSYSTEM_PRESENT）只守护代码符号、不能让 asmdef 引用解析变可选。**未装这两个包的干净工程中 Agent 程序集有整体编译失败风险**（本工程两包齐全故未暴露）。
- **来源**：2026-09-02 Phase 6 unity-reviewer 交付审查 P2（0.3.0 发版仅做 README 措辞准确化，本项为正规化遗留）。
- **建议方案**：拆分守护 asmdef——新建 `AxuTestFramework.Agent.Input`（versionDefines: com.unity.inputsystem + defineConstraints: AXU_INPUTSYSTEM_PRESENT + references: InputSystem/Agent），monkey 的 InputSystem 注入路径移入；UGUI 降级路径同理或保留主程序集（com.unity.ugui 在多数工程默认存在）。或在 package.json 正式声明依赖（代价：强制所有接入工程安装 InputSystem）。
- **临时缓解（已做）**：README「包定位」Agent 条目已如实注明硬引用约束。

## 6. _Core 层历史遗留 UnityEngine 依赖清理【P2 级，2026-09-02 SO 创建窗口交付审查发现】

- **现象**：`Runtime/_Core/Extension/FloatExtension.cs:4` 与 `Runtime/_Core/Utility/RandomX/RandomNickname.cs:4` 存在 `using UnityEngine`（分别使用 Mathf 等），违反"_Core 纯 C# 零 Unity 依赖"分层红线。属框架迁包前的历史遗留，非本次改动引入；git status 确认本次未触碰这两个文件。
- **建议方案**：FloatExtension 的 NearZero/Near 类方法改用 `System.Math.Abs`；RandomNickname 剥离 `GetRandom` 引擎随机依赖（改用 `_Core/RandomX` 的引擎无关随机）；若剥离代价过高则将对应成员移出 _Core 至 Common 层。
- **验收**：`AxuFrameKit.Core` 程序集编译不依赖 UnityEngine（可在 asmdef 层面用 versionDefines 或编译目标验证），包内既有测试全绿。
