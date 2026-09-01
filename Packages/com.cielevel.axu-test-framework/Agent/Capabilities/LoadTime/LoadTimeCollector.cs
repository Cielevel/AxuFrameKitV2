using System.Collections.Generic;
using AxuTestFramework.Agent.Collectors;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Capabilities.LoadTime
{
    /// <summary>
    /// LoadTime 结果采集器（runner 自有，经 <see cref="MetricSession.TryAttachCollector"/> 挂入会话，
    /// 不进 CollectorRegistry）：EndScene 落每场景序列，Output 动态产出每场景 stat 与聚合。
    /// 产出键（collector = "loadtime"）：
    /// scene.&lt;safe&gt;.loadMs / .firstFrameMs / .stableMs（Last）；
    /// sceneCount（Sum）、loadMs.max（Max）、loadMs.avg（Avg）、unstableCount（Sum）。
    /// </summary>
    public class LoadTimeCollector : MetricCollector
    {
        readonly List<string> _sceneKeys = new List<string>();
        MetricSession _session;
        string _currentKey;

        /// <inheritdoc/>
        public override string Id => "loadtime";

        /// <summary>动态输出声明（调用点为收尾汇总，每 scenario 一次，允许分配）。</summary>
        public override CollectorOutput Output => BuildOutput();

        /// <inheritdoc/>
        public override void Start(ScenarioContext context, MetricSession session)
        {
            _session = session;
            _sceneKeys.Clear();
            _currentKey = null;

            // total 类聚合恒产出（含 0 值）：语义上恒存在的总量在 Start 播种 0 值采样，
            // 让"0"成为可命中预算的合法产出（全 0 不再缺键被误判 fail）；
            // "缺失=fail" 哲学不变——只针对真正缺失的观测（如全部加载失败时无 loadMs 采样，依旧缺键判 fail）
            _session?.Record(Id, "unstableCount", 0);
            _session?.Record(Id, "sceneCount", 0);
        }

        /// <summary>开始一个场景的计时记录（sceneName 清洗后作为 stat 键片段；重复名自动扩展）。</summary>
        public void BeginScene(string sceneName)
        {
            _currentKey = AgentPaths.SanitizeFileName(sceneName);
            if (_sceneKeys.Contains(_currentKey))
                _currentKey += "_" + (_sceneKeys.Count + 1);
            _sceneKeys.Add(_currentKey);
        }

        /// <summary>结束当前场景：loadMs/firstFrameMs/stableMs 自加载起点计（毫秒）；stable=false 计入 unstableCount。</summary>
        public void EndScene(double loadMs, double firstFrameMs, double stableMs, bool stable)
        {
            var key = _currentKey;
            if (string.IsNullOrEmpty(key)) return;

            _session?.Record(Id, "scene." + key + ".loadMs", loadMs);
            _session?.Record(Id, "scene." + key + ".firstFrameMs", firstFrameMs);
            _session?.Record(Id, "scene." + key + ".stableMs", stableMs);
            _session?.Record(Id, "loadMs", loadMs);
            _session?.Record(Id, "sceneCount", 1);
            if (!stable) _session?.Record(Id, "unstableCount", 1);
            _currentKey = null;
        }

        CollectorOutput BuildOutput()
        {
            var stats = new List<CollectorStatOutput>(_sceneKeys.Count * 3 + 4);
            foreach (var key in _sceneKeys)
            {
                stats.Add(new CollectorStatOutput("scene." + key + ".loadMs", MetricAggregator.Last));
                stats.Add(new CollectorStatOutput("scene." + key + ".firstFrameMs", MetricAggregator.Last));
                stats.Add(new CollectorStatOutput("scene." + key + ".stableMs", MetricAggregator.Last));
            }
            stats.Add(new CollectorStatOutput("sceneCount", MetricAggregator.Sum));
            stats.Add(new CollectorStatOutput("loadMs.max", MetricAggregator.Max, "loadMs"));
            stats.Add(new CollectorStatOutput("loadMs.avg", MetricAggregator.Avg, "loadMs"));
            stats.Add(new CollectorStatOutput("unstableCount", MetricAggregator.Sum));
            return new CollectorOutput(stats.ToArray());
        }
    }
}
