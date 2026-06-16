using UnityEngine;
using AxuFrameKit.Common.Utility.Event;

namespace AxuFrameKit.Common.Utility.Auto.AutoDestroy
{
    /// <summary>
    /// 事件频道-触发后切换激活状态
    /// </summary>
    public class AutoEventChannelToggleActive : AutoEventChannelBase
    {
        public bool onReceiveEventSetActive;

        private void Awake()
        {
            _eventChannel.Register(SetActive);
        }

        private void SetActive()
        {
            gameObject.SetActive(onReceiveEventSetActive);
        }

        private void OnDestroy()
        {
            _eventChannel.Unregister(SetActive);
        }
    }
}