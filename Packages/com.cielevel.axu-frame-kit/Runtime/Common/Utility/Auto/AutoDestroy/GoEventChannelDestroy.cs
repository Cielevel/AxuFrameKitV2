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
            if (eventChannel == null)
            {
                Debug.LogWarning($"{nameof(GoEventChannelDestroy)}: eventChannel 未赋值，跳过事件注册。", this);
                return;
            }

            eventChannel.Register(Trigger);
        }

        private void Trigger()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // 未赋值时 Awake 已警告过，拆除期不再重复刷日志
            if (eventChannel == null) return;

            eventChannel.Unregister(Trigger);
        }
    }
}