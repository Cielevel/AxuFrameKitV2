# Agent 集成

包提供的是一套"Agent 读得到"的规则。这篇讲清楚：写到哪、怎么写、怎么维护、团队怎么协作。

## 写入位置

在 `AxuFrameKit ▸ CLI Agent Kit` 窗口里按需勾选。**首次运行不写死默认值**：安装器探测工程里已存在的
客户端目录（`.claude/`、`.zcode/`、`.agents/`、`.cursor/`、`.windsurf/`、`.clinerules/`），
把探测到的作为建议目标；一个都没探测到才退回 `claude-code`。你手动勾选后以你的选择为准
（存在 EditorPrefs，按机器+工程），窗口里的 **Detect clients used in this project** 可重跑探测。

| 目标 | 位置 | 形式 |
|---|---|---|
| Claude Code | `.claude/skills/unity-cli-workflows/` | **完整副本**：`SKILL.md` + `references/` + `CHANGELOG.md` |
| ZCode | `.zcode/skills/unity-cli-workflows/` | **完整副本**（ZCode 扫描 `.zcode/skills` 优先于 `.agents/skills`） |
| 跨工具通用 | `.agents/skills/unity-cli-workflows/` | **完整副本**，被 Codex / Cursor / Copilot / Gemini CLI / Zed / OpenCode / Cline / Windsurf / Kilo / Amp 等读取 |
| Cursor 规则 | `.cursor/rules/unity-cli-workflows.mdc` | 规则文件（`description` / `alwaysApply` frontmatter）+ 摘要 + 指向正文的路径 |
| Windsurf 规则 | `.windsurf/rules/unity-cli-workflows.md` | 同上（`trigger: model_decision`） |
| Cline 规则 | `.clinerules/unity-cli-workflows.md` | 纯 markdown 规则 |
| VS Code / Copilot | `.github/instructions/unity-cli-workflows.instructions.md` | 指令文件（`applyTo: "**"`） |
| 任意 Agent | `AGENTS.md` 的托管区块 | `<!-- axu-cli-kit:begin -->` … `<!-- axu-cli-kit:end -->`，原地重写 |

### 目录式 skill vs 规则文件

**目录式 skill**（`SKILL.md` + 同目录的 `references/`）是可跨工具搬运的形态，因为它自带全部内容。
**规则文件**（`.cursor/rules/*.mdc`、`.clinerules/*.md`…）是各客户端的另一套机制，通常用于
"始终生效的短规则"；本包生成的规则文件是**摘要 + 指向正文的路径**，不复制全文。

两者可以并用：想只保留一份就选 `.agents/skills/`；想让 Cursor / Cline 在没加载 skill 时也能看到
硬规则，就再勾上它们各自的规则目标。

### 通用目录：`.agents/skills/`（复数）

如果你只想勾一个目标，就勾它。`npx skills` 的路径表把 `.agents/skills/` 列为
Codex、Cursor、GitHub Copilot、Gemini CLI、Zed、OpenCode、Cline、Windsurf、Kilo Code、
Amp、Antigravity、Droid 等一大批客户端的工程内 skill 目录。ZCode 也读它
（`<repo>/.agents/skills/`，优先级低于 `<repo>/.zcode/skills/`）。

> ⚠️ 是 **`.agents/`（复数）**。单数的 `.agent/` 不是任何工具的规范路径——
> 如果你在别处看到 `.agent/skills/`，那是笔误或某个私有约定。

## 状态文件

```jsonc
// <工程根>/.axu-cli-kit/state.json
{
  "packageVersion": "0.1.0",
  "updatedUtc": "2026-09-17T14:06:11Z",
  "entries": [
    { "target": "claude-code", "skill": "unity-cli-workflows",
      "path": ".claude/skills/unity-cli-workflows/SKILL.md", "hash": "4efe75baa0588faa" }
  ]
}
```

- 记录**本工具写过什么**，卸载只动这些文件。
- `hash` 是写入时的内容哈希，用来区分"上游更新了"和"有人手改了"。
- 空的 `.axu-cli-kit/` 会自动删除。
- 提交与否都行：提交 → 团队共享同一份安装记录；`.gitignore` 掉 → 每台机器自行维护。
  推荐**提交**，理由见下。

## 幂等、冲突与强制

| 情况 | 行为 |
|---|---|
| 文件不存在 | 创建 |
| 内容与包内一致 | 不动（Unchanged） |
| 内容与该 target 上次写入的哈希一致，但包内容变了 | 覆盖（Updated）——这是正常升级 |
| 内容既非包内内容，也不是我们写的 | **跳过并报告**（SkippedModified），不覆盖 |
| 勾了 Dry run | 只列计划，不落盘 |

`Force` 才会覆盖手改文件。`AGENTS.md` 不走哈希判定：只重写标记之间的区块，
区块外的一律不动。

## 团队协作建议

1. **把生成物提交进版本库**（`.claude/skills/…`、`.cursor/rules/…`、`AGENTS.md` 区块）。
   Agent 读到的规则因此和代码一起评审、一起回滚。
2. 拉下代码的人不需要做任何事——安装器是幂等的，同事再跑一次也不会产生 diff。
3. 改规则要改**包里的源文件**（`Skills~/`），然后在窗口里 Refresh；
   直接手改 `.claude/skills/…` 会在下一次 Refresh 时被报告为冲突。
4. 不想在每个工程各放一份，就用 `unity skill install <client>`（用户级全局安装）——
   代价是规则不再随工程版本走。

## 官方 skill 怎么进来

CLI 里内嵌了 Unity 官方的 `unity-cli` skill；工程装了 `com.unity.pipeline` 时还会带上
`unity-pipeline`。它们在二进制和 `Library/PackageCache` 里，**Agent 默认看不见**，需要显式落盘：

```bash
unity skill install claude-code --local --yes    # 客户端：claude-code / cursor / windsurf / vscode / cline / codex / grok
unity skill install --list                       # 各客户端支持的路径与当前状态
unity skill refresh                              # CLI 升级后，重渲染所有已安装副本
unity skill show --list                          # 二进制里这份 skill 包含哪些文件
```

窗口里的 **Unity's own skills → Install official skills** 按钮就是同一条命令，省得切终端。

公开合集（URP 迁移、Sprite Atlas、本地化、IAP 等 31 个 skill）：

```bash
npx skills add Unity-Technologies/skills
```

> 这些文本由 Unity 以 Unity Companion License 发布。**不要**把它们粘进你自己的包再分发——
> 让 CLI 去装，版本自然与二进制对齐。

## 客户端什么时候读取

绝大多数客户端在**会话启动时**扫描 skill 目录。安装完请新开一个会话；
Cursor / Copilot 的规则文件通常立即生效，但已缓存的上下文可能不会重读。

## 与其它工具的关系

| 工具 | 关系 |
|---|---|
| `unity mcp` | MCP 服务端配置，与 skill 互补：MCP 给的是工具调用，skill 给的是判断规则。两者可以同时用 |
| `npx skills add …` | 通用 skill 分发器；本包是 UPM 分发，不冲突，但注意别装同一 skill 的两份副本 |
| 本包 `.claude/skills/` 与官方 `unity-cli` skill | 命名空间不重叠（`unity-cli-workflows` vs `unity-cli`），可共存 |
