# 将项目勘察结果写入 .zcode 知识库

## 已完成的勘察（Phase 1）

已通过 3 个 Explore 子代理并行完成全项目勘察，信息已收集齐并交叉验证：

- **子代理 1（框架包）**：`Packages/com.cielevel.axu-frame-kit` 完整结构 — package.json（0.1.0，依赖 ugui/TMP/newtonsoft-json）、3 个 asmdef 依赖链（AxuFrameKit.Core 零依赖纯 C# ← AxuFrameKit.Runtime ← AxuFrameKit.Editor 仅 Editor）、262 个 .cs 的模块清单（_Core 80 / Common 151 / Editor 31）、包内无测试无文档。
- **子代理 2（Assets 与配置）**：Assets 顶层布局（AxuFrameKit 残留文档层、Modules 未包化、TestAndSample 示例、AxuIndieModules、AxuExperimental）、manifest.json 依赖、ProjectSettings（URP PC_RPAsset、Linear、IL2CPP 等）、README/docs(docfx) 概况。
- **子代理 3（文档与历史）**：git 演进时间线（核心节点 5277cdd 包迁移）、全项目文档清单、AGENTS.md 的 7 类过时条目、AI 工具配置目录概况。

## 实施方案（Phase 2-4）

在 `D:/UnityProject/AxU/AxuFrameKitV2/.zcode/`（已存在、为空）下新建 8 个中文 Markdown 文件，作为后续 AI 会话可直接复用的项目知识库：

1. **README.md** — 知识库导航索引：各文件主题、建议阅读顺序、一句话项目速览（Unity 6 URP 应用框架，核心已迁入嵌入式包）。
2. **01-project-overview.md** — 项目概览与工程配置：Unity 6000.0.23f1 / URP 17.0.3（Assets/URP/Settings/PC_RPAsset）/ Linear 色彩空间 / Android IL2CPP / UNITY_TEST 宏；三层架构定义与包迁移现状；manifest.json 依赖表；嵌入式包解析方式（source: embedded）。
3. **02-package-structure.md** — 嵌入式包结构：package.json 元信息；3 个 asmdef 的名称/引用/平台约束/依赖链；目录树（Editor/Common/* + Runtime/_Core + Runtime/Common）；代码规模统计（262 个 .cs，包内无 Tests、无 .md）。
4. **03-core-modules.md** — _Core 层模块清单：按子模块列表格（模块名 / 路径 / 命名空间 / 关键类 / 职责）——BetterFSM、FSM、EventCenter、ObjectPool、DesignPattern、Structure（IndexedList/BiDictionary/ReactiveProperty 等）、Extension、Computed、Encrypt（完整加密体系）、Math/Interpolation（AxuTweener/Ease 全套）、TaskQ、Multiton、RandomX 等。
5. **04-common-modules.md** — Common 层模块清单：UI Framework（Manager→Area→Panel→Page）、DataBind（19 个 UIBind 组件）、UIListView、Group/Toast/ToggleBtnGroup/Scroll/Typewriter/Video、EventCentral/EventChannel、Mono 单例族（SingletonMono/SimpleMonoCr/CrDs 等）、Tween、Auto* 零代码组件集、Net 三层（Http/HttpClient/Socket）、Save（策略化存储+Builder）、CSV、GameObjectPool、Timer、UseMono 等。
6. **05-editor-modules.md** — Editor 层模块清单：KitBoxRevV2 工具箱窗口（多页签插件体系）、Hierarchy Colorizer/Icon、独立窗口（AssetFavorites/SceneJumper/AvatarDownloader/SpriteToAnimation）、Utility（TerrainToMeshConverter、CSV 工具、ScriptDefineSymbolSetter 等）、各 Inspector 绘制器。
7. **06-assets-layout.md** — Assets 布局：Assets/AxuFrameKit 现状（_Core/Common 仅剩文档 0 个 .cs；Modules 37 个 .cs 待包化：AITools/GizmosUtil/LiveStream/Noise）；TestAndSample 各 Sample 场景与脚本主题；AxuIndieModules（StatPipeline/SceneJumper 等）；AxuExperimental 9 个实验模块；docfx 文档站 docs/；根目录杂项（旧 csproj、build.log、reasonix.toml 等）。
8. **07-docs-and-history.md** — 文档现状与演进史：全项目 .md 文档清单表（路径/主题/状态）；AGENTS.md 与 README.md 的过时条目明细；git 演进时间线（2026-02 ~ 2026-08，含 5277cdd 迁移节点、3231a6e TerrainToMesh 等）。

## 约束

- 只在 `.zcode/` 内新建文件，不修改项目任何现有文件。
- 内容全部来自本次勘察结果，中文撰写；模块清单用表格，保证后续会话可直接检索。
- 文件名用英文编号前缀保证排序，正文中文。