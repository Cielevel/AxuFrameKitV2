using UnityEngine;
using AxuFrameKit.Common.Utility.Event;

namespace TestAndSample.Sample_Common.Utility.Event
{
    public class ChannelReceiver : MonoBehaviour
    {
        public EventChannelCore eventChannel;

        private void Start()
        {
            eventChannel.TryRegister(NoParamMethod);
            eventChannel.TryRegister<int>(Method);
            return;

            void Method(int p)
            {
                Debug.Log($"参数事件接收: {p}");
            }

            void NoParamMethod()
            {
                Debug.Log("无参函数接收");
                gameObject.SetActive(false);
            }
        }
    }
}