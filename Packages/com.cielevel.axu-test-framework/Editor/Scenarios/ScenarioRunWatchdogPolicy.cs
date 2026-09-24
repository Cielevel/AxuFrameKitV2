using System;

namespace AxuTestFramework.Editor
{
    /// <summary>状态缺失或不属于本轮时的恢复边界；让看门狗决策可不依赖真实 Editor 时钟测试。</summary>
    public static class ScenarioRunWatchdogPolicy
    {
        /// <summary>
        /// 无法认领状态后的收尾判定。保留启动宽限以覆盖进 Play、域重载和 Agent 首次写状态；之后
        /// 以运行记录的发起时间提供硬上限，并允许明显陈旧的可读 running 状态提前触发收尾。
        /// </summary>
        public static bool ShouldAbortUnmatched(ScenarioRunRecord record, AxuScenarioRunStatus status, DateTime utcNow)
        {
            if (record == null || record.MatchesStatus(status))
                return false;

            if (record.TryGetStartedUtc(out var startedAtUtc))
            {
                if (startedAtUtc - utcNow > TimeSpan.FromSeconds(AxuRunLiveness.StartGraceSeconds))
                    return true;

                if (utcNow - startedAtUtc < TimeSpan.FromSeconds(AxuRunLiveness.StartGraceSeconds))
                    return false;

                if (utcNow - startedAtUtc >= TimeSpan.FromMinutes(AxuScenarioRunStatus.StaleMinutes))
                    return true;
            }
            else
            {
                // 无法解析发起时间时，宽限期之后没有可靠依据继续等待。
                return true;
            }

            return status != null && status.IsRunning && status.IsStale(utcNow);
        }
    }
}
