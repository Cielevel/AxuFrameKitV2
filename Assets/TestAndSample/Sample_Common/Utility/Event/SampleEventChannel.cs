using UnityEngine;
using AxuFrameKit.Common.Utility.Event;

namespace TestAndSample.Sample_Common.Utility.Event
{
    [CreateAssetMenu(fileName = "SampleEventChannel", menuName = "AxuSample/EventChannel/SampleChannel")]
    public class SampleEventChannel : EventChannel<int>
    {

    }
}