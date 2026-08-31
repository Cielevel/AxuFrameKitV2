using System.Collections.Generic;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>
    /// `-axu*` 命令行参数的解析结果（亦可由 AxuTestAgent.RunSuite 的 configure 回调直接填充）。
    /// </summary>
    public class ScenarioCliOptions
    {
        /// <summary>suite 或单 scenario JSON 路径（-axuSuite；空则宿主静默待命）。</summary>
        public string SuitePath;

        /// <summary>只跑指定 id（-axuScenario，可重复；匹配 ref 或 resolved id）。</summary>
        public List<string> ScenarioIds = new List<string>();

        /// <summary>报告输出路径（-axuReport；空则用宿主默认）。</summary>
        public string ReportPath;

        /// <summary>截图/序列数据目录（-axuArtifacts；空则报告同目录 artifacts/）。</summary>
        public string ArtifactsDir;

        /// <summary>跑完自动退出（-axuQuitOnDone；Editor 内一律不生效）。</summary>
        public bool? QuitOnDone;

        /// <summary>warn 计入 fail（-axuWarnAsFail；影响退出码）。</summary>
        public bool? WarnAsFail;

        /// <summary>随机种子覆盖（-axuSeed，可复现）。</summary>
        public int? Seed;

        /// <summary>游戏内 Overlay 显隐（-axuOverlay / -axuNoOverlay；Phase 1 仅解析存储）。</summary>
        public bool? OverlayHint;

        /// <summary>无法识别的 -axu* 参数原样记录（非 -axu* 参数一律忽略，不记录）。</summary>
        public List<string> IgnoredArgs = new List<string>();
    }
}
