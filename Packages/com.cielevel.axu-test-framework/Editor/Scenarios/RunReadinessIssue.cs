namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 一条运行就绪问题（看板/引导界面的最小展示单元）：阻断项（<see cref="IsBlocking"/>=true）
    /// 让运行入口不可用并给出修复指引；非阻断项（如"编译中"）只提示不拦截。
    /// <para>文案面向不了解本项目的用户：<see cref="Message"/> 讲影响、<see cref="FixHint"/> 讲修复/等待方式，
    /// 两者都可能在界面上独立出现（横幅/悬浮说明），因此各自成句、不依赖上下文。</para>
    /// </summary>
    public sealed class RunReadinessIssue
    {
        /// <summary>稳定的问题标识（"agent_define" / "in_play_mode" / "utf_run_in_flight" /
        /// "scenario_run_in_flight" / "build_scenes_empty" / "compiling"），供界面定位与测试断言。</summary>
        public string Id;

        /// <summary>影响说明（中文，说明"为什么现在跑不了/结果可能不可信"）。</summary>
        public string Message;

        /// <summary>修复或等待指引（中文；非阻断提示可空）。</summary>
        public string FixHint;

        /// <summary>修复动作类别（<see cref="ReadinessFixKind.None"/> 表示无按钮化动作，仅文案指引）。</summary>
        public ReadinessFixKind FixKind = ReadinessFixKind.None;

        /// <summary>true = 阻断运行；false = 仅提示。</summary>
        public bool IsBlocking;

        /// <summary>构造阻断项（带修复指引与动作类别）。</summary>
        public static RunReadinessIssue Blocking(string id, string message, string fixHint, ReadinessFixKind fixKind)
        {
            return new RunReadinessIssue
            {
                Id = id,
                Message = message,
                FixHint = fixHint,
                FixKind = fixKind,
                IsBlocking = true,
            };
        }

        /// <summary>构造非阻断提示（只有说明，不拦运行）。</summary>
        public static RunReadinessIssue Warning(string id, string message)
        {
            return new RunReadinessIssue
            {
                Id = id,
                Message = message,
                IsBlocking = false,
            };
        }
    }
}
