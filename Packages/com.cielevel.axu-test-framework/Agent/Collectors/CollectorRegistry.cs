using System;
using System.Collections.Generic;

namespace AxuTestFramework.Agent.Collectors
{
    /// <summary>
    /// 采集器注册表（显式工厂注册，禁运行时反射扫描——IL2CPP stripping 安全）。
    /// 内建：fps / frameTime / memory / rendering / gc；游戏工程可经 Register 追加自定义采集器。
    /// </summary>
    public static class CollectorRegistry
    {
        static readonly Dictionary<string, Func<MetricCollector>> Factories =
            new Dictionary<string, Func<MetricCollector>>(StringComparer.Ordinal);

        static bool _builtInRegistered;

        /// <summary>注册采集器工厂；重复 id 忽略（首个生效）。</summary>
        public static void Register(string id, Func<MetricCollector> factory)
        {
            if (string.IsNullOrEmpty(id) || factory == null) return;
            if (Factories.ContainsKey(id)) return;
            Factories.Add(id, factory);
        }

        /// <summary>按 id 创建采集器实例；未知 id 或工厂抛异常返回 false。</summary>
        public static bool TryCreate(string id, out MetricCollector collector)
        {
            EnsureBuiltIn();
            collector = null;
            if (string.IsNullOrEmpty(id) || !Factories.TryGetValue(id, out var factory)) return false;

            try
            {
                collector = factory();
                return collector != null;
            }
            catch (Exception)
            {
                return false; // 工厂异常按未知处理，由宿主产出 COLLECTOR_UNKNOWN Finding
            }
        }

        /// <summary>注册全部内建采集器（幂等；首次 TryCreate 前自动调用）。</summary>
        public static void EnsureBuiltIn()
        {
            if (_builtInRegistered) return;
            _builtInRegistered = true;
            Register("fps", () => new FpsCollector());
            Register("frameTime", () => new FrameTimeCollector());
            Register("memory", () => new MemoryCollector());
            Register("rendering", () => new RenderingCollector());
            Register("gc", () => new GcCollector());
        }
    }
}
