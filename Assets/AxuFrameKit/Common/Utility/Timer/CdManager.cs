using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Advanced.ObjectPool;
using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Timer
{
    public interface ICdManager
    {
        /// <summary>
        /// 启动一个倒计时
        /// </summary>
        /// <param name="time">倒计时时长（秒）</param>
        /// <param name="onComplete">完成回调</param>
        /// <returns>计时器句柄，可用于取消或控制计时器</returns>
        ICdHandle StartCd(float time, Action onComplete);

        /// <summary>
        /// 启动一个倒计时，带 Tick 回调
        /// </summary>
        /// <param name="time">倒计时时长（秒）</param>
        /// <param name="onTick">每帧回调，参数为剩余时间</param>
        /// <param name="onComplete">完成回调</param>
        /// <returns>计时器句柄，可用于取消或控制计时器</returns>
        ICdHandle StartCd(float time, Action<float> onTick, Action onComplete);
    }

    /// <summary>
    /// 计时器句柄接口，用于控制单个计时器
    /// </summary>
    public interface ICdHandle
    {
        /// <summary>
        /// 剩余时间
        /// </summary>
        float RemainingTime { get; }

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
    }

    /// <summary>
    /// CountDownManager，倒计时管理器；倒计时数量多时推荐使用。
    /// </summary>
    public class CdManager : SingletonMonoCrDs<CdManager>, ICdManager
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

                unit.Tick(deltaTime);

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

        ICdHandle ICdManager.StartCd(float time, Action onComplete)
        {
            var unit = _cdPool.Get();

            unit.SetCallbacks(null, onComplete);
            unit.SetTime(time);

            _cdRunningList.Add(unit);

            return unit;
        }

        ICdHandle ICdManager.StartCd(float time, Action<float> onTick, Action onComplete)
        {
            var unit = _cdPool.Get();

            unit.SetCallbacks(onTick, onComplete);
            unit.SetTime(time);

            _cdRunningList.Add(unit);

            return unit;
        }

        #region CdUnit

        private class CdUnit : ICdHandle
        {
            private Action<float> _onTick;
            private Action _onComplete;

            public float RemainingTime { get; private set; }
            public bool IsCompleted => RemainingTime <= 0f && !IsCancelled;
            public bool IsPaused { get; private set; }
            public bool IsCancelled { get; private set; }

            public void SetTime(float time) => RemainingTime = time;

            public void SetCallbacks(Action<float> onTick, Action onComplete)
            {
                _onTick = onTick;
                _onComplete = onComplete;
            }

            public void Tick(float deltaTime)
            {
                RemainingTime -= deltaTime;

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
                IsCancelled = true;
            }

            public void Pause()
            {
                IsPaused = true;
            }

            public void Resume()
            {
                IsPaused = false;
            }

            public void Reset()
            {
                RemainingTime = 0f;
                IsPaused = false;
                IsCancelled = false;

                // 直接设为 null，避免 GC（不再创建空委托）
                _onTick = null;
                _onComplete = null;
            }
        }

        #endregion
    }
}