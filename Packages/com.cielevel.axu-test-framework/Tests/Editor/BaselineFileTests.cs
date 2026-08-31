using System.Collections.Generic;
using System.IO;
using AxuTestFramework.Core.Baseline;
using AxuTestFramework.Core.Json;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] BaselineFile 基线 JSON 落盘回路（Temp 目录，TearDown 清理；Phase 2 契约驱动测试）。
    /// 被测类型由并行实现落地，本套件签名即契约；实现若有出入，集成期以实现核对后同步修正。
    /// 关键契约点：
    ///  - BaselineFile POCO：Schema("axu-baseline/1")/ScenarioId/Metrics(Dictionary&lt;string,double&gt; 两段键→值)；
    ///  - BaselineFile.Save(path, baseline) 落盘 UTF-8 JSON；Load(path) 校验 schema 后还原；
    ///  - schema 不符或缺失 → AxuSchemaException（沿用 AxuJson.FromFile 语义）；
    ///  - Load 目标文件不存在 → FileNotFoundException（AxuJson 内层 File.ReadAllText 原样抛出）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class BaselineFileTests
    {
        static readonly string TempDir = Path.Combine(Path.GetTempPath(), "AxuBaselineFileTests");

        [SetUp]
        public void SetUp() => Directory.CreateDirectory(TempDir);

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(TempDir)) Directory.Delete(TempDir, recursive: true);
        }

        static string PathOf(string fileName) => Path.Combine(TempDir, fileName);

        static BaselineFile SampleBaseline() => new BaselineFile
        {
            ScenarioId = "perf.combat.fhd",
            Metrics = new Dictionary<string, double>
            {
                ["fps.avg"] = 48.1,
                ["frameTime.cpuP95"] = 24.8,
                ["memory.allocatedMax"] = 1832.4,
            },
        };

        // ------------------------------------------------------------------ 读写回路

        [Test]
        public void Save后Load_字段往返一致()
        {
            var path = PathOf("roundtrip.json");

            BaselineFile.Save(path, SampleBaseline());
            Assert.That(File.Exists(path), Is.True, "Save 应在指定路径落盘。");

            var loaded = BaselineFile.Load(path);

            Assert.That(loaded.Schema, Is.EqualTo("axu-baseline/1"));
            Assert.That(loaded.ScenarioId, Is.EqualTo("perf.combat.fhd"));
            Assert.That(loaded.Metrics, Is.Not.Null);
            Assert.That(loaded.Metrics, Has.Count.EqualTo(3));
            Assert.That(loaded.Metrics["fps.avg"], Is.EqualTo(48.1).Within(1e-9));
            Assert.That(loaded.Metrics["frameTime.cpuP95"], Is.EqualTo(24.8).Within(1e-9));
            Assert.That(loaded.Metrics["memory.allocatedMax"], Is.EqualTo(1832.4).Within(1e-9));
        }

        // ------------------------------------------------------------------ schema 校验

        [Test]
        public void Load_schema不符_抛AxuSchemaException()
        {
            var path = PathOf("wrong-schema.json");
            File.WriteAllText(path, "{ \"schema\": \"axu-baseline/2\", \"scenarioId\": \"x\", \"metrics\": {} }");

            Assert.Throws<AxuSchemaException>(() => BaselineFile.Load(path));
        }

        [Test]
        public void Load_schema字段缺失_抛AxuSchemaException()
        {
            var path = PathOf("no-schema.json");
            File.WriteAllText(path, "{ \"scenarioId\": \"x\", \"metrics\": {} }");

            Assert.Throws<AxuSchemaException>(() => BaselineFile.Load(path));
        }

        // ------------------------------------------------------------------ 缺失文件

        [Test]
        public void Load_文件不存在_抛明确异常()
        {
            var path = PathOf("missing.json");
            Assert.That(File.Exists(path), Is.False);

            Assert.Throws<FileNotFoundException>(() => BaselineFile.Load(path));
        }
    }
}
