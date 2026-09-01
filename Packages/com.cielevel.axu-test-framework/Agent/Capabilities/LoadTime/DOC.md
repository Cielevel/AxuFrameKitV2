# loadtime —— 加载时长（方案 §4.3）

场景矩阵模式：`scenes[]` 逐个 `LoadSceneAsync`（Single 模式，三级回退解析），
记录 **loadMs（加载完成）/ firstFrameMs（再等一渲染帧）/ stableMs（达到稳定判据）**，均自该场景加载起点计时。

## 参数表（scenario.params）

| 键 | 类型 | 默认 | clamp | 说明 |
|---|---|---|---|---|
| `scenes` | string[] | 必填 | — | 场景路径清单；**空 → Error finding `LOADTIME_NO_SCENES`，scenario 置 error** |
| `targetFps` | float | `30` | 1..1000 | 稳定判据目标帧率（每帧 `unscaledDeltaTime ≤ 1/targetFps`） |
| `stableFrames` | int | `30` | 1..10000 | 稳定判据连续帧数 |
| `stabilityTimeoutSeconds` | float | `30` | 1..600 | 稳定判定超时；超时 → **Warning finding `SCENE_NOT_STABLE`**，stable=false（stableMs 记超时值） |
| `timeoutSec` | float | suite 配置 | >0 | scenario 级超时覆盖（宿主解析） |
| 通用保留键 | — | — | — | 见 DOC/ScenarioSchema.md §4（宿主统一应用/恢复） |

- 单场景加载失败 → **Error finding `LOAD_FAILED`**（含场景名），跳到下一场景；**最终 status=fail**（无失败则 pass）。
- 失败场景不落 EndScene（无采样即不产出该场景的 stat），`loadMs.max/.avg`/`sceneCount` 只含成功场景。

## 产出的指标键（collector = `loadtime`）

| 键 | 聚合 | 说明 |
|---|---|---|
| `loadtime.scene.<safe>.loadMs` | Last | 该场景加载完成耗时（ms） |
| `loadtime.scene.<safe>.firstFrameMs` | Last | 该场景加载完成 + 一渲染帧耗时（ms） |
| `loadtime.scene.<safe>.stableMs` | Last | 该场景达到稳定判据耗时（ms；未稳定=超时值） |
| `loadtime.sceneCount` | Sum | 成功记录的场景数 |
| `loadtime.loadMs.max` / `loadtime.loadMs.avg` | Max / Avg | 场景间加载耗时聚合 |
| `loadtime.unstableCount` | Sum | 未达稳定判据的场景数 |

`<safe>` = 场景文件名（去扩展名）经文件名清洗（非法字符 → `_`；重复名追加序号）。

**total 类聚合恒产出（含 0 值，采集器 Start 播种）**：`sceneCount` 与 `unstableCount` 即使全 0 也产出键，
是可命中预算的合法产出；per-scene stat 与 `loadMs.max/.avg` 无采样不产出（全部加载失败时缺失 → 预算按 fail 判定）。

## budget 示例

```json
"budgets": [
  { "metric": "loadtime.loadMs.max",  "op": "<=", "value": 5000, "unit": "ms" },
  { "metric": "loadtime.loadMs.avg",  "op": "<=", "value": 2500, "unit": "ms" },
  { "metric": "loadtime.unstableCount", "op": "<=", "value": 0 }
]
```

## 平台备注

- **前置条件：目标场景须已加入 Build Settings/Build Profile 场景清单**——Unity 6 起 LoadSceneAsync 在
  Editor 与 Player **一致强制校验**（Unity 6000.0.23f1 实测；Editor 曾有"任意路径可载"的说明在本版本不成立，
  当前已打开场景是唯一例外）。清单缺失时 `AgentScenes` 前置检查给出可读指引（框架不做运行时临时注册）；
- 稳定判定用 `unscaledDeltaTime`（不受 timeScale 影响）；计时用真实时钟；
- 场景加载/稳定窗期间通用采集器（fps/memory 等）仍在同一度量会话采样，可与 loadtime 指标并存使用。
