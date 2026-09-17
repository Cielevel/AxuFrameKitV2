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

            // 句柄与单元分离：句柄记录创建时版本，单元归还池后再复用会递增版本，旧句柄的写操作自动失效
            return new CdHandle(unit);
        }

        #region CdHandle

        /// <summary>
        /// 倒计时句柄：持有单元引用与创建时的版本号。
        /// 单元归还对象池后（OnGet/OnRelease 重置时）版本号递增，
        /// 之后通过旧句柄的任何写操作都会被忽略，防止串写到已复用的单元上。
        /// </summary>
        private class CdHandle : ICountdownHandle
        {
            private readonly CdUnit _unit;
            private readonly int _version;   // 创建时单元版本
            private bool _staleWarned;       // 失效告警只提示一次

            public CdHandle(CdUnit unit)
            {
                _unit = unit;
                _version = unit.Version;
            }

            /// <summary>句柄是否仍指向当前代次的单元</summary>
            private bool IsValid
            {
                get
                {
                    if (_unit.Version == _version)
                        return true;

                    if (!_staleWarned)
                    {
                        _staleWarned = true;
                        Debug.LogWarning("[CountdownManager] 计时器已结束/取消并回收，句柄失效，本次写操作被忽略");
                    }
                    return false;
                }
            }

            // 读属性直接透传：完成后的 IsCompleted 等查询语义与旧版保持一致
            public float RemainingTime => _unit.RemainingTime;
            public float RemainingRatio => _unit.RemainingRatio;
            public float PassedRatio => _unit.PassedRatio;
            public bool IsCompleted => _unit.IsCompleted;
            public bool IsPaused => _unit.IsPaused;
            public bool IsCancelled => _unit.IsCancelled;

            public ICountdownHandle OnTick(Action<float> onTick) { if (IsValid) _unit.OnTick(onTick); return this; }
            public ICountdownHandle OnComplete(Action onComplete) { if (IsValid) _unit.OnComplete(onComplete); return this; }
            public ICountdownHandle OnPause(Action onPause) { if (IsValid) _unit.OnPause(onPause); return this; }
            public ICountdownHandle OnPause(Action<float> onPause) { if (IsValid) _unit.OnPause(onPause); return this; }
            public ICountdownHandle OnResume(Action onResume) { if (IsValid) _unit.OnResume(onResume); return this; }
            public ICountdownHandle OnResume(Action<float> onResume) { if (IsValid) _unit.OnResume(onResume); return this; }
            public ICountdownHandle OnCancel(Action onCancel) { if (IsValid) _unit.OnCancel(onCancel); return this; }
            public ICountdownHandle SetIgnoreTimeScale(bool ignoreTimeScale) { if (IsValid) _unit.SetIgnoreTimeScale(ignoreTimeScale); return this; }

            public void Cancel() { if (IsValid) _unit.Cancel(); }
            public void Pause() { if (IsValid) _unit.Pause(); }
            public void Resume() { if (IsValid) _unit.Resume(); }
            public void Restart() { if (IsValid) _unit.Restart(); }
        }

        #endregion

        #region CdUnit

        /// <summary>
        /// 倒计时单元：由管理器驱动与池化，不再直接作为句柄暴露（见 <see cref="CdHandle"/>）。
        /// </summary>
        private class CdUnit
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

            /// <summary>单元版本号：每次 Reset（含池的 OnGet/OnRelease）递增，用于句柄失效判定</summary>
            public int Version { get; private set; }

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

            public CdUnit OnTick(Action<float> onTick)
            {
                _onTick += onTick;
                return this;
            }

            public CdUnit OnComplete(Action onComplete)
            {
                _onComplete += onComplete;
                return this;
            }

            public CdUnit OnPause(Action onPause)
            {
                _onPause += onPause;
                return this;
            }

            public CdUnit OnPause(Action<float> onPause)
            {
                _onPauseWithTime += onPause;
                return this;
            }

            public CdUnit OnResume(Action onResume)
            {
                _onResume += onResume;
                return this;
            }

            public CdUnit OnResume(Action<float> onResume)
            {
                _onResumeWithTime += onResume;
                return this;
            }

            public CdUnit OnCancel(Action onCancel)
            {
                _onCancel += onCancel;
                return this;
            }

            public CdUnit SetIgnoreTimeScale(bool ignoreTimeScale)
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
                // 版本号递增：使持有旧版本号的句柄失效（OnGet/OnRelease 均会经过此处）
                Version++;

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