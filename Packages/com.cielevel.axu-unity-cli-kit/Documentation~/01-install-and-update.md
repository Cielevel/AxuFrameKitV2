# 安装与更新

两件事分开看：**包**怎么进工程，**CLI** 怎么上机器。

## 一、把包装进工程

### Git 依赖（跨工程共享的首选）

Package Manager → **Add package from git URL**：

```
https://github.com/<org>/<repo>.git?path=/Packages/com.cielevel.axu-unity-cli-kit
```

或写进 `Packages/manifest.json`：

```json
{
  "dependencies": {
    "com.cielevel.axu-unity-cli-kit": "https://github.com/<org>/<repo>.git?path=/Packages/com.cielevel.axu-unity-cli-kit#v0.1.0"
  }
}
```

- `?path=` 指定仓库里的子目录（本包在仓库的 `Packages/` 下）。
- `#v0.1.0` 是 git tag。**生产工程锁 tag**：升级是显式动作，不会被上游改动突袭。
  不锁 tag 则跟随默认分支 HEAD，Package Manager 的 **Update** 会拉到最新。

### 嵌入式包

把 `com.cielevel.axu-unity-cli-kit/` 整个目录拷进目标工程的 `Packages/`。UPM 自动识别，
**不要**在 `manifest.json` 里声明。适合"改动包本身"或"完全离线"的场景。

### 本地路径依赖

```json
{ "com.cielevel.axu-unity-cli-kit": "file:../../axu-unity-cli-kit" }
```

改包后无需重新拉取；适合在多个工程间同时迭代这个包。

## 二、更新

| 安装形态 | 动作 |
|---|---|
| Git 依赖（锁 tag） | 改 tag → 保存 `manifest.json` → Unity 自动重解析 |
| Git 依赖（不锁） | Package Manager → **Update**，或 `unity command package_resolve` |
| 嵌入式 / 本地路径 | 覆盖目录 → Unity 自动重导入 |

**包更新不等于工程内 skill 更新**：写进 `.claude/skills/…`、`AGENTS.md` 的是普通文件，属于你的工程。
对齐方式：

```
AxuFrameKit ▸ CLI Agent Kit ▸ Refresh Installed Skills
```

安装器按内容哈希比对，只改变化的部分；窗口里的 *Last result* 会逐条列出 Created / Updated /
Unchanged / Skipped。手改过的文件会被跳过并报告，不会被静默覆盖。

## 三、装 unity CLI

```bash
which unity && unity --version        # 先看有没有
```

**macOS / Linux**

```bash
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash
```

**Windows（PowerShell）**

```powershell
$env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex
```

装完**新开一个终端**让 `unity` 进入 PATH，然后：

```bash
unity --version           # 应有 1.0.0-beta.N
unity self-update         # 之后升级 CLI 用这个
unity changelog           # 看这版改了什么
```

> GUI 启动的 Unity Editor 不会继承登录 shell 的 PATH。本包的窗口会自己去找
> `~/.unity/bin`、`/usr/local/bin`、`/opt/homebrew/bin`，找不到时再退回 shell 查找，
> 因此"编辑器里显示没装 CLI"通常只是 PATH 问题。

## 四、需要驱动"已打开的 Editor"时（可选）

```bash
unity pipeline install        # 给当前工程装 com.unity.pipeline（Unity 6+）
unity pipeline list           # 确认实例与版本
unity command editor_status   # 确认 Editor 服务有应答
```

`unity pipeline` 的版本要与 CLI 匹配：CLI 明显较新时，旧 Pipeline 会回
`too old to parse command lines`，此时 `unity pipeline upgrade` 即可。

## 五、安装后自检

```bash
unity --version                        # CLI 可用
unity status --format json             # 有 Editor 在线吗（state: ready）
unity command                          # 这个 Editor 暴露了哪些命令
unity skill install --list             # 官方 skill 在各客户端的安装状态
```

Editor 里：`AxuFrameKit ▸ CLI Agent Kit ▸ Check Unity CLI` 会打印 CLI 路径与版本；
窗口的 *Editor instances* 折叠区可以直接跑 `unity status`。

## 六、卸载

- 包：从 `manifest.json` 移除依赖，或删掉 `Packages/` 下的目录。
- 工程内 skill：`AxuFrameKit ▸ CLI Agent Kit ▸ Uninstall Skills from This Project`。
  只删除 `.axu-cli-kit/state.json` 里记录过的文件；手改过的文件保留。
  `AGENTS.md` 只移除托管区块，其余内容原样保留。
- 状态目录 `.axu-cli-kit/`：条目清空后自动删除。
