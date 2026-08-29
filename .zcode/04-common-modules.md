# 04 · Common 层模块清单（AxuFrameKit.Runtime 程序集，151 个 .cs）

> 路径：`Packages/com.cielevel.axu-frame-kit/Runtime/Common/`。命名空间：`AxuFrameKit.Common.{子模块}`。
> 特点：依赖 Unity API 与 `_Core` 层（AxuFrameKit.Core），含 UI 框架与各类运行时组件。

## UI 子系统

| 子模块 | 路径 | 命名空间 | 关键类 | 职责 |
|---|---|---|---|---|
| **UI Framework** | `Common/UI/Framework/` | `AxuFrameKit.Common.UI.Framework` | `UIBaseComponent`、`UIBaseUnit`、`UIBaseArea`、`UIBasePanel`、`UIBasePage`、`UIBaseManager<T>` | UI 页面框架：Manager(单例)→Area→Panel→Page 层级，按 ID Toggle/Show/Hide |
| **UI DataBind** | `Common/UI/Kit/DataBind/` | `AxuFrameKit.Common.UI.Kit.DataBind`（含 `.Component`） | `UIBindParent`、`UIData/IUIDataUnit`、`UIBindComponent(abstract)`、`UIBindComponentGeneric<T>` + 约 19 个具体绑定：`UIBindTextStr/Float/Int`、`UIBindTmpTextStr/Int/Float`、`UIBindImage(ImageColor/ImageMaterial/RawImage*/Texture)`、`UIBindSlider/Dropdown/InputField/TmpInputField/Active` | 声明式 UI 数据绑定（数据 → UI 单向绑定） |
| **UI ListView** | `Common/UI/Kit/ListView/` | `AxuFrameKit.Common.UI.Kit.ListView` | `UIListView`（LayoutMode/ListViewSettings/SelectionSettings）、`UIListViewItem`、`UIListViewAdapter<T>`、`UIListViewDataBindItem`、`UIListViewItemPool`、`UIListViewDataBuilder`、`Vertical/Horizontal/GridLayoutEngine(IListViewLayoutEngine)`、`UIListViewExtensions` | 虚拟化列表（数据绑定 + 对象池 + 可插拔布局引擎），文档最全（见 07 号文档 DOC 清单） |
| UI Group | `Common/UI/Kit/Group/` | `AxuFrameKit.Common.UI.Kit.Group` | `UIImageGroupID/GroupIndex`、`UISpriteGroupID/GroupIndex` | 按 ID/Index 切换 Image/Sprite 的图组组件 |
| UI Toast | `Common/UI/Kit/Toast/` | `AxuFrameKit.Common.UI.Kit.Toast` | `UIToastUnit`、`UISideToast<T>`、`ToastState`、`PushDirection` | 边缘推入式 Toast 弹窗 |
| UI ToggleBtnGroup | `Common/UI/Kit/ToggleBtnGroup/` | `AxuFrameKit.Common.UI.Kit.ToggleBtnGroup` | `UIToggleParent(/ForPanel)`、`UIToggleChild`、`UIToggleBaseUnit` + `UIUnitToggleGo/Color/Sprite` | 互斥按钮组（子项表现可替换：显隐/变色/换图） |
| UI Scroll | `Common/UI/Kit/Scroll/` | `AxuFrameKit.Common.UI.Kit.Scroll` | `UILoopVerticalScrollRect` | 循环滚动列表 |
| UI Typewriter | `Common/UI/Kit/Typewriter/` | `AxuFrameKit.Common.UI.Kit.Typewriter` | `TmpTypewriter`、`TmpTypewriterManager`、`Item` | TMP 打字机效果（06-12 新增） |
| UI Video | `Common/UI/Kit/Video/` | `AxuFrameKit.Common.UI.Kit.Video` | `UIVideoKit`、`UIVideoConfig` | 视频播放套件 |

## 基础设施 Utility

| 子模块 | 路径 | 命名空间 | 关键类 | 职责 |
|---|---|---|---|---|
| Event | `Common/Utility/Event/` | `AxuFrameKit.Common.Utility.Event` | `EventCentral`（字符串事件中心，EventUnit/EventUnit<T>）、`EventChannel<T>`（SO 事件通道）、`IUnRegister` + `UnRegisterOnDestroy/OnDisable/SceneUnloadedTrigger` | 两套事件系统，均带生命周期自动注销 |
| Singleton(Mono) | `Common/Utility/Singleton/Singletons.cs` | `AxuFrameKit.Common.Utility.Singleton` | `SingletonLazy<T>`、`SingletonMono<T>`、`SingletonCustomMono<T>`、`SingletonMonoCr<T>`、`SingletonMonoCrDs<T>` | Mono 侧单例族（Cr=自动创建 GameObject，Ds=DontDestroyOnLoad） |
| Multiton(Mono) | `Common/Utility/Multiton/` | `AxuFrameKit.Common.Utility.Multiton` | `MultitonMono<T>`、`MultitonAutoMono(R)<T>`、`MultitonManager<TManager,TItem>` | Mono 侧多例与多例管理器（MultitonManager 为 06-12 新增） |
| UseMono | `Common/Utility/UseMono/` | `AxuFrameKit.Common.Utility.UseMono` | `IUseMonoBehaviourSeparate/Common`、`MonoHandler(/Common)`、`MonoSingle` | 非 Mono 类代理挂接 Update 等生命周期 |
| Tween | `Common/Utility/Math/Interpolation/` | `AxuFrameKit.Common.Utility.Math.Interpolation`（含 `.Tweener`） | `AxuTween/AxuTweenRunner`、`AxuTweenExtension` + 具体 Tweener：`MoveVec2/Vec3`、`RotEuler/RotQua`、`Scale`、`AnchoredMove(X/Y)`、`CanvasGroupAlpha`、`GraphicColor/Fade`、`AxuSequence` | 基于 _Core IEase 的 Unity 补间运行器 + 各组件 Tween 实现 |
| Net | `Common/Utility/Net/` | `AxuFrameKit.Common.Utility.Net` | Basic: `HttpUtil`、`Downloader`；Advanced: `HttpClient`、`HttpResult<T>`；Socket: `SocketClient`、`SocketClientOptions`、`ISocketPacketCodec`、`LengthPrefixedPacketCodec`、`SocketConnectionState/DisconnectReason/DisconnectedEvent` | 三层网络：静态 HTTP 工具、可配置 HttpClient、带长度前缀分包协议的 TCP Socket 客户端（生产级，03-08 新增） |
| Save | `Common/Utility/Save/` | `AxuFrameKit.Common.Utility.Save` | `SaveUtil` + `SaveUtilBuilder`、`IStorage/IStorageExtended`、`LocalFileStorage`、`JsonFileStorage`、`BinaryFileStorage`、`PlayerPrefsStorage`、`SimpleSave` | 存档系统：策略化存储后端 + Builder 组装 |
| Serializer | `Common/Utility/Serializer/` | `AxuFrameKit.Common.Utility.Serializer` | `ISerializer`、`BinarySerializer`、`UnityJsonSerializer` | 可替换序列化后端 |
| Table/CSV | `Common/Utility/Table/CSV/` | `AxuFrameKit.Common.Utility.Table.CSV` | `CsvParser`（CsvParseOptions/Result、CsvColumnAttribute、FinalParseAttribute）、`SimpleCsvTool`、`CsvContainerSo<T>`（CSV→SO 容器）、`NumberToBooleanConverter`；Obsoleted: `CsvUnit<T>` | CSV 配置表解析与 ScriptableObject 容器化（依赖 Newtonsoft） |
| ObjectPool(Mono) | `Common/Utility/ObjectPool/GameObjectPool/` | `AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool` | `GameObjectPools<T>`（SO 配置）、`ObjectUnit<T>`、`Poolable<T>` + Int/Str 特化 | GameObject 层级对象池（ScriptableObject 配置键值池） |
| Timer | `Common/Utility/Timer/` | `AxuFrameKit.Common.Utility.Timer` | `CountdownManager`（倒计时）、`StopwatchManager`（秒表）、`Timers` | 全局计时管理单例 |
| TaskQ(Mono) | `Common/Utility/TaskQ/CoroutineQ.cs` | `AxuFrameKit.Common.Utility.TaskQ` | `ICoroutineQ`、`CoroutineQ` | 协程任务队列 |
| CustomAttribute | `Common/CustomAttribute/` | `AxuFrameKit.Common.CustomAttribute` | `CustomMonoBehaviour`、`CustomScriptableObject`、`InsButtonAttribute`、`InspectorIntroAttribute`、`InspectorTitleAttribute` | 带扩展点的 MonoBehaviour/SO 基类与 Inspector 增强属性 |
| Extension | `Common/Extension/` | `AxuFrameKit.Common.Extension` | Vector/Transform/Color/GameObject/RectTransform/AsyncOperation 扩展 | Unity 常用类型扩展方法 |

## Auto* 零代码组件集（`Common/Utility/Auto/`）

| 组件族 | 关键类 | 职责 |
|---|---|---|
| AutoSequence | `AutoSequence`、`SimpleSequence/SequencePlayer` + `WaitTime/TriggerEvent/WaitUntil/SetActive/RaiseEvent` 等 Item | 序列播放器（时间轴式步骤编排，配编辑器 SequenceEditor） |
| AutoEventChannel | `AutoEventChannelBase` + `OneShot/Continue/ToggleActive/AnimeRaise` | 事件通道触发组件 |
| AutoDestroy | `GoAutoDestroy`、`GoEventChannelDestroy` | 自动销毁 |
| AutoSnapshot | `SnapshotRecorder<T>/SnapshotPlayer<T>/Builder`（TimeScaleMode/TimeRecordMode） | 快照录制/回放 |
| AutoCamera | `GameObjectFaceCamera`、`FaceCameraManager` | 物体面向相机 |
| AutoTracker | `PositionTracker`、`SmoothPositionTracker` | 位置追踪 |

## 其他 Utility

| 子模块 | 关键类 | 职责 |
|---|---|---|
| Analysis | `PerformanceMonitor/PerfData`、`Debugger` | 性能监控、游戏内调试器 |
| 2D | `EdgeColliderCircleShape` 等 | 2D 碰撞形状工具 |
| Raycast | `SimpleCamClicker`、`SimpleCamHover` | 相机点击/悬停射线 |
| Res | `ResourceLoader` | 资源加载 |
| Static | `DefaultInputPreset`、`Type2Name`、`Type2DefaultStrValue`、`EnumeratorWait` | 静态映射表与等待工具 |
| Trigger | `AbsTrigger/AbsTriggerable/TriggerLimit` | 触发器抽象 |
