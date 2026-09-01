# soak —— 浸泡/泄漏（方案 §4.5）

流程：对当前场景长时运行 → 每 `sampleIntervalSec` 采样（可选先 GC.Collect）→ 每 N 分钟截图留证 →
每 `flushEveryMinutes` 分钟把部分进度覆盖写为 partial report（`truncated: true`）→ 结束时泄漏/FPS 衰减判定 +
回归数据 CSV → 正常结束时删除 partial 文件（宿主随后写最终完整报告）。

## 参数表（scenario.params）

| 键 | 类型 | 默认 | clamp | 说明 |
|---|---|---|---|---|
| `durationMinutes` | float | `60` | 0.1..1440 | 浸泡时长（分钟；支持 `-axuSuite` 整夜长跑） |
| `sampleIntervalSec` | float | `30` | 1..600 | 采样间隔（真实时钟帧轮询，不用 WaitForSecondsRealtime——本 Editor 环境其等待契约失效，见 `AgentWait` 实现说明） |
| `gcCollectBeforeSample` | bool | `true` | — | 采样前 `GC.Collect`（+ WaitForPendingFinalizers + 再 Collect）：把回收波动从趋势里剥离，斜率更接近真实泄漏 |
| `screenshotEveryMinutes` | float | `0` | 0 / 0.1..duration | 每 N 分钟截图 `<scenarioId>_soak_{分:000}m.png` 进 artifacts；**0 = 不截** |
| `leakSlopeMBPerMin` | double | `5` | — | 泄漏判定阈值（MB/min），见下"判定语义" |
| `fpsDecayPercent` | float | `15` | — | FPS 衰减判定阈值（%），见下"判定语义" |
| `flushEveryMinutes` | float | `5` | 0.5..duration | partial report 刷新间隔（≤0 回落默认 5），见下"partial flush 行为" |
| `collectors` | string[] | fps/frameTime/memory/rendering/gc | — | 宿主侧采集器清单（与 soak 自有采样并存，见"产出指标"） |
| `sampleIntervalMs` | float | `250` | >0 | 宿主采集器采样间隔 |
| `timeoutSec` | float | suite 配置 | >0 | scenario 级超时覆盖（**soak 必配**：默认看门狗 600s < 60min 时长，会把正常浸泡误杀为 RUNTIME_TIMEOUT） |
| 通用保留键 | — | — | — | `timeScale`/`vsync`/`targetFrameRate`/`qualityLevel`/`resolution`/`seed`（DOC/ScenarioSchema.md §4，宿主统一应用/恢复） |

## 产出的指标键

除宿主 `collectors` 产出外，soak 每次采样经 `custom` gauge 通道追加曲线（聚合 `custom.gauge.<key>.avg/.max`，
`.max` 对累计量即最终值）：

| 键 | 含义 |
|---|---|
| `custom.gauge.soak.memory.allocatedMb` / `reservedMb` / `monoUsedMb` | 每采样点内存三项（MB） |
| `custom.gauge.soak.fps` | 每采样点瞬时帧率（`1/unscaledDeltaTime`） |
| `custom.gauge.soak.errors.total` | 错误累计曲线（`AgentLogWatch` 口径，Error/Exception/Assert） |

artifacts 产物：

| 文件 | 说明 |
|---|---|
| `<scenarioId>_soak_memory.csv` | 回归数据：`timeMin,allocatedMb,reservedMb,monoUsedMb,fps` 逐采样行（泄漏 finding 的取证数据） |
| `<scenarioId>_soak_{分:000}m.png` | 周期截图（`screenshotEveryMinutes > 0` 时） |

## 判定语义（Finding）

| code | severity | 条件 |
|---|---|---|
| `SOAK_MEMORY_LEAK_SUSPECT` | warn | `memory.allocated` 最近 ≤20 个样本滑窗最小二乘斜率（`MetricStats.SlopePerSecond`，复用 Core 统计）× 60 > `leakSlopeMBPerMin`（MB/min）。warn 不 fail：斜率敏感于缓存预热/场景流式加载，需结合 CSV 曲线人工复核 |
| `SOAK_FPS_DECAY` | warn | FPS 样本（每半程 ≥3 个）后半程 p50 比前半程低超 `fpsDecayPercent`%（性能随时间退化的信号：缓存膨胀/降频/泄漏连带） |
| `SOAK_FPS_DECAY_SKIPPED` | info | FPS 样本不足（`durationMinutes`/`sampleIntervalSec` 组合过短），跳过衰减判定 |
| `SOAK_CSV_WRITE_FAILED` | info | 回归 CSV 写盘失败（不影响判定） |
| `SOAK_PARTIAL_FLUSH_SKIPPED` | info | 未能从状态文件解析报告路径，本次未做 partial flush（无 artifacts/状态文件异常环境） |

soak 本体**不产生 error/fail**：结果状态由 findings（warn）与 budgets 共同决定；崩溃/超时由宿主看门狗定稿为
error，此时 partial 文件保留。

## partial flush 行为（风险 #9 / §9-9）

- 报告路径/suiteId 从运行状态文件（`AgentPaths.StatusFilePath`，宿主每 scenario 收尾刷新）**只读**解析；
- partial 文件 = 正式报告同目录、文件名插 `.partial` 后缀（`{name}.partial.json`），**覆盖写**；
- 内容与最终报告同一 schema（`axu-scenario-report/1`），额外带 **`"truncated": true`**（`ScenarioReport.Truncated`，
  完整报告该字段省略）；当前 scenario 条目 `status: "running"`，含实时 metrics 汇总、错误计数、已落盘 artifacts
  与 findings 快照；序列化走 `ScenarioReportWriter.WriteReport` 同一路径；
- 正常结束：runner 写完回归 CSV 后**删除 partial 文件**，宿主随后写最终完整报告；
- 崩溃/看门狗超时/取消中断：协程被打断，最后一次 flush 的 partial 文件**留在盘上**，前段数据不丢。
