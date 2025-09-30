using System;
using System.Threading.Tasks;

namespace AxuFrameKit.Common.Utility.TaskX
{
    public interface ITaskQ
    {
        ITaskQ Then(Func<Task> func);
        ITaskQ Then(Action action);
        ITaskQ Delay(float seconds);
        void Run();
        void Stop();
    }
}