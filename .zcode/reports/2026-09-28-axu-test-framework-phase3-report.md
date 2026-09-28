# AxuTestFramework 可用性改造 · 阶段 3 交付与验收报告

> 方案：`.zcode/plans/axu-test-framework-usability-refactor.md` 阶段 3。实施日期 2026-09-28；Windows / Unity 6000.0.23f1；包版本 0.6.0。

## 改动

| 验收主题 | 已落地行为 |
|---|---|
| 默认视图的范围和下一步 | 总览结果卡标题明确为“最近一次场景套件运行（不含单元测试）”；徽章限定“场景套件”或“示例自检”；显示时间、用时、通过/警告/失败数，以及本轮优先问题和检查方向。只有本轮状态指名的报告可用于本轮建议；无状态时回退的报告标为历史。自检通过只引导继续跑项目测试，不要求为内置 echo 配预算。 |
| 单元测试与场景结果分开 | Tests 页显示 EditMode/PlayMode 与报告中的过滤范围。本轮状态文件若为 running/error 或报告缺失，清空旧报告；无状态文件时才显示标明“历史”的报告。失败/跳过用例给中文说明与检查建议，原始消息、堆栈和类别留在技术详情。Console 错误日志可含预期日志，单独列示而不覆盖 NUnit 用例结论。 |
| 场景问题与技术详情 | 失败/警告场景先显示问题和建议；重复 finding 按 code 汇总，首层最多展示三类，全部原始 finding 及指标、预算、基线、工件保留在技术详情。导出和打开 JSON/工件操作归入单独的“技术详情与导出”折叠区。未设预算不会再显示“无异常”。 |
| 首次使用遗留 | 首用卡的阻断口径与场景页一样包括尚未收尾的运行记录；预检问题集合不变时不再每 800ms 重建修复按钮。 |

报告 schema 和 pipeline 命令参数未变。新增 `UtfResultPresenter` 与 `Phase3PresenterTests`；新脚本 `.meta` 由 Unity 资产导入生成。

## 验证

| 项目 | 结果 |
|---|---|
| `dotnet build AxuTestFramework.Tests.Editor.csproj --no-restore -v:q` | 通过；0 错误，1 个既有 `SuiteEntry.Label` 未赋值警告。 |
| `axu_test_run {mode:editor, category:"Axu/TestFramework"}` | **298/298 通过**；报告 `Library/AxuTestReports/20260928-073155-editor.json`。此前定向 `Phase3PresenterTests` 5/5；其后新增自检边界与 warnAsFail 建议用例，最终包回归覆盖全部。 |
| 内置 `axu-selfcheck-echo` 经 `axu_scenario_run` | `completed`，1/1 通过、exitCode 0，状态文件 runId `78c17d070df04b58a967dda20da9e6c3`；`editor_status` 证实自动退 Play，`playMode=stopped`。 |
| 本轮 UTF 无报告故障注入 | 临时写入 `error` 且 `ReportPath=null` 的状态文件，直接调用窗口加载入口；结果为“最近一次单元测试没有可用报告”且 `_report==null`。`finally` 原样还原真实状态文件；最终窗口重新加载 298/298 报告。 |
| `axu_doctor` | 8 pass / 0 warn / 0 fail，识别包版本 0.6.0。 |
| `axu_lint` | 4 pass / 1 warn / 0 fail；唯一 warn 仍为改动前已有的 24 处 `file.type_name` 家族文件，新文件无违规。 |

## 看板实图

- [总览：本轮示例自检](axu-phase3-screenshots/overview-selfcheck.png)：范围、结论、时间、数量及“下一步跑项目测试”的限定语。
- [场景页：历史失败报告](axu-phase3-screenshots/scenario-failure-history.png)：按类收敛的失败解释、历史标识与折叠技术详情；此图使用 2026-09-24 的既有失败报告进行视觉检查，不代表本次重新运行该失败套件。
- [单元测试页：本轮 298/298](axu-phase3-screenshots/unit-test-result.png)：测试类型/类别范围、结果及 Console 预期日志提示。

## 边界

- 本阶段真实运行了成功的自检和 EditMode 包回归；失败场景、警告、中断及无预算的显示行为由既有报告视觉检查和 presenter 单测覆盖，本阶段未重新制造这些运行终态。
- 尚未开展方案阶段 4 的快速上手文档和未参与开发者试用。
