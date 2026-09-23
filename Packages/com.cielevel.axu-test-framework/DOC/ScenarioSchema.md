# Scenario Schema（v2 场景测试数据模型）

> **状态：草案（0.2.0，Phase 0 落稿）**——本文档定义的 schema 是场景测试平台的数据契约；
> JSON 读写实现于 `AxuTestFramework.Core`（Phase 1），预算判定实现于 Phase 2，能力运行器实现于 Phase 3。
> 字段以本文档为准，实现必须与本文档同步更新。

三个 schema 构成数据链路：

```
axu-suite/1 ──引用──▶ axu-scenario/1 ──运行产生──▶ axu-scenario-report/1
（一次批量计划）        （单个测试情景定义）            （结构化结果 + 预算判定）
```

通用约定：

- JSON 编码 UTF-8，无 BOM；时间一律 UTC ISO 8601；
- 所有 schema 带 `schema` 字段做自描述，读取时校验，不认识的**附加字段忽略**（向前兼容）；
- `id` 全小写点分（`perf.combat.fhd`）， suite 内唯一；指标键为 `<collector>.<stat>` 两段式（stat 名即报告 metrics 内层键，如 `fps.avg`、`frameTime.cpuP95`、`memory.allocatedMax`）。

---

## 1. axu-scenario/1 —— 单个测试情景

```jsonc
{
  "schema": "axu-scenario/1",
  "id": "perf.combat.fhd",
  "capability": "perf",                      // 必填；须是注册表中的 capability id
  "title": "战斗场景 1080p 基准",              // 可选，展示用
  "enabled": true,                           // 可选，默认 true；false 时 suite 跳过
  "params": { /* 各 capability 自定参数，见第 4 节通用参数 */ },
  "budgets": [                               // 可选；无 budgets 时结果标 unbudgeted=true
    { "metric": "fps.avg",             "op": ">=", "value": 45 },
    { "metric": "frameTime.cpuP95",    "op": "<=", "value": 25,  "unit": "ms", "severity": "warn" },
    { "metric": "memory.allocatedMax", "op": "<=", "value": 2048, "unit": "MB" }
  ],
  "baseline": "baselines/combat-fhd.json"    // 可选；相对 suite 文件的路径，存在则自动 diff
}
```

### budgets 规则

| 字段 | 必填 | 说明 |
|---|---|---|
| `metric` | ✔ | 指标键，须为本次运行实际产出的指标 |
| `op` | ✔ | `>=` / `<=` / `==` / `range`（range 时用 `valueMin`/`valueMax` 替代 `value`） |
| `value` | ✔（range 除外） | 阈值 |
| `unit` |  | 展示单位（`ms`/`MB`/`fps`…），不参与换算——写入方负责单位一致 |
| `severity` |  | 违约判定级别，`fail`（默认）/ `warn` |

scenario 总判定 = 最严 budget 结果（fail > warn > pass）。

指标键 = `<collector>.<stat>` 两段式（stat 名与报告 metrics 内层键一致）。custom 打点通道的 stat 名本身可含点，
此时预算键按第一个 '.' 分段解析，如 `custom.scope.AI.Tick.p95`、`custom.gauge.Room.EnemyCount.max`。

---

## 2. axu-suite/1 —— 批量运行计划

```jsonc
{
  "schema": "axu-suite/1",
  "id": "nightly-perf",
  "title": "每夜性能套件",
  "scenarios": [
    "perf.combat.fhd",                        // 字符串形式：引用同文件内联定义或已注册场景
    { "ref": "perf.combat.fhd", "profile": "low-end" }   // 对象形式：引用 + 环境档扇出
  ],
  "profiles": {                               // 可选；环境档 = 对 scenario.params 的深合并覆盖
    "low-end": { "resolution": [1280, 720], "qualityLevel": 0 }
  },
  "settings": {
    "stopOnFailure": false,                   // 某 scenario fail 后是否中断剩余
    "warnAsFail": false,                      // warn 计入 fail（影响退出码）
    "scenarioTimeoutSec": 600                 // 单 scenario 看门狗超时
  }
}
```

扇出规则：`ref + profile` 生成派生 scenario，`id` 追加档名后缀（`perf.combat.fhd@low-end`），报告独立成条。

`ref` 的解析来源（按优先级）：

1. **本文件内联定义**——`scenarios[]` 里带完整对象的条目（同 id 首条生效）；
2. **进程内注册的代码态 scenario**——游戏侧经 `AxuTestAgent.RegisterScenario(ScenarioDef.FromCode(...))` 注册
   （见 `Agent/Capabilities/GameHook/DOC.md`），同 id 时覆盖内联定义。注册只发生在运行时，因此
   **Editor 侧的静态发现/预检不解析纯 `ref`**：`axu_scenario_list` 照常列出这类 suite 并给出 `unresolvedRefs`
   与 `note`，看板/菜单/`axu_scenario_run` 的启动前预检也放行（进 Play 后由 Agent 解析）；
3. 两处都找不到 → **明确报错**（`… 既未在 suite 内联定义中、也不在已注册的代码态 scenario 清单中`），
   由宿主写成 `status=error`（`reason=suite_load_failed`），而不是静默跳过。

> 纯 `ref` 的 suite 文件（无任何内联定义）是受支持的形态，工程内示例：
> `Assets/Tests/Scenarios/gamehook-ref-demo.suite.json`（引用 `GameHookE2eRegistration.cs` 注册的 `hook.fromcode`）。

---

## 3. axu-scenario-report/1 —— 运行报告

```jsonc
{
  "schema": "axu-scenario-report/1",
  "suiteId": "nightly-perf",
  "startedAt": "2026-08-31T12:00:00Z",
  "endedAt": "2026-08-31T12:05:00Z",
  "environment": {
    "unityVersion": "6000.0.23f1", "agentVersion": "0.2.0",
    "isEditor": false, "developmentBuild": true,
    "deviceModel": "iPhone 14 Pro", "os": "iOS 18.5",
    "graphicsDevice": "Apple A16", "graphicsApi": "Metal",
    "systemMemoryMB": 6144, "qualityLevel": 2, "resolution": [2556, 1179],
    "buildGuid": "…"
  },
  "summary": { "total": 3, "passed": 2, "warned": 1, "failed": 0 },
  "exitCode": 0,                               // 0=全 pass；1=有 fail；2=仅 warn；3=基础设施错误
  "results": [
    {
      "scenarioId": "perf.combat.fhd", "capability": "perf", "status": "warn",
      "durationMs": 71234, "unbudgeted": false,
      "metrics": {                             // 每指标聚合值（p50/p95/min/max/avg/slope 按采集器能力）
        "fps": { "avg": 46.2, "p50": 47.9, "p95": 41.0, "min": 33.5 },
        "frameTime": { "cpuP95": 24.8, "gpuP95": 26.1 },
        "memory": { "allocatedMax": 1832.4, "monoUsedMax": 210.7 }
      },
      "budgets": [
        { "metric": "fps.avg",          "op": ">=", "threshold": 45, "actual": 46.2, "verdict": "pass" },
        { "metric": "frameTime.cpuP95", "op": "<=", "threshold": 25, "actual": 26.1, "unit": "ms", "verdict": "warn" }
      ],
      "baselineDiff": [                        // 有 baseline 且对比成功时存在；条目语义见 §5
        { "metric": "fps.avg", "base": 48.1, "current": 46.2, "deltaRatio": -0.0395, "verdict": "pass" }
      ],
      "series": { "dir": "series/perf.combat.fhd/", "files": ["fps.csv", "memory.csv"] },
      "artifacts": [ "artifacts/perf.combat.fhd_end.png" ],
      "log": { "errorCount": 0, "warningCount": 3, "samples": [] },
      "findings": [
        { "severity": "warn", "code": "BUDGET_WARN", "message": "frameTime.p95=26.1ms > 25ms" }
      ]
    }
  ]
}
```

`results[].status` 取值：`pass` / `warn` / `fail` / `error`（error = scenario 执行异常，区别于预算 fail）。

---

## 4. 通用 params（各 capability 共享的保留键）

| 键 | 类型 | 默认 | 说明 |
|---|---|---|---|
| `scene` | string | 无 | 目标场景路径；**须已加入 Build Settings/Build Profile 场景清单**（Unity 6 起 Editor 与 Player 一致校验，Unity 6000.0.23f1 实测；Editor 曾有"任意 `Assets/…` 路径可载"的说明在本版本不成立） |
| `warmupSeconds` | float | 0 | 预热期，期间不采样 |
| `measureSeconds` | float | 按 capability | 度量期时长 |
| `timeScale` | float | 1.0 | `Time.timeScale` 覆盖 |
| `vsync` | int | 不改 | 覆盖 `QualitySettings.vSyncCount` |
| `targetFrameRate` | int | 不改 | 覆盖 `Application.targetFrameRate` |
| `resolution` | [w,h] | 不改 | 覆盖分辨率 |
| `qualityLevel` | int | 不改 | 覆盖 `QualitySettings.selectedQualityLevel` |
| `seed` | int | 0 | 随机种子（0 = 时间派生）；写进报告便于复现 |
| `sampleIntervalMs` | int | 250 | 采样聚合间隔（宿主逐帧累计驱动采集器 OnInterval） |
| `collectors` | string[] | capability 默认 | 启用的采集器清单；Phase 2 宿主缺省启用全部 5 个（fps/frameTime/memory/rendering/gc），custom 打点通道恒可用、无需配置 |
| `timeoutSec` | float | suite.settings | scenario 级超时覆盖 |

> capability 专属参数（如 stress 的 `phases`、soak 的 `durationMinutes`）在各能力的 DOC 中定义，
> 保留键之外的键由 capability 自行解释，未知键忽略。

---

## 5. axu-baseline/1 —— 指标基线

单 scenario 的指标基线文件。`-axuBaselineSave <path>` 运行收尾时为每个 scenario 写
`<path目录>/<scenarioId>.baseline.json`（`<path>` 给目录或文件路径均可，取其目录分量）；
scenario 的 `baseline` 字段（相对 suite/scenario 文件的路径）指向基线文件后，每次运行自动 diff
并把 `baselineDiff[]` 写入报告（fail → Finding `BASELINE_REGRESSION` 并升 status 为 fail，warn → `BASELINE_DRIFT` 升 warn）。

| 字段 | 类型 | 说明 |
|---|---|---|
| `schema` | string | 恒 `axu-baseline/1`，读取时校验（缺失/不符抛 AxuSchemaException） |
| `scenarioId` | string | 基线所属 scenario id |
| `createdAtUtc` | string | 生成时间（UTC ISO 8601；Save 时空缺自动补当前时间） |
| `metrics` | object | 扁平两段键（`<collector>.<stat>`，如 `fps.avg`）→ 聚合值 |

最小示例：

```json
{
  "schema": "axu-baseline/1",
  "scenarioId": "perf.combat.fhd",
  "createdAtUtc": "2026-09-01T12:00:00.000Z",
  "metrics": {
    "fps.avg": 46.2,
    "frameTime.cpuP95": 24.8,
    "memory.allocatedMax": 1832.4
  }
}
```

对比分级（`BaselineDiff.Compare`，双侧对称，方向性判定留后续阶段按采集器元数据细化）：
`|deltaRatio| ≤ 0.10 → pass`；`0.10 < |deltaRatio| ≤ 0.25 → warn`；`> 0.25 → fail`（阈值可配，边界含端点）。
基线值为 0 时退化为绝对差分支：同一阈值作用于 `|current - base|`。
仅比较两侧都存在的键，基线独有键忽略；报告 metrics 中基线没有的键不产生条目。
