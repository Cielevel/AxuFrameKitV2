namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 首次运行引导的一个目标（见 <see cref="FirstRunSuiteCatalog.Goals"/>）：
    /// id 是外部契约（下游按 id 接线：选目标 → 就绪判定 → 运行/建套件），
    /// Needs* 两项决定该目标做就绪判定时需要哪些前提（喂给 <see cref="RunReadinessCheck.Evaluate"/>）。
    /// </summary>
    public sealed class FirstRunGoal
    {
        /// <summary>目标 id（"selfcheck" | "code" | "scene" | "perf"，勿改——下游按 id 接线）。</summary>
        public string Id;

        /// <summary>按钮/标题文案（中文短词）。</summary>
        public string Title;

        /// <summary>目标说明（中文，一句话讲清楚做什么；面向不了解本项目的用户）。</summary>
        public string Description;

        /// <summary>该目标是否要求已开启 AXU_TEST_AGENT（场景类目标需要；单元测试不需要）。</summary>
        public bool NeedsAgentDefine;

        /// <summary>该目标是否要求构建场景清单非空（scene 目标需要）。</summary>
        public bool NeedsBuildScenes;
    }
}
