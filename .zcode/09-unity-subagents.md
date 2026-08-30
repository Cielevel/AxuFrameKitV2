# 09 · Unity 全流程子代理体系（ZCode Subagents）

> 2026-08-30 建立。以 Unity 工程师视角设计的一套覆盖 Unity 开发全流程的 ZCode 子代理，
> 采用**双层结构**：全局层（`~/.zcode/agents/`，任何 Unity 6 工程可用）+ 工程层（`.zcode/agents/`，本文件所在工程定制版，同名覆盖全局层）。

## 1. 加载与覆盖机制（已从 ZCode 主程序验证）

- 扫描顺序：先用户级 `~/.zcode/agents/`，再工程级 `<工程>/.zcode/agents/`（**递归**扫描 `.md`/`.markdown`，按文件名排序）
- 同名解析：所有 agent 以 `name` 为键放入 Map，**后写覆盖先写** → 优先级 **工程层 > 用户层 > 插件**；同名是**整文件替换**（不合并正文），故工程版必须自包含
- frontmatter：`name`、`description` 必填；可选 `tools`、`color`（red/blue/green/yellow/purple/orange/pink/cyan）、`model`、`maxTurns`、`memory`、`skills`、`injectAgentsMd`（默认 true，子代理会自动收到工作区 AGENTS.md）
- **工程级文件的 `permissionMode` 会被 ZCode 安全剥离**（不要依赖该字段）
- 新增/修改 agent 文件需**重启会话**（或新开会话）才生效
- agents 目录内**不要放非 agent 的 .md 文件**（会被解析并告警）

## 2. 代理清单（两层同名，工程版注入本工程规范）

| 代理 | 职责 | 工具姿态 |
|---|---|---|
| `unity-architect` | 架构/方案设计、asmdef 与包边界决策、任务拆解 | 只读 |
| `unity-runtime-dev` | 运行时 C#（_Core/Common 层组件、工具、模式） | 写代码 |
| `unity-editor-dev` | Editor 扩展（KitBox 页面/Inspector/窗口/UI Toolkit） | 写代码 |
| `unity-ui-dev` | UI 框架（Manager→Area→Panel→Page）、UIBind* 绑定、UIListView | 写代码 |
| `unity-asset-scene` | 资产组织/导入设置/场景搭建/Prefab/.meta 纪律 | 设计+经 API 执行 |
| `unity-pipeline` | **Editor 执行器**：驱动运行中 Editor（编译/测试/资产/场景/烘焙/构建/截图），唯一可执行写命令的代理 | 可执行（副作用） |
| `unity-pipeline-observe` | Editor 只读观测：状态/Console/进度 | 只读 |
| `unity-tester` | 测试编写（EditMode/PlayMode）与运行解读 | 写代码+跑测试 |
| `unity-debugger` | 报错/异常/编译错误根因诊断与最小修复 | 读+定点修 |
| `unity-build` | 构建配置、无头/Pipeline 构建、包版本与 CHANGELOG | 配置+执行 |
| `unity-reviewer` | 交付前规范审查（分层红线/命名/.meta/性能） | 只读 |

## 3. 派发矩阵（主代理按此路由）

- **先想清楚要什么**：方案 → architect；代码 → 三个 dev；执行 → pipeline；状态 → observe；测试 → tester；报错 → debugger；发版 → build；验收 → reviewer
- 需求/方案类 → `unity-architect`（产出方案后由主代理拆发）
- 改 `Runtime/_Core|Common` 非 UI 代码 → `unity-runtime-dev`；UI（Framework/Kit）→ `unity-ui-dev`；包根 `Editor/` → `unity-editor-dev`
- 资产/场景/Prefab 实际操作 → `unity-asset-scene`（含 Pipeline API 执行）；单纯任意 Editor 命令 → `unity-pipeline`
- Editor 运行中且要"实际执行/验证"→ `unity-pipeline`；只看状态/日志 → `unity-pipeline-observe`；Editor 未运行且需无头验证 → `unity-build`；纯日志/堆栈分析 → `unity-debugger`
- 写/跑测试 → `unity-tester`（跑测试也走 Pipeline API，仅限测试类命令）
- 交付前 → `unity-reviewer`（对照 AGENTS.md 审查单）

## 4. 两层差异约定

- 全局版：通用 Unity 6 知识 + 通用 Pipeline API 用法（`Library/Pipeline/.unity-pipeline-port` 服务发现），自适任何装了 `com.unity.pipeline` 的工程；开工先读工作区 AGENTS.md/知识库
- 工程版：硬编码本工程路径、asmdef 表、命名空间规范、知识库索引（01-08）、分层红线与本工程陷阱（域重载关闭、autoReferenced:false 等）
- **维护规则**：改全局版行为后，检查工程版是否需要同步；工程版优先（同名覆盖），长期漂移以工程版为准并回灌全局版

## 5. 相关文档

- Pipeline API 细节（端点/命令/鉴权/实测示例）→ [08-unity-pipeline-api.md](08-unity-pipeline-api.md)
- 分层/命名/包结构规范 → 根目录 `AGENTS.md`、[02-package-structure.md](02-package-structure.md)
