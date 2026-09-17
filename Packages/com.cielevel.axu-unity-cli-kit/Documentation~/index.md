# Axu CLI Agent Kit — 文档

这个包只做一件事：**让"用命令行驱动 Unity"这件事在工程里可被发现、可被复用、可被更新。**
它不包含运行时代码，不依赖任何其它包，因此可以安全地拉进任何 Unity 工程。

## 阅读顺序

| # | 文档 | 什么时候读 |
|---|---|---|
| 1 | [安装与更新](01-install-and-update.md) | 第一次接入，或升级到新版本 |
| 2 | [Agent 集成](02-agent-integration.md) | 想知道 skill 写到了哪、团队怎么协作、冲突怎么处理 |
| 3 | [Unity CLI 速览](03-unity-cli-basics.md) | 需要命令行全景：编辑器/工程/构建/测试/认证 |
| 4 | [实时 Editor（Pipeline）](04-live-editor-pipeline.md) | 要驱动已打开的 Editor：改场景、重编译、跑测试、热重载 |
| 5 | [端到端配方](05-recipes.md) | 想直接抄步骤：新机准备、CI、批量资产、Player |
| 6 | [故障排查](06-troubleshooting.md) | 连不上、卡住、退出码看不懂、skill 不生效 |
| 7 | [扩展与发布](07-extending-the-kit.md) | 要加自己的 skill，或把包发到自己的仓库 |

## 包内结构

```
com.cielevel.axu-unity-cli-kit/
├── package.json          UPM 清单（无依赖）
├── README.md             包级说明与快速开始
├── CHANGELOG.md          版本变更
├── LICENSE.md            MIT + 第三方声明
├── Documentation~/       本目录（人读）
├── Skills~/              Agent 读的 skill（Unity 不导入 ~ 结尾的目录）
├── Editor/               安装器与状态窗口（AxuCliKit.Editor，仅编辑器）
├── Samples~/             可导入示例（CI workflow / AGENTS 片段 / skill 模板）
└── Tests/                EditMode 自检（技能契约、目标表、包清单）
```

## 两类 skill，各管一段

| 来源 | 谁维护 | 内容 | 怎么装 |
|---|---|---|---|
| `unity-cli`、`unity-pipeline` | Unity，随 CLI / Pipeline 包发布 | flag 级命令参考 | `unity skill install <client> --local` |
| `unity-cli-workflows` | 本包 | 路由规则、端到端配方、故障判读、项目内落地 | `AxuFrameKit ▸ CLI Agent Kit` |

两者互补，不重叠：本包**不重分发** Unity 的 skill 文本（Unity Companion License），
只在你需要时调用官方安装命令把它们落到工程里。见 [Agent 集成](02-agent-integration.md)。

## 环境要求

| 项 | 要求 |
|---|---|
| Unity | 2021.3+（Editor 工具本身）；驱动实时 Editor 需 Unity 6+ |
| `unity` CLI | 可选但强烈建议；文档对齐 `1.0.0-beta.10` |
| `com.unity.pipeline` | 可选；需要驱动实时 Editor 时装（`unity pipeline install`） |
| 平台 | macOS / Linux / Windows（安装器只用 `System.IO` 与 `Process`） |
