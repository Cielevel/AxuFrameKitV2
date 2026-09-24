using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using AxuTestFramework.Editor;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] scenario / UTF 两侧"运行状态与在途判定"契约（阶段 1：运行闭环与可信状态）。
    /// 关键契约点（每条都对应一个真实的错误行为）：
    ///  - **判活按最后活动时间**（心跳 &gt; 状态文件写入时间 &gt; 启动时间）：运行中的 20 分钟 soak
    ///    不许被当作陈旧放行（旧实现只看 startedAtUtc，15 分钟后互斥闸失守）；
    ///  - 心跳停止（崩溃/中断残留）后越过 15 分钟即判陈旧，互斥闸能自动恢复；
    ///  - 中断由看门狗补写 <c>aborted</c> 终态（与 completed/error 一样是终态）；
    ///  - <c>runId</c> 标识本轮运行：消费端据此拒收"不属于本次"的状态文件；
    ///  - scenario 状态文件的 summary 必须解析出 <c>warned</c>（同一命名空间下有同名 UTF 汇总类型，
    ///    类型绑定写错会静默丢掉 warned 计数）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class ScenarioRunStateTests
    {
        static readonly string TempDir = Path.Combine(Path.GetTempPath(), "AxuScenarioRunStateTests");
        static readonly DateTime Now = new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp() => Directory.CreateDirectory(TempDir);

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(TempDir)) Directory.Delete(TempDir, recursive: true);
        }

        static string PathOf(string fileName) => Path.Combine(TempDir, fileName);

        static string Iso(DateTime utc) => utc.ToString("o", CultureInfo.InvariantCulture);

        // ------------------------------------------------------------------ 长跑互斥（20 分钟 soak）

        [Test]
        public void ScenarioStatus_运行20分钟且心跳新鲜_判在途而非陈旧()
        {
            var status = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Running,
                StartedAtUtc = Iso(Now.AddMinutes(-20)), // 启动时间早已越过 15 分钟阈值
                HeartbeatUtc = Iso(Now.AddSeconds(-3)),  // 但 Agent 还在心跳
            };

            Assert.That(status.IsStale(Now), Is.False, "长跑运行期间不得被判为陈旧");
            Assert.That(AxuScenarioRunStatus.IsInFlight(status, Now), Is.True,
                "20 分钟的 soak 必须继续占用互斥闸");
        }

        [Test]
        public void ScenarioStatus_心跳停止超阈值_判陈旧()
        {
            var status = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Running,
                StartedAtUtc = Iso(Now.AddMinutes(-40)),
                HeartbeatUtc = Iso(Now.AddMinutes(-16)),
            };

            Assert.That(status.IsStale(Now), Is.True);
            Assert.That(AxuScenarioRunStatus.IsInFlight(status, Now), Is.False, "中断残留不应永久占用互斥闸");
        }

        [Test]
        public void ScenarioStatus_缺心跳时_回退到启动时间判定()
        {
            var fresh = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Running,
                StartedAtUtc = Iso(Now.AddMinutes(-2)),
            };
            var old = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Running,
                StartedAtUtc = Iso(Now.AddMinutes(-30)),
            };

            Assert.That(fresh.IsStale(Now), Is.False);
            Assert.That(old.IsStale(Now), Is.True);
        }

        [Test]
        public void ScenarioStatus_时间字段全缺_保守视为陈旧()
        {
            var status = new AxuScenarioRunStatus { Status = AxuScenarioRunStatus.Running };

            Assert.That(status.IsStale(Now), Is.True, "无法判活的 running 不应长期占用互斥闸");
        }

        [Test]
        public void TestStatus_心跳新鲜的长跑_判在途()
        {
            var status = new AxuTestRunStatus
            {
                Status = AxuTestRunStatus.Running,
                StartedAtUtc = Iso(Now.AddMinutes(-25)),
                HeartbeatUtc = Iso(Now.AddSeconds(-2)),
            };

            Assert.That(AxuTestRunStatus.IsInFlight(status, Now), Is.True,
                "单条用例跑 25 分钟时仍须占用互斥闸");

            status.HeartbeatUtc = Iso(Now.AddMinutes(-20));
            Assert.That(AxuTestRunStatus.IsInFlight(status, Now), Is.False);
        }

        [Test]
        public void TestStatus_终态_恒不在途()
        {
            var status = new AxuTestRunStatus
            {
                Status = AxuTestRunStatus.Completed,
                StartedAtUtc = Iso(Now.AddSeconds(-5)),
                HeartbeatUtc = Iso(Now.AddSeconds(-5)),
            };

            Assert.That(AxuTestRunStatus.IsInFlight(status, Now), Is.False);
        }

        // ------------------------------------------------------------------ 文件往返（键契约 + 汇总解析）

        [Test]
        public void ScenarioStatus_文件往返_保留runId与warned计数()
        {
            var path = PathOf("axu_scenario_status.json");
            File.WriteAllText(path,
                "{\n" +
                "  \"status\": \"completed\",\n" +
                "  \"suiteId\": \"suite.demo\",\n" +
                "  \"runId\": \"run-abc\",\n" +
                "  \"startedAtUtc\": \"" + Iso(Now.AddMinutes(-3)) + "\",\n" +
                "  \"heartbeatUtc\": \"" + Iso(Now) + "\",\n" +
                "  \"endedAtUtc\": \"" + Iso(Now) + "\",\n" +
                "  \"completedCount\": 3,\n" +
                "  \"totalCount\": 3,\n" +
                "  \"reportPath\": \"C:/tmp/report.json\",\n" +
                "  \"summary\": { \"total\": 3, \"passed\": 2, \"warned\": 1, \"failed\": 0 },\n" +
                "  \"exitCode\": 2\n" +
                "}\n");

            var status = AxuScenarioRunStatus.TryRead(path);

            Assert.That(status, Is.Not.Null);
            Assert.That(status.RunId, Is.EqualTo("run-abc"));
            Assert.That(status.IsTerminal, Is.True);
            Assert.That(status.IsCompleted, Is.True);
            Assert.That(status.Summary, Is.Not.Null);
            Assert.That(status.Summary.Total, Is.EqualTo(3));
            Assert.That(status.Summary.Passed, Is.EqualTo(2));
            Assert.That(status.Summary.Warned, Is.EqualTo(1),
                "warned 必须解析出来（同名 UTF 汇总类型没有该字段，类型绑定写错会静默丢弃）");
            Assert.That(status.Summary.Failed, Is.EqualTo(0));
            Assert.That(status.ExitCode, Is.EqualTo(2));
        }

        [Test]
        public void ScenarioStatus_aborted是终态且不等同于完成()
        {
            var status = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Aborted,
                Reason = AxuScenarioRunStatus.ReasonInterrupted,
                Message = "Play Mode 在运行结束前退出",
            };

            Assert.That(status.IsTerminal, Is.True);
            Assert.That(status.IsCompleted, Is.False);
            Assert.That(status.IsRunning, Is.False);
        }

        [Test]
        public void ScenarioStatus_文件写入时间算作活动信号()
        {
            var path = PathOf("axu_scenario_status_stale.json");
            File.WriteAllText(path,
                "{\n  \"status\": \"running\",\n  \"startedAtUtc\": \"" + Iso(Now.AddMinutes(-40)) + "\",\n" +
                "  \"heartbeatUtc\": \"" + Iso(Now.AddMinutes(-40)) + "\"\n}\n");
            File.SetLastWriteTimeUtc(path, Now.AddMinutes(-1)); // 文件最近才被写过

            var status = AxuScenarioRunStatus.TryRead(path);

            Assert.That(status.FileLastWriteUtc, Is.Not.Null);
            Assert.That(status.IsStale(Now), Is.False, "文件写入时间比心跳新时应按文件时间判活");
        }

        // ------------------------------------------------------------------ 运行记录（启动-收尾契约）

        [Test]
        public void RunRecord_往返_保留runId与来源()
        {
            var path = PathOf("axu_scenario_run.json");
            var record = ScenarioRunRecord.Create("C:/proj/Assets/Tests/Scenarios/demo.suite.json",
                new[] { "echo.demo" }, ScenarioRunRecord.SourceDashboard);

            ScenarioRunRecord.Write(record, path);
            var loaded = ScenarioRunRecord.TryRead(path);

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.RunId, Is.EqualTo(record.RunId));
            Assert.That(loaded.RunId, Is.Not.Empty);
            Assert.That(loaded.Source, Is.EqualTo(ScenarioRunRecord.SourceDashboard));
            Assert.That(loaded.ScenarioIds, Is.EqualTo(new[] { "echo.demo" }));
            Assert.That(loaded.AutoStopOnDone, Is.True);
            Assert.That(loaded.SuitePath, Does.EndWith("demo.suite.json"));
        }

        [Test]
        public void RunRecord_runId不匹配或缺失_不认领状态文件()
        {
            var record = ScenarioRunRecord.Create("C:/proj/demo.suite.json", null, ScenarioRunRecord.SourceMenu);

            var other = new AxuScenarioRunStatus { Status = AxuScenarioRunStatus.Completed, RunId = "run-other" };
            var same = new AxuScenarioRunStatus { Status = AxuScenarioRunStatus.Completed, RunId = record.RunId };
            var legacy = new AxuScenarioRunStatus { Status = AxuScenarioRunStatus.Completed };

            Assert.That(record.MatchesStatus(other), Is.False, "别的运行的状态不得当作本次结果");
            Assert.That(record.MatchesStatus(same), Is.True);
            Assert.That(record.MatchesStatus(legacy), Is.False, "无 runId 的旧状态一律不认领（宁可显示中断）");
            Assert.That(record.MatchesStatus(null), Is.False);
        }

        [Test]
        public void RunRecord_在途判定_Play中恒在途_陈旧记录放行()
        {
            var fresh = ScenarioRunRecord.Create("C:/proj/demo.suite.json", null, ScenarioRunRecord.SourcePipeline);
            fresh.StartedAtUtc = Iso(Now.AddSeconds(-10));

            Assert.That(fresh.IsInFlight(Now, isPlaying: true), Is.True);
            Assert.That(fresh.IsInFlight(Now, isPlaying: false), Is.True,
                "刚退 Play 的收尾窗口（看门狗 0.5s 内清理）也按在途保守处理");

            var longRun = ScenarioRunRecord.Create("C:/proj/demo.suite.json", null, ScenarioRunRecord.SourcePipeline);
            longRun.StartedAtUtc = Iso(Now.AddMinutes(-40)); // 长跑：发起时间早已越过阈值

            Assert.That(longRun.IsInFlight(Now, isPlaying: true), Is.True,
                "Play 中的运行恒算在途（20 分钟 soak 的发起时间必然过期）");
            Assert.That(longRun.IsInFlight(Now, isPlaying: false), Is.False, "陈旧残留自动放行");
        }

        [Test]
        public void RunRecord_删除后读回为空()
        {
            var path = PathOf("axu_scenario_run.json");
            ScenarioRunRecord.Write(ScenarioRunRecord.Create("C:/proj/demo.suite.json", null,
                ScenarioRunRecord.SourcePipeline), path);

            ScenarioRunRecord.Delete(path);

            Assert.That(ScenarioRunRecord.TryRead(path), Is.Null);
        }

        // ------------------------------------------------------------------ 看门狗丢失/不匹配状态恢复

        [Test]
        public void Watchdog_状态文件缺失_保留接手宽限后有界收尾()
        {
            var record = RecordStartedAt(Now);
            var missing = AxuScenarioRunStatus.TryRead(PathOf("missing-status.json"));

            Assert.That(missing, Is.Null);
            Assert.That(ScenarioRunWatchdogPolicy.ShouldAbortUnmatched(record, missing,
                Now.AddSeconds(AxuRunLiveness.StartGraceSeconds - 1)), Is.False);
            Assert.That(ScenarioRunWatchdogPolicy.ShouldAbortUnmatched(record, missing,
                Now.AddMinutes(15)), Is.True);
        }

        [Test]
        public void Watchdog_状态JSON损坏_按缺失状态的有界策略收尾()
        {
            var path = PathOf("broken-status.json");
            File.WriteAllText(path, "{ this is broken");
            var record = RecordStartedAt(Now);
            var unreadable = AxuScenarioRunStatus.TryRead(path);

            Assert.That(unreadable, Is.Null);
            Assert.That(ScenarioRunWatchdogPolicy.ShouldAbortUnmatched(record, unreadable,
                Now.AddMinutes(16)), Is.True);
        }

        [Test]
        public void Watchdog_runId不匹配_陈旧活动可提前收尾且发起时间提供硬上限()
        {
            var record = RecordStartedAt(Now);
            var otherRun = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Running,
                RunId = "other-run",
                StartedAtUtc = Iso(Now.AddMinutes(-20)),
                HeartbeatUtc = Iso(Now.AddMinutes(-16))
            };

            Assert.That(record.MatchesStatus(otherRun), Is.False);
            Assert.That(ScenarioRunWatchdogPolicy.ShouldAbortUnmatched(record, otherRun, Now.AddMinutes(2)), Is.True,
                "状态可读但活动已停超过阈值时可提前结束");

            otherRun.HeartbeatUtc = Iso(Now.AddMinutes(14));
            Assert.That(ScenarioRunWatchdogPolicy.ShouldAbortUnmatched(record, otherRun, Now.AddMinutes(14)), Is.False,
                "别的 runId 有新活动时仍保留观察窗口");
            Assert.That(ScenarioRunWatchdogPolicy.ShouldAbortUnmatched(record, otherRun, Now.AddMinutes(15)), Is.True,
                "任何不匹配状态都受本轮发起时间硬上限约束");
        }

        [Test]
        public void Watchdog_正常Agent接手_启动宽限不误判()
        {
            var record = RecordStartedAt(Now);
            Assert.That(ScenarioRunWatchdogPolicy.ShouldAbortUnmatched(record, null,
                Now.AddSeconds(AxuRunLiveness.StartGraceSeconds - 1)), Is.False);

            var claimed = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Running,
                RunId = record.RunId,
                StartedAtUtc = record.StartedAtUtc,
                HeartbeatUtc = Iso(Now.AddSeconds(AxuRunLiveness.StartGraceSeconds + 1))
            };
            Assert.That(record.MatchesStatus(claimed), Is.True);
            Assert.That(ScenarioRunWatchdogPolicy.ShouldAbortUnmatched(record, claimed, Now.AddSeconds(15)), Is.False);
        }

        // ------------------------------------------------------------------ 启动准备失败回滚（桥文件写入注入）

        [Test]
        public void RunPreparation_桥文件写失败_回滚本轮状态与记录且可重试()
        {
            var recordPath = PathOf("prepare-run.json");
            var statusPath = PathOf("prepare-status.json");
            var bridgePath = PathOf("foreign-bridge.json");
            File.WriteAllText(bridgePath, "{\"runId\":\"another-run\"}");
            const string previousStatus = "{\"status\":\"completed\",\"runId\":\"another-run\"}";
            File.WriteAllText(statusPath, previousStatus);
            var record = ScenarioRunRecord.Create("C:/proj/demo.suite.json", null, ScenarioRunRecord.SourceMenu);

            var prepare = typeof(ScenarioRunService).GetMethod("PrepareRunFiles",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(prepare, Is.Not.Null);
            var arguments = new object[]
            {
                record, "demo", recordPath, statusPath, bridgePath,
                (Action)(() => throw new IOException("injected bridge write failure"))
            };

            var failure = Assert.Throws<TargetInvocationException>(() => prepare.Invoke(null, arguments));
            Assert.That(failure.InnerException, Is.TypeOf<IOException>());
            Assert.That(ScenarioRunRecord.TryRead(recordPath), Is.Null);
            Assert.That(File.ReadAllText(statusPath), Is.EqualTo(previousStatus), "回滚应恢复其他运行的状态文件");
            Assert.That(File.Exists(bridgePath), Is.True, "回滚不能删除其他 runId 的文件");
            var preserved = AxuScenarioRunStatus.TryRead(statusPath);
            Assert.That(AxuScenarioRunStatus.IsInFlight(preserved, Now), Is.False,
                "上轮终态不能让互斥闸误判当前仍在途");
            Assert.That(ScenarioRunStatusPresenter.Label(preserved), Does.Not.Contain("正在运行"),
                "看板不能显示启动失败的假 running 状态");

            File.Delete(bridgePath);
            Assert.Throws<TargetInvocationException>(() => prepare.Invoke(null, new object[]
            {
                record, "demo", recordPath, statusPath, bridgePath,
                (Action)(() =>
                {
                    File.WriteAllText(bridgePath, "{\"runId\":\"" + record.RunId + "\"}");
                    throw new IOException("injected failure after partial bridge creation");
                })
            }));
            Assert.That(ScenarioRunRecord.TryRead(recordPath), Is.Null);
            Assert.That(File.ReadAllText(statusPath), Is.EqualTo(previousStatus));
            Assert.That(File.Exists(bridgePath), Is.False, "本轮已写出的孤儿桥文件应被清理");

            prepare.Invoke(null, new object[]
            {
                record, "demo", recordPath, statusPath, bridgePath,
                (Action)(() => File.WriteAllText(bridgePath, "{\"runId\":\"" + record.RunId + "\"}"))
            });
            Assert.That(ScenarioRunRecord.TryRead(recordPath).RunId, Is.EqualTo(record.RunId));
            Assert.That(AxuScenarioRunStatus.TryRead(statusPath).RunId, Is.EqualTo(record.RunId));
            Assert.That(File.Exists(bridgePath), Is.True);
        }

        static ScenarioRunRecord RecordStartedAt(DateTime started)
        {
            var record = ScenarioRunRecord.Create("C:/proj/demo.suite.json", null, ScenarioRunRecord.SourcePipeline);
            record.StartedAtUtc = Iso(started);
            return record;
        }
    }
}
