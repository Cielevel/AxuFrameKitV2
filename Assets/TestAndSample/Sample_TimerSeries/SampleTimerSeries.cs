using AxuFrameKit.Common.Utility.Timer;
using UnityEngine;

namespace TestAndSample.Sample_TimerSeries
{
    public class SampleTimerSeries : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                CountDownU.Get().StartTik(5).OnComplete += () =>
                {
                    Debug.Log("5秒到");
                };
            }
        }
    }
}