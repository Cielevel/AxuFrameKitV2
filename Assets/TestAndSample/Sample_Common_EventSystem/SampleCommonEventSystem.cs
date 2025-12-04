using System;
using AxuFrameKit.Common.Utility.Event;
using UnityEngine;

namespace TestAndSample.Sample_Common_EventSystem
{
    public class SampleCommonEventSystem : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                EventCentral.Send(new TestEvent(3));
            }
        }
    }
}
