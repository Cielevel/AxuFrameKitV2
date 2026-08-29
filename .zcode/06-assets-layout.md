# 06 · Assets 布局（包外内容）

> 勘察时间：2026-08-29（同日晚些时候完成模块拆包后更新）。框架与模块代码全部在 `Packages/` 下（框架见 02 号文档，模块包清单见下），
> **Assets 内已无任何框架/模块代码**；唯一的 asmdef 是测试程序集 `Assets/Tests/Editor/AxuFrameKitV2.Tests.Editor.asmdef`（其余 Assets 侧脚本编译进 Assembly-CSharp）。

## 1. Assets 顶层目录树

```
Assets/
├── AxuExperimental/        # 实验性模块集（BehaviorTree/EventSystem/InGameConsole/HtmlCommunicate/ModuleConfig/
│                           #   NewNetFrame/Noise/OllamaModelCall/SceneCurveDraw 共 9 个，各带 ReadMe；未包化）
├── DevDoc/                 # 开发过程文档（Socket 技术设计、Common 纯 C# 剥离分析）
├── TEMP/                   # 临时测试 CSV（Test_SampleData*.csv）
├── TestAndSample/          # 示例与测试（见第 2 节）
├── TextMesh Pro/           # TMP Essential 默认资源
├── URP/                    # URP 管线资产（PC/Mobile RPAsset + Renderer，全局用 PC_RPAsset；TutorialInfo 模板遗留）
├── _CommonDoc/             # AxuFrameKit_Core_Common_移植评估.md（包迁移评估文档）
└── _DevDoc/                # 关于TaskQ系列代码的改进.txt
```

> **2026-08-29 拆包移除**：`Assets/AxuFrameKit/`（先迁文档后迁 Modules，已整体删除）、`Assets/AxuIndieModules/`（拆包删除）、
> `Assets/TestAndSample/Sample_Modules/`（示例随包迁入各包 `Samples~/`）。

## 2. 模块包清单（2026-08-29 拆包结果）

| 包 | 来源 | 内容 | asmdef | 框架依赖 |
|---|---|---|---|---|
| `com.cielevel.axu-module-aitools` | Assets/AxuFrameKit/Modules/AITools | AI 对话/填充/生成窗口（UXML 定位已改脚本相对式；Editor-only 隔离修复） | AITools + .Editor | 无 |
| `com.cielevel.axu-module-gizmos-util` | …/GizmosUtil | Gizmos 绘制库（partial 抽屉 + 检视器） | GizmosUtil + .Editor | 无 |
| `com.cielevel.axu-module-livestream` | …/LiveStream | 直播互动模拟（触发器 SO/UI 面板/模拟与录制窗口） | LiveStream + .Editor | **axu-frame-kit + Newtonsoft + TMP/ugui** |
| `com.cielevel.axu-module-noise` | …/Noise | 噪声生成（FastNoiseLite MIT 声明在 `Third Party Notices.md`） | Noise + .Editor | 无 |
| `com.cielevel.axu-indie-scene-jumper` | Assets/AxuIndieModules/SceneJumper | 场景收藏/快跳窗口 | Editor-only | 无 |
| `com.cielevel.axu-indie-selection-to-csv` | …/SelectionToCsv | 选中导出 CSV 窗口 | Editor-only | 无 |
| `com.cielevel.axu-indie-sprite-to-animation` | …/SpriteToAnimation | 序列帧转动画窗口 | Editor-only | 无 |
| `com.cielevel.axu-indie-stat-pipeline` | …/StatPipeline | 数值管线（31 cs；Resources 计算器资产 GUID 链完整） | StatPipeline + .Editor | 无 |

- 各包含 package.json（0.1.0）/README/CHANGELOG；示例（场景/Prefab/测试脚本/SO 实例）在各自 `Samples~/`，经 Package Manager → Samples 导入。
- 框架包内与独立包重复的 `SceneJumperWindow`/`SpriteToAnimationWindow` 已删除（独立包为唯一来源）；KitBox 的 `SelectionToCsvRevPage` 为重写版，保留。
- AIChatConfig 配置资产（**含明文 API key，属用户数据不入包**）迁至 `Assets/TestAndSample/AITools_Config/`。

## 3. Assets/TestAndSample/ 示例组织

按框架分层镜像组织；场景统一以 `SC Sample ...` 命名。**无 asmdef，编入 Assembly-CSharp**。

| 目录 | 代表场景 | 示例脚本主题 |
|---|---|---|
| Sample_Core/Advanced | （无场景） | BetterFSMUsage |
| Sample_Core/DesignPattern | SC Sample Core Design ChainOfResponsibility / Memento | SampleChainOfResponsibility、SampleCommand、SampleMemento |
| Sample_Core/Structure | SC Sample Core Structure ReactiveProperty | SampleReactiveProperty |
| Sample_Core/Utility | SC Sample Core Computed、SC SampleEncrypt、SC Sample Core RandomAvatar | Sample_Computed、SampleEncrypt、RandomAvatarDownload |
| Sample_Common/CustomAttribute | SC Sample ListDrawer | SampleCustomAttribute |
| Sample_Common/UI/Kit | SC Sample Common UIBindData / ListView / Opt Scroll / Toggle Group / Typewriter | SampleUIBindData、ListViewDemo(+Controller)、SampleOptScroll、SampleCommonToggleGroup、SampleCommonTypewriter |
| Sample_Common/Utility | SC Sample Debugger / Auto Timeline / EventChannel / EventSystem / AxuTween / Socket / GameObjectPools / Common Save / Common Csv / TimerSeries / Common Trigger / UseMono | SampleDebugger、SampleAutoSnapshot、EventChannel 收发、SampleCommonEventSystem、SampleAxuTween 系列、HttpClientExample、SocketExample、SampleGameObjectPools、Save/Csv/Timer/Trigger/UseMono 示例 |
| Sample_Experimental | SC Sample Modules BehaviorTree、SC Sample Common InGameConsole | SampleBehaviorTree、SampleInGameConsole |
| Sample_PackageDev | SC Sample PackageDev | TestPackageInfo（包开发验证） |
| _Sample_Inner | SC Sample Animation State、SC Sample ListIdTitle | SampleAnimationState{Bigger,Smaller}、SampleListIdTitle |
| AITools_Config | — | AIChatConfig-gemini / glm4.7flashx（含明文 key，勿提交到公开仓库/勿随包分发） |

（原 `Sample_Modules/` 已删除——AI/Gizmos/LiveStream 示例迁入各模块包 `Samples~/`。）
EditorBuildSettings 唯一构建场景：`SC Sample UseMono.unity`。

## 4. 文档目录与根目录杂项

- 文档目录：`Assets/DevDoc/`（Socket TechnicalDesign、Common_To_PureCS_Analysis）、`Assets/_CommonDoc/`（移植评估 = trans doc）、`Assets/_DevDoc/`（TaskQ 改进备忘）、根 `docs/`（**docfx 文档站**：docfx.json、api/ 生成 YAML、_site/ 产物；index.md 为占位 stub；该目录被 .gitignore 忽略）。
- 根杂项：旧版 `AxuFrameKit.*.csproj` 与 `AxuFrameKitV2.sln`（Unity 生成物，被 gitignore）；`LiveUtil.csproj/LiveUtil.Editor.csproj`（LiveUtil→LiveStream 更名前的遗迹，仅存在于磁盘）；`build.log/build_error.log`（被 *.log 忽略）；`AxuLogo.jpeg`；`reasonix.toml`；`.vsconfig`。
- AI 工具配置：`.agent/skills/`（2 个 UI Toolkit 相关技能）、`.claude/`（ignored）、`.reasonix/`（ignored）、`.vscode/`、`.antigravity/`（含 refactor_namespace.ps1）、`.zcode/`（本知识库 + reports/）。
