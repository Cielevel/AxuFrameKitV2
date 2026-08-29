using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream
{
    [Serializable]
    public abstract class LiveTriggerBase : ScriptableObject
    {
        [field: SerializeField]
        public string Remark { get; protected set; } = "Null";

        /// <summary>
        /// 一些特殊情况可能会使用该方法，比如：多个user触发该方法
        /// </summary>
        public virtual void Trigger(params UserDataUnit[] userDataUnits) { }
    }
}