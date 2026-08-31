using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>IAgentLog 的 Unity 控制台实现（统一 [AxuTestAgent] 前缀，便于过滤）。</summary>
    public class AgentLog : IAgentLog
    {
        const string Prefix = "[AxuTestAgent] ";

        /// <summary>共享实例（无状态）。</summary>
        public static readonly AgentLog Instance = new AgentLog();

        /// <summary>Debug.Log。</summary>
        public void Info(string message) => Debug.Log(Prefix + message);

        /// <summary>Debug.LogWarning。</summary>
        public void Warning(string message) => Debug.LogWarning(Prefix + message);

        /// <summary>Debug.LogError。</summary>
        public void Error(string message) => Debug.LogError(Prefix + message);
    }
}
