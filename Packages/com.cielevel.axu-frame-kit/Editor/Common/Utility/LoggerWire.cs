using UnityEditor;
using UnityEngine;
using Logger = AxuFrameKit._Core.Utility.Common.Logger;

namespace AxuFrameKit.Common.Editor.Utility
{
    /// <summary>
    /// 编辑器阶段（含 EditMode，如 EditMode 测试）把 _Core 纯 C# Logger 的输出接线到 Unity Console。
    /// Play 模式由 Common 层的 LoggerUnityWire 接线（构建产物只有后者）。
    /// </summary>
    internal static class LoggerWire
    {
        [InitializeOnLoadMethod]
        private static void Wire()
        {
            // LogAssertion 带 [Conditional] 特性，不能直接以方法组创建委托，需 lambda 包装
            Logger.SetSinks(Debug.Log, Debug.LogWarning, Debug.LogError, Debug.LogException, assertion => Debug.LogAssertion(assertion));
        }
    }
}
