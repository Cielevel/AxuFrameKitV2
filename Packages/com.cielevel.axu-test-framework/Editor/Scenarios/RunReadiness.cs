using System.Collections.Generic;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 一次运行就绪判定的结果（<see cref="RunReadinessCheck.Evaluate"/> 的返回值）：
    /// 问题清单（按稳定判定顺序）+ 派生结论。
    /// <para><see cref="IsReady"/> 只看阻断项——非阻断提示（如编译中）不影响可运行结论；
    /// <see cref="HasWarnings"/> 供界面决定是否额外渲染提示区。</para>
    /// </summary>
    public sealed class RunReadiness
    {
        /// <summary>问题清单（按固定判定顺序排列；无问题时为空列表，不为 null）。</summary>
        public List<RunReadinessIssue> Issues = new List<RunReadinessIssue>();

        /// <summary>是否就绪可运行（无任何 <see cref="RunReadinessIssue.IsBlocking"/> 项）。</summary>
        public bool IsReady
        {
            get
            {
                foreach (var issue in Issues)
                {
                    if (issue != null && issue.IsBlocking)
                        return false;
                }
                return true;
            }
        }

        /// <summary>是否含非阻断提示（如编译中）。</summary>
        public bool HasWarnings
        {
            get
            {
                foreach (var issue in Issues)
                {
                    if (issue != null && !issue.IsBlocking)
                        return true;
                }
                return false;
            }
        }
    }
}
