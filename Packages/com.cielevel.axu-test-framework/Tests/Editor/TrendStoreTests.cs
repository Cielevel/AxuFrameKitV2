using System.Collections.Generic;
using System.IO;
using System.Linq;
using AxuTestFramework.Core.Json;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Trend;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] TrendStore 趋势 CSV 落盘回路（Temp 目录，TearDown 清理；Phase 4 契约驱动测试）。
    /// 被测类型由并行实现落地，本套件签名即契约；实现若有出入，集成期以实现核对后同步修正。
    /// 关键契约点：
    ///  - TrendStore.SchemaHeader == "axu-trend/1"；
    ///  - Append(path, point) 首次追加自动写入 header 行，文件已存在则纯追加；
    ///  - Read(path)：缺文件 → 空表；header 不符 → AxuSchemaException；CSV 字段需正确转义（逗号/引号/换行）；
    ///  - FromReport(report, suiteId)：每个 metric 一行（键形如 collector.stat）+ 一行 Metric="" 的 run 级行（Value=null）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class TrendStoreTests
    {
        static readonly string TempDir = Path.Combine(Path.GetTempPath(), "AxuTrendStoreTests");

        [SetUp]
        public void SetUp() => Directory.CreateDirectory(TempDir);

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(TempDir)) Directory.Delete(TempDir, recursive: true);
        }

        static string PathOf(string fileName) => Path.Combine(TempDir, fileName);

        static TrendPoint Point(double value, string timestamp = "2026-09-01T00:00:00Z") => new TrendPoint
        {
            TimestampUtc = timestamp,
            Suite = "suite.perf",
            Scenario = "perf.combat.fhd",
            Capability = "perf",
            Result = "pass",
            ExitCode = 0,
            DurationMs = 1234.5,
            Metric = "fps.avg",
            Value = value,
        };

        // ------------------------------------------------------------------ 追加与 header

        [Test]
        public void Append_文件不存在_自动写入SchemaHeader()
        {
            var path = PathOf("fresh.csv");

            TrendStore.Append(path, Point(48.1));

            Assert.That(File.Exists(path), Is.True, "Append 应自动创建文件。");
            var firstLine = File.ReadLines(path).First();
            Assert.That(firstLine, Is.EqualTo(TrendStore.SchemaHeader),
                "首行应为 schema header（axu-trend/1）。");
        }

        [Test]
        public void SchemaHeader_常量值为axuTrend1()
        {
            Assert.That(TrendStore.SchemaHeader, Is.EqualTo("axu-trend/1"));
        }

        [Test]
        public void Append后Read_字段往返一致()
        {
            var path = PathOf("roundtrip.csv");
            var point = Point(48.1, timestamp: "2026-09-01T08:30:00Z");

            TrendStore.Append(path, point);
            var loaded = TrendStore.Read(path);

            Assert.That(loaded, Has.Count.EqualTo(1));
            Assert.That(loaded[0].TimestampUtc, Is.EqualTo("2026-09-01T08:30:00Z"));
            Assert.That(loaded[0].Suite, Is.EqualTo("suite.perf"));
            Assert.That(loaded[0].Scenario, Is.EqualTo("perf.combat.fhd"));
            Assert.That(loaded[0].Capability, Is.EqualTo("perf"));
            Assert.That(loaded[0].Result, Is.EqualTo("pass"));
            Assert.That(loaded[0].ExitCode, Is.EqualTo(0));
            Assert.That(loaded[0].DurationMs, Is.EqualTo(1234.5).Within(1e-9));
            Assert.That(loaded[0].Metric, Is.EqualTo("fps.avg"));
            Assert.That(loaded[0].Value, Is.EqualTo(48.1).Within(1e-9));
        }

        [Test]
        public void 多次追加后Read_按写入顺序读回()
        {
            var path = PathOf("order.csv");

            TrendStore.Append(path, Point(10, timestamp: "2026-09-01T00:00:00Z"));
            TrendStore.Append(path, Point(20, timestamp: "2026-09-01T01:00:00Z"));
            TrendStore.Append(path, Point(30, timestamp: "2026-09-01T02:00:00Z"));

            var loaded = TrendStore.Read(path);

            Assert.That(loaded, Has.Count.EqualTo(3));
            Assert.That(loaded.Select(p => p.TimestampUtc),
                Is.EqualTo(new[] { "2026-09-01T00:00:00Z", "2026-09-01T01:00:00Z", "2026-09-01T02:00:00Z" }));
            Assert.That(loaded.Select(p => p.Value), Is.EqualTo(new double?[] { 10, 20, 30 }));
        }

        [Test]
        public void Append批量_一次写入多点且header只写一次()
        {
            var path = PathOf("batch.csv");
            var points = new[]
            {
                Point(1, timestamp: "2026-09-01T00:00:00Z"),
                Point(2, timestamp: "2026-09-01T01:00:00Z"),
            };

            TrendStore.Append(path, points);

            var loaded = TrendStore.Read(path);
            Assert.That(loaded, Has.Count.EqualTo(2));
            Assert.That(loaded.Select(p => p.Value), Is.EqualTo(new double?[] { 1, 2 }));
        }

        // ------------------------------------------------------------------ CSV 转义

        [TestCase("perf.combat,fhd", "逗号")]
        [TestCase("perf.\"quoted\"", "引号")]
        [TestCase("line1\nline2", "换行")]
        public void Scenario名含特殊字符_读回原样(string scenario, string what)
        {
            var path = PathOf($"escape-{what}.csv");
            var point = Point(48.1);
            point.Scenario = scenario;

            TrendStore.Append(path, point);
            var loaded = TrendStore.Read(path);

            Assert.That(loaded, Has.Count.EqualTo(1));
            Assert.That(loaded[0].Scenario, Is.EqualTo(scenario),
                $"含 {what} 的 scenario 名应经 CSV 转义后无损读回。");
        }

        // ------------------------------------------------------------------ Read 边界

        [Test]
        public void Read_文件不存在_返回空表()
        {
            var path = PathOf("missing.csv");
            Assert.That(File.Exists(path), Is.False);

            var loaded = TrendStore.Read(path);

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded, Is.Empty);
        }

        [Test]
        public void Read_header不符_抛AxuSchemaException()
        {
            var path = PathOf("wrong-schema.csv");
            File.WriteAllText(path, "axu-trend/2\nsome,data\n");

            Assert.Throws<AxuSchemaException>(() => TrendStore.Read(path));
        }

        [Test]
        public void Read_空文件_抛AxuSchemaException()
        {
            var path = PathOf("empty.csv");
            File.WriteAllText(path, string.Empty);

            Assert.Throws<AxuSchemaException>(() => TrendStore.Read(path));
        }

        // ------------------------------------------------------------------ FromReport

        static ScenarioReport SampleReport()
        {
            return new ScenarioReport
            {
                SuiteId = "ignored-report-suite",
                StartedAt = "2026-09-01T08:00:00Z",
                EndedAt = "2026-09-01T08:01:00Z",
                ExitCode = ScenarioExitCode.Success,
                Results = new List<ScenarioReportResult>
                {
                    new ScenarioReportResult
                    {
                        ScenarioId = "perf.combat.fhd",
                        Capability = "perf",
                        Status = "pass",
                        DurationMs = 60000,
                        Metrics = new Dictionary<string, Dictionary<string, double>>
                        {
                            ["fps"] = new Dictionary<string, double> { ["avg"] = 55.5 },
                            ["frameTime"] = new Dictionary<string, double> { ["cpuP95"] = 20.5 },
                            ["memory"] = new Dictionary<string, double> { ["allocatedMax"] = 1800 },
                        },
                    },
                },
            };
        }

        [Test]
        public void FromReport_含三个metric_产出四行()
        {
            var rows = TrendStore.FromReport(SampleReport(), "suite.perf").ToList();

            Assert.That(rows, Has.Count.EqualTo(4),
                "行数 = metric 数(3) + 1 条 run 级行。");
        }

        [Test]
        public void FromReport_metric行_字段取自报告与suiteId参数()
        {
            var rows = TrendStore.FromReport(SampleReport(), "suite.perf").ToList();
            var metricRows = rows.Where(r => r.Metric != "").ToList();

            Assert.That(metricRows, Has.Count.EqualTo(3));
            Assert.That(metricRows.Select(r => r.Metric).OrderBy(m => m),
                Is.EqualTo(new[] { "fps.avg", "frameTime.cpuP95", "memory.allocatedMax" }));

            foreach (var row in metricRows)
            {
                Assert.That(row.TimestampUtc, Is.EqualTo("2026-09-01T08:00:00Z"));
                Assert.That(row.Suite, Is.EqualTo("suite.perf"),
                    "Suite 取 suiteId 参数而非 report.SuiteId。");
                Assert.That(row.Scenario, Is.EqualTo("perf.combat.fhd"));
                Assert.That(row.Capability, Is.EqualTo("perf"));
                Assert.That(row.Result, Is.EqualTo("pass"));
                Assert.That(row.ExitCode, Is.EqualTo(0));
                Assert.That(row.DurationMs, Is.EqualTo(60000).Within(1e-9));
            }

            var fpsRow = metricRows.First(r => r.Metric == "fps.avg");
            Assert.That(fpsRow.Value, Is.EqualTo(55.5).Within(1e-9));
        }

        [Test]
        public void FromReport_run行_Metric为空且Value为null()
        {
            var rows = TrendStore.FromReport(SampleReport(), "suite.perf").ToList();

            var runRow = rows.Single(r => r.Metric == "");
            Assert.That(runRow.Value, Is.Null, "run 级行 Value 应为 null。");
            Assert.That(runRow.Scenario, Is.EqualTo("perf.combat.fhd"));
            Assert.That(runRow.DurationMs, Is.EqualTo(60000).Within(1e-9));
        }

        [Test]
        public void FromReport_无metric结果_仅run行()
        {
            var report = SampleReport();
            report.Results[0].Metrics = null;

            var rows = TrendStore.FromReport(report, "suite.perf").ToList();

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].Metric, Is.EqualTo(""));
            Assert.That(rows[0].Value, Is.Null);
        }
    }
}
