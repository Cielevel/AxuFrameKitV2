# AxuTestFramework

AxuFrameKit 体系之外的**通用测试框架包**：覆盖 EditMode 与 PlayMode，为框架包 / 模块包 / indie 包提供统一的测试工具层与结构化报告通道。**零框架依赖**（不引用任何 `AxuFrameKit.*` 程序集），可独立于本工程复用。

设计上与 [Unity Pipeline 本地 API]（`com.unity.pipeline`，见工程 `.zcode/08-unity-pipeline-api.md`）配合，形成
"Agent/人 → pipeline 驱动 → UTF 跑测试 → `[TestRunCallback]` 收结构化 JSON 报告 → 看板"的闭环；人与 AI 代理均可驱动。

## 包定位

- **Runtime 程序集 `AxuTestFramework`**：测试编写工具层（等待 / 场景 / 截图 / 日志监视 / 静态重置 / 报告模型）。
  全平台可编译，`defineConstraints: [UNITY_INCLUDE_TESTS]` 保证正常出包时**不参与编译**，运行时零影响。
- **Editor 程序集 `AxuTestFramework.Editor`**：报告落盘（`[TestRunCallback]` → `AxuTestReportWriter`）、
  pipeline 自定义命令（`axu_test_run` / `axu_test_report` / `axu_test_status`，经 TypeCache 自动注册、
  pipeline 包零改动）、UITK 看板窗口（菜单 `AxuFrameKit/AxuTestRunner`）。`autoReferenced: false`，仅 Editor 参与。
- **Samples~**：EditMode / PlayMode 各一组最小示例，经 Package Manager → Samples 导入。

## 依赖

| 依赖 | 说明 |
|---|---|
| `com.unity.test-framework` 1.4.6 | UTF（Unity Test Framework），必需 |
| `com.unity.nuget.newtonsoft-json` 3.2.2 | 报告 JSON 序列化，必需（asmdef 引用名 `Unity.Nuget.Newtonsoft-Json`） |
| `com.unity.pipeline` 0.5.0-exp.1 | **必需依赖**（已声明在 dependencies 中；实验版，本工程以嵌入式包 `Packages/com.unity.pipeline` 提供）。`AxuTestFramework.Editor` 经 TypeCache 自动注册 `axu_test_*` 自定义命令到 pipeline 本地 HTTP API，获得"命令行跑测试 + 收报告"能力。在其他工程使用本包需一并拷入该包，并注意 macOS 端口探测补丁（升级该包版本时补丁会丢失，按工程 `.zcode/08-unity-pipeline-api.md` 第 8 节重打） |

## 目录结构

```
com.cielevel.axu-test-framework/
├── package.json / README.md / CHANGELOG.md
├── Runtime/                          # asmdef: AxuTestFramework（全平台，UNITY_INCLUDE_TESTS）
│   ├── Wait/AxuWait.cs               #   帧 / 条件 / 秒等待（[UnityTest] 协程 yield）
│   ├── Scene/AxuTestScene.cs         #   场景按路径加载 / 卸载 / 重载
│   ├── Capture/AxuCapture.cs         #   PlayMode 截图到报告 artifacts 目录
│   ├── Logging/AxuLogWatch.cs        #   Error/Exception/Assert 日志监视
│   ├── Logging/AxuNoErrorsScope.cs   #   "无错误"作用域（using 友好）
│   ├── Logging/LogEntry.cs           #   单条日志条目模型
│   ├── State/AxuTestState.cs         #   静态/单例重置登记表（域重载关闭纪律）
│   └── Reports/                      #   报告 POCO（schema: axu-test-report/1）
│       ├── AxuTestReport.cs / FilterInfo.cs / ReportSummary.cs
│       ├── ReportResult.cs / ReportConsole.cs
│       └── ReportArtifact.cs / ReportEnvironment.cs
├── Editor/                           # asmdef: AxuTestFramework.Editor（Editor-only, autoReferenced:false）
│   ├── Commands/AxuTestPipelineCommands.cs  # [CliCommand] axu_test_run / axu_test_report / axu_test_status
│   ├── Commands/AxuTestRunService.cs        # 运行编排核心（命令与窗口共用）
│   ├── Commands/AxuTestAssemblyScanner.cs   # 安全测试程序集枚举（剔除 Unity.Pipeline.Tests*）
│   ├── Commands/AxuTestRunRequest.cs        # 运行请求模型
│   ├── Commands/AxuTestRunOutcome.cs        # 运行结果模型
│   ├── Commands/AxuWindowScreenshotCommand.cs # [Windows/<6000.7] axu_window_screenshot 后台窗口截图（PrintWindow）
├── Reports/AxuTestReportWriter.cs       # [TestRunCallback] 报告落盘（schema axu-test-report/1）
│   ├── Reports/AxuConsoleRing.cs            # 运行期 Console 环形缓冲（Error/Warning 采样）
│   ├── Reports/AxuTestRunContext.cs         # 跨域重载运行上下文（Temp 文件）
│   ├── Reports/AxuTestRunStatus.cs          # 运行状态文件模型
│   ├── Reports/AxuTestReportPaths.cs        # 路径约定
│   ├── Window/AxuTestRunnerWindow.cs        # UITK 看板（AxuFrameKit/AxuTestRunner）
│   └── UI/AxuTestTokens.uss                 # 窗口样式 token（独立自持，仿 KitBox 意象）
├── Tests/Editor/                            # asmdef: AxuTestFramework.Tests.Editor（包自身测试）
│   └── AxuTestRunSelectionTests.cs          #   "选定测试"通道解析/FilterInfo/并发闸语义
└── Samples~/
    └── Tests/
        ├── Editor/                   # SampleEditMode（PM → Samples 导入）
        └── Runtime/                  # SamplePlayMode（PM → Samples 导入）
```

## Editor 层（报告落盘 / axu_test_* 命令 / 看板）

### 报告 schema `axu-test-report/1`

每次测试运行结束（`[TestRunCallback]`，域重载开/关两种形态都成立）自动落盘：

```
Library/AxuTestReports/{yyyyMMdd-HHmmss}-{mode}.json     # 报告（out_path 可覆盖）
Library/AxuTestReports/artifacts/*.png                    # AxuCapture 截图产物
```

| 节 | 内容 |
|---|---|
| `schema` / `startedAt` / `endedAt` / `mode` | 标识与时间（UTC ISO 8601；mode = editor / playmode / unknown） |
| `filter` | `{type, value}`：assembly / category / testName / **testNames**（选定运行，Value 为用例数描述）/ none |
| `summary` | `{total, passed, failed, skipped, inconclusive}` |
| `results[]` | 叶子用例：`fullName / status(NUnit ResultState) / durationMs / message / stackTrace / assembly / categories` |
| `console` | 运行期 Console 采样：`errorCount`（Error+Exception+Assert）/ `warningCount` / `samples[]`（≤20 条，单条截断 500 字） |
| `artifacts[]` | `{test, screenshot, note}`——取 `AxuCapture.CapturedPaths`；无产物时缺省 |
| `environment` | `unityVersion / pipelineVersion / reporterVersion` |

运行参数（mode/filter/report/out_path/startedAt）经 `Temp/axu_test_run_context.json` 在启动前写入、RunFinished 时读回后删除——
这是域重载开启时 PlayMode 报告仍能拿到参数的唯一可靠载体（静态字段跨不过域重载）。

### axu_test_run（pipeline 自定义命令）

```
POST /api/exec  {"command":"axu_test_run","parameters":{...},"timeout":150000}
```

| 参数 | 默认 | 说明 |
|---|---|---|
| `mode` | `editor` | `editor` / `playmode` / `all`（all = EditMode 同步完成后接 PlayMode 异步） |
| `filter` | 空 | 过滤值；`filter_type=testName` 时为**大小写不敏感的全名包含匹配**（`test_names` 非空时不参与） |
| `filter_type` | `testName` | `testName` / `assembly` / `category` |
| `category` | 空 | 便捷参数，等价 `filter_type=category`；逗号分隔多个 |
| `assembly` | 空 | 便捷参数，等价 `filter_type=assembly`；对安全程序集清单做包含匹配 |
| `test_names` | 空 | **"选定测试"通道**：用例**精确全名**的 JSON 数组字符串（如 `["Ns.Class.Method","Ns.Class.Method(1,2)"]`；不要用逗号分隔——参数化用例全名含逗号）。非空时优先于 `filter`（testName 子串），与安全程序集清单 AND 收敛；0 匹配直接报错（含"勾选用例不属于当前模式"提示）。注意：经 `testNames` 点名的 `[Explicit]` 用例会被执行（UTF 语义：点名即跑），全量跑时仍跳过 |
| `clear_console` | `true` | 运行前清空 Editor Console 窗口 |
| `report` | `true` | 是否落盘报告 JSON |
| `out_path` | 空 | 报告路径覆盖：`.json` 结尾视为文件，否则视为目录；相对工程根或绝对路径。artifacts 同步重定向到 `<报告目录>/artifacts/` |
| `timeout_ms` | `120000` | EditMode 完成等待预算；HTTP 请求 timeout 需大于它 |

- **EditMode**：同步等待（内部轮询状态文件）后直接返回 `{status:"completed", summary, allPassed, reportPath, assemblies, ...}`。
- **PlayMode**：**强制异步**——立即返回 `{status:"playmode_running", statusPath, assemblies, ...}`，随后轮询
  `axu_test_status` 至 `completed`，报告路径在状态文件的 `reportPath`。Editor 会自动进入并退出 Play 模式。
- **安全行为（硬性）**：`assemblyNames` 恒为安全测试程序集清单——`CompilationPipeline` 枚举引用了 TestRunner 的
  程序集并**无条件剔除 `Unity.Pipeline.Tests*`**（其 `SecurityTokenTests` 会清空 pipeline 本地 API 鉴权 token，
  导致全部请求 401 的"token 中毒"事故）。返回值中的 `assemblies`（实际运行）与 `excludedAssemblies`（被剔除）即审计线索。
  无程序集范围收敛的裸跑在本命令中不存在。
- **并发**：axu_* 命令与看板窗口之间有静态闸互斥；pipeline 自带 `run_tests` 无法纳入该闸（不改 pipeline 包），
  请勿与 `axu_test_run` 并发调用（UTF 自身单运行约束兜底）。EditMode 同步等待期间 pipeline 的执行闸会阻塞其他命令，
  与内置 `run_tests` 同步模式行为一致。

示例：

```bash
TOKEN=$(python3 -c "import json;print(json.load(open('Library/Pipeline/.unity-pipeline-port'))['evalToken'])")
PORT=$(python3 -c "import json;print(json.load(open('Library/Pipeline/.unity-pipeline-port'))['port'])")

# EditMode：按程序集
curl -s -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"command":"axu_test_run","parameters":{"mode":"editor","assembly":"AxuFrameKitV2.Tests.Editor"},"timeout":150000}' \
  "http://127.0.0.1:$PORT/api/exec"

# PlayMode：异步 + 轮询
curl -s ... -d '{"command":"axu_test_run","parameters":{"mode":"playmode","assembly":"AxuFrameKit.Tests.Runtime"}}' ...
curl -s ... -d '{"command":"axu_test_status","parameters":{}}' ...

# 按类别
... {"mode":"editor","category":"Axu/Smoke"} ...

# 选定测试：精确全名 JSON 数组（优先于 filter；与安全程序集清单取交集）
... {"mode":"editor","test_names":"[\"AxuFrameKit.Tests.Editor.AxuFrameKit.Tests.Editor.CoreMathEaseTests\",\"AxuFrameKit.Tests.Runtime.SomeClass.SomeTest\"]","timeout_ms":120000} ...
```

### axu_test_report / axu_test_status

- `axu_test_report`：`{"latest":true}`（默认；`LastReportPath` → 报告目录里最新文件）或 `{"path":"..."}`；
  返回报告 JSON **原文**。超过 256KB 返回 `{status:"too_large", path, sizeBytes, limitBytes}`，请直接读文件。
- `axu_test_status`：读 `Temp/axu_test_run_status.json` 原文；`status` 为 `running` / `completed` / `error`，
  含 `summary`、`reportPath`、`startedAtUtc`（判断陈旧状态）；无文件返回 `{status:"idle"}`。

### axu_window_screenshot（Windows 专属回退：后台无感窗口截图）

> **适用范围**：① 仅 **Windows**；② 仅适用于**没有 CaptureToRenderTexture 能力的 Unity 版本（< 6000.7）**——
> 本实现在 6000.7+ 不参与编译，届时请改用 pipeline 自带的 `capture_editor_element`（引擎级离屏渲染，跨平台、无需窗口可见）。
>
> **macOS 无需对应实现（2026-08-31 决策）**：命令入口有运行时平台守卫，非 WindowsEditor 在任何 Win32 调用之前
> 即返回一句说明文本，不崩溃、无副作用。Mac 截图能力不缺：测试内截图走 `AxuCapture`（跨平台引擎 API），
> pipeline 侧 `capture_game_view` / `capture_scene_view` / `screenshot` 亦跨平台。若未来 <6000.7 的 Mac
> 确需任意 EditorWindow 后台截图，优先升级 Unity 到 6000.7+ 使用 `capture_editor_element`，
> **不要**另写 CGWindowListCreateImage 变体。

- `POST /api/exec {"command":"axu_window_screenshot","parameters":{...}}`
- `window`（必填）：EditorWindow 标题（titleContent.text 精确匹配）
- `menu`（可选）：窗口不存在时经此菜单路径自动打开（如 `AxuFrameKit/AxuTestRunner`）
- `output`（可选）：PNG 路径，默认 `Temp/pipeline-screenshots/axu_win_<标题>_<时间戳>.png`
- 原理：`PrintWindow(PW_RENDERFULLCONTENT)` 离屏渲染 + `GetDIBits`/`GetBitmapBits` 读回。
  **窗口可处于被遮挡/失焦状态，全程不抢焦点、不置顶**；限制：最小化或已关闭无法截取（可经 menu 重开）、
  停靠（docked）窗口没有独立 OS 窗口无法截取
- 实现说明：`GetDIBits` 在部分系统组合下静默失败，命令内建 `GetBitmapBits` 兜底并统一翻转行序；
  原生窗口按 进程PID + 客户区尺寸最优匹配 定位（Unity 浮动窗口的 Win32 标题可能为空）

### 看板窗口

菜单 **`AxuFrameKit/AxuTestRunner`**（与 AxuKitBox 同级；样式仿 KitBox 但包内自持独立 USS，无任何代码引用）：
左侧测试套件树（按程序集/类分组，顶部过滤输入做全名包含匹配），右侧最近报告结果树（FAIL 默认展开，消息/堆栈/类别可看），
顶栏 EditMode/PlayMode 切换 + **Run 全部 / Run 选定（N）** 双按钮 + 刷新 + 最近报告路径（点击 Reveal）。
窗口 Run 与 `axu_test_run` 走同一内部服务。

- **Run 全部**：当前模式全量（过滤输入非空时作为 testName 子串过滤收窄）。
- **Run 选定**：左侧树勾选用例经 `test_names` 精确通道执行（按当前模式；勾选用例不属于当前模式时被预检 0 匹配拦下）。
  用例行带勾选框；程序集/命名空间/类各级 Foldout 头部带**三态勾选框**——点击批量勾选/取消其下当前可见用例
  （全选态点击=全不选，其余态点击=全选；部分勾选显示为橙色填充态），勾选框拦截冒泡不连带折叠层级。
  勾选集合按 FullName 键控，跨树重建/模式切换保留；过滤输入触发的树重建后三态按可见叶子重算（所见即所勾）。

### 与 pipeline 自带 run_tests 的关系

`[TestRunCallback]` 报告器对**所有**测试运行生效（含 pipeline 自带 `run_tests` 发起的运行）——上下文文件缺失时报告
`mode` 记为 `unknown`，其余内容照常收集。若只要 axu 报告链路的完整行为，用 `axu_test_run`。

## Runtime 工具层速览

命名空间统一为 `AxuTestFramework`。

### AxuWait（等待）

```csharp
// 条件等待：每帧检查，5s 超时 → Assert.Fail（附清晰超时消息）
yield return AxuWait.Until(() => ui.isReady, 5f);
// 条件等待 + 按真实时间间隔检查（不逐帧）
yield return AxuWait.Until(() => done, 10f, 0.5f);
// 帧等待 / 秒等待
yield return AxuWait.ForFrames(3);
yield return AxuWait.ForSeconds(0.2f);
```

### AxuTestScene（场景）

```csharp
// 按路径加载（Editor 下支持 Assets/ 完整路径，无需加入 Build Settings）
yield return AxuTestScene.LoadAsync("Assets/Scenes/Demo.unity");
// 卸载 / 重载
yield return AxuTestScene.UnloadAsync("Demo");
yield return AxuTestScene.ReloadAsync("Assets/Scenes/Demo.unity");
```

### AxuCapture（截图，仅 PlayMode）

```csharp
// 截图到报告 artifacts 目录（默认 Library/AxuTestReports/artifacts/，文件名带时间戳）
yield return AxuCapture.ScreenshotCoroutine("game-ui-check");
Debug.Log(AxuCapture.LastScreenshotPath);   // 完整路径
```

EditMode 下调用会抛 `InvalidOperationException`（EditMode 无 GameView 渲染输出；替代方案见异常消息）。

### AxuLogWatch / AxuNoErrorsScope（日志监视）

```csharp
// 方式一：作用域——结束时若有 Error/Exception/Assert，Assert.Fail 汇总失败
using (AxuLogWatch.ExpectNoErrors())
{
    DoSomethingThatMustNotError();
}

// 方式二：手动监视
using (var watch = new AxuLogWatch())
{
    DoWork();
    Assert.IsFalse(watch.HasErrors, string.Join("\n", watch.Errors));
}
```

预期内的错误日志仍应先用 `LogAssert.Expect` 声明（AxuLogWatch 只做被动记录，不与 LogAssert 交互）。
内部只挂 `Application.logMessageReceivedThreaded`（全量、含非主线程），收集加锁，线程安全。

### AxuTestState（静态/单例重置）

```csharp
[UnitySetUp]
public IEnumerator SetUp()
{
    AxuTestState.ResetStatics();                                  // 执行已登记重置 + 清空登记表
    AxuTestState.Register("MySystem", MySystem.ResetForTest);     // 重新登记本轮所需重置项
    yield return AxuWait.ForFrames(1);
}
```

域重载关闭（Enter Play Mode Options 置 3）时静态/单例残留是主要风险面，**编写规范强制**在 `[UnitySetUp]`
中调用 `ResetStatics()`；`ResetAll()` 逐项捕获异常并返回失败汇总，不会因单项异常中断。

### Reports POCO（报告模型，schema `axu-test-report/1`）

`AxuTestReport` / `FilterInfo` / `ReportSummary` / `ReportResult` / `ReportConsole` / `ReportArtifact` /
`ReportEnvironment`——纯数据模型，由 M2 的报告器填充并序列化；`AxuCapture.CapturedPaths` 可作为
`Artifacts` 的数据源。

## 各包接入（M2/M3 落地后补全"接入一页纸"）

1. 在被测包内建 `Tests/Editor` 与/或 `Tests/Runtime`，asmdef 命名 `{被测程序集}.Tests.{Editor|Runtime}`；
2. references：被测程序集 + `AxuTestFramework` + `UnityEngine.TestRunner`（Editor 侧再加 `UnityEditor.TestRunner`）；
3. `overrideReferences: true` + `precompiledReferences: ["nunit.framework.dll"]`、
   `defineConstraints: ["UNITY_INCLUDE_TESTS"]`、`autoReferenced: false`；
4. 包 `package.json` **不声明**测试依赖（缺包时测试程序集不编译是特性）。

## 版本

见 [CHANGELOG.md](CHANGELOG.md)。
