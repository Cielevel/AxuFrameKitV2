using System;

namespace AxuFrameKit.Modules.LiveStream
{
    [Serializable]
    public abstract class LiveTriggerGiftBase : LiveTriggerBase
    {
        /// <summary>
        /// 礼物触发
        /// </summary>
        public abstract void Trigger(UserDataUnit userDataUnit, int count); // 保留这个参数，保留应对特殊情况
    }
}