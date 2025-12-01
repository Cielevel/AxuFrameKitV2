using System;
using AxuFrameKit.Common.Utility.Timer;
using UnityEngine;

namespace TestAndSample.Sample_TimerSeries
{
    public class SampleTimerSeries : MonoBehaviour
    {
        private void Start()
        {
            for (int i = 6; i < 10; i++)
            {
                int index = i;
                CdTimer.StartCd(index, () => { Debug.Log("Time=>" + index); });
            }
        }
    }
}