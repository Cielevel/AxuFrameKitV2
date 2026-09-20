# AxuFrameKitV2 项目知识库（.zcode）

> 本目录由 ZCode 于 2026-08-29 通过 3 个并行 Explore 子代理对全项目勘察后生成，
> 供后续 AI 会话快速建立项目认知，避免重复勘察。如项目结构发生大变化，建议重新生成。

## 项目速览

**AxuFrameKit**（阿修框架工具包，by Cielevel）是面向 **Unity 6（6000.0.23f1 / URP 17.0.3）** 的应用程序框架。
分层架构：`_Core`（纯 C# 核心库，无 Unity 依赖）→ `Common`（Unity 运行时组件与 UI 框架）→ `Modules`（业务模块，2026-08-29 起已全部包化）。

关键现状：**框架本体（_Core 80 + Common 151 + Editor 29 个 .cs）在嵌入式包 `Packages/com.cielevel.axu-frame-kit`（3 个 asmdef）**；
2026-08-29 起 8 个功能模块也全部拆分为独立嵌入式包（4 个 `axu-module-*` + 4 个 `axu-indie-*`，其中仅 LiveStream 依赖框架包）；
此后又增 2 个可选包——测试基建 `com.cielevel.axu-test-framework` 与 `com.cielevel.axu-unity-cli-kit`（后者无运行时代码），
**可选包共 10 个**，`Packages/` 下另有 Unity 官方实验包 `com.unity.pipeline`。
`Assets/AxuFrameKit/` 与 `Assets/AxuIndieModules/` 已整体移除。

## 文件导航（建议按编号顺序阅读）

| 文件 | 内容 | 适用场景 |
|---|---|---|
| [01-project-overview.md](01-project-overview.md) | 项目概览、Unity/URP/构建设置、依赖包清单 | 了解项目全貌与工程配置 |
| [02-package-structure.md](02-package-structure.md) | 嵌入式包结构、3 个 asmdef、目录树与代码规模 | 改框架代码前必读 |
| [03-core-modules.md](03-core-modules.md) | `_Core` 层模块清单（FSM/事件/对象池/加密/缓动/数据结构等） | 查找核心工具与设计模式 |
| [04-common-modules.md](04-common-modules.md) | `Common` 层模块清单（UI 框架/数据绑定/网络/存档/CSV/Tween 等） | 查找 Unity 运行时组件 |
| [05-editor-modules.md](05-editor-modules.md) | `Editor` 层模块清单（KitBox 工具箱/Hierarchy 增强/独立窗口等） | 查找或扩展编辑器工具 |
| [06-assets-layout.md](06-assets-layout.md) | Assets 布局、示例场景、独立模块、实验模块、文档目录 | 找示例场景、新增模块 |
| [07-docs-and-history.md](07-docs-and-history.md) | 全项目文档清单与状态、AGENTS.md 过时条目、git 演进时间线 | 判断文档可信度、了解历史决策 |
| [08-unity-pipeline-api.md](08-unity-pipeline-api.md) | **Unity Pipeline 本地 API**（HTTP 直连运行中 Editor：142 个命令、鉴权、示例） | **需要编译/测试/读 Console/操作资产时优先用这个** |
| [09-unity-subagents.md](09-unity-subagents.md) | **Unity 全流程子代理体系**（11 个代理的分工、派发矩阵、全局/工程双层覆盖机制） | **派发子代理前、调整代理定义时先读这个** |
| [plans/](plans/) | **进行中/已完成工作的方案与交接文档**。AxuTestFramework v1 测试框架：已于 2026-08-31 全部完成交付（M0-M5；交接归档见 [plans/axu-test-framework-resume.md](plans/axu-test-framework-resume.md)）。**v2 游戏级测试平台（Phase 0-6 已完成，Phase 5 真机/Overlay 按需未启动，2026-09-02）**：[plans/axu-test-framework-game-test-platform-plan.md](plans/axu-test-framework-game-test-platform-plan.md)——方案全文 + 逐 Phase 进度台账（含开工问答/实施记录/DoD 证据），相关子代理 charter 已按其对齐（2026-08-31） | 实施/评审 v2 扩展、了解场景测试平台设计时阅读；查各 Phase 做到哪、遗留了什么先看其第 7 节台账 |
| [backlog.md](backlog.md) | **待办事项**：已识别暂缓处理的问题（含发现来源与建议方案）；2026-09-20 核验：#2/#6 已解决销账，#4 P0 已完成（P1/P2 留后续），未决 #5（Agent asmdef 依赖正规化） | 动工前先看有无相关待办；发现新问题记入此处 |

## 重要提醒（信息时效性）

- 根目录 `AGENTS.md` 与 `README.md` 已于 2026-08-29 重写、2026-09-18 再次同步（补入 `axu-unity-cli-kit`、修正 `com.unity.pipeline` 版本为 0.7.0-exp.1）；若发现二者与本库描述冲突，以更新时间较新者为准并回报修正。
- 框架文档已于 2026-08-29 迁入包内、随代码放置（19 份散落 + 6 份 KitDoc），包根新增 README.md（含文档索引）/ CHANGELOG.md。
  07 号文档的文档清单为迁移前快照，仅供历史对照。
- 本库 01/02/06 号文档的核心数据为 **2026-08-29 快照**（包化刚完成时），此后新增的包与测试体系未逐一回填，需要精确现状时以根 `AGENTS.md` 与各包 README 为准。
- 命名空间规范：`AxuFrameKit._Core.*`（Core 层）、`AxuFrameKit.Common.*`（Common 层）、`AxuFrameKit.Common.Editor.*`（Editor 层）、`AxuCliKit.*`（CLI Kit 包）。
