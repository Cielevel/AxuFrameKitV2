using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using AxuFrameKit.Common.Utility.Multiton;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.TaskQ
{
    /// <summary>
    /// 任务队列控制（停止时，将在执行完当前任务后停止）（自动ID多例）
    /// </summary>
    public class TaskQAuto : MultitonAuto<TaskQAuto>, ITaskQ
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
                        await taskFunc.Invoke();
                    else
                        await Task.Yield();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("任务队列已取消，TaskQ.Key：" + Key);
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
        
        ITaskQ ITaskQ.Delay(float delay)
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
                Debug.LogException(e);
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

        public override void Dispose() => _taskQueue.Clear();
    }
}
