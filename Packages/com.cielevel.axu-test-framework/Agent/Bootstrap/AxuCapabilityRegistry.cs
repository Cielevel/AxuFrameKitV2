using System.Collections.Generic;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace AxuTestFramework.Agent.Bootstrap
{
    /// <summary>
    /// 能力注册表（显式 API 注册，禁运行时反射扫描——IL2CPP stripping 安全）。
    /// 游戏工程注册自定义能力即接入点（方案 §4.8 gamehook）。
    /// </summary>
    public static class AxuCapabilityRegistry
    {
        static readonly Dictionary<string, IAxuTestCapability> Capabilities = new Dictionary<string, IAxuTestCapability>();

        /// <summary>注册能力；重复 Id 记警告并忽略（首个生效）。</summary>
        public static void Register(IAxuTestCapability capability)
        {
            if (capability == null || string.IsNullOrEmpty(capability.Id))
            {
                Debug.LogWarning("[AxuTestAgent] 忽略无效能力注册（null 或 Id 为空）");
                return;
            }

            if (Capabilities.ContainsKey(capability.Id))
            {
                Debug.LogWarning($"[AxuTestAgent] 能力 \"{capability.Id}\" 重复注册，忽略（首个生效）");
                return;
            }

            Capabilities.Add(capability.Id, capability);
        }

        /// <summary>按 id 查找能力。</summary>
        public static bool TryGet(string id, out IAxuTestCapability capability)
        {
            capability = null;
            return !string.IsNullOrEmpty(id) && Capabilities.TryGetValue(id, out capability);
        }

        /// <summary>全部已注册能力（只读快照视图）。</summary>
        public static IReadOnlyCollection<IAxuTestCapability> All => Capabilities.Values;

        /// <summary>清空注册表（主要供测试使用）。</summary>
        public static void Clear() => Capabilities.Clear();
    }
}
