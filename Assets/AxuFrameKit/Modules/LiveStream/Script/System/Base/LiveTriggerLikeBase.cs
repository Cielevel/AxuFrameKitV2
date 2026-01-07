using System;
using AxuFrameKit.Modules.LiveStream.Script.System.Unit;

namespace AxuFrameKit.Modules.LiveStream.Script.System.Base
{
    [Serializable]
    public abstract class LiveTriggerLikeBase : LiveTriggerBase
    {
        /// <summary>
        /// 点赞触发
        /// </summary>
        public abstract void Trigger(UserDataUnit userDataUnit, int count); // 保留这个参数，保留应对特殊情况
    }
}