using System.Collections;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace AxuTestFramework.Agent.Capabilities.Stress
{
    /// <summary>
    /// 对象翻搅器（churn 工具，方案 §4.4）：每秒销毁上一批并重建 createPerSec 个对象，
    /// 持续 durationSec 秒——压 Instantiate/Destroy 分配与 GC 回收健康度
    /// （配合宿主的 memory/gc 采集器看 alloc 曲线）。产出 churn.created/destroyed.total。
    /// 计时一律真实时钟（AgentWait 契约），逐帧响应取消。
    /// </summary>
    public class ChurnRunner
    {
        readonly StressSpawner _spawner;
        readonly IAgentLog _log;

        /// <summary>本 phase（自 Run 起）累计创建数。</summary>
        public long CreatedTotal { get; private set; }

        /// <summary>本 phase 累计销毁数。</summary>
        public long DestroyedTotal { get; private set; }

        /// <summary>单秒批次实际创建数的峰值。</summary>
        public int PeakPerTick { get; private set; }

        /// <param name="spawner">对象生成器（复用其 prefab 解析与网格散布）。</param>
        /// <param name="log">宿主日志（可空）。</param>
        public ChurnRunner(StressSpawner spawner, IAgentLog log)
        {
            _spawner = spawner;
            _log = log;
        }

        /// <summary>执行翻搅（协程；结束时清场，保证不把残留对象带进下一个 phase）。</summary>
        public IEnumerator Run(StressPhaseParams parameters, ScenarioRunHandle handle)
        {
            var perTick = parameters.ClampedCreatePerSec;
            var startInstantiated = _spawner.InstantiatedTotal;
            var startDestroyed = _spawner.DestroyedTotal;

            var deadline = Time.realtimeSinceStartupAsDouble + parameters.ClampedDurationSec;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                if (handle != null && handle.CancelRequested) break;

                _spawner.RemoveDownTo(0); // 销毁上一批
                _spawner.Add(perTick);    // 重建本批
                if (perTick > PeakPerTick) PeakPerTick = perTick;

                yield return AgentWait.RealtimeSeconds(1f);
            }

            _spawner.Clear();
            CreatedTotal = _spawner.InstantiatedTotal - startInstantiated;
            DestroyedTotal = _spawner.DestroyedTotal - startDestroyed;

            _log?.Info($"churn 完成：created={CreatedTotal}，destroyed={DestroyedTotal}，" +
                       $"perTick={perTick}，duration={parameters.ClampedDurationSec:0.##}s");
        }
    }
}
