using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.Utility.Multiton
{
    /// <summary>
    /// Multiton 多例的集中登记处。
    /// Multiton&lt;T&gt; 的静态构造器会把各自的清理动作登记到此，
    /// 供宿主环境在域重载关闭时统一清理所有多例实例（见 Common 层 AxuStaticResetGuard）。
    /// </summary>
    public static class MultitonRegistry
    {
        private static readonly object Lock = new object();
        private static readonly Dictionary<Type, Action> ClearActions = new Dictionary<Type, Action>(8);

        public static void Register(Type multitonType, Action clearAction)
        {
            lock (Lock)
            {
                ClearActions[multitonType] = clearAction;
            }
        }

        /// <summary> 调用所有已登记多例的清理动作 </summary>
        public static void ClearAll()
        {
            Action[] actions;
            lock (Lock)
            {
                if (ClearActions.Count == 0)
                    return;

                actions = new Action[ClearActions.Count];
                ClearActions.Values.CopyTo(actions, 0);
            }

            foreach (var action in actions)
                action?.Invoke();
        }
    }
}
