using System;

namespace AxuFrameKit._Core.Utility.Math.Interpolation
{
    /*
     * 不为借口增添：AsWorld，AsLocal 这样的接口；
     * 因为后续会增加DoFade，DoColor一类的操作，所以本地与世界坐标这一类并不是通用的操作。
    */

    /// <summary>
    /// AxuTweener 动画器接口定义
    /// </summary>
    public interface IAxuTweener
    {
        /// <summary> 是否已完成 </summary>
        bool IsCompleted { get; }
        /// <summary> 是否处于暂停状态 </summary>
        bool IsPaused { get; }

        /// <summary> 动画总时长（秒） </summary>
        float Duration { get; }
        /// <summary> 当前已运行时间 </summary>
        float RunTime { get; }
        /// <summary> 当前进度 [0, 1] </summary>
        float Progress { get; }

        /// <summary> 重置状态（清空回调与进度） </summary>
        void Reset();

        /// <summary>
        /// 激活，得到初始状态。
        /// </summary>
        void Startup();

        /// <summary> 驱动一帧逻辑 </summary>
        /// <param name="deltaTime">自上一帧以来的增量时间</param>
        void Tik(float deltaTime);

        // AxuTween SetEase(EaseType easeType);

        // void OnComplete(Action callback);

        /// <summary> 手动修改动画总时长 </summary>
        void SetDuration(float duration);

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

    /// <summary>
    /// 动画器基类，封装通用更新逻辑
    /// </summary>
    /// <typeparam name="TTarget">动画操作的目标对象类型</typeparam>
    public abstract class AxuTweener<TTarget> : IAxuTweener
    {
        // --public--
        /// <inheritdoc/>
        public bool IsCompleted { get; private set; }
        /// <inheritdoc/>
        public bool IsPaused { get; private set; }
        /// <inheritdoc/>
        public float Duration { get; protected set; }
        /// <inheritdoc/>
        public float RunTime { get; protected set; }
        /// <inheritdoc/>
        public float Progress { get; protected set; }

        // --protected--
        protected float _t;
        protected TTarget _target;

        // --private--
        private EaseType _easeType;
        private IEase _ease = EaseFactory.Get(EaseType.InCubic);
        private Action _onComplete;

        public AxuTweener(float duration) => Duration = duration;

        private static float Clamp01(float value) => System.Math.Clamp(value, 0, 1);

        void IAxuTweener.Startup()
        {
            IsCompleted = false;
            IsPaused = false;

            RunTime = 0f;
            Progress = 0f;

            OnStartup();
        }

        void IAxuTweener.Reset()
        {
            IsCompleted = false;
            IsPaused = false;

            RunTime = 0f;
            Progress = 0f;

            _onComplete = null;
        }

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

        /// <summary> 设置动画缓动类型 </summary>
        public AxuTweener<TTarget> SetEase(EaseType easeType)
        {
            _easeType = easeType;

            _ease = EaseFactory.Get(_easeType);

            return this;
        }

        /// <summary> 设置完成后的回调 </summary>
        public AxuTweener<TTarget> OnComplete(Action callback)
        {
            _onComplete = callback;

            return this;
        }

        /// <summary> 设置目标对象 </summary>
        public AxuTweener<TTarget> SetTarget(TTarget target)
        {
            _target = target;
            return this;
        }

        public void SetDuration(float duration) => Duration = duration;

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

            IsPaused = false;
            IsCompleted = false;

            RunTime = 0f;
        }

        public void Pause() => IsPaused = true;

        public void Kill() => IsCompleted = true;

        protected abstract void Action();

        protected abstract void OnStartup(); // Start时，Replay时
    }
}