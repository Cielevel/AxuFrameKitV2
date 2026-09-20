# monkey —— 猴子测试（方案 §4.7，v1）

流程：加载目标场景（可选）→ 显式 seed 的随机点按序列（间隔 ±50% 抖动、坐标取 region 归一化区域）
→ 每操作记 JSONL 流水 → `AgentLogWatch` 监错（错误即截图，stopOnError 可即停）→ 卡死检测
（frameCount 阈值墙钟无推进 → 截图 + Error + 终止）→ 结束产出 `monkey.*` metrics。

输入注入由 `Agent/Tools/AgentInput.cs` 统一封装（见下方"输入注入"）。

## 参数表（scenario.params）

| 键 | 类型 | 默认 | clamp | 说明 |
|---|---|---|---|---|
| `scene` | string | `""` | — | 目标场景路径（三级回退解析，同 perf）；空 = 不加载、对当前场景施虐。加载失败 → Error finding `MONKEY_SCENE_LOAD_FAILED` |
| `durationSec` | float | `60` | 0.1..7200 | 施虐时长（真实时钟） |
| `actionsPerSecond` | float | `2` | 0.1..30 | 每秒点按期望值（实际间隔为均值 ±50% 均匀抖动，下限 0.02s） |
| `seed` | int | `0` | — | 随机种子（见"seed 复现"）；launch.Seed 优先于此值 |
| `region` | object | `null` | — | 操作区域 `{x,y,w,h}`，**归一化 0-1 坐标域**（左下原点），null = 全屏；越界自动 clamp，w/h 下限 0.01 |
| `stopOnError` | bool | `true` | — | 检测到 Error/Exception/Assert 日志是否立即截图并终止；false 时仅计数继续 |
| `hangThresholdSec` | float | `5` | 0.5..600 | 卡死检测阈值（见"卡死检测语义"） |
| 通用保留键 | — | — | — | `timeScale`/`vsync`/`targetFrameRate`/`qualityLevel`/`resolution`/`seed`（宿主统一应用） |

## seed 复现

坐标与间隔序列全部出自 `new System.Random(seed)` 的**单条消费流**（region 采样与间隔抖动交替取数）——
同 seed + 同 region + 同屏幕分辨率下序列逐次一致（即必复现）。生效 seed 解析顺序：
`launch.Seed`（`ScenarioContext.Seed`）> `params.seed` > 0。seed 值随开始日志打印，失败后可回放分析
JSONL 流水（每条含坐标）定位问题操作。注意：改 `actionsPerSecond`/`durationSec` 不改变坐标序列本身，
但会改变实际执行到的子序列长度与节拍。

## 输入注入（AgentInput）

> **前提**：坐标注入（Input System 主路径）要求工程 Player Settings 的 `activeInputHandler` 为
> **Both** 或 **Input System Package (New) only**——旧 Input Manager only 时队列事件不会被派发。

后端经 `Agent/Tools/AgentInputBackends.cs` 反射发现（主 Agent 程序集对输入包**零硬引用**；两个后端均为
可选守护程序集，未装包即整体不编译，见包 README「包定位」）：

- **主路径**（工程装有 Input System ≥1.0 → `AxuTestFramework.Agent.Input` 程序集编译）：
  `InputSystem.QueueStateEvent` 注入 move → down → up 三事件（`MouseState.WithPosition/WithButton`，
  鼠标设备）；无鼠标设备（触屏形态）退 `Touchscreen.current` 注入 `TouchState` 的
  Began → Moved → Ended（固定 touchId=1）。事件在下一次 Input System update 生效。命中与否由引擎
  事件系统自行分派，与 UGUI raycast 检测互不干扰。
- **降级**（未装 Input System，或两形态设备均缺；工程装有 uGUI → `AxuTestFramework.Agent.UGUI` 程序集编译）：
  对 UGUI raycast 命中对象 `ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>` 精确点按；未命中仅记录坐标。
  降级发生时出**一次性** info finding `MONKEY_INPUT_FALLBACK`。
- **均不可用**（两个包都未装）：仅记录坐标，不产生输入事件，出一次性 info finding 说明缺失依赖。
- UGUI 命中检测（`EventSystem.RaycastAll`）仅用于流水留痕（hit 对象名）与降级执行。

## 卡死检测语义

监控 `Time.frameCount`：主循环逐帧轮询（真实墙钟 `Time.realtimeSinceStartupAsDouble` 驱动，
**不使用 WaitForSecondsRealtime**——其等待契约在本工程环境不可靠，见 `AgentWait` 实现说明）。
若 frameCount 连续 `hangThresholdSec` 秒（真实墙钟）无推进 → 截图
`<scenarioId>_monkey-terminate.png` + Error finding `MONKEY_HANG_DETECTED`（"疑似卡死"）+ 终止，
scenario 置 error。注意：若主线程完全死锁，本协程同样无法推进，此检测覆盖的是
"帧不推进但协程调度仍存活"的半卡死形态（如死循环外的长阻塞、单帧超长）；
完全死锁由宿主级看门狗（真实超时）兜底。

## 错误监视

`AgentLogWatch`（订阅 `Application.logMessageReceivedThreaded`，Error/Exception/Assert 计数）：
- `stopOnError=true`：首错即截图 + Error finding `MONKEY_ERROR_DETECTED`（含最近错误快照）+ 终止；
- `stopOnError=false`：仅累计计数，结束时随 `monkey.errorsTotal` 产出，不影响判定。

## JSONL 流水格式（artifacts/`<scenarioId>-monkey-actions.jsonl`）

每行一条操作（UTF-8，AutoFlush 逐条落盘，异常退出保住前文）：

```json
{"seq":1,"t":0.482,"pos":[743,391],"hit":"BtnStart"}
{"seq":2,"t":0.977,"pos":[120,655],"hit":null}
```

| 字段 | 说明 |
|---|---|
| `seq` | 操作序号（从 1 起） |
| `t` | 距运行开始的秒数（保留 3 位小数） |
| `pos` | 屏幕像素坐标 `[x,y]`（左下原点，取整） |
| `hit` | UGUI raycast 命中对象名；未命中为 `null` |

## 产出的指标键

| 键 | 说明 |
|---|---|
| `monkey.actionsTotal` | 实际注入的操作总数 |
| `monkey.errorsTotal` | 运行窗口内 Error/Exception/Assert 日志总数 |
| `monkey.durationSec` | 实际运行秒数（含提前终止时短于 durationSec） |

（经 `AxuMetrics.Gauge` 打点，报告中呈现为 `custom.gauge.monkey.*.avg/.max`。）

## 平台备注

- 场景前置条件同 perf：`scene` 须已加入 Build Settings/Build Profile 场景清单；
- Input System 主路径要求存在 Mouse 或 Touchscreen 设备（Editor/桌面常规均满足）；
- 截图依赖 GameView 渲染输出，未落盘仅告警不失败；
- Player/IL2CPP：输入后端由可选守护程序集按包存在性在编译期裁剪（未装包即不编译），主 Agent 程序集零输入包硬引用，任意组合均可构建。
