using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Utility.TaskQ
{
    /// <summary>
    /// TaskQ 家族的公共消费引擎：ConcurrentQueue + SemaphoreSlim 信号量等待。
    /// 以"代数（generation）"标记每一轮消费循环——Run 启动新循环、Stop/StopAndClear 结束旧循环时都会推进代数，
    /// 旧循环从 WaitAsync 醒来后发现代数不匹配立即退出，从根本上杜绝"Stop 后立即 Run 出现新旧双消费者并发"的竞态。
    /// 单任务异常经构造注入的回调上报，不会中止整队。任务统一签名为 Func&lt;CancellationToken, Task&gt;，
    /// 无取消语义的队列（TaskQ 系）传 CancellationToken.None 即可。
    /// </summary>
    internal sealed class TaskQueueEngine
    {
        private readonly ConcurrentQueue<Func<CancellationToken, Task>> _taskQueue = new();

        // 初始 0 上限 int.MaxValue：仅用作"有新任务/被唤醒"的信号，不限制并发入队
        private readonly SemaphoreSlim _signal = new(0, int.MaxValue);

        private readonly Action<Exception> _onTaskError;

        // 状态锁：仅保护 Run/Stop 对运行标记与代数的切换，不覆盖任务执行本身
        private readonly object _gate = new();

        private int _generation; // 消费循环代数：每轮 Run 递增并捕获本地值，Stop/StopAndClear 也递增以作废旧循环

        private bool _isRunning;

        public bool IsRunning => Volatile.Read(ref _isRunning);

        public TaskQueueEngine(Action<Exception> onTaskError) => _onTaskError = onTaskError;

        /// <summary> 入队一个任务并唤醒可能在等待的消费循环（线程安全） </summary>
        public void Enqueue(Func<CancellationToken, Task> task)
        {
            _taskQueue.Enqueue(task);
            _signal.Release(); // 唤醒可能在等待的队列循环
        }

        /// <summary> 清空未执行的任务（不影响运行状态） </summary>
        public void Clear()
        {
            _taskQueue.Clear();
            _signal.Release(); // 唤醒等待中的循环，令其重新确认队列状态
        }

        /// <summary>
        /// 启动一轮新的消费循环（若已在运行则忽略，保持 Run 幂等）。
        /// token 会原样传给每个任务，供带取消语义的队列（TaskQc 系）使用。
        /// </summary>
        public void Run(CancellationToken token = default)
        {
            int generation;
            lock (_gate)
            {
                if (Volatile.Read(ref _isRunning))
                    return;

                Volatile.Write(ref _isRunning, true);
                generation = Interlocked.Increment(ref _generation);
            }

            // 锁外启动循环：同步完成的任务仍在 Run 调用内按 FIFO 执行（对齐旧语义），
            // Stop 也无需等待本锁；Run/Run 并发由锁内检查保证任何时刻至多一个有效消费者
            _ = ProcessAsync(generation, token);
        }

        /// <summary> 停止消费（当前任务执行完后生效），并推进代数使旧循环彻底失效 </summary>
        public void Stop()
        {
            lock (_gate)
            {
                Volatile.Write(ref _isRunning, false);
                Interlocked.Increment(ref _generation);
            }

            _signal.Release(); // 唤醒可能在等待的队列循环以退出
        }

        /// <summary> 停止消费并清空未执行的任务 </summary>
        public void StopAndClear()
        {
            Stop();
            Clear();
        }

        private async Task ProcessAsync(int generation, CancellationToken token)
        {
            try
            {
                // 运行标记被清除或代数不匹配（被 Stop/新 Run 取代）即退出
                while (Volatile.Read(ref _isRunning) && generation == Volatile.Read(ref _generation))
                {
                    if (_taskQueue.TryDequeue(out var taskFunc))
                    {
                        try
                        {
                            await taskFunc.Invoke(token);
                        }
                        catch (Exception ex)
                        {
                            // 单任务失败只上报、不中止整队
                            _onTaskError?.Invoke(ex);
                        }
                    }
                    else
                    {
                        await _signal.WaitAsync(); // 等待新任务入队或 Stop 唤醒，避免空转轮询烧 CPU
                    }
                }
            }
            catch (Exception ex)
            {
                // 引擎自身异常兜底（如上报回调抛出），对齐旧版 RunAsync 的兜底口径
                Logger.Exception(ex);
            }
            finally
            {
                // 仅当本循环仍是最新一代时才复位运行标记，
                // 防止旧一代循环退出时误关新一轮 Run 的运行状态
                if (generation == Volatile.Read(ref _generation))
                    Volatile.Write(ref _isRunning, false);
            }
        }
    }
}
