using System.Collections.Generic;
using System.Threading.Tasks;
using AxuFrameKit._Core.Utility.Multiton;
using AxuFrameKit._Core.Utility.TaskQ;
using NUnit.Framework;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Core/Util] TaskQ 基本队列语义：EnQ/Then FIFO 顺序执行。
    /// EditMode 下仅使用同步完成的任务（Task.CompletedTask）——Run() 内同步任务会立即顺序执行完；
    /// 含真实异步等待的语义（Delay/Stop 挂起）需要跨帧，属 PlayMode 范畴，此处不覆盖。
    /// </summary>
    [Category("Axu/Core/Util")]
    public class CoreTaskQTests
    {
        [Test]
        public void TaskQ_入队同步任务_Run_按FIFO顺序执行()
        {
            // Arrange：独立 Multiton key，测试后彻底清理
            const string key = "AxuTest.TaskQ.Fifo";
            var q = TaskQ.GetInstance(key);
            var order = new List<int>();

            try
            {
                // Act：混合 EnQ(Func&lt;Task&gt;) 与 Then(Action)，链式入队
                q.EnQ(() =>
                    {
                        order.Add(1);
                        return Task.CompletedTask;
                    })
                    .Then(() => order.Add(2))
                    .Then(() => order.Add(3));
                q.Run();

                // Assert：同步任务在 Run 调用内即按 FIFO 完成
                CollectionAssert.AreEqual(new[] { 1, 2, 3 }, order);
            }
            finally
            {
                q.Stop();
                Multiton<TaskQ>.DisposeInstance(key);
            }
        }
    }
}
