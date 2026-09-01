# gamehook —— 游戏侧显式注册规范（方案 §4.8 / D7 决策）

框架不依赖游戏、游戏按需注册——依赖方向永远向外。本期（P1）只提供**显式注册 API**；
`[AxuScenario]` Attribute 扫描烘焙不做（D7 兜底决策）。注册全部为 IL2CPP 安全路径：
无运行时反射扫描、无 UnityEditor 引用，Player / IL2CPP / stripping 可编译可执行。

## 注册时序（硬要求）

注册表是**进程内非持久化**静态字典——域重载（含 Editor 进 Play）即清空，须每次启动重新注册：

```csharp
public static class MyGameTestRegistration
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        AxuTestAgent.RegisterCapability(new HookSampleCapability());      // 自定义能力
        AxuTestAgent.RegisterScenario(ScenarioDef.FromCode(               // 同步 setup 形态
            id: "game.hook-smoke", capability: "hook.sample",
            setup: ctx => { /* 分发前的同步准备 */ },
            parameters: new Dictionary<string, object> { ["message"] = "hello" }));

        AxuTestAgent.RegisterScenario(ScenarioDefExtensions.FromCodeRoutine( // 协程准备形态
            id: "game.hook-perf", capability: "perf",
            prepare: () => PrepareBossFight(),                              // Func<IEnumerator>
            parameters: new Dictionary<string, object> { ["durationSec"] = 30 }));
    }
}
```

- 推荐挂 `RuntimeInitializeLoadType.AfterSceneLoad`：宿主在 BeforeSceneLoad 启动 suite 协程，
  但协程首步即让出一帧——suite 解析（注册表快照）发生在 AfterSceneLoad 之后的首帧，
  挂 AfterSceneLoad 的注册必然先于解析被看见；亦早于任何 suite 运行触发（桥文件 / 命令行 / `AxuTestAgent.RunSuite`）。
- 经 `AxuTestAgent.RunSuite`（Editor 桥 / 控制台）触发时，只需保证注册先于该调用。
- 所有注册 API 以 `AXU_TEST_AGENT` 守护：define 关闭时为空操作，游戏代码无需 `#if` 包裹。

## suite 侧引用（按 id 合入）

suite 解析（`SuiteLoader.Resolve`）时注册项按 id 参与，规则：

| suite 条目 | 行为 |
|---|---|
| `{"ref": "game.hook-perf"}`（无内联定义） | **补充**：用注册的定义展开执行 |
| 内联 scenario 与注册项同 id | **覆盖**：注册定义优先（params/budgets/setup 全量替换，profile 仍在其上深合并） |
| 未被 suite 引用 | 不执行（注册 ≠ 运行；运行仍由 suite/ref 驱动） |

同 id 重复注册：**后者覆盖**并记 warning（能力注册表同语义，游戏可借此替换内建能力实现）。

## FromCode 两种形态

| API | 层 | 执行时机 | 适用 |
|---|---|---|---|
| `ScenarioDef.FromCode(id, capability, Action<ScenarioContext>, IDictionary<string,object>)` | Core（纯委托） | 能力分发前**同步**调用一次 | 纯 C# 准备（改静态量、塞参数、置标记） |
| `ScenarioDefExtensions.FromCodeRoutine(id, capability, Func<IEnumerator>, IDictionary<string,object>)` | Agent | 拼在能力 Runner 协程**之前**、同一超时看门狗内 | 需要等待的准备（加载场景/资源/等帧），等待用 `AgentWait.RealtimeSeconds` |

- 两形态可并存于同一注册体系，但单个 ScenarioDef 只挂一份委托（`CodeSetup` / `CodePrepare` 透传到 ResolvedScenario）。
- `setup` 抛异常 → scenario 置 Error（finding `RUNTIME_EXCEPTION`），不进入能力 Runner；
  `prepare` 协程与 Runner 同受 `scenarioTimeoutSec` 看门狗与取消约束。
- 返回的 `ScenarioDef` 可继续设置 `Budgets`/`Baseline`/`Title`/`Enabled`；`parameters` 深转 JObject，与 JSON `params` 等价。

## 生命周期与幂等

- 注册表随进程/域存活；`AxuScenarioRegistry.Clear()` / `AxuCapabilityRegistry.Clear()` 仅供测试清理。
- 重复注册幂等性：同 id 后者覆盖（warn）；`BuiltInCapabilities.RegisterAll()` 自身幂等，不会反向覆盖游戏注册。
- 注册的委托由宿主在**每次执行**该 scenario 时调用（`prepare` 工厂每执行一次调用一次），委托内不要假设只跑一次。

## HookSampleCapability

本目录 `HookSampleCapability.cs` 是**游戏侧自定义能力模板**（id `"hook.sample"`），**不在内建注册之列**——
复制到游戏工程改造后经 `RegisterCapability` 接入；最小闭环：参数 POCO + Runner 读参数 + 日志 + 交回宿主判 Passed。
