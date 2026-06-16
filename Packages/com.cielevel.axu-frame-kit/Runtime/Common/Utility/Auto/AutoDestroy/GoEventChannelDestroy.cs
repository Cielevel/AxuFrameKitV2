using UnityEngine;
using AxuFrameKit.Common.Utility.Event;

namespace AxuFrameKit.Common.Utility.Auto.AutoDestroy
{
    /// <summary>
    /// 事件频道-触发后销毁物体
    /// </summary>
    public class GoEventChannelDestroy : MonoBehaviour
    {
        public EventChannelCore eventChannel;

        private void Awake()
        {
            eventChannel.Register(Trigger);
        }

        private void Trigger()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            eventChannel.Unregister(Trigger);
        }
    }
}