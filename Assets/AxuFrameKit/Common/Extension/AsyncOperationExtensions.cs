using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace AxuFrameKit.Common.Extension
{
    public static class AsyncOperationExtensions
    {
        public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
        {
            var tcs = new TaskCompletionSource<object>();
            asyncOp.completed += _ => tcs.SetResult(null);
            return ((Task)tcs.Task).GetAwaiter();
        }

        public static TaskAwaiter<Object> GetAwaiter(this ResourceRequest request)
        {
            var tcs = new TaskCompletionSource<Object>();
            request.completed += op => tcs.SetResult(request.asset);
            return tcs.Task.GetAwaiter();
        }
    }
}