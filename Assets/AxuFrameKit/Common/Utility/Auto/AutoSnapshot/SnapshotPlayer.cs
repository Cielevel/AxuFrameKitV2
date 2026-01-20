using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Auto.AutoSnapshot
{
    /// <summary>
    /// 快照回放器 - 用于按时间顺序输出快照数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class SnapshotPlayer<T>
    {
        private readonly SnapshotData<T> _data;
        private readonly TimeScaleMode _scaleMode;
        private readonly TimeRecordMode _recordMode;
        private readonly List<TimestampedSnapshot<T>> _remainingSnapshots;

        private float _playbackStartTime;
        private int _currentIndex;
        private bool _isPlaying;
        private Coroutine _playbackCoroutine;

        // 事件回调
        private Action<T, float> _onSnapshotOutput;
        private Action _onPlaybackComplete;

        /// <summary>
        /// 是否正在回放
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// 剩余待输出的快照数量
        /// </summary>
        public int RemainingCount => _remainingSnapshots.Count - _currentIndex;

        /// <summary>
        /// 总快照数量
        /// </summary>
        public int TotalCount => _remainingSnapshots.Count;

        /// <summary>
        /// 当前进度 (0-1)
        /// </summary>
        public float Progress => _remainingSnapshots.Count > 0
            ? (float)_currentIndex / _remainingSnapshots.Count
            : 1f;

        /// <summary>
        /// 从 SnapshotData 创建播放器
        /// </summary>
        /// <param name="data">快照数据</param>
        public SnapshotPlayer(SnapshotData<T> data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _scaleMode = data.ScaleMode;
            _recordMode = data.RecordMode;
            _remainingSnapshots = new List<TimestampedSnapshot<T>>(data.Snapshots);
            _currentIndex = 0;
            _isPlaying = false;

            // 按时间排序以确保正确顺序
            _remainingSnapshots.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        }

        /// <summary>
        /// 从 JSON 创建播放器
        /// 元数据由框架内部自动处理，反序列化器只负责反序列化用户数据类型 T
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <param name="dataSerializer">用户数据反序列化器 (只需处理 T 类型)</param>
        public static SnapshotPlayer<T> FromJson(string json, ISnapshotDataSerializer dataSerializer)
        {
            if (dataSerializer == null)
            {
                throw new ArgumentNullException(nameof(dataSerializer));
            }

            // 使用 Unity JsonUtility 反序列化整个结构 (非泛型)
            var serializedData = JsonUtility.FromJson<SnapshotSerializedData>(json);

            // 转换为 SnapshotData<T>
            var data = new SnapshotData<T>
            {
                ScaleMode = (TimeScaleMode)serializedData.Metadata.ScaleMode,
                RecordMode = (TimeRecordMode)serializedData.Metadata.RecordMode,
                CreatedAtUtc = serializedData.Metadata.CreatedAtUtc,
                InitTime = serializedData.Metadata.InitTime,
                Snapshots = new List<TimestampedSnapshot<T>>(serializedData.Snapshots.Count)
            };

            // 使用用户提供的反序列化器反序列化每个数据项
            foreach (var serializedSnapshot in serializedData.Snapshots)
            {
                var item = dataSerializer.DeserializeItem<T>(serializedSnapshot.Data);
                data.Snapshots.Add(new TimestampedSnapshot<T>(item, serializedSnapshot.Timestamp));
            }

            return new SnapshotPlayer<T>(data);
        }

        /// <summary>
        /// 设置快照输出回调
        /// </summary>
        /// <param name="callback">回调函数 (数据, 时间戳)</param>
        public SnapshotPlayer<T> OnSnapshotOutput(Action<T, float> callback)
        {
            _onSnapshotOutput = callback;
            return this;
        }

        /// <summary>
        /// 设置回放完成回调
        /// </summary>
        /// <param name="callback">回调函数</param>
        public SnapshotPlayer<T> OnPlaybackComplete(Action callback)
        {
            _onPlaybackComplete = callback;
            return this;
        }

        /// <summary>
        /// 使用接口处理器设置回调
        /// </summary>
        /// <param name="handler">回放处理器</param>
        public SnapshotPlayer<T> UseHandler(ISnapshotPlaybackHandler<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _onSnapshotOutput = handler.OnSnapshotOutput;
            _onPlaybackComplete = handler.OnPlaybackComplete;
            return this;
        }

        /// <summary>
        /// 获取当前时间 (根据时间缩放模式)
        /// </summary>
        private float GetCurrentTime()
        {
            return _scaleMode == TimeScaleMode.Scaled ? Time.time : Time.unscaledTime;
        }

        /// <summary>
        /// 获取 deltaTime (根据时间缩放模式)
        /// </summary>
        private float GetDeltaTime()
        {
            return _scaleMode == TimeScaleMode.Scaled ? Time.deltaTime : Time.unscaledDeltaTime;
        }

        /// <summary>
        /// 开始回放
        /// </summary>
        /// <param name="coroutineHost">用于运行协程的 MonoBehaviour</param>
        public void Play(MonoBehaviour coroutineHost)
        {
            if (coroutineHost == null)
            {
                throw new ArgumentNullException(nameof(coroutineHost));
            }

            if (_isPlaying)
            {
                Debug.LogWarning("[SnapshotPlayer] Already playing.");
                return;
            }

            if (_remainingSnapshots.Count == 0)
            {
                Debug.LogWarning("[SnapshotPlayer] No snapshots to play.");
                _onPlaybackComplete?.Invoke();
                return;
            }

            _isPlaying = true;
            _playbackStartTime = GetCurrentTime();
            _currentIndex = 0;

            // 如果是 RecordFromGameStart 模式，需要跳过已经过期的快照
            if (_recordMode == TimeRecordMode.RecordFromGameStart)
            {
                SkipExpiredSnapshots();
            }

            _playbackCoroutine = coroutineHost.StartCoroutine(PlaybackCoroutine());
        }

        /// <summary>
        /// 跳过已过期的快照 (仅用于 RecordFromGameStart 模式)
        /// </summary>
        private void SkipExpiredSnapshots()
        {
            float currentGameTime = GetCurrentTime();

            while (_currentIndex < _remainingSnapshots.Count)
            {
                var snapshot = _remainingSnapshots[_currentIndex];
                if (snapshot.Timestamp > currentGameTime)
                {
                    // 找到第一个未过期的快照
                    break;
                }

                Debug.Log($"[SnapshotPlayer] Skipping expired snapshot at time {snapshot.Timestamp} (current: {currentGameTime})");
                _currentIndex++;
            }

            if (_currentIndex > 0)
            {
                Debug.Log($"[SnapshotPlayer] Skipped {_currentIndex} expired snapshots.");
            }
        }

        /// <summary>
        /// 回放协程
        /// </summary>
        private IEnumerator PlaybackCoroutine()
        {
            while (_isPlaying && _currentIndex < _remainingSnapshots.Count)
            {
                var snapshot = _remainingSnapshots[_currentIndex];
                float targetTime;

                if (_recordMode == TimeRecordMode.RecordFromGameStart)
                {
                    // 绝对时间模式：等待到达快照的时间点
                    targetTime = snapshot.Timestamp;
                }
                else
                {
                    // 相对时间模式：从回放开始时计算
                    targetTime = _playbackStartTime + snapshot.Timestamp;
                }

                // 等待直到到达目标时间
                while (GetCurrentTime() < targetTime)
                {
                    if (!_isPlaying)
                    {
                        yield break;
                    }
                    yield return null;
                }

                // 输出快照
                _onSnapshotOutput?.Invoke(snapshot.Data, snapshot.Timestamp);
                _currentIndex++;
            }

            // 回放完成
            _isPlaying = false;
            _onPlaybackComplete?.Invoke();
        }

        /// <summary>
        /// 停止回放
        /// </summary>
        /// <param name="coroutineHost">用于停止协程的 MonoBehaviour</param>
        public void Stop(MonoBehaviour coroutineHost)
        {
            if (!_isPlaying)
            {
                return;
            }

            _isPlaying = false;

            if (_playbackCoroutine != null && coroutineHost != null)
            {
                coroutineHost.StopCoroutine(_playbackCoroutine);
                _playbackCoroutine = null;
            }
        }

        /// <summary>
        /// 暂停回放
        /// </summary>
        public void Pause()
        {
            _isPlaying = false;
        }

        /// <summary>
        /// 恢复回放
        /// </summary>
        /// <param name="coroutineHost">用于运行协程的 MonoBehaviour</param>
        public void Resume(MonoBehaviour coroutineHost)
        {
            if (_isPlaying)
            {
                return;
            }

            if (_currentIndex >= _remainingSnapshots.Count)
            {
                Debug.LogWarning("[SnapshotPlayer] Playback already completed.");
                return;
            }

            _isPlaying = true;

            // 重新计算回放开始时间以保持相对时间正确
            if (_recordMode == TimeRecordMode.RecordFromInit && _currentIndex > 0)
            {
                var currentSnapshot = _remainingSnapshots[_currentIndex];
                _playbackStartTime = GetCurrentTime() - currentSnapshot.Timestamp;
            }

            _playbackCoroutine = coroutineHost.StartCoroutine(PlaybackCoroutine());
        }

        /// <summary>
        /// 重置回放器到初始状态
        /// </summary>
        public void Reset()
        {
            _currentIndex = 0;
            _isPlaying = false;
            _playbackCoroutine = null;
        }

        /// <summary>
        /// 立即输出所有快照 (不考虑时间)
        /// </summary>
        public void FlushAll()
        {
            for (int i = _currentIndex; i < _remainingSnapshots.Count; i++)
            {
                var snapshot = _remainingSnapshots[i];
                _onSnapshotOutput?.Invoke(snapshot.Data, snapshot.Timestamp);
            }

            _currentIndex = _remainingSnapshots.Count;
            _isPlaying = false;
            _onPlaybackComplete?.Invoke();
        }

        /// <summary>
        /// 获取所有快照数据 (只读)
        /// </summary>
        public IReadOnlyList<TimestampedSnapshot<T>> GetAllSnapshots()
        {
            return _remainingSnapshots.AsReadOnly();
        }

        /// <summary>
        /// 获取剩余的快照数据
        /// </summary>
        public List<TimestampedSnapshot<T>> GetRemainingSnapshots()
        {
            return _remainingSnapshots.GetRange(_currentIndex, _remainingSnapshots.Count - _currentIndex);
        }
    }
}
