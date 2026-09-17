using UnityEngine;
using Logger = AxuFrameKit._Core.Utility.Common.Logger;

namespace AxuFrameKit.Common.Utility.Static
{
    /// <summary>
    /// 把 _Core 纯 C# Logger 的输出接线到 Unity Console（覆盖 Play 模式与构建产物）。
    /// 编辑器非 Play 阶段由 Editor 程序集的 LoggerWire 完成接线。
    /// </summary>
    internal static class LoggerUnityWire
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Wire()
        {
            // LogAssertion 带 [Conditional] 特性，不能直接以方法组创建委托，需 lambda 包装
            Logger.SetSinks(Debug.Log, Debug.LogWarning, Debug.LogError, Debug.LogException, assertion => Debug.LogAssertion(assertion));
        }
    }
}
