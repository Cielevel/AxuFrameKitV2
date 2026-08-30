# AxuFrameKit

AxuFrameKit by Cielevel —— Unity 6 应用程序框架。提供清晰分层架构（纯 C# 核心 + Unity 运行时组件 + 编辑器工具）与可复用的 UI、网络、存档等组件。

- **Unity 版本要求**: 6000.0+
- **渲染管线**: URP（运行时组件不强制依赖 URP）
- **依赖**: com.unity.ugui、com.unity.textmeshpro、com.unity.nuget.newtonsoft-json

## 程序集结构

```
AxuFrameKit.Editor (Editor/, 仅编辑器)
        ▲
AxuFrameKit.Runtime (Runtime/Common/, Unity 运行时组件与 UI 框架)
        ▲
AxuFrameKit.Core (Runtime/_Core/, 纯 C# 零 Unity 依赖)
```

| 目录 | 内容 |
|---|---|
| `Runtime/_Core/` | 纯 C# 核心：BetterFSM/FSM、EventCenter、对象池、设计模式、数据结构（BiDictionary/ReactiveProperty 等）、Computed、Encrypt 加密全套、Ease/插值基座、TaskQ、RandomX |
| `Runtime/Common/` | Unity 运行时：UI 框架（Manager→Area→Panel→Page）、UIBind 数据绑定、UIListView、EventCentral、单例/多例族、AxuTween、Net（Http/HttpClient/Socket）、Save 存档、CSV、GameObjectPool、Timer、Auto* 零代码组件等 |
| `Editor/` | 编辑器工具：AxuKitBox 工具箱窗口、Hierarchy 增强、TerrainToMesh 转换器、CSV 工具、独立工作流窗口等 |
| `Tests/Editor/` + `Tests/Runtime/` | EditMode 与 PlayMode 测试（`AxuFrameKit.Tests.{Editor,Runtime}`，`UNITY_INCLUDE_TESTS` 守护不进构建；运行用 pipeline `axu_test_run` 命令或菜单 `AxuFrameKit/AxuTestRunner`） |

## 安装

本项目以**嵌入式包**方式使用（位于 `Packages/` 下，随项目直接开发）。在其他项目中引用时，可将本目录拷贝到目标项目的 `Packages/` 下，或在 manifest.json 中以本地路径引用。

## 文档索引

文档随代码放置在各模块目录内：

| 主题 | 位置（相对包根） |
|---|---|
| 包变更记录 | [CHANGELOG.md](CHANGELOG.md) |
| BetterFSM 状态机 | `Runtime/_Core/Advanced/BetterFSM/ReadMe.md` |
| 设计模式（Command/Memento/责任链/单例） | `Runtime/_Core/DesignPattern/README.md` |
| 扩展方法 | `Runtime/_Core/Extension/README.md` |
| 加密模块（总览/API/速查） | `Runtime/_Core/Utility/Encrypt/{EncryptUtility,APIReference,QuickReference}.md` |
| UI 页面框架 | `Runtime/Common/UI/Framework/UI框架.md` |
| DataBind 数据绑定 | `Runtime/Common/UI/Kit/DataBind/DOC/HOWTOUSE.md` |
| UIListView 虚拟化列表 | `Runtime/Common/UI/Kit/ListView/DOC/`（README/QUICKSTART/ARCHITECTURE/IMPROVEMENTS/INDEX） |
| UI 图组 / Toast / 按钮组 | `Runtime/Common/UI/Kit/{Group,Toast,ToggleBtnGroup}/DOC/HOWTOUSE.md` |
| Math 数学与插值 | `Runtime/Common/Utility/Math/Doc_MathUtility.md` |
| Save 存档（另见 ReadMe/Usage） | `Runtime/Common/Utility/Save/` |
| CSV 表格 | `Runtime/Common/Utility/Table/CSV/Doc_TableCsvUtility.md` |
| AutoSnapshot 快照录制/回放 | `Runtime/Common/Utility/Auto/AutoSnapshot/README.md` |
| Net 网络（HttpClient/Socket） | `Runtime/Common/Utility/Net/Advanced/DOC/`、`Runtime/Common/Utility/Net/Socket/README.md` |
| 测试体系（测试基建 + axu_test_* 命令 + 报告 schema + 接入约定） | `../../com.cielevel.axu-test-framework/README.md`（独立嵌入式包 AxuTestFramework） |

## 版本

当前版本 0.1.0，变更历史见 [CHANGELOG.md](CHANGELOG.md)。
