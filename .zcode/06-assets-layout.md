# 06 · Assets 布局（包外内容）

> 勘察时间：2026-08-29。框架代码主体在 `Packages/com.cielevel.axu-frame-kit`（见 02 号文档），本文记录 Assets 内剩余内容。
> **Assets 内没有任何 .asmdef**，所有 Assets 侧脚本（Modules、TestAndSample 等）均编译进 Assembly-CSharp。

## 1. Assets 顶层目录树

```
Assets/
├── AxuFrameKit/            # 仅剩 Modules/（原 _Core/Common/KitDoc 文档已于 2026-08-29 迁入包内，旧骨架已删除）
│   └── Modules/            #   AITools / GizmosUtil / LiveStream / Noise —— 仍含完整代码+资产（37 个 .cs，待包化）
├── AxuExperimental/        # 实验性模块集（每个子目录多带 ReadMe.md，见第 5 节）
├── AxuIndieModules/        # 独立可裁剪模块（见第 4 节）
├── DevDoc/                 # 开发过程文档（Socket 技术设计、Common 纯 C# 剥离分析）
├── TEMP/                   # 临时测试 CSV（Test_SampleData*.csv）
├── TestAndSample/          # 示例与测试（见第 3 节）
├── TextMesh Pro/           # TMP Essential 默认资源
├── URP/                    # URP 管线资产（PC/Mobile RPAsset + Renderer，全局用 PC_RPAsset）
├── _CommonDoc/             # AxuFrameKit_Core_Common_移植评估.md（包迁移评估文档）
└── _DevDoc/                # 关于TaskQ系列代码的改进.txt
```

> 注意：旧 AGENTS.md 提到的 `Assets/Doc/`、`Assets/PreTestFunc/`、`Assets/Scripts/Generated/` **已不存在**。

## 2. Assets/AxuFrameKit 残留状态（重要）

- **纯文档残留（已于 2026-08-29 迁出）**：原 `_Core/`（3 份）与 `Common/`（16 份）的模块文档已迁入包内、随代码放置（如 `Runtime/_Core/Advanced/BetterFSM/ReadMe.md`、`Runtime/Common/UI/Kit/ListView/DOC/`、`Runtime/Common/Utility/Save/` 等），文中旧路径已修正，旧目录骨架已删除。
- **仍保留完整代码+资产的 Modules（37 个 .cs，无 asmdef，未包化）**：
  | 模块 | 内容 |
  |---|---|
  | AITools | AI 对话窗口、AI 脚本填充、AI 脚本生成（UI Toolkit uxml/uss + AIChatConfig-gemini/glm4.7flashx 配置资产） |
  | LiveStream | 直播互动模拟：LiveTriggerComment/Gift/Like 基类、各 ConfigSo、UIPanel 系列、场景 `SC LiveStreamUtil UI.unity`、美术资源、TODO.txt |
  | GizmosUtil | GizmosDrawer 系列（partial 分文件）+ Editor |
  | Noise | FastNoiseLite + 编辑器窗口 |
- `KitDoc/`（已于 2026-08-29 迁出并删除）：原 6 份文档迁入包内对应模块目录——Math/Save/CSV 3 份 → `Runtime/Common/Utility/{Math,Save,Table/CSV}/`，Encrypt 3 份 → `Runtime/_Core/Utility/Encrypt/`。

## 3. Assets/TestAndSample/ 示例组织

按框架分层镜像组织；场景统一以 `SC Sample ...` 命名。**无 asmdef，编入 Assembly-CSharp**（根目录旧 `AxuFrameKit.TestAndSample.csproj` 为历史生成物）。

| 目录 | 代表场景 | 示例脚本主题 |
|---|---|---|
| Sample_Core/Advanced | （无场景） | BetterFSMUsage |
| Sample_Core/DesignPattern | SC Sample Core Design ChainOfResponsibility / Memento | SampleChainOfResponsibility、SampleCommand、SampleMemento |
| Sample_Core/Structure | SC Sample Core Structure ReactiveProperty | SampleReactiveProperty |
| Sample_Core/Utility | SC Sample Core Computed、SC SampleEncrypt、SC Sample Core RandomAvatar | Sample_Computed、SampleEncrypt、RandomAvatarDownload |
| Sample_Common/CustomAttribute | SC Sample ListDrawer | SampleCustomAttribute |
| Sample_Common/UI/Kit | SC Sample Common UIBindData / ListView / Opt Scroll / Toggle Group / Typewriter | SampleUIBindData、ListViewDemo(+Controller)、SampleOptScroll、SampleCommonToggleGroup、SampleCommonTypewriter |
| Sample_Common/Utility | SC Sample Debugger / Auto Timeline / EventChannel / EventSystem / AxuTween / Socket / GameObjectPools / Common Save / Common Csv / TimerSeries / Common Trigger / UseMono | SampleDebugger、SampleAutoSnapshot、EventChannel 收发、SampleCommonEventSystem、SampleAxuTween 系列（Sequence/UI/Toast）、HttpClientExample、SocketExample、SampleGameObjectPools、SampleCommonSaveUtil(UserProfile)/SampleCommonSimpleSave、SampleCsv、SampleTimerSeries、SampleTrigger、SampleUseMono |
| Sample_Experimental | SC Sample Modules BehaviorTree、SC Sample Common InGameConsole | SampleBehaviorTree、SampleInGameConsole |
| Sample_Modules | SC Sample Modules AI Generators、SC Sample Module Gizmos、SC Sample Modules LiveStream | UIController、LiveTriggerComment/Gift/Like + TestPanelGift/User + SO 配置 |
| Sample_PackageDev | SC Sample PackageDev | TestPackageInfo（包开发验证，引用嵌入式包 API）——迁移后关键目录 |
| _Sample_Inner | SC Sample Animation State、SC Sample ListIdTitle | SampleAnimationState{Bigger,Smaller}（StateMachineBehaviour，附说明.md）、SampleListIdTitle |
| 根 | — | Test.cs |

EditorBuildSettings 唯一构建场景：`SC Sample UseMono.unity`。

## 4. Assets/AxuIndieModules/ 独立模块

根 ReadMe 声明："每一个文件夹都提供一个独立模块，可按需选择性导入"。

| 模块 | 结构 | 职责 |
|---|---|---|
| **StatPipeline** | `Script/`（Stat: StatCore/StatDouble/StatFloat/StatInt/StatLong/StatPipelineStages；Modifier: StatModifier/StatModifierCore/StatModifierLink + 编辑器 StatCalculatorGeneratorWindow；Calculator: 13 种 CommonCalculator + 2 种 LinkCalculator + 基类；Analysis: StatDebugger）+ `Resources/Calculator` 预置资产 + `Temp/` 示例场景 `SC Sample StatPipeline.unity` | 数值管线/属性修改器系统（类 roguelike 数值计算，依赖 Unity） |
| SceneJumper | SceneJumperWindow.cs（UI Toolkit） | 编辑器场景跳转（包内 Editor/Common/Window 另有纳入版） |
| SelectionToCsv | SelectionToCsvWindow.cs | 选中对象导出 CSV |
| SpriteToAnimation | SpriteToAnimationWindow.cs | 精灵序列转动画 |

## 5. Assets/AxuExperimental/ 实验模块（9 个，多带 ReadMe）

| 模块 | 说明 |
|---|---|
| AI | （空目录） |
| BehaviorTree | Editor + Runtime，行为树（docfx API 已收录） |
| EventSystem | EventBus / EventListener / GameEventChannel_T |
| HtmlCommunicate | LocalWebServer + test html + 场景 |
| InGameConsole | 运行时控制台（含 Resources） |
| ModuleConfig | 模块化实体 ModuleEntity/Modules + "SC PreTest Module" 场景 |
| NewNetFrame | NetInterface / NetProvider / NetUtilTester |
| Noise | 噪声可视化（SC Noise 场景） |
| OllamaModelCall | OllamaClient + OllamaScriptFillerWindow |
| SceneCurveDraw | PathController + Editor |

## 6. 文档目录与根目录杂项

- 文档目录：`Assets/DevDoc/`（Socket TechnicalDesign、Common_To_PureCS_Analysis）、`Assets/_CommonDoc/`（移植评估 = trans doc）、`Assets/_DevDoc/`（TaskQ 改进备忘）、根 `docs/`（**docfx 文档站**：docfx.json、api/ 生成 YAML、_site/ 产物；index.md 为占位 stub）。
- 根杂项：十余个旧版 `AxuFrameKit.*.csproj` 与 `AxuFrameKitV2.sln`（Unity 生成物）；`build.log/build_error.log`（UTF-16，早期 dotnet 编译尝试的报错日志）；`bin/`、`obj/`（dotnet 产物）；`AxuLogo.jpeg`；`reasonix.toml`（权限白名单，内嵌**已失效的 macOS 旧路径**）；`.vsconfig`（VS 的 Unity 工作负载）。
- AI 工具配置：`.agent/skills/`（2 个 UI Toolkit 相关技能）、`.claude/settings.local.json`（1 条 find 权限白名单）、`.reasonix/`（会话元数据）、`.vscode/`（隐藏 Unity 产物等设置）、`.antigravity/`（含命名空间批量重构脚本 refactor_namespace.ps1，迁移配套产物）、`.zcode/`（本知识库）。
