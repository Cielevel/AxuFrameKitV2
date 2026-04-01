using AxuFrameKit.Common.Utility.Timer;
using UnityEngine;

namespace TestAndSample.Sample_Common.Utility.Timer
{
    public class SampleTimerSeries : MonoBehaviour
    {
        private void Start()
        {
            for (int i = 6; i < 10; i++)
            {
                int index = i;
                Timers.StartCd(index).OnComplete(() => { Debug.Log("Time=>" + index); });
            }
        }
    }
}