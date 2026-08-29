using System;
using System.Text.RegularExpressions;

namespace AxuFrameKit.Modules.LiveStream
{
    [Serializable]
    public abstract class LiveTriggerCommentBase : LiveTriggerBase
    {
        /// <summary>
        /// 评论触发
        /// </summary>
        public virtual void Trigger(UserDataComment userDataUnit) { }

        /// <summary>
        /// 评论触发，将显式传递chatMessage
        /// </summary>
        public virtual void Trigger(UserDataComment userDataUnit, string chatMessage) { }

        /// <summary>
        /// 由正则表达式触发（不是必须实现的方法）
        /// </summary>
        public virtual void Trigger(UserDataComment userDataUnit, Match match) { }
    }
}