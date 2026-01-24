using UnityEngine;
using AxuFrameKit.Common.Utility.Event;

namespace AxuFrameKit.Common.Utility.Auto.AutoDestroy
{
    /// <summary>
    /// 事件频道销毁
    /// </summary>
    public class GoEventChannelDestroy : MonoBehaviour
    {
        public EventChannelCore eventChannel;

        private void Awake()
        {
            eventChannel.Register(OnDestroy);
        }

        private void OnDestroy()
        {
            eventChannel.Unregister(OnDestroy);
        }
    }
}