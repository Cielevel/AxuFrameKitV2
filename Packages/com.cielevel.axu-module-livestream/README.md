# AxuFrameKit Modules - LiveStream

直播互动模拟模块（AxuFrameKit 家族的可选模块，依赖 AxuFrameKit）。提供直播评论 / 礼物 / 点赞的触发器体系、编辑器模拟与录制窗口、运行时 UI 面板，以及"秋恋猫"HTTP 直播数据对接。

## 模块组成

- **触发器 SO 体系**（`Runtime/Script/System/`）：`LiveTriggerBase` 及 Comment / Gift / Like 三类基类，配合 `TirggerCollectionSo` 聚合分发；`UserConfigSo` / `GiftConfigSo` / `Trigger*ConfigSo` 提供用户与礼物配置（含编辑器右键批量生成辅助）。
- **模拟器与录制窗口**（`Editor/EditorWindow/`）：
  - `AxuFrameKit/Modules/LiveStream/Simulator`：`LiveStreamSimulatorWindow`，选择用户配置后模拟发送评论 / 礼物 / 点赞，并内置日志调试器。
  - `AxuFrameKit/Modules/LiveStream/Operation Recorder`：`LiveStreamRecorderWindow`，把模拟器发生的操作录制并导出为 JSON（可配时间缩放 / 记录模式 / 录制内容）。
- **运行时 UI 面板**（`Runtime/Script/UI/`）：`UIPanelUser` / `UIPanelGift` / `UIPanelComment` / `UIUnitUserID`，基于框架 UIBasePanel 与循环滚动、数据绑定组件。
- **秋恋猫 HTTP 对接**（`Runtime/Script/Utility/FallCatLive/`）：`RequestLiveStream` 拉取直播互动数据；另有操作录制器 `LiveStreamOpRecorder`（基于框架 AutoSnapshot/Serializer）及若干 UI 辅助组件。

## 依赖

- `com.cielevel.axu-frame-kit`（Core / Runtime 程序集）
- `com.unity.nuget.newtonsoft-json`（RequestLiveStream 的 JSON 解析）
- `com.unity.ugui`、TextMesh Pro（UI 面板与字体）

## 示例导入

通过 Package Manager 选中本包（Packages: In Project → AxuFrameKit Modules - LiveStream），在 Samples 区点击 Import，即可将示例导入到 `Assets/Samples/` 下。示例包含两个演示场景（`SC LiveStreamUtil UI`、`SC Sample Modules LiveStream`）、UI Unit Prefab、TestPanel 演示脚本、自定义触发器实现（`LiveTriggerComment/Gift/Like`）与全部 SO 实例（UserConfig / GiftConfig / TriggerConfig / TirggerCollection）——场景引用的脚本与 SO 实例都在 Samples 内，随导入一起就位。

## 字体许可

`Runtime/Art/Font/Valid/SmileySans-Oblique.otf`（得意黑 Smiley Sans）以 [SIL Open Font License 1.1](https://openfontlicense.org/) 授权。
