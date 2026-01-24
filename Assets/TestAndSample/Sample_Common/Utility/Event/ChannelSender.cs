using UnityEngine;
using AxuFrameKit.Common.Utility.Event;

namespace TestAndSample.Sample_Common.Utility.Event
{
    public class ChannelSender : MonoBehaviour
    {
        public EventChannelCore eventChannel;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("无参事件发送");
                eventChannel.TryRaise();
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("默认参数事件发送");
                eventChannel.TryRaiseDefaultValue();
            }
        }
    }
}