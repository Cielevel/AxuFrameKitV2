# Unity CLI 速览

对齐 `unity` CLI `1.0.0-beta.10`。命令会随版本变化——**`unity <组> --help` 与
`unity commands --format json` 才是权威**，本文是可搜索的地图。

## 心智模型

```
unity <组> <动作> [参数]          一个组一个领域：editors / projects / build / test / …
unity command <名称> [参数]       驱动【正在运行的 Editor 或 Player】（需要 com.unity.pipeline）
```

前者是"机器与工程的运维"，后者是"活着的 Unity 进程"。两套词汇不要混。

## 全局旗标（对所有命令有效）

| 旗标 | 用途 |
|---|---|
| `--format human\|json\|tsv\|ndjson\|github`（`--json`） | 机器可读输出；脚本里一律显式指定 |
| `--no-banner` / `--quiet` / `--no-pager` | 脚本环境去噪 |
| `--non-interactive`（配 `--yes`） | CI 里禁止任何交互 |
| `--verbose` | 失败时打印堆栈与原因链 |
| `--proxy <url>` / `--proxy-disable` / `--log-proxy` | 代理控制与排障 |
| `--color auto\|always\|never` | 确定性 ANSI 输出 |

`--accelerator` / `--no-accelerator` 不是全局旗标，只在 `build` / `test` / `run` 上，
且要写在命令名之后。

## 退出码：CI 的分支依据

| 码 | 含义 | 能重试吗 |
|---|---|---|
| 0 | 成功 | — |
| 1 | 一般错误 | 视情况 |
| 2 | 参数错误（含非法 `--proxy`） | 改调用 |
| 3 | 认证失败 | 重新登录 |
| 4 | 前置条件不满足（无许可证等） | 先修前置条件 |
| 6 | 命令自身失败；**测试没跑出结论**也归这里 | 可以（基础设施） |
| 8 | **仅 `unity test`：跑完了且有失败用例** | **不可以**，交给人 |
| 130 / 143 | Ctrl+C / 被 SIGTERM（CI 超时） | — |

6 与 8 分离是刻意设计：让 CI 只重试基础设施抖动，永不重试真实失败。

## 机器输出

`--format json` 是一个信封；`--format ndjson` 每行一个对象，最后是 `{"type":"result",…}`。

```jsonc
{ "success": false, "data": null, "errors": [ { "code": "TESTS_FAILED", "message": "…" } ] }
```

- **按 `success` 分支**，不要按 `data` 是否为空（少数命令失败时 `data` 也带内容，
  例如 `unity auth switch` 的歧义候选）。
- **不要解析 stderr**：机器格式下 stderr 只有人类诊断文本。
- `errors[0].code` 是稳定 token（`TESTS_FAILED` / `TEST_RUN_ERROR` / `TEST_TIMED_OUT` …）。

## 常用命令一览

```bash
# 编辑器与安装
unity editors --installed --format json
unity releases --stream lts --limit 5 --format json
unity install lts --module android --module ios --yes --accept-eula
unity modules / unity install-modules / unity install-path

# 工程与模板
unity projects list | create | open | info | size | clean | exec
unity templates list --editor lts --format json
unity open <path> [--wait]                 # --wait 阻塞到 Editor 退出（macOS/Linux）

# 构建 / 测试 / 运行
unity build <path> --target <t> [--execute-method <M>] [-o <out>]
unity build --list-targets | --list-profiles | --create-profile <t> | run [--path <last-build>]
unity test  <path> --mode EditMode|PlayMode --report-format junit --output <file>
unity run   <path> [--command <已注册命令>] [--timeout <s>]
unity watch test                           # 文件变化即重跑 --affected

# 资产包（离线）
unity assets inspect <file.unitypackage>   # 不导入就列出内容：路径 / GUID / 体积 / 预览标记

# 认证 / 许可 / 云
unity auth status | login | switch | list
unity license status | activate | return | server
unity cloud org | project

# 配置
unity config list | get | set | unset | resolve <key> [project]
unity context save|use|list|current|delete
unity config proxy | update-check | accelerator

# 版本控制
unity vcs setup|status|sync|switch|doctor|conflicts|resolve|diff|blame|affected|hooks
unity vcs uvcs locks|changesets|review        # 与工程数据联结的读操作
unity uvcs <args>                             # 原样转发给 cm

# 诊断
unity doctor -json / env -json / status -json / logs --follow / diagnose proxy|accelerator
unity cache / version / changelog / bug / completion <shell> / shell

# 给 Agent 用的
unity status / list / command / commands / pipeline / mcp / skill
```

## 构建默认值的分层

```
命令行旗标  >  ProjectSettings/UnityCliConfig.json（随工程提交）  >  unity config set（本机）
```

`unity config resolve build.target <project>` 会**打印最终值以及是哪一层给的** ——
当某个值出乎意料时，先跑它。

## CI 的最小可用形态

```bash
unity auth login --client-id "$UNITY_SERVICE_ACCOUNT_ID" --secret-from-stdin <<<"$UNITY_SERVICE_ACCOUNT_SECRET"
unity license activate
unity test  . --mode EditMode --report-format junit --output ./test-results.xml --allow-install --timeout 900
unity build . --target StandaloneLinux64 --execute-method Builder.PerformBuild --allow-install
unity license return --yes
```

服务账号凭据走环境变量而非命令行，避免进入进程参数表与 shell 历史。
可直接抄的完整 workflow 见 `Samples~/GitHubActionsCI/`。

## 不该用 CLI 做的事

- **UPM 包管理**：`unity command package_add` 能应付简单场景，但版本、作用域注册表、
  tarball 等要走 C# `UnityEditor.PackageManager.Client`（Unity 官方
  `unity-package-management` skill 专讲这件事）。
- **编辑场景/Prefab/资产的文本文件**：只要 Editor 在线就用 `unity command`（见
  [实时 Editor](04-live-editor-pipeline.md)）。

更细的旗标表、环境变量表、各命令的完整选项见 skill 内的
`Skills~/unity-cli-workflows/references/cli-essentials.md`。
