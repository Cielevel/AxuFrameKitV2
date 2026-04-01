using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Advanced.ObjectPool;
using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Timer
{
    public interface ICountdownManager
    {
        /// <summary>
        /// 启动一个倒计时
        /// </summary>
        /// <param name="time">倒计时时长（秒）</param>
        /// <returns>计时器句柄，可用于通过链式调用绑定回调或控制状态</returns>
        ICountdownHandle StartCd(float time);
    }

    /// <summary>
    /// 计时器句柄接口，用于控制单个计时器
    /// </summary>
    public interface ICountdownHandle
    {
        /// <summary>
        /// 剩余时间
        /// </summary>
        float RemainingTime { get; }

        /// <summary>
        /// 剩余时间的比例 (剩余时间 / 总时间)，范围 0~1
        /// </summary>
        float RemainingRatio { get; }

        /// <summary>
        /// 已经过时间的比例 (已过时间 / 总时间)，范围 0~1
        /// </summary>
        float PassedRatio { get; }

        /// <summary>
        /// 是否已完成
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// 是否已暂停
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// 是否已被取消
        /// </summary>
        bool IsCancelled { get; }

        /// <summary> 每帧回调 </summary>
        ICountdownHandle OnTick(Action<float> onTick);

        /// <summary> 完成回调 </summary>
        ICountdownHandle OnComplete(Action onComplete);

        /// <summary> 暂停回调 </summary>
        ICountdownHandle OnPause(Action onPause);
        ICountdownHandle OnPause(Action<float> onPause);

        /// <summary> 恢复（继续）回调 </summary>
        ICountdownHandle OnResume(Action onResume);
        ICountdownHandle OnResume(Action<float> onResume);

        /// <summary> 取消回调（强行取消时触发） </summary>
        ICountdownHandle OnCancel(Action onCancel);

        /// <summary> 设置不受时间缩放（Time.timeScale）影响 </summary>
        ICountdownHandle SetIgnoreTimeScale(bool ignoreTimeScale);

        /// <summary>
        /// 取消计时器
        /// </summary>
        void Cancel();

        /// <summary>
        /// 暂停计时器
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复计时器
        /// </summary>
        void Resume();

        /// <summary>
        /// 重新开始计时（仅重置时间，不修改运行或暂停状态。对于已经取消或完成的计时器无效）
        /// </summary>
        void Restart();
    }

    /// <summary>
    /// CountDownManager，倒计时管理器；倒计时数量多时推荐使用。
    /// </summary>
    public class CountdownManager : SingletonMonoCrDs<CountdownManager>, ICountdownManager
    {
        private const int DefaultCapacity = 32;

        private SimpleObjPool<CdUnit> _cdPool;
        private List<CdUnit> _cdRunningList;

        // 用于在遍历过程中安全地标记需要移除的项
        private List<int> _pendingRemoveIndices;

        protected override void Awake()
        {
            base.Awake();

            _cdPool = new SimpleObjPool<CdUnit>(Constructor, OnGet, OnRelease, DefaultCapacity);
            _cdRunningList = new List<CdUnit>(DefaultCapacity);
            _pendingRemoveIndices = new List<int>(DefaultCapacity);
        }

        private void Update()
        {
            if (_cdRunningList.Count == 0)
                return;

            var deltaTime = Time.deltaTime;
            var unscaledDeltaTime = Time.unscaledDeltaTime;

            // 遍历所有计时器
            for (int i = 0; i < _cdRunningList.Count; i++)
            {
                var unit = _cdRunningList[i];

                // 跳过已暂停或已取消的
                if (unit.IsPaused || unit.IsCancelled)
                {
                    if (unit.IsCancelled)
                        _pendingRemoveIndices.Add(i);
                    continue;
                }

                unit.Tick(deltaTime, unscaledDeltaTime);

                if (unit.IsCompleted)
                {
                    unit.InvokeComplete();
                    _pendingRemoveIndices.Add(i);
                }
            }

            // 从后向前移除，避免索引偏移
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
                var unit = _cdRunningList[index];

                // 使用 swap-and-pop 优化移除性能（O(1) 而非 O(n)）
                var lastIndex = _cdRunningList.Count - 1;
                if (index != lastIndex)
                {
                    _cdRunningList[index] = _cdRunningList[lastIndex];
                }

                _cdRunningList.RemoveAt(lastIndex);
                _cdPool.Release(unit);
            }

            _pendingRemoveIndices.Clear();
        }

        #region Pool Method

        private CdUnit Constructor() => new CdUnit();

        private void OnGet(CdUnit cdUnit) => cdUnit.Reset();

        private void OnRelease(CdUnit cdUnit) => cdUnit.Reset();

        #endregion

        ICountdownHandle ICountdownManager.StartCd(float time)
        {
            var unit = _cdPool.Get();

            unit.SetTime(time);

            _cdRunningList.Add(unit);

            return unit;
        }

        #region CdUnit

        private class CdUnit : ICountdownHandle
        {
            private Action<float> _onTick;
            private Action _onComplete;
            private Action _onPause;
            private Action<float> _onPauseWithTime;
            private Action _onResume;
            private Action<float> _onResumeWithTime;
            private Action _onCancel;
            private bool _ignoreTimeScale;

            private float _totalTime;

            public float RemainingTime { get; private set; }
            public float RemainingRatio => _totalTime > 0f ? Mathf.Clamp01(RemainingTime / _totalTime) : 0f;
            public float PassedRatio => _totalTime > 0f ? Mathf.Clamp01(1f - (RemainingTime / _totalTime)) : 0f;

            public bool IsCompleted => RemainingTime <= 0f && !IsCancelled;
            public bool IsPaused { get; private set; }
            public bool IsCancelled { get; private set; }

            public void SetTime(float time)
            {
                _totalTime = time;
                RemainingTime = time;
            }

            public ICountdownHandle OnTick(Action<float> onTick)
            {
                _onTick += onTick;
                return this;
            }

            public ICountdownHandle OnComplete(Action onComplete)
            {
                _onComplete += onComplete;
                return this;
            }

            public ICountdownHandle OnPause(Action onPause)
            {
                _onPause += onPause;
                return this;
            }

            public ICountdownHandle OnPause(Action<float> onPause)
            {
                _onPauseWithTime += onPause;
                return this;
            }

            public ICountdownHandle OnResume(Action onResume)
            {
                _onResume += onResume;
                return this;
            }

            public ICountdownHandle OnResume(Action<float> onResume)
            {
                _onResumeWithTime += onResume;
                return this;
            }

            public ICountdownHandle OnCancel(Action onCancel)
            {
                _onCancel += onCancel;
                return this;
            }

            public ICountdownHandle SetIgnoreTimeScale(bool ignoreTimeScale)
            {
                _ignoreTimeScale = ignoreTimeScale;
                return this;
            }

            public void Tick(float deltaTime, float unscaledDeltaTime)
            {
                float dt = _ignoreTimeScale ? unscaledDeltaTime : deltaTime;
                RemainingTime -= dt;

                // 只有剩余时间 > 0 时才调用 Tick
                if (RemainingTime > 0f)
                    _onTick?.Invoke(RemainingTime);
            }

            public void InvokeComplete()
            {
                _onComplete?.Invoke();
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
                _onPauseWithTime?.Invoke(RemainingTime);
            }

            public void Resume()
            {
                if (!IsPaused) return;
                IsPaused = false;
                _onResume?.Invoke();
                _onResumeWithTime?.Invoke(RemainingTime);
            }

            public void Restart()
            {
                if (IsCancelled || IsCompleted) return;
                RemainingTime = _totalTime;
            }

            public void Reset()
            {
                _totalTime = 0f;
                RemainingTime = 0f;
                IsPaused = false;
                IsCancelled = false;
                _ignoreTimeScale = false;

                // 直接设为 null，避免 GC（不再创建空委托）
                _onTick = null;
                _onComplete = null;
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