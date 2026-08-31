using System.Collections;
using UnityEngine;

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>
    /// 真实时间等待原语（不受 timeScale 影响）。
    /// <para>实现说明（2026-08-31 修复）：早期版本产出 WaitForSecondsRealtime 分段（每段 ≤0.25s）
    /// 交给 Unity 协程调度；但实测本工程环境（Unity 6000.0.23f1 Editor）下 WFSRT 的等待契约不可靠
    /// ——诊断显示指令被逐帧放行（恢复间隔≈1 帧、m_WaitUntilTime 保持 -1 或 keepWaiting=True 仍被放行），
    /// 2s 等待实测仅 ~0.7s。现改为基于 <see cref="Time.realtimeSinceStartup"/>（单调真实时钟）的
    /// 逐帧轮询：不依赖宿主对等待指令的调度支持，对"Unity 协程 / 手动 MoveNext"两种驱动方式均幂等正确；
    /// yield null 使宿主看门狗可逐帧介入（超时/取消响应粒度从 ≤0.25s 提升到 ≤1 帧）。</para>
    /// </summary>
    public static class AgentWait
    {
        /// <summary>按真实时间等待指定秒数（≤0 时不等待，立即完成）。</summary>
        public static IEnumerator RealtimeSeconds(float seconds)
        {
            if (seconds <= 0f) yield break;

            var deadline = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < deadline)
                yield return null;
        }
    }
}
