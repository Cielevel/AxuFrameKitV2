using System;
using System.Collections.Generic;

namespace AxuTestFramework
{
    /// <summary>
    /// 静态/单例重置登记表：在域重载关闭（Enter Play Mode Options 置 3）或同一会话连续运行多个测试时，
    /// 显式登记并批量执行各系统的重置动作，防止测试间状态残留（M0 实测确认的高优先级风险面）。
    /// 建议 [UnitySetUp] 中先 <see cref="ResetStatics"/>，再登记本轮需要的重置项；登记的重置动作必须幂等。
    /// 仅设计为主线程（测试流程）使用，不做并发防护。
    /// </summary>
    public static class AxuTestState
    {
        private static readonly Dictionary<string, Action> _resets = new Dictionary<string, Action>();

        /// <summary>当前已登记的重置项数量（诊断用）。</summary>
        public static int RegisteredCount
        {
            get { return _resets.Count; }
        }

        /// <summary>
        /// 登记一个重置项；同名 key 会覆盖旧登记。
        /// </summary>
        /// <param name="key">登记名（建议用"系统名/用途"形式，如 "MySystem/Session"）。</param>
        /// <param name="reset">重置动作，必须幂等且无副作用扩散。</param>
        public static void Register(string key, Action reset)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key 不能为空。", nameof(key));
            if (reset == null)
                throw new ArgumentNullException(nameof(reset));

            _resets[key] = reset;
        }

        /// <summary>
        /// 移除登记项。
        /// </summary>
        /// <returns>是否存在该 key 并已被移除。</returns>
        public static bool Unregister(string key)
        {
            return _resets.Remove(key);
        }

        /// <summary>
        /// 遍历执行所有已登记的重置动作，保留登记表。
        /// 单项异常被捕获并继续执行后续项，异常随返回值汇总报告。
        /// </summary>
        /// <returns>失败项描述列表（"key → 异常类型: 异常消息"）；全部成功则为空列表。</returns>
        public static IReadOnlyList<string> ResetAll()
        {
            List<string> failures = new List<string>();
            foreach (KeyValuePair<string, Action> pair in _resets)
            {
                try
                {
                    pair.Value();
                }
                catch (Exception e)
                {
                    failures.Add(pair.Key + " → " + e.GetType().Name + ": " + e.Message);
                }
            }
            return failures;
        }

        /// <summary>
        /// <see cref="ResetAll"/> 的别名，并在执行后清空登记表（完全复位，登记项需重新注册）。
        /// 适用 [UnitySetUp] 首行：先清残留 + 清登记，再按本轮需要重新 Register。
        /// </summary>
        /// <returns>同 <see cref="ResetAll"/> 的失败汇总。</returns>
        public static IReadOnlyList<string> ResetStatics()
        {
            IReadOnlyList<string> failures = ResetAll();
            _resets.Clear();
            return failures;
        }
    }
}
