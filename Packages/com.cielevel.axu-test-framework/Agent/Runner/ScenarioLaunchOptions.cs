using System.Collections.Generic;

namespace AxuTestFramework.Agent.Runner
{
    /// <summary>
    /// 合并后的启动参数（来源：CLI 解析 / Editor 桥文件 / AxuTestAgent.RunSuite API），
    /// 宿主唯一消费形态；QuitOnDone/WarnAsFail 已解析为最终布尔值。
    /// </summary>
    public class ScenarioLaunchOptions
    {
        /// <summary>suite 或单 scenario JSON 路径（绝对或工程相对）。</summary>
        public string SuitePath;

        /// <summary>只跑指定 id（匹配 ref 或 resolved id）。</summary>
        public List<string> ScenarioIds = new List<string>();

        /// <summary>报告输出路径（空则宿主按 AgentPaths 默认）。</summary>
        public string ReportPath;

        /// <summary>截图/序列数据目录（空则报告同目录 artifacts/）。</summary>
        public string ArtifactsDir;

        /// <summary>跑完自动退出（Editor 内一律不生效）。</summary>
        public bool QuitOnDone;

        /// <summary>warn 计入 fail（与 suite.settings.warnAsFail 取或，影响退出码）。</summary>
        public bool WarnAsFail;

        /// <summary>随机种子覆盖（可复现）。</summary>
        public int? Seed;

        /// <summary>启动来源："cli" / "editor-bridge"。</summary>
        public string Source = "cli";

        /// <summary>游戏内 Overlay 显隐（Phase 1 仅解析存储，未接 Overlay）。</summary>
        public bool? OverlayHint;

        /// <summary>基线保存路径（-axuBaselineSave；宿主收尾按 scenarioId 写 &lt;目录&gt;/&lt;scenarioId&gt;.baseline.json）。</summary>
        public string BaselineSavePath;
    }
}
