using UnityEngine;
using AxuFrameKit._Core.Advanced.EventSystem;
using AxuFrameKit._Core.Utility.Multiton;
using AxuFrameKit.Common.Utility.Event;

namespace AxuFrameKit.Common.Utility.Static
{
    /// <summary>
    /// 域重载静态状态守卫。
    /// 当 Enter Play Mode Options 关闭域重载（或未来被改动）时，
    /// 静态事件订阅与多例/单例实例会跨 Play 残留（悬空订阅指向已销毁对象），
    /// 这里在每次进入 Play 的最早期统一清理。
    /// MultitonRegistry 现为全框架静态重置登记中心：Multiton/MultitonAuto/MultitonMono 族、
    /// Singleton 家族（Lazy/Mono/CustomMono/MonoCr/DesignPattern Singleton）、UseMono handler
    /// 均在各自静态构造器中向其登记清理动作，新增静态持有者请照此登记，避免漏网。
    /// </summary>
    internal static class AxuStaticResetGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            EventCenter.Default.Clear();
            EventCentral.Default.Clear();
            EventChannelCore.ResetAllInstances();
            MultitonRegistry.ClearAll();
        }
    }
}
