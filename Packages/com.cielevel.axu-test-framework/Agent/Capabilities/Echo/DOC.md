# echo 能力（链路自检）

最小执行体，用于在**不依赖任何游戏逻辑**的前提下验证 Agent → Runner → 采集 → 预算 → 报告整条链路。
也是新工程接入场景测试平台后的首选首跑能力（见包 README「场景测试接入一页纸」）。

## 行为

1. 打一条 `message` 日志；
2. 分 `steps` 步等待，每步 `delaySeconds` 秒（真实时间）；
3. 期间宿主默认采集器照常采样（fps/frameTime/memory/rendering/gc），`custom.scope.echo.*` 记录单场景耗时。

## 参数（`params`）

| 参数 | 类型 | 默认 | 说明 |
|---|---|---|---|
| `message` | string | `"hello"` | 开始时打印的日志内容 |
| `delaySeconds` | float | `1.0` | 单步等待秒数（真实时间，非缩放时间） |
| `steps` | int | `1` | 等待步数；总时长 ≈ `steps × delaySeconds` |

通用保留键（`scene`/`timeoutSec`/`seed` 等）由宿主统一应用，见 `DOC/ScenarioSchema.md` §4。

## 指标与预算示例

echo 自身除 `custom.scope.echo.{avg,p95,max}`（毫秒，场景总耗时）外不产出专属指标；
给 echo 配预算的意义在于验证预算链路本身：

```json
"budgets": [
  { "metric": "fps.avg", "op": ">=", "value": 15 },
  { "metric": "custom.scope.echo.p95", "op": "<=", "value": 3000, "unit": "ms", "severity": "warn" }
]
```

## 备注

- 无 `budgets` 时结果标 `unbudgeted: true`（pass，但提示"没人写阈值"）。
- echo 运行极短（默认 1s），指标样本量小，**不适合作为基线比对载体**——微场景 run-to-run 噪声
  容易超基线偏差阈值（低样本场景的正常表现，非链路缺陷）。
