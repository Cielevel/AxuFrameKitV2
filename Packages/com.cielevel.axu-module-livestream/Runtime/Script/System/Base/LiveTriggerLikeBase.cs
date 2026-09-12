using System;

namespace AxuFrameKit.Modules.LiveStream
{
    /// <summary>
    /// 点赞触发器基类。
    /// <para>
    /// 与 Comment / Gift 不同，Like 没有配置层的多种触发形态，其多样性取决于具体游戏，
    /// TriggerLikeConfigSo 只是入口；连击、阈值、限流等差异化逻辑由游戏侧在此实现中自行定义。
    /// </para>
    /// </summary>
    [Serializable]
    public abstract class LiveTriggerLikeBase : LiveTriggerBase
    {
        /// <summary>
        /// 点赞触发
        /// </summary>
        public abstract void Trigger(UserDataLike userDataUnit, int count); // 保留这个参数，保留应对特殊情况
    }
}