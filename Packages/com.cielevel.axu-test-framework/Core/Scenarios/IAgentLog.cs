namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>Agent 运行日志抽象（Core 不依赖引擎，由 Agent 层桥接到 Unity 控制台）。</summary>
    public interface IAgentLog
    {
        /// <summary>普通信息。</summary>
        void Info(string message);

        /// <summary>警告。</summary>
        void Warning(string message);

        /// <summary>错误。</summary>
        void Error(string message);
    }
}
