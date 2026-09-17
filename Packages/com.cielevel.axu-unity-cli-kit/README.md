# Axu CLI Agent Kit

**一个可被任意 Unity 工程直接拉取的 UPM 包，交付"用命令行驱动 Unity"这件事的完整文档 + 立即可用的 Agent Skill。**

包内没有运行时代码、没有第三方依赖：它带来的是 **知识**（CLI / Pipeline 的准确用法与配方）和
**安装器**（把这套知识变成你工程里 Claude Code / Cursor / Windsurf / Cline / Copilot / Codex
能自动读到的 skill 文件）。

- 目标 Unity：2021.3+（Editor 工具本身）；**实时驱动已打开 Editor** 的能力需要 Unity 6+ 与
  `com.unity.pipeline` 包（由 `unity pipeline install` 安装，本包不强制依赖）
- 依赖：无（`unity` CLI 是可选的外部可执行文件，缺失时安装器只会提示）

---

## 为什么需要这个包

`unity` CLI 与 `com.unity.pipeline` 把 Editor 变成了可编程对象：`unity status` 看有没有
Editor 在跑，`unity command` 列出它当前暴露的命令，`unity command eval` 直接执行 C#，
`unity test` / `unity build` 跑 CI。能力很强，但**默认只存在于 CLI 二进制和 `Library/PackageCache`
里**——Agent 看不到，团队里每个人的用法也不一致。

这个包做三件事：

1. **把知识落成项目文件**：`Skills~/` 下是可被 AI 客户端直接读取的 skill（`SKILL.md` +
   `references/`），跟着工程一起版本化，同事拉下代码就有一致的用法。
2. **一条命令装上**：Editor 菜单 / 窗口里的安装器把 skill 写进各客户端的项目级目录，
   幂等、可刷新、可卸载，且不会覆盖你手改过的文件。
3. **把官方 skill 接上**：一键调用 `unity skill install <client> --local`，把 Unity 官方
   维护的 `unity-cli` 与 `unity-pipeline` skill（含 beta.10 的完整命令参考）同步进本工程。

---

## 安装

### 作为 Git 依赖（推荐，其它工程用这个）

Package Manager → **Add package from git URL**：

```
https://github.com/<your-org>/<your-repo>.git?path=/Packages/com.cielevel.axu-unity-cli-kit
```

或直接写进 `Packages/manifest.json`：

```json
{
  "dependencies": {
    "com.cielevel.axu-unity-cli-kit": "https://github.com/<your-org>/<your-repo>.git?path=/Packages/com.cielevel.axu-unity-cli-kit#v0.1.0"
  }
}
```

`#v0.1.0` 是 git tag，**生产工程建议锁 tag**；想跟随最新就把 `#...` 去掉。

### 作为嵌入式包（本包当前的形态）

把整个 `Packages/com.cielevel.axu-unity-cli-kit/` 目录拷进目标工程的 `Packages/` 即可，
UPM 会自动识别，无需在 `manifest.json` 声明。

### 本地路径安装（改包时用）

```json
{
  "dependencies": {
    "com.cielevel.axu-unity-cli-kit": "file:../../axu-unity-cli-kit"
  }
}
```

---

## 更新

| 安装方式 | 更新方法 |
|---|---|
| Git 依赖（锁 tag） | 改 `manifest.json` 里的 tag，或在 Package Manager 里点 **Update**（需先移除 tag 锁定） |
| Git 依赖（不锁） | Package Manager → **Update**；CLI：`unity command package_resolve` 后重开工程 |
| 嵌入式 / 本地路径 | 直接覆盖目录；本包无 Runtime 代码，刷新即生效 |

包更新后，**已装到项目里的 skill 不会自动变**（它们是普通文件，属于你的工程）。打开
`AxuFrameKit ▸ CLI Agent Kit` 窗口点 **Refresh Installed Skills**，或再执行一次安装即可对齐；
窗口里的 *Drift* 一栏会明确告诉你"包内版本 vs 项目内版本"是否有差异。

---

## 快速开始（消费方工程）

1. 确认 CLI 可用：终端执行 `unity --version`（缺失时见 `Documentation~/01-install-and-update.md`）。
2. 打开 `AxuFrameKit ▸ CLI Agent Kit ▸ Open Kit Window`。
3. 首次导入包时 Editor 会弹一次询问框，选 **Install** 即可；也可以之后在窗口里手动装。
4. 装完你会得到（按勾选的目标）：

| 目标 | 写入位置 | 形式 |
|---|---|---|
| Claude Code | `.claude/skills/unity-cli-workflows/` | 完整 skill 副本（`SKILL.md` + `references/`） |
| ZCode | `.zcode/skills/unity-cli-workflows/` | 完整 skill 副本 |
| **跨工具通用** | `.agents/skills/unity-cli-workflows/` | 完整 skill 副本。Codex / Cursor / Copilot / Gemini CLI / Zed / OpenCode / Cline / Windsurf / Kilo 等一批客户端都读这里 |
| Cursor 规则 | `.cursor/rules/unity-cli-workflows.mdc` | 规则文件 + 指针（与上面的 skills 目录是两种机制，可并用） |
| Windsurf 规则 | `.windsurf/rules/unity-cli-workflows.md` | 规则文件 + 指针 |
| Cline 规则 | `.clinerules/unity-cli-workflows.md` | 规则文件 + 指针 |
| VS Code / Copilot 指令 | `.github/instructions/unity-cli-workflows.instructions.md` | 指令文件 + 指针 |
| 任意 Agent | `AGENTS.md` 里的托管区块 | 指针 + 硬规则，`<!-- axu-cli-kit:begin -->` 包裹，可安全重写 |

首次运行时安装器会**探测工程里已存在的客户端目录**（`.claude/`、`.zcode/`、`.agents/`、`.cursor/`、
`.windsurf/`、`.clinerules/`）作为建议目标；一个都没探测到时退回到 `claude-code`。
窗口里的 **Detect clients used in this project** 可随时重跑探测。

> 注意：跨工具通用目录是 **`.agents/skills/`（复数）**。单数的 `.agent/` 不是任何工具的规范路径。

安装状态记录在 `<工程根>/.axu-cli-kit/state.json`（可提交，也可加进 `.gitignore`）。
所有写入都带内容哈希：**你手改过的文件不会被覆盖**，安装器会报告冲突并以 `--force` 才覆盖。

> 团队协作建议：把 `.claude/skills/` 等生成物**提交进版本库**（安装器是幂等的，同事不会看到噪音），
> 这样每个人的 Agent 读到的规则完全一致。

---

## 包内有什么

```
com.cielevel.axu-unity-cli-kit/
├── package.json                  # UPM 清单（无运行时依赖）
├── README.md                     # 本文件：包级说明与快速开始
├── CHANGELOG.md                  # 版本变更
├── LICENSE.md                    # MIT + 第三方声明
├── Documentation~/               # 人读的包级文档（8 篇，见下表）
├── Skills~/                      # Agent 读的 skill（~ 结尾，Unity 不导入）
│   └── unity-cli-workflows/
│       ├── SKILL.md              # 触发条件 + 硬规则 + 路由表
│       ├── CHANGELOG.md
│       └── references/
│           ├── cli-essentials.md     # CLI 全局旗标/环境变量/退出码/命令组
│           ├── live-editor.md        # Pipeline：连接、命令发现、编译-测试闭环、热重载
│           ├── recipes.md            # 端到端配方（新机、场景编辑、CI、Player）
│           ├── troubleshooting.md    # 症状 → 根因 → 处置
│           └── agent-integration.md  # skill 装在哪、怎么更新、怎么加自己的
├── Editor/                       # 安装器与状态窗口（AxuCliKit.Editor，仅编辑器）
├── Samples~/                     # 可导入示例（CI workflow / AGENTS 片段 / skill 模板）
└── Tests/                        # EditMode 自检：目标表、frontmatter 转换、哈希与状态文件
```

### 文档索引（`Documentation~/`）

| 文件 | 内容 |
|---|---|
| `index.md` | 文档总览与阅读顺序 |
| `01-install-and-update.md` | 三种安装方式、版本锁定、更新与回滚、CLI 自身的安装 |
| `02-agent-integration.md` | 各 AI 客户端的目录约定、目标开关、状态文件、团队协作与排错 |
| `03-unity-cli-basics.md` | CLI 命令全景（对齐 `1.0.0-beta.10`）：编辑器/工程/构建/测试/认证 |
| `04-live-editor-pipeline.md` | `com.unity.pipeline`：端口、命令发现、编辑-重编译-测试闭环、Player |
| `05-recipes.md` | 可直接抄的配方：新机准备、活体编辑、CI 测试与构建、批量资产操作 |
| `06-troubleshooting.md` | 故障矩阵：Safe Mode、沙箱、多 Editor、退出码语义、日志 |
| `07-extending-the-kit.md` | 加自己的 skill、发布新版本、把包拆成独立仓库的清单 |

### Skill 索引（`Skills~/`）

| Skill | 触发场景 |
|---|---|
| `unity-cli-workflows` | 需要判断"有没有 Editor 在跑 / 能不能直接驱动它 / 该用 CLI 还是 Pipeline / 命令失败了怎么办" |

> 本包**不**重复发布 Unity 官方 skill。官方的 `unity-cli` / `unity-pipeline` 覆盖到 flag 级别的
> 细节，由 CLI 自己维护并与版本同步；本包负责把它们**接进来**（窗口里的一键按钮），
> 自己只写官方没覆盖的部分：项目内落地、端到端配方、故障判读与团队协作。

---

## 环境要求

| 项 | 说明 |
|---|---|
| `unity` CLI | 可选但强烈建议。macOS/Linux：`curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh \| UNITY_CLI_CHANNEL=beta bash`；Windows 见 `Documentation~/01`。`1.0.0-beta.10` 为本包文档对齐版本 |
| `com.unity.pipeline` | 可选。需要驱动实时 Editor 时装：`unity pipeline install`（Unity 6+） |
| Unity Editor | Editor 工具代码兼容 2021.3+；驱动实时 Editor 需 Unity 6+ |
| 操作系统 | 安装器只用 `System.IO` + `System.Diagnostics.Process`，三平台通用 |

---

## 发布清单（把本包发到自己的仓库时）

1. `package.json` 里的 `documentationUrl` / `changelogUrl` / `licensesUrl` 指向真实仓库地址。
2. 打 tag：`git tag v0.1.0 && git push --tags`（消费方用 `#v0.1.0` 锁定）。
3. 若有本地改动：同步更新 `CHANGELOG.md` 与 `Skills~/unity-cli-workflows/CHANGELOG.md`。
4. 包更新后回到本工程，用窗口 **Refresh Installed Skills** 对齐项目内副本。

## 许可

本包文本与代码：MIT（见 `LICENSE.md`）。
Unity CLI、`com.unity.pipeline`、[Unity-Technologies/skills](https://github.com/Unity-Technologies/skills)
（Unity Companion License）均为 Unity 产品，本包只做文档化与调用，不重分发其内容。
