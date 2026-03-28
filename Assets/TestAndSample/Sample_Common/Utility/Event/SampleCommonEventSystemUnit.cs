using AxuFrameKit.Common.Utility.Event;
using UnityEngine;

namespace TestAndSample.Sample_Common.Utility.Event
{
    public struct TestEvent
    {
        public int Count { get; }

        public TestEvent(int count) => Count = count;
    }

    public class SampleCommonEventSystemUnit : MonoBehaviour
    {
        private void Awake()
        {
            EventCentral.Default.Register<TestEvent>(Trigger)
                        .UnRegisterWhenDisabled(gameObject)
                        .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Trigger(TestEvent evt)
        {
            Debug.Log("Evt.Count: " + evt.Count + ", gameObject name: " + gameObject.name);
        }
    }
}
