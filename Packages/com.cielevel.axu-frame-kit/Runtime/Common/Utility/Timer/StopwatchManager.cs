using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Advanced.ObjectPool;
using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Timer
{
    public interface IStopwatchManager
    {
        /// <summary>
        /// 启动一个纯粹的正计时器（无限计时，直到调用 Cancel 取消为止）
        /// </summary>
        /// <returns>计时器句柄，可用于通过链式调用绑定回调或控制状态</returns>
        IStopwatchHandle StartStopwatch();
    }

    /// <summary>
    /// 正计时器句柄接口
    /// </summary>
    public interface IStopwatchHandle
    {
        /// <summary>
        /// 已经经过的时间
        /// </summary>
        float ElapsedTime { get; }

        /// <summary>
        /// 是否已暂停
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// 是否已被取消
        /// </summary>
        bool IsCancelled { get; }

        /// <summary> 每帧回调，参数为已经经过的时间 </summary>
        IStopwatchHandle OnTick(Action<float> onTick);

        /// <summary> 手动打表回调，参数为打表时的事件点（运行经过的时间） </summary>
        IStopwatchHandle OnLap(Action<float> onLap);

        /// <summary> 暂停回调 </summary>
        IStopwatchHandle OnPause(Action onPause);
        IStopwatchHandle OnPause(Action<float> onPause);

        /// <summary> 恢复（继续）回调 </summary>
        IStopwatchHandle OnResume(Action onResume);
        IStopwatchHandle OnResume(Action<float> onResume);

        /// <summary> 取消回调（强行取消时触发） </summary>
        IStopwatchHandle OnCancel(Action onCancel);

        /// <summary> 设置不受时间缩放（Time.timeScale）影响 </summary>
        IStopwatchHandle SetIgnoreTimeScale(bool ignoreTimeScale);

        /// <summary>
        /// 取消计时器（停止计时并移除）
        /// </summary>
        void Cancel();

        /// <summary>
        /// 手动打表（记录当前时间点并触发 OnLap 回调）
        /// </summary>
        void Lap();

        /// <summary>
        /// 暂停计时器
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复计时器
        /// </summary>
        void Resume();

        /// <summary>
        /// 重新开始正向计时（仅重置已经历时间为 0，不修改运行或暂停状态。对于已经取消的计时器无效）
        /// </summary>
        void Restart();
    }

    /// <summary>
    /// 正计时管理器；与 CdManager 相对应，提供正向计时的能力以及Fluent API调用。
    /// </summary>
    public class StopwatchManager : SingletonMonoCrDs<StopwatchManager>, IStopwatchManager
    {
        private const int DefaultCapacity = 32;

        private SimpleObjPool<StopwatchUnit> _pool;
        private List<StopwatchUnit> _runningList;
        private List<int> _pendingRemoveIndices;

        protected override void Awake()
        {
            base.Awake();

            _pool = new SimpleObjPool<StopwatchUnit>(Constructor, OnGet, OnRelease, DefaultCapacity);
            _runningList = new List<StopwatchUnit>(DefaultCapacity);
            _pendingRemoveIndices = new List<int>(DefaultCapacity);
        }

        private void Update()
        {
            if (_runningList.Count == 0)
                return;

            var deltaTime = Time.deltaTime;
            var unscaledDeltaTime = Time.unscaledDeltaTime;

            // 遍历所有计时器
            for (int i = 0; i < _runningList.Count; i++)
            {
                var unit = _runningList[i];

                // 跳过已暂停或已取消的
                if (unit.IsPaused || unit.IsCancelled)
                {
                    if (unit.IsCancelled)
                        _pendingRemoveIndices.Add(i);
                    continue;
                }

                unit.Tick(deltaTime, unscaledDeltaTime);
            }

            // 移除已完成或已取消的计时器
            RemovePendingUnits();
        }

        private void RemovePendingUnits()
        {
            if (_pendingRemoveIndices.Count == 0)
                return;

            // 从后向前移除，保证索引有效性
            for (int i = _pendingRemoveIndices.Count - 1; i >= 0; i--)
            {
                var index = _pendingRemoveIndices[i];
                var unit = _runningList[index];

                // 使用 swap-and-pop 优化移除性能
                var lastIndex = _runningList.Count - 1;
                if (index != lastIndex)
                {
                    _runningList[index] = _runningList[lastIndex];
                }

                _runningList.RemoveAt(lastIndex);
                _pool.Release(unit);
            }

            _pendingRemoveIndices.Clear();
        }

        #region Pool Method

        private StopwatchUnit Constructor() => new StopwatchUnit();
        private void OnGet(StopwatchUnit unit) => unit.Reset();
        private void OnRelease(StopwatchUnit unit) => unit.Reset();

        #endregion

        IStopwatchHandle IStopwatchManager.StartStopwatch()
        {
            var unit = _pool.Get();

            _runningList.Add(unit);

            return unit;
        }

        #region StopwatchUnit

        private class StopwatchUnit : IStopwatchHandle
        {
            private Action<float> _onTick;
            private Action<float> _onLap;
            private Action _onPause;
            private Action<float> _onPauseWithTime;
            private Action _onResume;
            private Action<float> _onResumeWithTime;
            private Action _onCancel;
            private bool _ignoreTimeScale;

            public float ElapsedTime { get; private set; }

            public bool IsPaused { get; private set; }
            public bool IsCancelled { get; private set; }

            public IStopwatchHandle OnTick(Action<float> onTick)
            {
                _onTick += onTick;
                return this;
            }

            public IStopwatchHandle OnLap(Action<float> onLap)
            {
                _onLap += onLap;
                return this;
            }

            public IStopwatchHandle OnPause(Action onPause)
            {
                _onPause += onPause;
                return this;
            }

            public IStopwatchHandle OnPause(Action<float> onPause)
            {
                _onPauseWithTime += onPause;
                return this;
            }

            public IStopwatchHandle OnResume(Action onResume)
            {
                _onResume += onResume;
                return this;
            }

            public IStopwatchHandle OnResume(Action<float> onResume)
            {
                _onResumeWithTime += onResume;
                return this;
            }

            public IStopwatchHandle OnCancel(Action onCancel)
            {
                _onCancel += onCancel;
                return this;
            }

            public IStopwatchHandle SetIgnoreTimeScale(bool ignoreTimeScale)
            {
                _ignoreTimeScale = ignoreTimeScale;
                return this;
            }

            public void Tick(float deltaTime, float unscaledDeltaTime)
            {
                float dt = _ignoreTimeScale ? unscaledDeltaTime : deltaTime;
                ElapsedTime += dt;

                _onTick?.Invoke(ElapsedTime);
            }

            public void Lap()
            {
                if (IsCancelled || IsPaused) return;
                _onLap?.Invoke(ElapsedTime);
            }

            public void Cancel()
            {
                if (IsCancelled) return;
                IsCancelled = true;
                _onCancel?.Invoke();
            }

            public void Pause()
            {
                if (IsPaused) return;
                IsPaused = true;
                _onPause?.Invoke();
                _onPauseWithTime?.Invoke(ElapsedTime);
            }

            public void Resume()
            {
                if (!IsPaused) return;
                IsPaused = false;
                _onResume?.Invoke();
                _onResumeWithTime?.Invoke(ElapsedTime);
            }

            public void Restart()
            {
                if (IsCancelled) return;
                ElapsedTime = 0f;
            }

            public void Reset()
            {
                ElapsedTime = 0f;
                IsPaused = false;
                IsCancelled = false;
                _ignoreTimeScale = false;

                // 直接设为 null，避免 GC
                _onTick = null;
                _onLap = null;
                _onPause = null;
                _onPauseWithTime = null;
                _onResume = null;
                _onResumeWithTime = null;
                _onCancel = null;
            }
        }

        #endregion
    }
}
