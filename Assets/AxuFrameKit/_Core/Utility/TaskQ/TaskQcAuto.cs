using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using AxuFrameKit._Core.Utility.Multiton;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit.Common.Utility.TaskQ
{
    /// <summary>
    /// 任务队列控制（需带参数：CancelToken，并要求在方法内实现Cancel的判断）（自动ID多例）
    /// </summary>
    public class TaskQcAuto : MultitonAuto<TaskQcAuto>, ITaskQc
    {
        public bool IsRunning { get; private set; }

        private readonly ConcurrentQueue<Func<CancellationToken, Task>> _taskQueue = new();

        private Task _currentTask;

        private CancellationTokenSource _cts;

        private async Task ProcessQueueAsync()
        {
            try
            {
                while (IsRunning && !_cts.IsCancellationRequested)
                {
                    if (_taskQueue.TryDequeue(out var taskFunc))
                        await taskFunc.Invoke(_cts.Token);
                    else
                        await Task.Yield();
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Error("任务队列已取消，TaskQ.Key：" + Key);
            }
            finally
            {
                IsRunning = false;
            }
        }

        public ITaskQc EnQ(Func<CancellationToken, Task> func)
        {
            _taskQueue.Enqueue(func);
            return this;
        }

        ITaskQc ITaskQc.Then(Func<CancellationToken, Task> func)
        {
            _taskQueue.Enqueue(func);
            return this;
        }

        ITaskQc ITaskQc.Then(Action action)
        {
            _taskQueue.Enqueue(_ =>
            {
                action?.Invoke();
                return Task.CompletedTask;
            });
            return this;
        }

        ITaskQc ITaskQc.Delay(float delay)
        {
            _taskQueue.Enqueue(async token => { await Task.Delay(TimeSpan.FromSeconds(delay), token); });
            return this;
        }

        /// <summary>
        /// 运行当前的同步队列，在stop后，也可再次Run实现Continue
        /// </summary>
        public async void Run()
        {
            try
            {
                if (IsRunning)
                    return;
                _cts = new CancellationTokenSource();
                IsRunning = true;
                await ProcessQueueAsync();
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            IsRunning = false;
        }

        public void StopAndClear()
        {
            _cts?.Cancel();
            IsRunning = false;
            _taskQueue.Clear();
        }

        protected override void OnDispose() => _taskQueue.Clear();
    }
}
