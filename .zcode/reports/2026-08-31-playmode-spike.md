# M0 Spike 收尾报告 — PlayMode 测试链路 A/B 两轮验证（2026-08-31）

> 执行者：unity-tester（ZCode 子代理）。前序：A 轮已在上一会话验证（交接文档 `.zcode/plans/axu-test-framework-resume.md` 第三节），本报告补齐 meta 清理、EditMode 基线、A 轮复核、**B 轮（域重载关闭）**、复原与收尾。
> 环境：Unity 6000.0.23f1（pid 45573，Editor 运行中），pipeline `com.unity.pipeline 0.5.0-exp.1`（嵌入式，端口 7800，回环 + Bearer evalToken）。

## 一、总判定

| 闸门 | 判定 |
|---|---|
| **A 轮（域重载开启，现状设置 Options=0）** | ✅ 通过（2/2 通过、自动退出 Play、TestRunCallback 落盘 passCount=2、Editor 日志出现 ReloadAssembly——预期行为） |
| **B 轮（域重载关闭，代码切 Options=3）** | ✅ 通过（2/2 通过、自动退出 Play、TestRunCallback 仍触发且落盘、运行窗口 **0** 条 ReloadAssembly） |
| **设置复原** | ✅ `git diff ProjectSettings/EditorSettings.asset` 为空；ProjectSettings/ 整体 git status 干净 |

**M0 闸门判定：A/B 两轮均绿 → M0 通过。** `[TestRunCallback]` 支点在两种域重载形态下都成立，M2 `AxuTestReportWriter` 的设计前提落实。

## 二、工程设置核实值（验证前基线）

- `ProjectSettings/EditorSettings.asset`：`m_EnterPlayModeOptionsEnabled: 1`（:27）、`m_EnterPlayModeOptions: 0`（:28）
- 语义（本次实测确认）：`0 = None`，即**进 Play 时域重载+场景重载照常发生**；`1` 仅表示"Enter Play Mode Options 特性开关打开"。详见第六节勘误。

## 三、逐项验证结果与证据原文

### 0. meta 清理项 → 判定为"非缺陷，无需修复"

- 现象：`PlayModeSpikeTests.cs.meta` / `SpikeRunCallback.cs.meta` 各 59 字节，仅 `fileFormatVersion: 2` + `guid:`，无 `MonoImporter:` 块。
- **关键发现：这是全工程既定惯例，不是损坏。** 抽查证实 `Packages/com.cielevel.axu-frame-kit` 下 261 个 `.cs.meta` 全部 <100 字节；工程既有测试 `Assets/Tests/Editor/FrameworkCoreSmokeTests.cs.meta` 同为 59 字节最小格式，且 A 轮早已跑通。
- 实验证据：触发 `recompile`（内部执行 `AssetDatabase.Refresh()`，见 `Packages/com.unity.pipeline/Editor/Commands/RecompileCommand.cs:59`）→ 结果 `up_to_date`，Unity 对"可解析但缺 MonoImporter 块"的 meta **容忍且不自动重写**。
- 加码实验：删除两个 meta → `recompile` → Unity 重新生成，内容仍是 59 字节最小格式且 **guid 从资产库缓存原样恢复**（未产生新 guid，无引用破坏）。
- 处置：保留现状（与全工程一致）。Console 此后 `get_console_logs` 返回 `total: 0`，零错误零警告。

### 1. EditMode 基线 → ✅ 6/6

调用：`run_tests {"mode":"editor","filter":"AxuFrameKitV2.Tests.Editor","filter_type":"assembly","timeout":180}`（**assembly 过滤**，原因见第四节事故 1）。

```json
{"Summary": {"Total": 6, "Passed": 6, "Failed": 0, "Skipped": 0, "Inconclusive": 0}}
```

6 个为 `AxuFrameKitV2.Tests.FrameworkCoreSmokeTests.BiDictionary_*`（4 个）与 `CircularBuffer_*`（2 个），`list_tests` 核对在册。

### 2. A 轮复核（域重载开启）→ ✅ 全绿

调用序列：删 `Temp/pipeline_spike_callback.json` → `run_tests {"mode":"playmode","filter":"PlayModeSpike","async_tests":true}` → 轮询 `test_status`。

- `test_status`：`completed`，`{"total": 2, "passed": 2, "failed": 0, "skipped": 0}`（`WaitForSeconds_YieldsPointTwoSeconds_Passes` 0.212s + `YieldNull_AcrossFiveFrames_Passes`）。
- 自动退出 Play：`editor_status` → `playMode: "stopped"`，`compiling: false`。
- TestRunCallback 落盘（重新生成的 `Temp/pipeline_spike_callback.json`）：
  ```json
  {"runStartedUtc": "2026-08-30T16:56:00.6393060Z", "runFinishedUtc": "2026-08-30T16:56:00.9129940Z",
   "rootFullName": "AxuFrameKitV2", "resultState": "Passed", "total": 2, "passCount": 2, "failCount": 0, "skipCount": 0}
  ```
- 域重载证据（`~/Library/Logs/Unity/Editor-prev.log`，注意本会话日志文件曾被轮换，见第四节事故 2）：
  ```
  Reloading assemblies for play mode.
  Reloading assemblies after forced synchronous recompile.
  Begin MonoManager ReloadAssembly
  Domain Reload Profiling: 1938ms
  ```
  与 RunStarted `16:56:00.6393060Z` 时间线吻合 → **域重载开启设置下进 Play 出现 ReloadAssembly，符合预期**（与 AGENTS.md 旧表述相反，见勘误节）。

### 3. B 轮（域重载关闭）→ ✅ 全绿

调用序列：`eval` 置 `EditorSettings.enterPlayModeOptions = (UnityEditor.EnterPlayModeOptions)3;` → 读回 `"now=3 enabled=True"` → 删旧 callback → 同款 async playmode 运行 → 轮询。

- `test_status`：`completed`，`{"total": 2, "passed": 2, "failed": 0, "skipped": 0}`。
- 自动退出 Play：`playMode: "stopped"`。
- TestRunCallback **在域重载关闭下仍触发**：
  ```json
  {"runStartedUtc": "2026-08-30T17:02:05.1359570Z", "runFinishedUtc": "2026-08-30T17:02:05.3604730Z",
   "rootFullName": "AxuFrameKitV2", "resultState": "Passed", "total": 2, "passCount": 2, "failCount": 0, "skipCount": 0}
  ```
- 运行窗口日志（Editor-prev.log 自 B 轮基线 31546 行之后共 170 行）：`grep -c "ReloadAssembly|Reloading assemblies|Domain Reload"` = **0**；窗口内含 `[SpikeRunCallback] RunStarted/RunFinished` 行 → **域重载确未发生**。
- 复原与刷盘：`eval` 置回 0（读回 `options=0`）→ 磁盘仍残留 3（见事故 5）→ `eval AssetDatabase.SaveAssets()` 强制 Unity 自序列化 → 磁盘恢复 `Enabled: 1 / Options: 0` → **`git diff ProjectSettings/EditorSettings.asset` 为空**。
- 复原后行为回归：`editor_play`/`editor_stop` 一轮，日志窗口再次出现 `Reloading assemblies for play mode.` + `Begin MonoManager ReloadAssembly` → 域重载行为恢复，与 HEAD 语义一致。

### 4. 收尾 → ✅

- 复原后 EditMode 重跑（assembly 过滤）：**6/6 通过**。
- `list_tests {"mode":"playmode"}`：全工程 21 个 PlayMode 测试在册，spike 2 个均在：
  - `AxuFrameKitV2.Tests.PlayModeSpike.PlayModeSpikeTests.WaitForSeconds_YieldsPointTwoSeconds_Passes`
  - `AxuFrameKitV2.Tests.PlayModeSpike.PlayModeSpikeTests.YieldNull_AcrossFiveFrames_Passes`

## 四、实际调用的端点与参数（复现清单）

- `GET /api/status`、`GET /api/editor_status`、`GET /api/commands`（150 个命令，注意分页字段）
- `POST /api/exec`：`recompile {"focus":false}`、`recompile_status {}`、`get_console_logs {"severity":"All","limit":50}`、`run_tests {"mode":"editor|playmode","filter":"...","filter_type":"assembly|testName","async_tests":true,"timeout":N}`、`test_status {}`、`list_tests {"mode":"editor|playmode"}`、`eval {"code":"...","timeout":ms}`、`console {"tail":200}`、`editor_play {}`、`editor_stop {}`
- 每次调用前重读 `Library/Pipeline/.unity-pipeline-port` 的 `evalToken`（会轮换）。

## 五、发现的坑与建议（供 M1/M2 与 pipeline 包维护参考）

### 事故 1（高）：无过滤 run_tests 自毁鉴权 —— pipeline 包缺陷，已上报

- 经过：按交接手册执行 `run_tests {"mode":"editor"}`（未带 filter）→ 实际跑了**全工程 704 个 EditMode 测试**（含 pipeline 包自带 664 个）→ 运行结束 `total=664, pass=658, fail=4`（4 个失败属 pipeline 自带套件，明细因连接中断未捕获，非本次被测对象）→ 此后所有 API 请求 401。
- 根因链（源码实证）：
  1. `Packages/com.unity.pipeline/Tests/Editor/SecurityTokenTests.cs` 的 `SetUp()`/`TearDown()` 调 `SecurityTokenManager.ClearCache()`（Runtime/SecurityTokenManager.cs:108），擦除 SessionState token 并触发重新生成；
  2. 描述符（`Library/Pipeline/.unity-pipeline-port`）只在**已鉴权请求**的 `GetServerStatus()`/`UpdateHeartBeat()`（EditorPipelineServer.cs:222/197）时刷新；
  3. token 变更后所有请求 401 → 心跳永远不执行 → 描述符永远滞后 → **死锁**。
- 恢复方法（已验证）：删除 `Assets/Tests/PlayModeSpike/*.cs.meta`（允许范围内的改动）+ 激活 Editor 触发 Auto Refresh → 脚本重导入 → 域重载 → 服务重启时 `CreateInstanceDescriptor()` 用 SessionState 中现行 token 重写描述符 → API 恢复。
- **建议**：
  1. 一切 `run_tests` 必须带 `filter`（推荐 `filter_type:"assembly"`）；M2 的 `axu_test_run` 应强制按包/程序集过滤，并对无过滤请求显式拒绝或告警；
  2. pipeline 包缺陷（不修，交主代理）：`SecurityTokenTests` 不应在共享 SessionState 的真 Editor 会话里跑，或 `ClearCache` 后应立即写回；`UpdateHeartBeat` 应在鉴权失败路径也刷新描述符（自愈）。

### 事故 2（中）：多版本 Unity 下禁用 `open -a Unity`

- 本机装有 5 个 Unity 版本；`open -a Unity` 拉起默认版本 **6000.5.1f1** 的临时实例（16:52:41Z），把 `~/Library/Logs/Unity/Editor.log` 轮换/截断（真 Editor 45573 的活动日志此后在 **Editor-prev.log**），临时实例因工程被锁自行退出。Editor 进程本体未受影响（描述符 pid/端口监听始终为 45573）。
- 建议：需要激活 Editor 时用 `osascript activate` 并显式路径；日志证据一律先确认目标进程实际写入的文件（mtime + 进程存活）。

### 坑 3：meta"不完整"是全工程惯例（见第三节 0），M3 文档同步时不要把它当缺陷写。

### 坑 4：Unity 6000.0.23f1 的 EnterPlayMode 属性陷阱（B 轮 eval）

- 正确拼写是 `enterPlayModeOptions`（"Mode" 的 M 大写）；`enterPlaymodeOptions` 编译错 CS0117。
- **旧属性对已被重映射**：`enterPlayModeOptionsEnabled` 的 getter 派生自 `options != 0`，setter 等价于"启用特性并写默认 options=3"——即"最后设 enabled=true"会把 options 顶回 3。**复原唯一正确姿势是只设 `enterPlayModeOptions = 0`**。
- 设置 Options=3 后，磁盘在**进 Play 时**被 Unity 序列化（`m_EnterPlayModeOptions: 3` 落盘，mtime 17:02:03Z）；eval 置回 0 不会自动刷盘，需 `eval AssetDatabase.SaveAssets()`（或一次进/出 Play）驱动 Unity 自序列化。

### 坑 5：无过滤 EditMode 运行还连带写了 3 个 ProjectSettings 文件（已复原）

- 00:40（该次运行时段）：`TimeManager.asset`（Fixed Timestep 0.02 → 同值的新序列化格式）、`GraphicsSettings.asset`（同值换行格式变化）、`ProjectSettings.asset`（`preloadedAssets` 被加入 `Assets/URP/InputSystem_Actions.inputactions`，guid 052faaac…，系 Input System 包自动注册行为）。另 `EditorBuildSettings.asset`/`URPProjectSettings.asset` 仅 mtime 变化、内容未变。
- 处置：三者已 `git checkout --` 复原，收尾 `git status --porcelain ProjectSettings/` 为空。
- 残留风险（如实记录）：若 Unity 未来再次自序列化，TimeManager 新格式与 preloadedAssets 条目可能重新出现（TimeManager 为同值漂移、无功能影响；preloadedAssets 是否保留属用户决策，建议在 M3 文档同步时向用户确认）。

### 其他观察

- 该次 EditMode 全量运行期间 UTF 反复打印 `Assertion failed on expression: 'targetScene != nullptr'`（`PerformUndoTask.cs:28`）共 4 次，与 4 个失败疑似相关，属 pipeline 自带套件内部问题，不影响本 spike。
- 同步 `run_tests`（async_tests=false）的 HTTP 连接会被进 Play 的域重载切断（响应丢失）——**PlayMode 一律 async_tests=true + 轮询**。
- `[TestRunCallback]` 的回调只包含过滤后的结果（A/B 轮 total 均为 2，无套件污染）→ M2 报告器可以干净拿到目标测试集。
- `Temp/` 证据文件在 Editor 重启/日志轮换场景下脆弱 → M2 报告落盘选 `Library/AxuTestReports/` 的决策再次被印证。

## 六、AGENTS.md 勘误（只记录，不改文档；M3 统一修）

**勘误对象**：AGENTS.md"编辑器设置"一节的"进入 Play 模式选项：已启用，不重新加载域/场景 (EnterPlayModeOptionsEnabled: 1)"。

**实际语义（本次 A/B 轮实测）**：`m_EnterPlayModeOptionsEnabled: 1` 只表示 Enter Play Mode Options **特性开关打开**；选项值 `m_EnterPlayModeOptions: 0 = None`，即**进 Play 时域重载与场景重载照常发生**。AGENTS.md 把 Enabled 标志误当成了选项值，"不重新加载域/场景"的表述自 Initial commit 起即与实际行为相反。

**证据**：A 轮（现状设置）进 Play 时 Editor 日志明确出现 `Reloading assemblies for play mode.` / `Begin MonoManager ReloadAssembly`（第三节 2）；B 轮代码切 `options=3`（DisableDomainReload|DisableSceneReload）后同窗口 ReloadAssembly 行归零（第三节 3），两相对照成立。

**建议措辞（M3 采用）**："进入 Play 模式选项：特性开关已启用，选项值为 0（None）——域重载与场景重载照常发生；如需关闭域重载须将选项置 3（DisableDomainReload|DisableSceneReload），当前工程未启用。"

## 七、对 M1/M2 设计的影响结论

1. **M2 支点成立**：`[TestRunCallback]`（`SpikeRunCallback.cs` 原型）在域重载开/关两种形态下都触发且可写结构化 JSON → `AxuTestReportWriter`（schema `axu-test-report/1`）按原方案实施，落盘目录按决策用 `Library/AxuTestReports/`。
2. **M2 `axu_test_run` 必须强制过滤**：参数设计按"包/程序集"过滤（`filter_type=assembly`），无过滤请求应拒绝或强制要求确认——否则触发事故 1（自毁鉴权 + 5 分钟全量套件 + 工程设置副作用）。可另设只读保护：检测到 `Unity.Pipeline.Tests.*` 将被命中时中止。
3. **PlayMode 只走 async**：`axu_test_run` 内部固定 `async_tests=true` + `test_status` 轮询，规避同步连接被域重载切断。
4. **M1 `AxuTestState.ResetStatics` 优先级提高**：B 轮证明域重载关闭下整条链路可用，静态/单例残留（含 pipeline 服务自身的 token 状态）是该模式下唯一新增风险面，工具层须提供显式重置并在编写规范中强制。
5. **编辑器设置切换类测试**（若 M2/M4 需要）：必须走 eval 代码切换 + `AssetDatabase.SaveAssets()` 刷盘 + git diff 验收三件套，且只动 `enterPlayModeOptions` 一个属性（坑 4 的 remap 行为）。

## 八、现场遗留状态

- `Assets/Tests/PlayModeSpike/`：保留（M3 由正式测试替换后删除）；两个 .cs.meta 为 Unity 重新生成的最小格式（guid 未变），与全工程惯例一致。
- `ProjectSettings/`：git 全干净（EditorSettings 已复原并经 Unity 自序列化确认；三个连带漂移文件已 checkout）。
- pipeline 服务：ready，token 为复原后描述符现值（验证过可正常执行命令）。
- 工程内测试最终态：EditMode 6/6 通过；PlayMode spike 2/2 通过（A、B 两轮各验证一次）。
