namespace AxuTestFramework.Agent.Bootstrap
{
    /// <summary>内建能力注册（Phase 1 仅 Echo 链路自检能力；后续 perf/loadtime/smoke 等在此追加）。</summary>
    public static class BuiltInCapabilities
    {
        static bool _registered;

        /// <summary>注册全部内建能力（幂等）。</summary>
        public static void RegisterAll()
        {
            if (_registered) return;
            _registered = true;
            AxuCapabilityRegistry.Register(new Capabilities.Echo.EchoCapability());
        }
    }
}
