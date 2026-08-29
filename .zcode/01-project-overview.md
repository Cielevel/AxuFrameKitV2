# 01 · 项目概览与工程配置

> 勘察时间：2026-08-29。来源：Packages/manifest.json、packages-lock.json、ProjectSettings/、根 README.md。

## 基本信息

| 项 | 值 |
|---|---|
| 项目名 | AxuFrameKitV2（阿修框架工具包） |
| 项目类型 | Unity 应用程序框架（框架本体以嵌入式 UPM 包形式随项目开发） |
| Unity 版本 | **6000.0.23f1**（Unity 6，见 ProjectSettings/ProjectVersion.txt） |
| 渲染管线 | URP（com.unity.render-pipelines.universal 17.0.3） |
| 色彩空间 | Linear（m_ActiveColorSpace: 1） |
| API 兼容级别 | .NET Standard（apiCompatibilityLevel: 6） |
| 脚本后端 | Android = IL2CPP；Android 等平台 managedStrippingLevel = 1（Low） |
| 脚本宏定义 | Standalone 平台启用 `UNITY_TEST` |
| 当前版本状态 | 包版本 0.1.0，开发中（HEAD 共 162 commits，单 main 分支） |

## 三层架构（当前形态）

```
Modules (Assets/AxuFrameKit/Modules，37 个 .cs，未包化，编译进 Assembly-CSharp)
   │  依赖
   ▼
Common (包内 Runtime/Common，151 个 .cs，asmdef: AxuFrameKit.Runtime)  ── 依赖 ──►
   │  依赖                                                            _Core
   ▼
_Core (包内 Runtime/_Core，80 个 .cs，asmdef: AxuFrameKit.Core，纯 C# 零 Unity 依赖)

Editor 工具层：包内 Editor/（31 个 .cs，asmdef: AxuFrameKit.Editor，横跨 Core+Common）
```

- `_Core`：纯 C# 实现，**不依赖任何 Unity API**，可在非 Unity 环境使用。
- `Common`：依赖 Unity API 与 `_Core`，提供 UI 框架、网络、存档、对象池等运行时组件。
- `Modules`：具体业务功能（AITools / GizmosUtil / LiveStream / Noise），**尚未纳入包程序集体系**。

## URP 管线资产

- 全局管线资产：`Assets/URP/Settings/PC_RPAsset.asset`（GraphicsSettings.m_CustomRenderPipeline，guid 4b83569d...）。
- `Assets/URP/Settings/` 下含：PC_RPAsset / PC_Renderer、Mobile_RPAsset / Mobile_Renderer、DefaultVolumeProfile、SampleSceneProfile、UniversalRenderPipelineGlobalSettings。
- QualitySettings 有 Mobile / PC 两档，但**未按档覆盖管线资产**（两档共用全局 PC 资产）。
- `Assets/URP/TutorialInfo` 为 URP 模板遗留。

## 构建相关

- EditorBuildSettings 中仅 1 个构建场景：`Assets/TestAndSample/Sample_Common/Utility/UseMono/SC Sample UseMono.unity`。
- 序列化模式 Force Text；无 CI 配置（无 .github/workflows）。
- 根目录存在大量旧版 Unity 生成的 `AxuFrameKit.*.csproj` 与 `AxuFrameKitV2.sln`；`build.log`/`build_error.log`（UTF-16）是早期尝试用 dotnet/MSBuild 直接编译的历史产物（含 Encrypt 模块 CS0283/CS0234 等报错），配套 `bin/`、`obj/`，与 Unity 构建无关。

## Packages/manifest.json 依赖

嵌入式包 `com.cielevel.axu-frame-kit` **不在 manifest.json 中显式声明**，靠 Packages 目录自动识别；
packages-lock.json 中解析为 `"source": "embedded", "depth": 0`，依赖 ugui 1.0.0、textmeshpro 3.0.0、newtonsoft-json 3.2.2。

| 包 | 版本/来源 | 说明 |
|---|---|---|
| com.termway.ide.antigravity | git URL（unity-ide-antigravity） | IDE 增强插件 |
| com.unity.render-pipelines.universal | 17.0.3 | URP |
| com.unity.inputsystem | 1.11.1 | 输入系统 |
| com.unity.ai.navigation | 2.0.4 | AI 导航 |
| com.unity.nuget.newtonsoft-json | 3.2.2 | Newtonsoft JSON（CSV/存档等依赖） |
| com.unity.test-framework | 1.4.6 | 测试框架（AGENTS.md 写 1.4.5，已过时） |
| com.unity.timeline | 1.8.7 | 时间轴 |
| com.unity.ugui | 2.0.0 | uGUI |
| com.unity.visualscripting | 1.9.4 | 可视化脚本 |
| com.unity.ide.rider / visualstudio | 3.0.38 / 2.0.22 | IDE 集成 |
| com.unity.collab-proxy | 2.9.3 | 版本管理插件 |
| com.unity.multiplayer.center | 1.0.0 | 多人游戏中心（模板默认） |
| com.unity.modules.* | 约 30 个 | 标准内置模块 |

## 根目录 README.md 现状

面向 GitHub 的框架介绍（配 AxuLogo.jpeg）：定义 AxuFrameKit 为"Unity 应用程序框架，清晰架构 + 可复用组件"，
附三层架构 mermaid 依赖图与各层模块例举。**注意：其目录说明仍按 Assets 时代布局（`Assets/AxuFrameKit/_Core` 等路径）描述，未反映包迁移现状。**

## 命名与代码规范速记（详见根 AGENTS.md，部分已过时）

- 命名空间：`AxuFrameKit._Core.{子模块}` / `AxuFrameKit.Common.{子模块}` / `AxuFrameKit.Modules.{模块名}` / `AxuIndieModules.{模块名}`。
- 每个类单独一个文件，文件名与类名一致；Editor 代码放 `Editor/` 子目录或用 `#if UNITY_EDITOR` 包裹。
- 分层铁律：`_Core` 绝不引用 Unity API；`Common` 只依赖 `_Core`；`Modules` 可依赖两者。
