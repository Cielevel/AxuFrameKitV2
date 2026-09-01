using AxuTestFramework.Agent.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] AgentLogWatch 计数/快照/上限/截断契约（Phase 3 P0，对齐已落地实现）。
    /// 经真实日志通道（Debug.LogError/LogWarning）驱动，全部预期日志先用 LogAssert.Expect 声明。
    /// 关键契约点：
    ///  - Error/Exception/Assert 计入 ErrorCount，Warning 计入 WarningCount；
    ///  - RecentErrors 为 FIFO 环形窗口：满 RecentErrorLimit(20) 后淘汰最旧、追加最新，
    ///    恒保留最近 N 条；被淘汰总数计入 DroppedCount（= ErrorCount − 当前保留条数）；
    ///  - 单条快照截断 EntryMaxLength(500) 字符 + "…"；
    ///  - ResetCounts 清零（含 DroppedCount）；Dispose 后不再计数（幂等）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class AgentLogWatchTests
    {
        [Test]
        public void LogWatch_Error与Warning_计数与快照正确()
        {
            using (var watch = new AgentLogWatch())
            {
                LogAssert.Expect(LogType.Error, "axu-watch-e1");
                Debug.LogError("axu-watch-e1");

                LogAssert.Expect(LogType.Warning, "axu-watch-w1");
                Debug.LogWarning("axu-watch-w1");

                Assert.That(watch.ErrorCount, Is.EqualTo(1));
                Assert.That(watch.WarningCount, Is.EqualTo(1));
                Assert.That(watch.RecentErrors.Count, Is.EqualTo(1));
                Assert.That(watch.RecentErrors[0], Does.Contain("axu-watch-e1"));
                Assert.That(watch.DroppedCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void ResetCounts_计数与快照清零()
        {
            var watch = new AgentLogWatch();
            try
            {
                LogAssert.Expect(LogType.Error, "axu-reset-e1");
                Debug.LogError("axu-reset-e1");

                watch.ResetCounts();

                Assert.That(watch.ErrorCount, Is.EqualTo(0));
                Assert.That(watch.RecentErrors, Is.Empty);
            }
            finally
            {
                watch.Dispose();
            }
        }

        [Test]
        public void RecentErrors_超过20条_FIFO淘汰保留最新20条()
        {
            var watch = new AgentLogWatch();
            try
            {
                for (var i = 1; i <= 23; i++)
                {
                    var message = "axu-limit-" + i;
                    LogAssert.Expect(LogType.Error, message);
                    Debug.LogError(message);
                }

                // FIFO 环形窗口语义：恒保留"最近 20 条"（第 21 条进入后第 1 条即不在快照中），
                // 被淘汰总数计入 DroppedCount（= ErrorCount − 当前保留条数）。ErrorCount 恒为全量。
                Assert.That(watch.ErrorCount, Is.EqualTo(23));
                Assert.That(watch.RecentErrors.Count, Is.EqualTo(AgentLogWatch.RecentErrorLimit));
                Assert.That(watch.DroppedCount, Is.EqualTo(3), "淘汰总数 = 23 − 20。");
                Assert.That(watch.RecentErrors[0], Does.Contain("axu-limit-4"), "最旧的 3 条已被淘汰，保留窗口从第 4 条开始。");
                Assert.That(watch.RecentErrors[19], Does.Contain("axu-limit-23"), "窗口末位为最新一条。");

                // ResetCounts 清零不变量：全零后 DroppedCount = ErrorCount − 保留数 = 0
                watch.ResetCounts();
                Assert.That(watch.ErrorCount, Is.EqualTo(0));
                Assert.That(watch.RecentErrors, Is.Empty);
                Assert.That(watch.DroppedCount, Is.EqualTo(0));
            }
            finally
            {
                watch.Dispose();
            }
        }

        [Test]
        public void RecentErrors_超长错误_截断至500字符加省略号()
        {
            var watch = new AgentLogWatch();
            try
            {
                var message = new string('x', 600);
                LogAssert.Expect(LogType.Error, message);
                Debug.LogError(message);

                Assert.That(watch.RecentErrors[0].Length,
                    Is.EqualTo(AgentLogWatch.EntryMaxLength + 1), "快照 = 前 500 字符 + 结尾省略号。");
                Assert.That(watch.RecentErrors[0], Does.EndWith("…"));
            }
            finally
            {
                watch.Dispose();
            }
        }

        [Test]
        public void Dispose后_不再计数()
        {
            var watch = new AgentLogWatch();
            watch.Dispose();
            watch.Dispose(); // 幂等

            LogAssert.Expect(LogType.Error, "axu-dispose-e1");
            Debug.LogError("axu-dispose-e1");

            Assert.That(watch.ErrorCount, Is.EqualTo(0));
            Assert.That(watch.RecentErrors, Is.Empty);
        }
    }
}
