using System;
using AxuFrameKit.Common.Utility.Multiton;

namespace AxuFrameKit.Common.Utility.Timer
{
    /// <summary>
    /// 通过Update实现的计时器，受到游戏时间缩放影响
    /// </summary>
    public class CountDownU : MultitonAutoMonoR<CountDownU>
    {
        public float TimeLeft { get; private set; }

        public bool IsRunning { get; private set; }

        public event Action<float> OnTik;
        public event Action OnComplete;
        
        private void Update()
        {
            if (IsRunning == false)
                return;
            
            if (0 < TimeLeft)
            {
                TimeLeft -= UnityEngine.Time.deltaTime;
                
                OnTik?.Invoke(TimeLeft);
                
                return;
            }

            IsRunning = false;
            OnComplete?.Invoke();
            
            Release(this);
        }
        
        public CountDownU StartTik(float time)
        {
            if (IsRunning)
                return this;
            
            TimeLeft = time;
            
            IsRunning = true;

            return this;
        }

        public void StopTik()
        {
            IsRunning = false;
        }
    }
}