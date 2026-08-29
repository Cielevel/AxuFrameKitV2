# AxuFrameKitV2 项目知识库（.zcode）

> 本目录由 ZCode 于 2026-08-29 通过 3 个并行 Explore 子代理对全项目勘察后生成，
> 供后续 AI 会话快速建立项目认知，避免重复勘察。如项目结构发生大变化，建议重新生成。

## 项目速览

**AxuFrameKit**（阿修框架工具包，by Cielevel）是面向 **Unity 6（6000.0.23f1 / URP 17.0.3）** 的应用程序框架。
分层架构：`_Core`（纯 C# 核心库，无 Unity 依赖）→ `Common`（Unity 运行时组件与 UI 框架）→ `Modules`（业务模块，待包化）。

关键现状：**框架本体（_Core 80 + Common 151 + Editor 29 个 .cs）在嵌入式包 `Packages/com.cielevel.axu-frame-kit`（3 个 asmdef）**；
2026-08-29 起 8 个可选模块也全部拆分为独立嵌入式包（4 个 `axu-module-*` + 4 个 `axu-indie-*`，其中仅 LiveStream 依赖框架包），
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

## 重要提醒（信息时效性）

- 根目录 `AGENTS.md` 与 `README.md` 仍描述 2026-06 包迁移**之前**的 `Assets/AxuFrameKit/...` 布局，**已过时**；
  以后者为准会找错位置，应以本知识库与包内实际代码为准（过时明细见 07 号文档）。
- 框架文档已于 2026-08-29 迁入包内、随代码放置（19 份散落 + 6 份 KitDoc），包根新增 README.md（含文档索引）/ CHANGELOG.md；
  `Assets/AxuFrameKit/` 下仅剩 `Modules/`（待包化）。07 号文档的文档清单为迁移前快照，仅供历史对照。
- 命名空间规范：`AxuFrameKit._Core.*`（Core 层）、`AxuFrameKit.Common.*`（Common 层）、`AxuFrameKit.Common.Editor.*`（Editor 层）。
