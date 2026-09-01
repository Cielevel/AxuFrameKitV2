# smoke —— 冒烟矩阵（方案 §4.6）

清单来源 → 逐场景：日志监视加载 → 稳定窗 → 截图留证 → 记行。产出"场景 × {进得去?、耗时、错误数、截图}"矩阵，
出包回归首选，也是 CI 门禁最便宜的一道闸。任一场景加载失败 → 最终 status=fail（其余照跑）。

## 清单来源（params.source，三选一）

| source | 说明 |
|---|---|
| `list`（默认，未知值归此） | 用 `scenes[]` 白名单 |
| `buildsettings` | Build Settings 全部已启用场景（`SceneManager.sceneCountInBuildSettings`） |
| `directory` | 扫描 `directory` 下 `*.unity`（递归），转为 `Assets/...` 相对路径；**仅 Editor 可用**——Player 下 → Error finding `SMOKE_DIRECTORY_UNAVAILABLE`，scenario 置 error |

清单解析为空 → Error finding `SMOKE_NO_SCENES`，scenario 置 error。

## 参数表（scenario.params）

| 键 | 类型 | 默认 | clamp | 说明 |
|---|---|---|---|---|
| `source` | string | `list` | — | 清单来源（见上表） |
| `scenes` | string[] | `[]` | — | source=list 的场景白名单 |
| `directory` | string | `""` | — | source=directory 的扫描目录（仅 Editor） |
| `stabilitySeconds` | float | `1` | 0..60 | 稳定窗时长（此窗口内 Error/Exception/Assert 计入该场景 errors） |
| `stabilityFrames` | int | `10` | 1..1000 | 稳定窗前置帧数（等其过完再 ResetCounts 起算稳定窗） |
| `screenshot` | bool | `true` | — | 逐场景截图 `<scenarioId>_<sceneSafe>.png` 进 artifacts |
| `timeoutSec` | float | suite 配置 | >0 | scenario 级超时覆盖（宿主解析） |
| 通用保留键 | — | — | — | 见 DOC/ScenarioSchema.md §4（宿主统一应用/恢复） |

- 加载失败的场景**照记行继续**（`loaded=false` 计入 `smoke.loadFailed.total`，行内 loadMs/errors 为失败前观测值），
  不产出该场景的 finding（失败本身由指标与最终 status 表达）。

## 产出的指标键（collector = `smoke`）

| 键 | 聚合 | 说明 |
|---|---|---|
| `smoke.scene.<safe>.loadMs` | Last | 该场景加载+稳定窗总耗时（ms；失败场景为失败前耗时） |
| `smoke.scene.<safe>.errors` | Last | 该场景稳定窗内错误条数（失败场景为加载期错误数） |
| `smoke.sceneCount` | Sum | 记录的场景数（含失败） |
| `smoke.loadMs.max` | Max | 场景间耗时聚合 |
| `smoke.errors.total` | Sum | 全部场景错误总数 |
| `smoke.loadFailed.total` | Sum | 加载失败场景数 |

`<safe>` = 场景文件名（去扩展名）经文件名清洗（非法字符 → `_`；重复名追加序号）。

**total 类聚合恒产出（含 0 值，采集器 Start 播种）**：`sceneCount` / `errors.total` / `loadFailed.total`
即使全 0 也产出键（可命中预算，e2e 实证全成功 suite 的 `loadFailed.total == 0` 曾因缺键误判 fail）；
per-scene stat 与 `loadMs.max` 无采样不产出（全部加载失败时缺失 → 预算按 fail 判定）。

## budget 示例

```json
"budgets": [
  { "metric": "smoke.loadFailed.total", "op": "<=", "value": 0 },
  { "metric": "smoke.errors.total",     "op": "<=", "value": 0 },
  { "metric": "smoke.loadMs.max",       "op": "<=", "value": 8000, "unit": "ms", "severity": "warn" }
]
```

## 平台备注

- **smoke directory 仅 Editor**（Player 无文件系统工程布局）；list/buildsettings 两种来源 Editor/Player 通用
  （Player 的 buildsettings = 构建清单内场景）；
- **前置条件：目标场景须已加入 Build Settings/Build Profile 场景清单**——Unity 6 起 LoadSceneAsync 在
  Editor 与 Player **一致强制校验**（Unity 6000.0.23f1 实测；Editor 曾有"任意路径可载"的说明在本版本不成立，
  当前已打开场景是唯一例外）。清单缺失时 `AgentScenes` 前置检查给出可读指引（框架不做运行时临时注册）；
  `buildsettings` 来源即枚举该清单，天然满足前置条件；
- 截图依赖 GameView / Player 渲染输出，`-batchmode -nographics` 下截图会告警缺失（不失败）；
- 错误计数经 `AgentLogWatch`（`logMessageReceivedThreaded`，全线程），稳定窗前的加载期错误不计入
  （稳定窗起算前 ResetCounts）——加载即报错的场景通常直接表现为加载失败。
