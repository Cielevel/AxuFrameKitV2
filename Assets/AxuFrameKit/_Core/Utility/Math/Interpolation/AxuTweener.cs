using System;

namespace AxuFrameKit._Core.Utility.Math.Interpolation
{
    /*
     * 不为借口增添：AsWorld，AsLocal 这样的接口；
     * 因为后续会增加DoFade，DoColor一类的操作，所以本地与世界坐标这一类并不是通用的操作。
    */ 
    
    public interface IAxuTweener
    {
        bool IsCompleted { get; }
        bool IsPaused { get; }
        
        float Duration { get; }
        float RunTime { get; }
        
        void Tik(float deltaTime);

        // AxuTween SetEase(EaseType easeType);
        
        // void OnComplete(Action callback);

        void SetProgress(float progress);
        
        void Pause();

        void Play();

        void Replay();
        
        void Kill();
    }
    
    public abstract class AxuTweener : IAxuTweener
    {
        public bool IsCompleted { get; private set; }
        public bool IsPaused { get; private set; }
        
        private EaseType _easeType;

        public float Duration { get; protected set; }
        public float RunTime { get; protected set; }

        private IEase _ease = EaseFactory.Get(EaseType.InCubic);

        private Action _onComplete;

        protected float _t;

        public AxuTweener(float duration) => Duration = duration;

        void IAxuTweener.Tik(float deltaTime)
        {
            if (IsPaused)
                return;
            
            if (IsCompleted)
                return;
            
            RunTime += deltaTime;
            
            // 使用新方法应用进度
            ApplyCurrentProgress();

            if (0 < Duration && 1 <= RunTime / Duration) // 检查是否完成
            {
                RunTime = Duration; 
                ApplyCurrentProgress();
                
                IsCompleted = true;
                _onComplete?.Invoke();
            }
        }
        
        // 【新增方法】用于计算 _t 并执行 Action()，供 Tik 和 SetProgress 调用
        private void ApplyCurrentProgress()
        {
            if (Duration <= 0) return; // 避免除以零

            float rate = RunTime / Duration;
            
            _t = _ease.Evaluate(rate);

            _t = Clamp01(_t); // 钳制
            
            Action();
        }

        public AxuTweener SetEase(EaseType easeType)
        {
            _easeType = easeType;

            _ease = EaseFactory.Get(_easeType);

            return this;
        }

        public AxuTweener OnComplete(Action callback)
        {
            _onComplete = callback;

            return this;
        }
        
        public void SetProgress(float progress) // 0~1
        {
            progress = Clamp01(progress);

            RunTime = progress * Duration;
            
            ApplyCurrentProgress();
        }

        public void Play() => IsPaused = false;
        
        void IAxuTweener.Replay()
        {
            IsPaused    = false;
            IsCompleted = false;

            RunTime = 0f;
        }

        public void Pause() => IsPaused = true;

        public void Kill() => IsCompleted = true;
        
        protected abstract void Action();
        
        private static float Clamp01(float value) => System.Math.Clamp(value, 0, 1);
    }
}