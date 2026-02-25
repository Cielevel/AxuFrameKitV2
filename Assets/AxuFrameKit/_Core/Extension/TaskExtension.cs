using System;
using System.Threading;
using System.Threading.Tasks;

namespace AxuFrameKit._Core.Extension
{
    public static class TaskExtension
    {
        /// <summary>
        /// Fire-and-forget 任务。默认吞掉异常，可通过回调处理异常。
        /// </summary>
        public static void Forget(this Task task, Action<Exception> onException = null)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            _ = ForgetInternal(task, onException);
        }

        /// <summary>
        /// 为 Task 添加超时控制，超时抛出 TimeoutException。
        /// </summary>
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            if (timeout != Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            if (task.IsCompleted || timeout == Timeout.InfiniteTimeSpan)
            {
                await task.ConfigureAwait(false);
                return;
            }

            Task completed = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
            if (completed != task)
                throw new TimeoutException($"Task execution timed out after {timeout}.");

            await task.ConfigureAwait(false);
        }

        /// <summary>
        /// 为 Task&lt;T&gt; 添加超时控制，超时抛出 TimeoutException。
        /// </summary>
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            if (timeout != Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            if (task.IsCompleted || timeout == Timeout.InfiniteTimeSpan)
                return await task.ConfigureAwait(false);

            Task completed = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
            if (completed != task)
                throw new TimeoutException($"Task execution timed out after {timeout}.");

            return await task.ConfigureAwait(false);
        }

        /// <summary>
        /// 为 Task 增加取消令牌支持，取消时抛出 OperationCanceledException。
        /// </summary>
        public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            if (!cancellationToken.CanBeCanceled)
            {
                await task.ConfigureAwait(false);
                return;
            }

            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                Task completed = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
                if (completed != task)
                    throw new OperationCanceledException(cancellationToken);
            }

            await task.ConfigureAwait(false);
        }

        /// <summary>
        /// 为 Task&lt;T&gt; 增加取消令牌支持，取消时抛出 OperationCanceledException。
        /// </summary>
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            if (!cancellationToken.CanBeCanceled)
                return await task.ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                Task completed = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
                if (completed != task)
                    throw new OperationCanceledException(cancellationToken);
            }

            return await task.ConfigureAwait(false);
        }

        private static async Task ForgetInternal(Task task, Action<Exception> onException)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }
        }
    }
}
