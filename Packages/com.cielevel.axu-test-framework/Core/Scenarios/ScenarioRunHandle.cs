using System.Collections.Generic;
using System.Diagnostics;
using AxuTestFramework.Core.Report;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>
    /// 单个 scenario 的运行句柄：状态、结论（Finding）收集、取消标记与真实时间计时（看门狗基准）。
    /// </summary>
    public class ScenarioRunHandle
    {
        readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public ScenarioRunHandle(string id)
        {
            Id = id;
        }

        /// <summary>本次执行的 scenario id（含 @profile 后缀）。</summary>
        public string Id { get; }

        /// <summary>当前状态（默认 Running；宿主在完成/异常/超时后定稿）。</summary>
        public RunStatus Status { get; set; } = RunStatus.Running;

        /// <summary>本次执行收集到的结论。</summary>
        public List<Finding> Findings { get; } = new List<Finding>();

        /// <summary>
        /// runner 产出的工件（截图等）完整路径；宿主在收尾时转为相对报告目录的路径写入报告 artifacts。
        /// （runner→宿主的加法式通道：runner 无法直接访问报告结果对象。）
        /// </summary>
        public List<string> Artifacts { get; } = new List<string>();

        /// <summary>取消是否已请求（Runner/宿主可在等待段间检查并尽快退出）。</summary>
        public bool CancelRequested { get; private set; }

        /// <summary>取消原因（MarkCancelled 记录）。</summary>
        public string CancelReason { get; private set; }

        /// <summary>真实时间累计秒数（不受 timeScale 影响）。</summary>
        public double ElapsedSeconds => _stopwatch.Elapsed.TotalSeconds;

        /// <summary>真实时间累计毫秒数。</summary>
        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

        /// <summary>标记取消（幂等）：置 CancelRequested，Running 态下转 Cancelled。</summary>
        public void MarkCancelled(string reason)
        {
            if (CancelRequested) return;
            CancelRequested = true;
            CancelReason = reason;
            if (Status == RunStatus.Running) Status = RunStatus.Cancelled;
        }

        /// <summary>收集一条结论并返回（便于追加信息）。</summary>
        public Finding AddFinding(FindingSeverity severity, string code, string message)
        {
            var finding = new Finding(severity, code, message);
            Findings.Add(finding);
            return finding;
        }

        /// <summary>追加既有结论。</summary>
        public void AddFinding(Finding finding)
        {
            if (finding != null) Findings.Add(finding);
        }
    }
}
