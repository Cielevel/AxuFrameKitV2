using System;
using System.Collections;
using AxuFrameKit.Common.Utility.UseMono;
using UnityEngine;

namespace TestAndSample.Sample_UseMono
{
    public class SampleUserMono : MonoBehaviour
    {
        private void Start()
        {
            for (int i = 3; i < 6; i++)
            {
                int time = i;
                
                var timer = new Timer
                {
                    Time     = time,
                    OnTikEnd = () => Debug.Log("Time's up: " + time)
                };
            
                timer.StartTik();
            }
        }
    }

    public class Timer : IUseMonoBehaviourSeparate
    {
        public float Time { get; set; }

        public Action OnTikEnd { get; set; }

        public void StartTik()
        {
            this.StartCoroutine(Tik());
            return;
            
            IEnumerator Tik()
            {
                while (0 < Time)
                {
                    Time -= UnityEngine.Time.deltaTime;
                    yield return null;
                }
                
                OnTikEnd?.Invoke();
            }
        }
    }
}
