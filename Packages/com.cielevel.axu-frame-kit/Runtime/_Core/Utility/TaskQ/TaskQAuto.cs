using System;
using System.Threading.Tasks;
using AxuFrameKit._Core.Utility.Multiton;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Utility.TaskQ
{
    /// <summary>
    /// 任务队列控制（停止时，将在执行完当前任务后停止）（自动ID多例）
    /// </summary>
    public class TaskQAuto : MultitonAuto<TaskQAuto>, ITaskQ
    {
        private readonly TaskQueueEngine _engine;

        public TaskQAuto() => _engine = new TaskQueueEngine(ex => Logger.Error($"任务执行失败: {ex}"));

        public bool IsRunning => _engine.IsRunning;

        public ITaskQ EnQ(Func<Task> func)
        {
            // 本队列无取消语义，统一适配为忽略令牌的引擎任务
            _engine.Enqueue(_ => func());
            return this;
        }

        ITaskQ ITaskQ.Then(Func<Task> func)
        {
            _engine.Enqueue(_ => func());
            return this;
        }

        ITaskQ ITaskQ.Then(Action action)
        {
            _engine.Enqueue(_ =>
            {
                action?.Invoke();
                return Task.CompletedTask;
            });
            return this;
        }

        ITaskQ ITaskQ.Delay(float delay)
        {
            // 不携带取消令牌：Stop 需等当前 Delay 自然结束（保持旧语义）
            _engine.Enqueue(async _ => { await Task.Delay(TimeSpan.FromSeconds(delay)); });
            return this;
        }

        /// <summary>
        /// 运行当前的同步队列，在stop后，也可再次Run实现Continue
        /// </summary>
        public void Run() => _engine.Run();

        public void Stop() => _engine.Stop();

        public void StopAndClear() => _engine.StopAndClear();

        protected override void OnDispose() => _engine.StopAndClear();
    }
}
