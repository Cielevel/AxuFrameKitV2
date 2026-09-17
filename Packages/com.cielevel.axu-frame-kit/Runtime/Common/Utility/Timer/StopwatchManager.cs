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

            // 句柄与单元分离：句柄记录创建时版本，单元归还池后再复用会递增版本，旧句柄的写操作自动失效
            return new StopwatchHandle(unit);
        }

        #region StopwatchHandle

        /// <summary>
        /// 正计时句柄：持有单元引用与创建时的版本号。
        /// 单元归还对象池后（OnGet/OnRelease 重置时）版本号递增，
        /// 之后通过旧句柄的任何写操作都会被忽略，防止串写到已复用的单元上。
        /// </summary>
        private class StopwatchHandle : IStopwatchHandle
        {
            private readonly StopwatchUnit _unit;
            private readonly int _version;   // 创建时单元版本
            private bool _staleWarned;       // 失效告警只提示一次

            public StopwatchHandle(StopwatchUnit unit)
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
                        Debug.LogWarning("[StopwatchManager] 计时器已取消并回收，句柄失效，本次写操作被忽略");
                    }
                    return false;
                }
            }

            // 读属性直接透传：保持与旧版一致的查询语义
            public float ElapsedTime => _unit.ElapsedTime;
            public bool IsPaused => _unit.IsPaused;
            public bool IsCancelled => _unit.IsCancelled;

            public IStopwatchHandle OnTick(Action<float> onTick) { if (IsValid) _unit.OnTick(onTick); return this; }
            public IStopwatchHandle OnLap(Action<float> onLap) { if (IsValid) _unit.OnLap(onLap); return this; }
            public IStopwatchHandle OnPause(Action onPause) { if (IsValid) _unit.OnPause(onPause); return this; }
            public IStopwatchHandle OnPause(Action<float> onPause) { if (IsValid) _unit.OnPause(onPause); return this; }
            public IStopwatchHandle OnResume(Action onResume) { if (IsValid) _unit.OnResume(onResume); return this; }
            public IStopwatchHandle OnResume(Action<float> onResume) { if (IsValid) _unit.OnResume(onResume); return this; }
            public IStopwatchHandle OnCancel(Action onCancel) { if (IsValid) _unit.OnCancel(onCancel); return this; }
            public IStopwatchHandle SetIgnoreTimeScale(bool ignoreTimeScale) { if (IsValid) _unit.SetIgnoreTimeScale(ignoreTimeScale); return this; }

            public void Cancel() { if (IsValid) _unit.Cancel(); }
            public void Lap() { if (IsValid) _unit.Lap(); }
            public void Pause() { if (IsValid) _unit.Pause(); }
            public void Resume() { if (IsValid) _unit.Resume(); }
            public void Restart() { if (IsValid) _unit.Restart(); }
        }

        #endregion

        #region StopwatchUnit

        /// <summary>
        /// 正计时单元：由管理器驱动与池化，不再直接作为句柄暴露（见 <see cref="StopwatchHandle"/>）。
        /// </summary>
        private class StopwatchUnit
        {
            private Action<float> _onTick;
            private Action<float> _onLap;
            private Action _onPause;
            private Action<float> _onPauseWithTime;
            private Action _onResume;
            private Action<float> _onResumeWithTime;
            private Action _onCancel;
            private bool _ignoreTimeScale;

            /// <summary>单元版本号：每次 Reset（含池的 OnGet/OnRelease）递增，用于句柄失效判定</summary>
            public int Version { get; private set; }

            public float ElapsedTime { get; private set; }

            public bool IsPaused { get; private set; }
            public bool IsCancelled { get; private set; }

            public StopwatchUnit OnTick(Action<float> onTick)
            {
                _onTick += onTick;
                return this;
            }

            public StopwatchUnit OnLap(Action<float> onLap)
            {
                _onLap += onLap;
                return this;
            }

            public StopwatchUnit OnPause(Action onPause)
            {
                _onPause += onPause;
                return this;
            }

            public StopwatchUnit OnPause(Action<float> onPause)
            {
                _onPauseWithTime += onPause;
                return this;
            }

            public StopwatchUnit OnResume(Action onResume)
            {
                _onResume += onResume;
                return this;
            }

            public StopwatchUnit OnResume(Action<float> onResume)
            {
                _onResumeWithTime += onResume;
                return this;
            }

            public StopwatchUnit OnCancel(Action onCancel)
            {
                _onCancel += onCancel;
                return this;
            }

            public StopwatchUnit SetIgnoreTimeScale(bool ignoreTimeScale)
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
                // 版本号递增：使持有旧版本号的句柄失效（OnGet/OnRelease 均会经过此处）
                Version++;

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
