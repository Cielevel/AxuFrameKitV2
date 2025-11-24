using System;
using AxuFrameKit.Common.Utility.Multiton;

namespace AxuFrameKit.Common.Utility.Timer
{
    /// <summary>
    /// 通过Update实现的计时器，受到游戏时间缩放影响；倒计时多时，推荐使用CdManager
    /// </summary>
    public class CountDownU : MultitonAutoMonoR<CountDownU>
    {
        private float _timeLeft;

        private bool _isRunning;

        public event Action<float> OnTik;
        public event Action OnComplete;
        
        private void Update()
        {
            if (_isRunning == false)
                return;
            
            if (0 < _timeLeft)
            {
                _timeLeft -= UnityEngine.Time.deltaTime;
                
                OnTik?.Invoke(_timeLeft);
                
                return;
            }

            _isRunning = false;
            OnComplete?.Invoke();
            
            Release(this);
        }
        
        public CountDownU StartTik(float time)
        {
            if (_isRunning)
                return this;
            
            _timeLeft = time;
            
            _isRunning = true;

            return this;
        }

        public void StopTik()
        {
            _isRunning = false;
        }
    }
}