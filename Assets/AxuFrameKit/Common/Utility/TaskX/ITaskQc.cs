using System;
using System.Threading;
using System.Threading.Tasks;

namespace AxuFrameKit.Common.Utility.TaskX
{
    public interface ITaskQc
    {
        ITaskQc Then(Func<CancellationToken, Task> func);
        ITaskQc Then(Action action);
        ITaskQc Delay(float seconds);
        void Run();
        void Stop();
    }
}