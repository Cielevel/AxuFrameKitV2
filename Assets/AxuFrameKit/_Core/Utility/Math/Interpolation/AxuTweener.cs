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
        float Progress { get; }

        /// <summary>
        /// 激活，得到初始状态。
        /// </summary>
        void Startup();
        
        void Tik(float deltaTime);

        // AxuTween SetEase(EaseType easeType);
        
        // void OnComplete(Action callback);

        /// <summary>
        /// 设置进度
        /// </summary>
        /// <param name="progress">进度[0,1]</param>
        void SetProgress(float progress);
        
        /// <summary>
        /// 暂停执行
        /// </summary>
        void Pause();

        /// <summary>
        /// 从暂停的状态恢复
        /// </summary>
        void Resume();

        /// <summary>
        /// 重新播放
        /// </summary>
        void Replay();
        
        /// <summary>
        /// 销毁该Tweener
        /// </summary>
        void Kill();
    }
    
    public abstract class AxuTweener : IAxuTweener
    {
        public bool IsCompleted { get; private set; }
        public bool IsPaused { get; private set; }

        private EaseType _easeType;

        public float Duration { get; protected set; }
        public float RunTime { get; protected set; }
        public float Progress { get; protected set; }

        private IEase _ease = EaseFactory.Get(EaseType.InCubic);

        private Action _onComplete;

        protected float _t;

        private static float Clamp01(float value) => System.Math.Clamp(value, 0, 1);
        
        public AxuTweener(float duration) => Duration = duration;

        void IAxuTweener.Startup() => OnStartup();

        void IAxuTweener.Tik(float deltaTime)
        {
            if (IsPaused)
                return;
            
            if (IsCompleted)
                return;
            
            RunTime += deltaTime;
            
            if (0 < Duration && Duration <= RunTime) // 检查是否完成
            {
                RunTime = Duration; 
                ApplyCurrentProgress();
                
                IsCompleted = true;
                _onComplete?.Invoke();
            }
            else
                ApplyCurrentProgress();
        }
        
        // 【新增方法】用于计算 _t 并执行 Action()，供 Tik 和 SetProgress 调用
        private void ApplyCurrentProgress()
        {
            if (Duration <= 0) // 避免除以零
                return;

            Progress = Clamp01(RunTime / Duration);

            _t = _ease.Evaluate(Progress);
            
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
        
        void IAxuTweener.SetProgress(float progress) // 0~1
        {
            progress = Clamp01(progress);

            RunTime = progress * Duration;

            Progress = progress;
            
            if (0 < Duration && Duration <= RunTime) // 检查是否完成
            {
                RunTime = Duration; 
                ApplyCurrentProgress();
                
                IsCompleted = true;
                _onComplete?.Invoke();
            }
            else
                ApplyCurrentProgress();
        }

        public void Resume() => IsPaused = false;
        
        void IAxuTweener.Replay()
        {
            // OnStart(); // 此处不用OnStart，Replay时会加入Runner，加入时会自动调用Start
            
            IsPaused    = false;
            IsCompleted = false;

            RunTime = 0f;
        }

        public void Pause() => IsPaused = true;

        public void Kill() => IsCompleted = true;
        
        protected abstract void Action();

        protected abstract void OnStartup(); // Start时，Replay时
    }
}