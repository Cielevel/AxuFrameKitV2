# 扩展与发布

## 加一个自己的 skill

1. 在 `Skills~/` 下建目录，**目录名 = frontmatter 的 `name`**，kebab-case：

   ```
   Skills~/my-project-skill/
   ├── SKILL.md            # 必须有，且在顶层
   ├── CHANGELOG.md        # 可选
   └── references/…        # 可选，正文里的深度
   ```

2. `SKILL.md` 的 frontmatter 三件事：

   ```yaml
   ---
   name: my-project-skill
   description: Use when <触发条件，用使用者会说的词>…   # 这句话决定 skill 会不会被加载
   allowed-tools:            # 可选，不要就整段删掉
     - Bash
   ---
   ```

3. 写正文：判断规则 + 路由表；细节放 `references/` 并用相对路径链接。
4. 打开 `AxuFrameKit ▸ CLI Agent Kit`，勾目标，**Install / Refresh**。

安装器自动发现 `Skills~/` 下所有含 `SKILL.md` 的目录，因此加技能不需要改任何 C# 代码。

契约检查在 EditMode 测试里（`Tests/Editor/`）：名称与目录一致、描述非空、每个 reference 都被
`SKILL.md` 链接、目标表 id 唯一且路径含 `{skill}` 占位符。跑测试：

```bash
# 在有测试框架的工程里（Test Runner 或 unity test）
unity test . --mode EditMode --filter AxuCliKitSkillContractTests
```

## 用 shell 之外的形态分发

| 形态 | 适合 | 代价 |
|---|---|---|
| UPM 包（本形态） | 团队/多工程复用，随工程版本化 | 需要能访问 git 仓库 |
| `unity skill install <client>` | 个人、跨工程统一 | 规则不随工程版本走 |
| `npx skills add <repo>` | 公开合集分发 | 需要 Node 工具链 |

三者可以共存，但**别让同一个 skill 出现两份不同版本**——Agent 可能读到旧的那份。

## 发布到自己的仓库（清单）

1. 改 `package.json` 的 `documentationUrl` / `changelogUrl` / `licensesUrl` 为真实地址。
2. 更新 `CHANGELOG.md` 与 `Skills~/unity-cli-workflows/CHANGELOG.md`
   （后者注明"对齐到哪个 CLI 版本"，这是使用者判断可否升级的关键信息）。
3. 打 tag 并推送：

   ```bash
   git tag v0.1.0 && git push --tags
   ```

4. 消费方用 `…?path=/Packages/com.cielevel.axu-unity-cli-kit#v0.1.0` 锁定。
5. 若包仍在本工程内嵌，回到工程用窗口 **Refresh Installed Skills** 对齐本地副本。

### 建议的版本号语义

| 变更 | 版本 |
|---|---|
| 文案修正、链接修复 | PATCH |
| 新增 skill、新增 target、新增命令 | MINOR |
| 目标路径变更、状态文件格式变更（会让人需要重新安装/重新提交） | MAJOR |

## 把包拆成独立仓库

当前形态是"嵌在大工程的 `Packages/` 下，用 `?path=` 被其它工程拉取"。想独立成仓库：

```bash
# 只把包目录拎出来，保留历史
git subtree split --prefix=Packages/com.cielevel.axu-unity-cli-kit -b cli-kit-split
git push <new-remote> cli-kit-split:main
```

之后 `package.json` 里的 URL 指向新仓库，消费方的 `?path=` 也要去掉。

## 更新文档时注意

- **事实以二进制为准**：CLI 处于 beta，flag 与命令会动。写文档时用
  `unity <组> --help`、`unity commands --format json`、`unity skill show --list` 核对，
  并在文首注明对齐的版本号。
- **不要复制 Unity 的 skill 文本**（Unity Companion License）。要么引用，要么用
  `unity skill install` 让使用者的机器自己拉。这也是本包选择"调用官方安装命令"而不是
  "内嵌官方内容"的原因。
- 包内 `.md` 属于 Unity 资源；`Skills~/` 与 `Documentation~/` 以 `~` 结尾，
  Unity 不导入（不生成 `.meta`，也不会进 AssetDatabase）。

## Editor 工具的结构

需要改安装器时看这几处：

| 文件 | 职责 |
|---|---|
| `Editor/AxuCliKitPaths.cs` | 包根/工程根解析、**目标表**（加客户端改这里）、技能发现 |
| `Editor/AxuCliKitInstaller.cs` | 安装/刷新/卸载、内容哈希、状态文件、托管区块 |
| `Editor/AxuCliKitSkillMeta.cs` | `SKILL.md` frontmatter 解析、Guardrails 提取 |
| `Editor/AxuCliKitCli.cs` | 定位并调用 `unity` 可执行文件 |
| `Editor/AxuCliKitWindow.cs` | 状态窗口（IMGUI） |
| `Editor/AxuCliKitMenu.cs` | 菜单项 + 首次导入询问（等编辑器在前台才弹） |

加一个新客户端目标：在 `AxuCliKitPaths.Targets` 加一条（`SkillDirectory` 写完整副本；
`PointerFile` 生成规则文件 + 指针），并在 `BuildPointerContent` 里按需补该客户端的
frontmatter 键。其余流程自动适配——包括状态记录与卸载。
