# stress —— 压力/承载（方案 §4.4）

组合能力：`params.phases` 声明要顺序执行的压力工具，Runner 逐个执行；
每个 phase 结束后（若还有后续）挂起采样（`MetricSession.Suspend`）→ 清场 → 真实时间等 0.5s → 恢复采样，
使各 phase 的指标段不被上一 phase 的销毁尖峰污染。指标由 runner 自有的 `StressCollector`
（挂入会话，id = `stress`）+ 宿主 `collectors`（建议 fps/gc/memory）共同产出。

## 参数表（scenario.params）

| 键 | 类型 | 默认 | clamp | 说明 |
|---|---|---|---|---|
| `phases` | array | `[]` | ≥1 项 | phase 列表（空 → Error finding `STRESS_NO_PHASES`）；每项含 `tool` + 该工具参数（下表平铺，未用到的忽略） |

### phase 公共 / 工具参数

| 键 | 用于 | 默认 | clamp | 说明 |
|---|---|---|---|---|
| `tool` | 全部 | `"spawner"` | — | 工具名：`spawner` / `capacity` / `churn` / `flood`（未知 → Error finding `STRESS_UNKNOWN_TOOL`，立即终止） |
| `prefabPath` | spawner/capacity/churn | `""` | — | **Resources 相对路径**（不含 `Resources/` 前缀与扩展名，如 `StressTarget`）。**空 = 内置 Primitive Cube 替身**（模板去 Collider、网格散布，无任何游戏资产也能跑）。**Player 侧只能加载随构建打包的 Resources 资产**——资源不在 Resources 目录的 prefab 请先入 Resources，或直接用替身；路径加载失败记 Warning 回落替身（不 Error） |
| `startCount` | spawner/capacity | `100` | 1..50000 | 起始实体数 |
| `rampStep` | spawner/capacity | `100` | 1..50000 | 每档增量 |
| `rampIntervalSec` | spawner/capacity | `1` | 0..600 | 每档加压后的间隔秒（真实时钟） |
| `maxCount` | spawner/capacity | `10000` | 1..100000 | 实体数上限（spawner 终点 / capacity ramp 上界） |
| `settleSec` | spawner/capacity | `2` | 0..600 | 加压后稳定等待秒 |
| `targetFps` | capacity | `60` | 1..1000 | 目标帧率阈值 |
| `belowThresholdSec` | capacity | `2` | 0.5..60 | fps 低于阈值持续该秒数 → 判超载 |
| `convergeTolerancePercent` | capacity | `5` | 1..50 | 二分收敛精度（相对区间宽度百分比） |
| `createPerSec` | churn | `50` | 1..10000 | 每秒 Create/Destroy 的对象数 |
| `durationSec` | churn/flood | `10` | 0.1..3600 | 持续秒数 |
| `eventsPerFrame` | flood | `1000` | 1..10^6 | 每帧发布事件数 |
| `subscribers` | flood | `8` | 1..256 | 自建事件中心的订阅者数 |

另：通用保留键与 `collectors` / `sampleIntervalMs` / `timeoutSec` 由宿主解析（见 DOC/ScenarioSchema.md §4）。

## 容量探测算法（capacity）

1. **ramp**：`startCount` 起每档 `+rampStep`（等 `rampIntervalSec`），用 60 帧滑动窗口 fps（真实时钟帧间隔）
   判超载——窗口 fps 连续低于 `targetFps` 达 `belowThresholdSec` → 该档为 firstBad；回稳到阈值之上持续 0.5s
   → 未超载，记录 (count, fps) 曲线采样并继续加压，直至超载或到达 `maxCount`（未超载到达上限时
   `capacity.maxEntities = maxCount`，为下界而非精确容量，曲线/日志可辨）。
2. **二分收敛**：在 [lastGood, firstBad] 区间二分，每档重设实体数 → 等 `settleSec` → 同判据定向，
   区间相对宽度 ≤ `convergeTolerancePercent`（步数硬上限 32 防边界抖动）时收敛，lastGood 即
   `capacity.maxEntities`；收敛点再观察一窗记录 `capacity.verifiedFps`（日志）。
3. 边界防御：观察期连 60 帧窗口都填不满（fps 病态低）视同超载；全部取消逐帧响应。

### 曲线产物（artifacts）

- `artifacts/{scenarioId}_capacity_curve.csv`（count,fps 每行一采样，按探测顺序含二分档）
- `artifacts/{scenarioId}_spawner_curve.csv`（spawner phase 的承载曲线，同格式）

## 产出指标键（collector = "stress"）

| 键 | 聚合 | 说明 |
|---|---|---|
| `stress.phase.<i>.<tool>.durationMs` | Last | 各 phase 耗时（指标段） |
| `stress.phase.count` | Sum | 实际执行的 phase 数 |
| `stress.spawner.maxCount` | Last | spawner/capacity phase 的峰值并发实体数 |
| `stress.spawner.instantiated.total` | Sum | 累计实例化数 |
| `stress.churn.created.total` / `stress.churn.destroyed.total` | Sum | churn 翻搅产出/销毁总量 |
| `stress.churn.peakPerTick` | Max | 单秒批次峰值 |
| `stress.flood.events.total` | Sum | flood 事件发布总量 |
| `stress.flood.eventsPerSec` | Last | 峰值每秒事件数 |
| `stress.flood.publishMs.max` | Max | 单帧发布最大耗时（ms） |
| `stress.capacity.maxEntities` | Last | 目标帧率下最大实体数（版本间对比直接量化"优化了多少"） |
| `stress.capacity.samples` | Sum | 曲线采样数 |

### budget 示例

```json
"budgets": [
  { "metric": "stress.capacity.maxEntities", "op": ">=", "value": 3000 },
  { "metric": "stress.flood.publishMs.max",   "op": "<=", "value": 2, "unit": "ms", "severity": "warn" }
]
```

## EventFlood 自建事件吞吐说明

`flood` 工具**不依赖任何 AxuFrameKit.* 事件中心**（测试基建不耦合被测框架），压的是自建
`ManagedEventHub`：N 订阅者的数组发布列表（顺序遍历、无 LINQ/闭包/每帧分配——payload 为复用实例），
订阅者做字段累加（副作用不可被 IL2CPP 消除）。语义为**托管事件洪峰吞吐基准**：每帧发布
`eventsPerFrame` 次 × `subscribers` 回调，度量峰值 events/s 与单帧发布耗时；配合宿主 fps 采集器
可观察洪峰对帧率的挤压。要与游戏自身事件系统对比时，请在游戏侧用 `AxuMetrics` 打点做 A/B。

## 平台备注

- **性能类能力须带渲染窗口**（`-batchmode -nographics` 下 fps/capacity 无意义）；
- `prefabPath` 走 `Resources.Load`：Editor 与 Player 语义一致，但 Player 只能命中随构建打包的
  Resources 资产；靶场示例见 `Assets/TestAndSample/Sample_AxuTest/`（PerfBench 场景、Spin 脚本、
  StressTarget prefab——prefab 需放入 Resources 才能被 `prefabPath` 引用，或置空用内置替身）；
- 内置替身 = 去 Collider 的 Primitive Cube（网格散布）：压力来源是实例化/变换/渲染，不含物理；
  要压脚本/物理负载请用自带逻辑的 prefab（如 StressTarget + Spin）；
- churn 建议配合 `collectors: ["fps","gc","memory"]` 观察 GC 分配与健康度。
