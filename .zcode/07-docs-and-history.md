# 07 · 文档现状与项目演进史

> 勘察时间：2026-08-29。来源：git log（HEAD 共 162 commits，仅 main 分支）、全项目 .md 清单。

## 1. 演进时间线（2026-02 ~ 2026-08 关键节点）

| 时间 | 提交 | 内容 |
|---|---|---|
| 02-28 ~ 03-08 | 3c54851…8b02226 | Save 工具完善、UI 框架修复、移除 Config 模块、新增生产级 TCP Socket 客户端、_Core 加 summary |
| 03-10 | 23f9d66 | Crypto 强化（RSA padding、新增 XOR）、新增 `Common_To_PureCS_Analysis.md`（纯 C# 剥离可行性分析） |
| 03-17 ~ 04-23 | d0cecb3…3c7f34a | Sprite to Animation、EventSystem static→Instance 重构、Timer/TaskQ/Multiton 目录升级、新增 Computed |
| 05-06 ~ 05-18 | 4082bd0…d78337f | CSV 工具窗口系列优化、indie modules selection to csv |
| 06-10 ~ 06-12 | 3bbe47b…c5eadc8 | 修复 IUseMonoBehaviour、新增 MultitonManager、Typewriter；**c5eadc8 首建包 `Packages/com.cielevel.axu-frame-kit`（空壳，0.1.0）** |
| 06-15 | 7bd8930 "add trans doc" | 新增迁移评估文档 `Assets/_CommonDoc/AxuFrameKit_Core_Common_移植评估.md`（299 行：9 项操作清单、依赖分析、工时估算 3-4 小时） |
| 06-16 | 5f4c8be | "fix namespace and update trans doc"——修正 Encrypt 等命名空间 + 文档二次复核 |
| **06-16** | **5277cdd "trans core and common to package"** | **核心迁移节点**：_Core（80 文件）→ 包内 `Runtime/_Core`（新建 AxuFrameKit.Core.asmdef）；Common（147 Runtime + 30 Editor）→ `Runtime/Common`（AxuFrameKit.Runtime.asmdef）与 `Editor/Common`（AxuFrameKit.Editor.asmdef）；package.json 补依赖 |
| 08-02 | 3a65756 "save only" | 存档相关改动 |
| 08-26 | **3231a6e "add converter -- terrain to mesh"** | 最新功能：包内 `Editor/Common/Utility/Converter/TerrainToMeshConverter.cs`（466 行） |
| 08-29 | c53ff4c / c00267f / fa5b847 / 15b9e8a | 文档迁入包、跨平台 gitignore/eol=lf、UI-Toolkit 警告修复；同日**模块层拆包**：Assets/AxuFrameKit/Modules 与 AxuIndieModules 拆为 8 个独立嵌入式包（`axu-module-*` ×4 + `axu-indie-*` ×4），框架包内重复窗口移除，Assets 中不再有框架/模块代码 |

另有陈旧未合并远端分支 `origin/expiremental-node-system-...`。包版本号自建立以来保持 0.1.0，无 CHANGELOG、无发布节奏记录。

## 2. 全项目文档清单（.md）

### 框架自带文档（2026-08-29 起已全部迁入包内、随代码放置；下表为迁移前位置，仅供历史对照）

> 迁移后新索引见包根 `Packages/com.cielevel.axu-frame-kit/README.md` 的文档索引表。

| 路径（Assets/ 下） | 主题 | 状态 |
|---|---|---|
| `AxuFrameKit/KitDoc/Common/Utility/Doc_MathUtility.md` | Math 插值工具 | 有效（对应包内代码） |
| `AxuFrameKit/KitDoc/Common/Utility/Doc_SaveUtility.md` | Save 存档系统 | 有效 |
| `AxuFrameKit/KitDoc/Common/Utility/Doc_TableCsvUtility.md` | CSV 表格工具 | 有效 |
| `AxuFrameKit/KitDoc/_Core/Utility/Encrypt/`（EncryptUtility.md / APIReference.md / QuickReference.md） | 加密模块（3 份） | 较新、有效 |
| `AxuFrameKit/Common/UI/Framework/UI框架.md` | UI 框架设计 | 有效 |
| `AxuFrameKit/Common/UI/Kit/DataBind/DOC/HOWTOUSE.md` | DataBind 用法 | 有效 |
| `AxuFrameKit/Common/UI/Kit/Group/DOC/HOWTOUSE.md` | UI 图组 | 有效 |
| `AxuFrameKit/Common/UI/Kit/ListView/DOC/`（README/INDEX/QUICKSTART/ARCHITECTURE/IMPROVEMENTS） | ListView 全套（最完整） | 有效 |
| `AxuFrameKit/Common/UI/Kit/Toast/DOC/HOWTOUSE.md`、`ToggleBtnGroup/DOC/HOWTOUSE.md` | Toast / 按钮组 | 有效 |
| `AxuFrameKit/Common/Utility/Auto/AutoSnapshot/README.md` | 快照录制回放 | 有效 |
| `AxuFrameKit/Common/Utility/Net/Advanced/DOC/`（HttpClient_README、PROJECT_OVERVIEW）、`Net/Socket/README.md` | 网络模块 | 有效 |
| `AxuFrameKit/Common/Utility/Save/`（ReadMe.md、Usage.md） | 存档系统 | 有效 |
| `AxuFrameKit/_Core/Advanced/BetterFSM/ReadMe.md`、`_Core/DesignPattern/README.md`、`_Core/Extension/README.md` | FSM / 设计模式 / 扩展 | 有效 |
| `DevDoc/AxuFrameKit/Common/Utility/Net/Socket/TechnicalDesign.md` | Socket 技术架构（03-08） | 有效 |
| `DevDoc/General/Common_To_PureCS_Analysis.md` | Common 纯 C# 剥离可行性分析（03-10） | 历史决策文档 |
| `_CommonDoc/AxuFrameKit_Core_Common_移植评估.md` | **包迁移评估（trans doc）**：9 项操作清单 + 二次复核记录（06-16） | 迁移已完成，但文档未标记"已执行完毕" |

### 项目级文档

| 路径 | 主题 | 状态 |
|---|---|---|
| `README.md`（根） | 框架简介 + 分层架构 | **已同步**（2026-08-29 重写，含可选模块包清单） |
| `AGENTS.md`（根） | AI 项目指南 | **已同步**（2026-08-29 重写，含 asmdef/依赖表/Tests） |
| `docs/`（根） | docfx 文档站 | **已删除**（2026-08-29 清理：87MB 的 2026-03 迁移前快照，gitignored，index 为 stub；如需 API 站可重建 docfx 配置） |
| `Assets/AxuExperimental/` 9 份 ReadMe | 实验模块说明 | 有效 |
| `Assets/AxuIndieModules/ReadMe.md` | 独立模块说明 | 有效 |
| `Packages/com.cielevel.axu-frame-kit/` 内 | **无任何 .md / README / CHANGELOG** | 文档缺口 |

## 3. 根 AGENTS.md 过时条目（历史记录：所列 7 条已于 2026-08-29 重写 AGENTS.md/README.md 时全部解决，仅存档备查）

1. **目录结构整体过时（核心问题）**：AGENTS.md 将三层代码定位在 `Assets/AxuFrameKit/{_Core,Common,Modules}` 并给出完整目录树。实际 `_Core`（80 文件）与 `Common`（177 文件）已于 06-16 迁入包；`Assets/AxuFrameKit/_Core`、`Common` 现存 **0 个 .cs**。仅 `Modules`（37 文件）仍在 Assets。
2. **完全未提及 UPM 包**：`com.cielevel.axu-frame-kit` 及 3 个 asmdef 零记载，旧的"无程序集定义"描述失效。
3. **"其他目录说明"失效**：列出的 `Doc/`、`PreTestFunc/`、`Scripts/Generated/` 已不存在；实际存在却未列：`AxuExperimental/`、`DevDoc/`、`_CommonDoc/`、`_DevDoc/`、`TEMP/`。
4. **TestAndSample 结构不全**：实际有 `Sample_Core/Common/Modules/Experimental/PackageDev` + `_Sample_Inner`；AGENTS.md 只列前 3 个，且漏掉迁移后关键的 `Sample_PackageDev`。
5. **依赖包列表过时**：缺嵌入式包、collab-proxy、multiplayer.center、newtonsoft-json；test-framework 实为 1.4.6（文中 1.4.5）。
6. **文档位置条目过时**：KitDoc 文件名实为 `KitDoc/Common/Utility/Doc_*.md`（文中写 `MathUtilityDoc.md` 等）；未提 Encrypt 3 份文档、`_CommonDoc/移植评估`、`DevDoc/`、根 `docs/`。
7. **命名空间规范**：迁移提交 5f4c8be 专门修过命名空间；包内实际命名空间为 `AxuFrameKit._Core.*` / `AxuFrameKit.Common.*`（见 03/04 号文档各模块表），AGENTS.md 的 `AxuFrameKit.{层}.{模块}` 写法需按此理解。

> README.md 同样按 Assets 时代布局描述，未反映包化现状。

## 4. 文档面三大欠账（2026-08-29 评估，同日已全部处理）

1. ~~包内无 README / CHANGELOG / 文档~~ → 已解决：包根新增 README.md（含文档索引表）与 CHANGELOG.md；25 份模块文档（19 散落 + 6 KitDoc）迁入包内随代码放置，package.json 补 documentationUrl。
2. ~~根 AGENTS.md、README.md 未随包迁移更新~~ → 已解决：两份文档已重写同步至包化现状。
3. ~~散落文档位置悬空~~ → 已解决：文档迁入包内并修正文中旧路径，`Assets/AxuFrameKit/{_Core,Common,KitDoc}` 旧骨架已删除（仅余 Modules）。
4. ~~剩余待办：Modules 层尚未包化~~ → **已于 2026-08-29 完成**：8 个模块包（含各自 README/CHANGELOG/asmdef/Samples~）；Modules 子模块的独立文档仍缺（可在后续按需补写）。当前文档面剩余事项：AxuExperimental（未包化、文档散在各自 ReadMe）。（2026-09-20 勘误：根 `docs/` docfx 站已删除，不再存在。）

## 5. AI 工具配置概况

| 路径 | 内容与用途 |
|---|---|
| `.agent/skills/ui-tool-kits-skill/` | Unity UI Toolkit 开发辅助技能（UXML/USS/VisualElement，含参考文档） |
| `.agent/skills/axu-ui-tool-kits-editor-skill/` | Axu Editor 工具的 UI Toolkit 样式规范（UE5 风格配色、组件样式、C# 代码模式） |
| `.claude/settings.local.json` | 1 条权限白名单（允许对 Assets/AxuFrameKit 执行 find） |
| `.reasonix/` + `reasonix.toml` | Reasonix 工具会话元数据与权限白名单（toml 内为**已失效的 macOS 旧路径**） |
| `.antigravity/refactor_namespace.ps1` | 命名空间批量重构脚本（包迁移配套产物） |
| `.vscode/` | 隐藏 Unity 产物文件、默认 solution、omnisharp.useModernNet=false |
| `.zcode/` | 本知识库（2026-08-29 生成） |
