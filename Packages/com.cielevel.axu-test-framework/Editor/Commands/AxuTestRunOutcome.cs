using System.Collections.Generic;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// axu_test_run 的执行结果（pipeline 命令返回值与看板窗口共用的出参模型；
    /// 由 Newtonsoft 序列化为命令响应）。
    /// </summary>
    public sealed class AxuTestRunOutcome
    {
        /// <summary>命令级成功：运行已启动或已完成（用例失败不改变本值，看 summary 与 allPassed）。</summary>
        public bool Success { get; set; }

        /// <summary>运行状态："completed"（含汇总）/ "playmode_running"（已启动异步运行）/ "timeout" / "error"。</summary>
        public string Status { get; set; }

        /// <summary>请求的运行模式（editor / playmode / all）。</summary>
        public string Mode { get; set; }

        /// <summary>本次实际运行的程序集清单（安全过滤后）。</summary>
        public List<string> Assemblies { get; set; }

        /// <summary>被安全过滤排除的程序集清单（Unity.Pipeline.Tests*）。</summary>
        public List<string> ExcludedAssemblies { get; set; }

        /// <summary>过滤条件描述。</summary>
        public FilterInfo Filter { get; set; }

        /// <summary>结果汇总（status=completed 时填写）。</summary>
        public ReportSummary Summary { get; set; }

        /// <summary>status=completed 时是否全部通过（total&gt;0 且 failed==0 且 inconclusive==0）。</summary>
        public bool AllPassed { get; set; }

        /// <summary>报告 JSON 完整路径（report=true 且已落盘时填写）。</summary>
        public string ReportPath { get; set; }

        /// <summary>报告所在目录（artifacts 目录为其下 artifacts/）。</summary>
        public string ReportDirectory { get; set; }

        /// <summary>运行状态文件完整路径（PlayMode 异步轮询用）。</summary>
        public string StatusPath { get; set; }

        /// <summary>耗时（毫秒；status=completed 时填写）。</summary>
        public double DurationMs { get; set; }

        /// <summary>all 模式下 EditMode 部分的汇总（EditMode 同步完成后、PlayMode 尚在异步运行时填写）。</summary>
        public ReportSummary EditorSummary { get; set; }

        /// <summary>面向调用方的说明消息。</summary>
        public string Message { get; set; }

        /// <summary>错误消息（status=error / timeout 时填写）。</summary>
        public string Error { get; set; }
    }
}
