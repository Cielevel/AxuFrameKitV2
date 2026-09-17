using System;
using System.Threading;
using System.Threading.Tasks;
using AxuFrameKit._Core.Utility.Multiton;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Utility.TaskQ
{
    /// <summary>
    /// 任务队列控制（需带参数：CancelToken，并要求在方法内实现Cancel的判断）（自动ID多例）
    /// </summary>
    public class TaskQcAuto : MultitonAuto<TaskQcAuto>, ITaskQc
    {
        private readonly TaskQueueEngine _engine;

        // 状态锁：保证 CTS 更替与引擎启停的原子性，避免 Stop/Run 并发交错出"令牌与循环不配对"
        private readonly object _gate = new();

        private CancellationTokenSource _cts;

        public TaskQcAuto() => _engine = new TaskQueueEngine(OnTaskError);

        public bool IsRunning => _engine.IsRunning;

        /// <summary>
        /// 单任务异常上报：Stop/StopAndClear/Dispose 触发的取消属正常停止流程，保持旧版"队列已取消"日志口径；
        /// 其余异常仅记录、不中止整队（修复旧版单任务异常中止整队的问题）。
        /// </summary>
        private void OnTaskError(Exception ex)
        {
            if (ex is OperationCanceledException && (_cts == null || _cts.IsCancellationRequested))
            {
                Logger.Error("任务队列已取消，TaskQ.Key：" + Key);
                return;
            }

            Logger.Error($"任务执行失败: {ex}");
        }

        public ITaskQc EnQ(Func<CancellationToken, Task> func)
        {
            // 令牌由引擎在任务实际执行时传入（取自当轮 Run 的 CTS），保持旧版语义
            _engine.Enqueue(func);
            return this;
        }

        ITaskQc ITaskQc.Then(Func<CancellationToken, Task> func)
        {
            _engine.Enqueue(func);
            return this;
        }

        ITaskQc ITaskQc.Then(Action action)
        {
            _engine.Enqueue(_ =>
            {
                action?.Invoke();
                return Task.CompletedTask;
            });
            return this;
        }

        ITaskQc ITaskQc.Delay(float delay)
        {
            _engine.Enqueue(async token => { await Task.Delay(TimeSpan.FromSeconds(delay), token); });
            return this;
        }

        /// <summary>
        /// 运行当前的同步队列，在stop后，也可再次Run实现Continue。
        /// 每轮 Run 更替全新 CTS（旧 CTS 取消并释放），修复旧版重复 Run 不释放旧 CTS 的泄漏。
        /// </summary>
        public void Run()
        {
            lock (_gate)
            {
                if (IsRunning)
                    return;

                ReplaceCts();
                _engine.Run(_cts.Token);
            }
        }

        public void Stop()
        {
            lock (_gate)
            {
                _cts?.Cancel();
                _engine.Stop();
            }
        }

        public void StopAndClear()
        {
            lock (_gate)
            {
                _cts?.Cancel();
                _engine.StopAndClear();
            }
        }

        protected override void OnDispose()
        {
            lock (_gate)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
                _engine.StopAndClear();
            }
        }

        /// <summary> 取消并释放旧 CTS 后新建，供新一轮消费循环使用全新令牌 </summary>
        private void ReplaceCts()
        {
            var old = _cts;
            _cts = new CancellationTokenSource();
            if (old == null)
                return;

            // 先取消再释放：让仍在途的旧令牌任务尽快短路结束（Task.Delay 等会立即返回取消）
            try
            {
                old.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // 已释放过则无需处理
            }

            old.Dispose();
        }
    }
}
