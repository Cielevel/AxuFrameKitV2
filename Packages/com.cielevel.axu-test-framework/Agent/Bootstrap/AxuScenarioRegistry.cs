using System.Collections.Generic;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace AxuTestFramework.Agent.Bootstrap
{
    /// <summary>
    /// 进程内代码态 scenario 注册表（方案 §4.8 gamehook / D7 决策：仅显式注册，不做 Attribute 烘焙）。
    /// 生命周期 = 进程/域：**非持久化**，域重载（含 Editor 进 Play）后清空，
    /// 游戏侧须在 RuntimeInitializeOnLoad（推荐 AfterSceneLoad）重新注册后再触发 suite 运行。
    /// </summary>
    public static class AxuScenarioRegistry
    {
        static readonly Dictionary<string, ScenarioDef> Scenarios =
            new Dictionary<string, ScenarioDef>(System.StringComparer.Ordinal);

        /// <summary>已注册 scenario 数。</summary>
        public static int Count => Scenarios.Count;

        /// <summary>
        /// 注册代码态 scenario；同 id 重复注册**后者覆盖**（记 warning）。
        /// 注册项在 suite 解析时按 id 生效：覆盖 suite 内同名定义、或为 suite ref 条目供定义。
        /// </summary>
        public static void Register(ScenarioDef scenario)
        {
            if (scenario == null || string.IsNullOrEmpty(scenario.Id) || string.IsNullOrWhiteSpace(scenario.Capability))
            {
                Debug.LogWarning("[AxuTestAgent] 忽略无效 scenario 注册（null / id 为空 / capability 为空）");
                return;
            }

            if (Scenarios.ContainsKey(scenario.Id))
                Debug.LogWarning($"[AxuTestAgent] scenario \"{scenario.Id}\" 重复注册，后者覆盖");

            Scenarios[scenario.Id] = scenario;
        }

        /// <summary>按 id 查找已注册 scenario。</summary>
        public static bool TryGet(string id, out ScenarioDef scenario)
        {
            scenario = null;
            return !string.IsNullOrEmpty(id) && Scenarios.TryGetValue(id, out scenario);
        }

        /// <summary>全部已注册 scenario（只读快照视图）。</summary>
        public static IReadOnlyCollection<ScenarioDef> All => Scenarios.Values;

        /// <summary>导出 id → def 字典快照（供 SuiteLoader.Resolve 的 externalById）。</summary>
        public static Dictionary<string, ScenarioDef> Snapshot() => new Dictionary<string, ScenarioDef>(Scenarios);

        /// <summary>清空注册表（主要供测试使用；域重载后静态初始化亦为空）。</summary>
        public static void Clear() => Scenarios.Clear();
    }
}
