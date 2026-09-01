# Scenario 示例（Samples~/Scenarios）

能力示例 suite 集合。经 Package Manager → AxuTestFramework → Samples → **Scenarios** 导入后，
JSON 会落到 `Assets/Samples/com.cielevel.axu-test-framework/<版本>/Scenarios/`，可直接改动为你的工程内容。

## 导入后最快跑通（接入一页纸）

1. 菜单 `AxuTestFramework → Enable Test Agent` 开启 `AXU_TEST_AGENT` define；
2. 菜单 `AxuTestFramework → Run Suite JSON...` 选择导入的 `echo.example.suite.json`，进 Play 自动执行；
3. 结束后 Console 会打印报告路径（Editor 下在 `Library/AxuTestReports/scenario/`），或用
   `AxuTestAgent.RunSuite("<suite.json 路径>")` 代码触发。

## 文件清单

| 文件 | 能力 | 说明 |
|---|---|---|
| `echo.example.suite.json` | echo | 链路自检：任意工程零改动可跑 |
| `perf.example.suite.json` | perf | 不指定 `scene` = 度量当前场景（warmup → measure → 预算判定） |
| `loadtime.example.suite.json` | loadtime | 场景加载矩阵；导入后把 `scenes` 换成你工程的场景路径 |
| `smoke.example.suite.json` | smoke | 遍历 Build Settings 场景（`source: "buildSettings"`），进得去 / 错误数 / 截图 |

## 常用参数速记

- 通用保留键：`scene` / `warmupSeconds` / `measureSeconds` / `timeScale` / `vsync` /
  `targetFrameRate` / `qualityLevel` / `resolution` / `seed` / `sampleIntervalMs` / `collectors` / `timeoutSec`
- 预算：`{"metric": "fps.avg", "op": ">=", "value": 30}`，键为 `<collector>.<stat>` 两段式；
  无 `budgets` 的 scenario 会标 `unbudgeted: true`（防"没人写阈值所以永远绿"）
- 完整 schema 见包内 `DOC/ScenarioSchema.md`；各能力专属参数见 `Agent/Capabilities/<能力>/DOC.md`
