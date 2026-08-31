using System.Collections.Generic;
using System.Reflection;
using AxuTestFramework.Editor;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] "选定测试"通道（TestNames → UTF Filter.testNames）的解析与编排语义：
    /// ResolveTestNames 去空白/去重/全空白报错；BuildFilterInfo 的 testNames 通道优先级；
    /// StartRunAsync 的并发闸拒绝（经状态文件制造在途态并在测后还原，全程不真正启动测试运行）。
    /// 私有方法经反射调用——签名变更时本套件应显式失败以提醒同步。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class AxuTestRunSelectionTests
    {
        // ------------------------------------------------------------------ ResolveTestNames

        [Test]
        public void ResolveTestNames_名单为null或空_返回null且无错误()
        {
            Assert.That(InvokeResolveTestNames(new AxuTestRunRequest(), out var error), Is.Null);
            Assert.That(error, Is.Null);
            Assert.That(InvokeResolveTestNames(new AxuTestRunRequest { TestNames = new List<string>() }, out error), Is.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void ResolveTestNames_含空白项与重复项_去空白去重并保留首现顺序()
        {
            var request = new AxuTestRunRequest
            {
                TestNames = new List<string> { " B.C.T2 ", "A.B.T1", "", "A.B.T1", "  " }
            };

            var names = InvokeResolveTestNames(request, out var error);

            Assert.That(error, Is.Null);
            Assert.That(names, Is.EqualTo(new[] { "B.C.T2", "A.B.T1" }));
        }

        [Test]
        public void ResolveTestNames_全为空白_返回错误且名单为null()
        {
            var request = new AxuTestRunRequest { TestNames = new List<string> { " ", "", "  " } };

            var names = InvokeResolveTestNames(request, out var error);

            Assert.That(names, Is.Null);
            Assert.That(error, Does.Contain("test_names").And.Contain("空白"));
        }

        // ------------------------------------------------------------------ BuildFilterInfo

        [Test]
        public void BuildFilterInfo_名单非空_类型为testNames且优先于其他过滤通道()
        {
            var request = new AxuTestRunRequest
            {
                Filter = "substring",
                FilterType = "testName",
                Category = "Axu/Smoke",
                Assembly = "Some.Tests",
                TestNames = new List<string> { "A.B.T1" }
            };

            var filter = InvokeBuildFilterInfo(request, new List<string> { "A.B.T1" });

            Assert.That(filter.Type, Is.EqualTo("testNames"));
            Assert.That(filter.Value, Does.Contain("1"));
        }

        [Test]
        public void BuildFilterInfo_名单为空_回落到既有过滤通道()
        {
            var withCategory = InvokeBuildFilterInfo(
                new AxuTestRunRequest { Category = "Axu/Smoke", Filter = "ignored" }, null);
            Assert.That(withCategory.Type, Is.EqualTo("category"));

            var none = InvokeBuildFilterInfo(new AxuTestRunRequest(), null);
            Assert.That(none.Type, Is.EqualTo("none"));
        }

        // ------------------------------------------------------------------ StartRunAsync 并发闸

        [Test]
        public void StartRunAsync_已有运行在途_拒绝并发并提示在途()
        {
            // pipeline 自带 run_tests 的异步在途标记存在时，闸的另一个分支先触发——此处只覆盖 axu 状态文件分支
            var pipelineRequestFile = AxuTestReportPaths.ToFullPath("Temp/pipeline_test_request.json");
            if (System.IO.File.Exists(pipelineRequestFile))
                Assert.Ignore("pipeline 自带 run_tests 在途，跳过 axu 状态文件分支的互斥断言。");

            // 制造在途态；外层若是 axu_test_run 会话则 backup 本身就是 running，测后原样还原即无净变化
            var backup = AxuTestRunStatus.TryRead();
            try
            {
                AxuTestRunStatus.Write(new AxuTestRunStatus
                {
                    Status = AxuTestRunStatus.Running,
                    Mode = "editor",
                    StartedAtUtc = System.DateTime.UtcNow.ToString("o")
                });

                var outcome = AxuTestRunService.StartRunAsync(new AxuTestRunRequest { Mode = "editor" })
                    .GetAwaiter().GetResult();

                Assert.That(outcome.Status, Is.EqualTo("error"));
                Assert.That(outcome.Error, Does.Contain("在途"));
            }
            finally
            {
                if (backup != null)
                    AxuTestRunStatus.Write(backup);
                else
                    AxuTestRunStatus.Delete();
            }
        }

        // ------------------------------------------------------------------ 反射辅助

        private static List<string> InvokeResolveTestNames(AxuTestRunRequest request, out string error)
        {
            var method = typeof(AxuTestRunService).GetMethod("ResolveTestNames", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "AxuTestRunService.ResolveTestNames 签名变更后未同步本测试。");
            var args = new object[] { request, null };
            var names = (List<string>)method.Invoke(null, args);
            error = (string)args[1];
            return names;
        }

        private static FilterInfo InvokeBuildFilterInfo(AxuTestRunRequest request, List<string> testNames)
        {
            var method = typeof(AxuTestRunService).GetMethod("BuildFilterInfo", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "AxuTestRunService.BuildFilterInfo 签名变更后未同步本测试。");
            return (FilterInfo)method.Invoke(null, new object[] { request, testNames });
        }
    }
}
