# perf —— 性能基准（方案 §4.2）

流程：加载目标场景（可选）→ 宿主应用通用参数（timeScale/vsync/targetFrameRate/qualityLevel/resolution/seed）
→ **warmup（挂起采样，丢弃）** → 度量期采样 → （可选）结束截图 → 采集器聚合 → 宿主预算判定。

## 参数表（scenario.params）

| 键 | 类型 | 默认 | clamp | 说明 |
|---|---|---|---|---|
| `scene` | string | `""` | — | 目标场景路径（三级回退解析：原样 → 去 `.unity` → 纯文件名）；**空 = 不加载，对当前场景采样**。**前置条件：场景须已加入 Build Settings/Build Profile 场景清单**。加载失败 → Error finding `PERF_SCENE_LOAD_FAILED`，scenario 置 error |
| `warmupSeconds` | float | `0` | 0..3600 | warmup 秒数（真实时钟；此期间采样挂起不写入） |
| `measureSeconds` | float | `30` | 0.1..3600 | 度量期秒数（真实时钟；逐帧响应取消） |
| `screenshot` | bool | `false` | — | 结束时截图 `<scenarioId>_end.png` 进 artifacts |
| `collectors` | string[] | fps/frameTime/memory/rendering/gc | — | 采样采集器清单（宿主解析） |
| `sampleIntervalMs` | float | `250` | >0 | 采样间隔（宿主解析） |
| `timeoutSec` | float | suite 配置 | >0 | scenario 级超时覆盖（宿主解析） |
| 通用保留键 | — | — | — | `timeScale`/`vsync`/`targetFrameRate`/`qualityLevel`/`resolution`/`seed`（见 DOC/ScenarioSchema.md §4，宿主统一应用/恢复） |

## 产出的指标键

由 `collectors` 选定的采集器产出（budget 键 = `<collector>.<stat>` 两段式）：

| 采集器 | 键 |
|---|---|
| `fps` | `fps.avg` / `fps.p50` / `fps.p95` / `fps.min` |
| `frameTime` | `frameTime.avg`（全程口径）及窗口分位（见采集器 Output 声明） |
| `memory` | `memory.allocated.max` / `memory.reserved.max` / `memory.monoUsed.max` |
| `rendering` | `rendering.drawCalls.avg` / `rendering.setPass.avg` / `rendering.tris.avg` |
| `gc` | `gc.allocPerFrame.avg` / `gc.count` |
| `custom` | 游戏侧 `AxuMetrics` 打点：`custom.counter.*` / `custom.gauge.*.avg/.max` / `custom.scope.*.avg/.p95/.max` |

## budget 示例

```json
"budgets": [
  { "metric": "fps.avg",              "op": ">=", "value": 45 },
  { "metric": "frameTime.p95",        "op": "<=", "value": 25, "unit": "ms", "severity": "warn" },
  { "metric": "memory.allocated.max", "op": "<=", "value": 2048, "unit": "MB" }
]
```

## 平台备注

- **前置条件：`scene` 指定的目标场景须已加入 Build Settings/Build Profile 场景清单**——Unity 6 起
  LoadSceneAsync 在 Editor 与 Player **一致强制校验**（Unity 6000.0.23f1 实测；Editor 曾有"任意路径可载"的
  说明在本版本不成立，当前已打开场景是唯一例外）。清单缺失时 `AgentScenes` 前置检查给出可读指引
  （框架不做运行时临时注册）；
- **resolution 仅 Player 生效**：Editor 下记 info 日志跳过（GameView 尺寸不经 `Screen.SetResolution` 控制）；
- **rendering 采集器在 Editor 不可用**（ProfilerRecorder 计数器无效，本机 Unity 6000.0.23f1 实证）——自检出
  warn 级 finding `COLLECTOR_UNAVAILABLE`，不静默 0 值；性能数据请以 Development Build Player / 真机为准；
- 性能类能力**必须带渲染窗口或真机**（`-batchmode -nographics` 下 fps/rendering 无意义）；
- 截图依赖 GameView 渲染输出；截图未落盘时仅告警不失败。
