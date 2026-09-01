namespace AxuTestFramework.Agent.Bootstrap
{
    /// <summary>内建能力注册（Phase 3 起：echo 链路自检 + perf/loadtime/smoke 首批 P0 能力；后续 soak/stress/monkey 等在此追加）。</summary>
    public static class BuiltInCapabilities
    {
        static bool _registered;

        /// <summary>注册全部内建能力（幂等）。</summary>
        public static void RegisterAll()
        {
            if (_registered) return;
            _registered = true;
            AxuCapabilityRegistry.Register(new Capabilities.Echo.EchoCapability());
            AxuCapabilityRegistry.Register(new Capabilities.Perf.PerfCapability());
            AxuCapabilityRegistry.Register(new Capabilities.LoadTime.LoadTimeCapability());
            AxuCapabilityRegistry.Register(new Capabilities.Smoke.SmokeCapability());
            AxuCapabilityRegistry.Register(new Capabilities.Soak.SoakCapability());
            AxuCapabilityRegistry.Register(new Capabilities.Stress.StressCapability());
            AxuCapabilityRegistry.Register(new Capabilities.Monkey.MonkeyCapability());
        }
    }
}
