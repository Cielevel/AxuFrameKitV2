# 端到端配方

每条都是可直接执行的最小序列。`~/Proj` 代表工程的绝对路径。

## 1. 换新机器：从零到能跑测试

```bash
# 1) CLI
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash
# 新开终端
unity --version

# 2) 账号与许可
unity auth status --format json     # 未登录 → unity auth login
unity license status --format json  # 无席位 → unity license activate

# 3) 编辑器（先定目标平台，因为它决定装哪些 module）
unity releases --stream lts --limit 5 --format json
unity install lts --module android --module ios --yes --accept-eula
unity editors --installed --format json

# 4) 工程
git clone <repo> ~/Proj
unity open ~/Proj                   # 版本不符会提示；unity projects info 可看所需版本

# 5) 打通"驱动 Editor"
unity pipeline install --project-path ~/Proj
unity status --format json          # state: ready
unity command editor_status
```

## 2. 用正确的编辑器版本打开工程

```bash
unity projects info ~/Proj --format json     # 读 editorVersion
unity editors --installed --format json
unity open ~/Proj                            # 缺版本时会警告
unity 6000.0.23f1 ~/Proj                     # 简写：等价 open --editor-version
unity open ~/Proj --wait                     # 阻塞到 Editor 退出（macOS/Linux）
```

## 3. 改场景/对象并当场验证

```bash
unity status --format json                   # 确认有 Editor 且拿到 project 路径
unity command list_open_scenes               # 确认活动场景
unity command create_gameobject --name Prop
unity command attach_script --target Prop --script Assets/Scripts/Prop.cs
unity command set_component_properties --target Prop --component Prop --properties '{"speed":3}'
unity command save_scene
unity command screenshot --path Temp/verify.png
```

多个 Editor 同时在线时，每条都加 `--project-path /abs/path`。

## 4. 批量资产处理（不点鼠标）

```bash
unity command create_folder --path Assets/Art/Props
unity command find_assets --filter "t:Texture2D" --folder Assets/Art/Props
unity command set_import_settings --path Assets/Art/Props/crate.png \
  --settings '{"textureType":"Sprite","spritePixelsPerUnit":100,"maxTextureSize":1024}'
unity command import_asset --path Assets/Art/Props/crate.png
unity command save_all
```

重复动作写成一个 `.cs` 用 `eval_file` 跑，比 N 次 shell 往返快得多。

## 5. 给工程加包

```bash
unity command package_search --query "cinemachine"
unity command package_add --identifier com.unity.cinemachine --confirm true
unity command package_resolve
unity command package_list --format json
```

需要精确控制版本 / 作用域注册表 / tarball 时改用 C# `UnityEditor.PackageManager.Client`。

## 6. CI：跑测试并出报告

```bash
unity test ~/Proj \
  --mode EditMode \
  --report-format junit --output ./test-results.xml \
  --allow-install --timeout 900
case $? in
  0) echo "passed" ;;
  8) echo "tests failed — 报告给开发者，不要重试" ;;
  *) echo "本次运行没有产出结论（基础设施问题）—— 可以重试" ;;
esac
```

- `--shard n/m` + `--shard-inventory <全量报告>`：把套件切给多个并行 CI job。
- `--retries 2`：失败重跑，重跑通过的报告为 **flaky** 而不是红。
- `--rerun-failed`：只重跑上次失败用例，内层循环提速。
- `--affected [--since <ref>]`：只跑改动能触达的用例；先 `--affected-compare` 看它会跳过什么。
- `--coverage`：需要 Code Coverage 包，缺失时只警告不失败。

完整 GitHub Actions 示例：`Samples~/GitHubActionsCI/`。

## 7. CI：无头构建

```bash
unity build ~/Proj \
  --target StandaloneLinux64 \
  --execute-method Builder.PerformBuild \
  --output-path ./Build/Game \
  --allow-install
```

- 不带 `--execute-method` 时走 Unity 内置构建，此时 `-o/--output-path` 必填。
- 平台不在 `--target` 列表里（Meta Quest 等）：`--list-profiles` / `--create-profile <target>` / `--profile`。
- 日志默认 `<project>/Logs/build-<target>-<时间戳>.log` 并同时输出到终端（`--no-tail` 关闭）。
- 退出码 143 = 被 runner 超时杀掉（已完成清理）。

构建产物验证：

```bash
unity build run --path ~/Proj/Builds/StandaloneLinux64
```

## 8. 开发版 Player + 热重载

```bash
unity command switch_build_target --target StandaloneOSX
unity command build --output Builds/Dev/MyGame.app

# 启动 Player 后
unity status --format json
unity command --runtime MyGame status
unity command --runtime MyGame log --level error

# 改一个方法体，不重启游戏
unity command --runtime MyGame reload_file --filename Assets/Spinner.cs
```

## 9. 诊断一台机器 / 一次故障

```bash
unity doctor --format json           # 认证 + 已装编辑器 + 最近错误
unity env --format json              # 路径、版本、解析到的代理
unity status --format json           # 在线实例
unity pipeline list                  # Pipeline 状态 / Safe Mode / 版本
unity logs --follow --level info     # 安装过程中追 Hub 日志
unity diagnose proxy                 # 代理专项诊断
```

## 10. Skill 的安装与同步

```bash
# 官方（Unity 维护）
unity skill install claude-code --local --yes
unity skill refresh

# 本包的（项目内）
#   Editor 菜单：AxuFrameKit ▸ CLI Agent Kit ▸ Install Skills into This Project
#   刷新：AxuFrameKit ▸ CLI Agent Kit ▸ Refresh Installed Skills
```

包升级后记得 Refresh 一次，让工程内副本与包内正文对齐。
