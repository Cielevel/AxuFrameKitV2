using AxuFrameKit.Common.Utility.Trigger;
using UnityEngine;

namespace TestAndSample.Sample_Common.Utility.Trigger
{
    public class SampleTriggerable : AbsTriggerable
    {
        protected override void OnExecute()
        {
            Debug.Log("SampleTriggerable Execute: " + gameObject.name);
        }
    }
}