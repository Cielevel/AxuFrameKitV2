using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using AxuFrameKit._Core.Utility.Multiton;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Utility.TaskQ
{
    /// <summary>
    /// 任务队列控制（停止时，将在执行完当前任务后停止）
    /// </summary>
    public class TaskQ : Multiton<TaskQ>, ITaskQ
    {
        public bool IsRunning { get; private set; }

        private readonly ConcurrentQueue<Func<Task>> _taskQueue = new();

        private Task _currentTask;

        private async Task ProcessQueueAsync()
        {
            try
            {
                while (IsRunning)
                {
                    if (_taskQueue.TryDequeue(out var taskFunc))
                    {
                        try
                        {
                            await taskFunc.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"任务执行失败: {ex}");
                        }
                    }
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

        public ITaskQ EnQ(Func<Task> func)
        {
            _taskQueue.Enqueue(func);
            return this;
        }

        ITaskQ ITaskQ.Then(Func<Task> func)
        {
            _taskQueue.Enqueue(func);
            return this;
        }

        ITaskQ ITaskQ.Then(Action action)
        {
            _taskQueue.Enqueue(() =>
            {
                action?.Invoke();
                return Task.CompletedTask;
            });
            return this;
        }

        public ITaskQ Delay(float delay)
        {
            _taskQueue.Enqueue(async () => { await Task.Delay(TimeSpan.FromSeconds(delay)); });
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
            IsRunning = false;
        }

        public void StopAndClear()
        {
            IsRunning = false;
            _taskQueue.Clear();
        }

        public override void OnDispose() => _taskQueue.Clear();
    }
}
