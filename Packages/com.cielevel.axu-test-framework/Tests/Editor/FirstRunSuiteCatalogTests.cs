using System.IO;
using System.Linq;
using AxuTestFramework.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] FirstRunSuiteCatalog：自检/目标 suite JSON 供给契约（阶段 2 首次运行引导）。
    /// 关键契约点：
    ///  - 自检 JSON 必须能过 <see cref="ScenarioRunService"/> 的 TryPrevalidateSuiteFile 真实校验（不是自欺）；
    ///  - 自检保持"纯工具链语义"：无 budgets、无 baseline、无场景依赖（echo 内建能力）；
    ///  - <see cref="FirstRunSuiteCatalog.EnsureSelfCheckSuiteFile"/> 幂等：重复调用路径稳定、内容一致；
    ///  - scene/perf 目标 JSON 同样过真实预检；未知目标返回 null。
    /// 注意：TryCreateGoalSuite 会写 Assets 并触发 AssetDatabase 刷新，不在单测里调用（留给 E2E）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class FirstRunSuiteCatalogTests
    {
        static readonly string TempDir = Path.Combine(Path.GetTempPath(), "AxuFirstRunSuiteCatalogTests");
        static readonly string SuiteIdScene = "axu-goal-scene-smoke";
        static readonly string SuiteIdPerf = "axu-goal-perf";

        [SetUp]
        public void SetUp() => Directory.CreateDirectory(TempDir);

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(TempDir))
                Directory.Delete(TempDir, recursive: true);

            // 清理 EnsureSelfCheckSuiteFile 落到工程 Temp 的真实文件（Temp 为会话级目录，删了无害）
            var selfCheckFullPath = AxuTestReportPaths.ToFullPath(FirstRunSuiteCatalog.SelfCheckSuiteTempPath);
            if (File.Exists(selfCheckFullPath))
                File.Delete(selfCheckFullPath);
        }

        static string WriteTemp(string fileName, string content)
        {
            var path = Path.Combine(TempDir, fileName);
            File.WriteAllText(path, content, new System.Text.UTF8Encoding(false));
            return Path.GetFullPath(path);
        }

        // ------------------------------------------------------------------ 自检套件（真实预检）

        [Test]
        public void 自检JSON_经TryPrevalidateSuiteFile_校验通过且suiteId为axu_selfcheck_echo()
        {
            var path = WriteTemp("axu_selfcheck_echo.suite.json", FirstRunSuiteCatalog.SelfCheckSuiteJson);

            var ok = ScenarioRunService.TryPrevalidateSuiteFile(path, out var suiteId, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(suiteId, Is.EqualTo(FirstRunSuiteCatalog.SelfCheckSuiteId));
        }

        [Test]
        public void 自检JSON_无budgets无baseline_保持纯工具链语义()
        {
            var root = JObject.Parse(FirstRunSuiteCatalog.SelfCheckSuiteJson);
            var scenario = root["scenarios"][0];

            Assert.That(scenario["capability"]?.ToString(), Is.EqualTo("echo"),
                "自检用 echo 内建能力：仅延时+日志，不依赖任何项目内容");
            Assert.That(scenario["budgets"], Is.Null.Or.Empty, "自检不得带预算（预算属项目语义）");
            Assert.That(scenario["baseline"], Is.Null.Or.Empty, "自检不得带基线");
            Assert.That(root["baseline"], Is.Null.Or.Empty);
            Assert.That(scenario["params"]?["scene"], Is.Null, "自检不得指定场景");
        }

        // ------------------------------------------------------------------ EnsureSelfCheckSuiteFile（幂等落盘）

        [Test]
        public void EnsureSelfCheckSuiteFile_重复调用_路径稳定内容一致()
        {
            var path1 = FirstRunSuiteCatalog.EnsureSelfCheckSuiteFile(out var error1);
            var path2 = FirstRunSuiteCatalog.EnsureSelfCheckSuiteFile(out var error2);

            Assert.That(error1, Is.Null);
            Assert.That(error2, Is.Null);
            Assert.That(path1, Is.Not.Null);
            Assert.That(path2, Is.EqualTo(path1), "幂等：路径不漂移");
            Assert.That(Path.GetFullPath(path1),
                Is.EqualTo(AxuTestReportPaths.ToFullPath(FirstRunSuiteCatalog.SelfCheckSuiteTempPath)));
            Assert.That(File.ReadAllText(path1), Is.EqualTo(FirstRunSuiteCatalog.SelfCheckSuiteJson),
                "幂等：内容不随调用次数变化");

            // 落盘内容能过真实预检（写文件与 JSON 契约不脱节）
            Assert.That(
                ScenarioRunService.TryPrevalidateSuiteFile(Path.GetFullPath(path1), out _, out var error),
                Is.True, error);
        }

        [Test]
        public void EnsureSelfCheckSuiteFile_目录缺失_自动创建不抛()
        {
            // Temp 目录在 Unity 会话中恒存在（无法安全移除验证"缺失"分支），退而验证：目录随写入自动就位
            var path = FirstRunSuiteCatalog.EnsureSelfCheckSuiteFile(out var error);

            Assert.That(error, Is.Null);
            Assert.That(path, Is.Not.Null);
            Assert.That(Directory.Exists(Path.GetDirectoryName(path)), Is.True, "目录应随写入自动存在");
        }

        // ------------------------------------------------------------------ 目标套件（scene / perf）

        [Test]
        public void BuildGoalSuiteJson_scene与perf_经TryPrevalidateSuiteFile_校验通过()
        {
            var sceneJson = FirstRunSuiteCatalog.BuildGoalSuiteJson("scene");
            var perfJson = FirstRunSuiteCatalog.BuildGoalSuiteJson("perf");
            Assert.That(sceneJson, Is.Not.Null);
            Assert.That(perfJson, Is.Not.Null);

            var scenePath = WriteTemp("scene.goal.suite.json", sceneJson);
            Assert.That(
                ScenarioRunService.TryPrevalidateSuiteFile(scenePath, out var sceneSuiteId, out var sceneError),
                Is.True, sceneError);
            Assert.That(sceneSuiteId, Is.EqualTo(SuiteIdScene));

            var perfPath = WriteTemp("perf.goal.suite.json", perfJson);
            Assert.That(
                ScenarioRunService.TryPrevalidateSuiteFile(perfPath, out var perfSuiteId, out var perfError),
                Is.True, perfError);
            Assert.That(perfSuiteId, Is.EqualTo(SuiteIdPerf));
        }

        [Test]
        public void BuildGoalSuiteJson_scene与perf_参数符合引导语义()
        {
            var scene = JObject.Parse(FirstRunSuiteCatalog.BuildGoalSuiteJson("scene"))["scenarios"][0];
            Assert.That(scene["capability"]?.ToString(), Is.EqualTo("smoke"));
            Assert.That(scene["params"]?["source"]?.ToString(), Is.EqualTo("buildSettings"),
                "scene 目标遍历构建场景清单");
            Assert.That(scene["budgets"], Is.Null.Or.Empty, "引导套件不带预算");

            var perf = JObject.Parse(FirstRunSuiteCatalog.BuildGoalSuiteJson("perf"))["scenarios"][0];
            Assert.That(perf["capability"]?.ToString(), Is.EqualTo("perf"));
            Assert.That(perf["params"]?["warmupSeconds"]?.Value<float>(), Is.EqualTo(1f));
            Assert.That(perf["params"]?["measureSeconds"]?.Value<float>(), Is.EqualTo(5f));
            Assert.That(perf["params"]?["scene"], Is.Null.Or.Empty, "不指定 scene = 采样当前场景");
            Assert.That(perf["budgets"], Is.Null.Or.Empty, "引导套件不带预算（perf 目标尤然）");
        }

        [Test]
        public void BuildGoalSuiteJson_未知目标_返回null()
        {
            Assert.That(FirstRunSuiteCatalog.BuildGoalSuiteJson("nope"), Is.Null);
            Assert.That(FirstRunSuiteCatalog.BuildGoalSuiteJson(null), Is.Null);
            Assert.That(FirstRunSuiteCatalog.BuildGoalSuiteJson(""), Is.Null);
            Assert.That(FirstRunSuiteCatalog.BuildGoalSuiteJson("selfcheck"), Is.Null,
                "selfcheck 用内置自检套件，不需要目标套件文件");
            Assert.That(FirstRunSuiteCatalog.BuildGoalSuiteJson("code"), Is.Null,
                "code 走 UTF 运行，不需要目标套件文件");
        }

        [Test]
        public void Goals_四个目标id稳定_前提项与目标语义一致()
        {
            var goals = FirstRunSuiteCatalog.Goals;

            Assert.That(goals.Select(g => g.Id).ToArray(), Is.EqualTo(new[] { "selfcheck", "code", "scene", "perf" }));

            var scene = goals.First(g => g.Id == "scene");
            Assert.That(scene.NeedsAgentDefine, Is.True);
            Assert.That(scene.NeedsBuildScenes, Is.True, "scene 目标是唯一要求构建场景清单的");

            var code = goals.First(g => g.Id == "code");
            Assert.That(code.NeedsAgentDefine, Is.False, "单元测试不经 Agent");

            foreach (var goal in goals.Where(g => g.Id != "scene"))
                Assert.That(goal.NeedsBuildScenes, Is.False);
        }
    }
}
