# 故障排查

先收集证据，再对症：`unity doctor --format json`、`unity env --format json`、
`unity status --format json`、`unity pipeline list`、`<命令> --verbose`。

## 包与安装

| 症状 | 原因 | 处置 |
|---|---|---|
| Unity 里看不到 `AxuFrameKit ▸ CLI Agent Kit` | 包未被识别（没重解析），或 asmdef 没编译 | 触发一次重解析：`unity command package_resolve`，或关掉再打开工程 |
| 菜单在但点了没反应 | 前一次编译失败导致编辑器进了 Safe Mode | 看 Console 的编译错误；修好后重开工程 |
| `AxuFrameKit ▸ CLI Agent Kit ▸ Check Unity CLI` 报找不到 | GUI 启动的 Editor 不继承 shell PATH | 确认 `~/.unity/bin/unity` 存在；本包会自动查常见安装路径，装好 CLI 后点窗口里的 Check |
| 拉 Git 依赖报路径错误 | `?path=` 写错，或缺 `#tag` | 用 `https://…git?path=/Packages/com.cielevel.axu-unity-cli-kit`；`packages-lock.json` 里应出现 `"source": "embedded"` 或 git 源 |

## Skill 没生效

| 症状 | 原因 | 处置 |
|---|---|---|
| Agent 完全不知道 Unity CLI | skill 还在 `Library/PackageCache` 或二进制里，客户端看不见 | `unity skill install <client> --local`（官方）+ 本包窗口 Install（项目） |
| 客户端是 Codex / Copilot / Gemini CLI / Zed / OpenCode 等 | 它们读的是 `.agents/skills/`，不是 `.claude/skills/` | 窗口里勾 **Shared skills dir — .agents/skills/**（或是点 Detect clients）；见 [Agent 集成](02-agent-integration.md) |
| 我的客户端没在目标列表里 | 该客户端用的是**规则文件**而非目录式 skill（Windsurf / Cline / Cursor 的 rules 机制） | 勾对应的规则目标；或看 `npx skills` 的路径表确认它是否读 `.agents/skills/` |
| 装完还是不生效 | 多数客户端在**会话启动时**扫描 skill 目录 | 新开一个会话 |
| 文件写到了不对的客户端目录 | target 勾选与客户端不匹配 | 窗口里对照 *Install targets* 的路径；见 [Agent 集成](02-agent-integration.md) |
| 刷新时提示 `Skipped (hand-edited)` | 该文件被手改过 | 要么改回包内正文（改 `Skills~/` 源文件后 Refresh），要么勾 `Force` 明确接管 |
| 包升级后规则没变 | 工程内副本是普通文件，不会自动更新 | `Refresh Installed Skills` |
| `AGENTS.md` 被改 | 只应重写标记之间的区块 | 检查 `<!-- axu-cli-kit:begin/end -->` 是否成对；区块外内容不会被删 |
| 同一 skill 有两份且不一致 | 同时用 `npx skills add` 与 UPM 分发装了同名 skill | 只保留一份；`.axu-cli-kit/state.json` 列出本包装了哪些 |

## 连不上正在运行的 Editor

| 症状 | 原因 | 处置 |
|---|---|---|
| `unity status` 空，但编辑器明明开着 | **Safe Mode**（工程有编译错误，Pipeline 不加载） | `unity pipeline list` 确认 → 修编译错误 → 重启编辑器 |
| 同上，且无编译错误 | **沙箱**把真实进程对本 CLI 隐藏了 | 不要据此断定"没有 Editor"；在沙箱外重试，或向使用者确认 |
| 同上，两者都排除 | 没装 Pipeline / 目标工程不对 | `unity pipeline install`；所有命令加 `--project-path <绝对路径>` |
| `AMBIGUOUS_EDITOR` | 多个 Editor 在线且无法判定 | 传 `--project-path` 或 `--instance host:port` |
| 命令超时 | Editor 未聚焦被节流；或已有长任务占着闸门 | `unity command set_autotick --enable true`；查 `unity command editor_status`；确认没有 PlayMode/soak 在跑 |
| 命令执行中连接中断 | 域重载正在进行 | 预期行为，重试直到状态命令给出终态 |
| `too old to parse command lines` | Pipeline 包版本比 CLI 旧（命令**带参数**时必现，只读命令仍可用） | `unity pipeline upgrade`；本工程实测：CLI `1.0.0-beta.10` 需 pipeline `≥0.6.0-exp.1`，已于 2026-09-17 升到 `0.7.0-exp.1` |
| 绑定端口失败 | 端口段被占（Editor 7800–7849 / 测试 7850–7899 / Runtime 7900–7999） | 关掉残留的 Editor/测试进程；确认没有别的程序占用该段 |

## 编译、测试、构建

| 症状 | 处置 |
|---|---|
| `recompile_status` 永远停在 `triggered` | Editor 没在 tick：先 `set_autotick --enable true`，再重新触发 |
| `recompile_status` 报 `failed: true` | 读返回里的 `errors[]`，修完再编译 |
| `run_tests` 返回内容含糊，看不到失败详情（已知缺陷） | 用更窄的 `--filter` 重跑，或看 `unity command console --level error --tail 50` / Test Runner |
| CI 退出 6 | 本次运行**没有**产出结论（编译失败/无许可/超时/崩溃），可重试 |
| CI 退出 8 | 测试确实失败了，**别重试**，交给人 |
| 本地过、CI 不过 | 对比 Editor 版本、`--timeout`、目标平台 module 是否齐 |
| 构建成功但产物不见 | 用了 `--execute-method` 时，输出路径由你的方法负责（会收到 `-buildOutput`） |

## 认证、许可、网络

| 症状 | 处置 |
|---|---|
| 退出码 3 | `unity auth login`；CI 用 `UNITY_SERVICE_ACCOUNT_ID` + `UNITY_SERVICE_ACCOUNT_SECRET` |
| 退出码 4 | 无可用许可：`unity license status` / `unity license activate` |
| 下载/安装失败（代理） | `unity env --format json` 看解析结果，`unity diagnose proxy`，必要时 `--proxy` / `UNITY_PROXY` |
| module 下载偶发失败 | `UNITY_INSTALL_RETRIES=<n>`；`--resume` 可续传被中断的下载 |
| 不想让 token 进 shell 历史 | 用 stdin 旗标：`--git-token-stdin`、`--secret-from-stdin`，来源用密钥管理器 |

## CLI 行为上的几个"坑"

- **有些命令 stdout 为空但退出码非 0**：它们仍把 `{"error": …}` 写到 stderr。加 `--verbose` 再看；
  这是该命令的已知形态，不是你拼错了。
- **分页只影响人类输出**：`unity projects list` 是**进程内**分页且只在 TTY 上启用，
  机器格式（json/ndjson）与非 TTY 自动关闭；脚本里也可直接 `--no-pager`。
- **命令集因工程而异**：`unity command`（不带名称）才是当前实例的权威列表。
- **对象名会被清洗**：表格/列表输出会剥离控制字符（防终端注入），别依赖原始字节。
- **CLI 处于 beta**：`unity self-update` 后重新跑一次 `unity skill refresh`，
  让官方 skill 与新二进制对齐。

## 出问题时该抄给谁

自动化流程排查时，把这四条一起贴出来基本能定位：
`unity --version`、`unity doctor --format json`、`unity pipeline list`、`unity status --format json`。
