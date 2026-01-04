using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace AxuFrameKit.Common.Extension
{
    /// <summary>
    /// 为 Unity 的 AsyncOperation 与 ResourceRequest 提供 await 支持的扩展方法。
    /// 包含空值检查并使用 TaskCompletionSource 实现异步等待。
    /// </summary>
    public static class AsyncOperationExtensions
    {
        /// <summary>
        /// 为 AsyncOperation 返回一个 awaiter，操作完成时触发。
        /// </summary>
        /// <param name="asyncOp">需要等待的 AsyncOperation 实例。</param>
        /// <returns>当操作完成后可继续执行的 TaskAwaiter。</returns>
        public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
        {
            if (asyncOp == null) throw new System.ArgumentNullException(nameof(asyncOp));

            var tcs = new TaskCompletionSource<bool>();
            asyncOp.completed += _ => tcs.SetResult(true);
            return ((Task)tcs.Task).GetAwaiter();
        }

        /// <summary>
        /// 为 ResourceRequest 返回一个 awaiter，资源加载完成后返回对应的 asset。
        /// </summary>
        /// <param name="request">需要等待的 ResourceRequest 实例。</param>
        /// <returns>完成后返回加载的资源对象的 TaskAwaiter。</returns>
        public static TaskAwaiter<object> GetAwaiter(this ResourceRequest request)
        {
            if (request == null) throw new System.ArgumentNullException(nameof(request));

            var tcs = new TaskCompletionSource<object>();
            request.completed += _ => tcs.SetResult(request.asset);
            return tcs.Task.GetAwaiter();
        }
    }
}
