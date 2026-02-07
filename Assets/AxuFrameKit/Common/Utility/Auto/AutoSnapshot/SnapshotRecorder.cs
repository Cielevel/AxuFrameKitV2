using System;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit.Common.Utility.Serializer;

namespace AxuFrameKit.Common.Utility.Auto.AutoSnapshot
{
    // ==========================================
    // 1. 枚举定义 (Enumerations)
    // ==========================================

    /// <summary>
    /// 时间缩放模式
    /// </summary>
    public enum TimeScaleMode
    {
        /// <summary>
        /// 受时间缩放影响 (使用 Time.time / Time.deltaTime)
        /// </summary>
        Scaled,

        /// <summary>
        /// 不受时间缩放影响 (使用 Time.unscaledTime / Time.unscaledDeltaTime)
        /// </summary>
        Unscaled
    }

    /// <summary>
    /// 时间记录起始模式
    /// </summary>
    public enum TimeRecordMode
    {
        /// <summary>
        /// 从游戏启动开始记录 (绝对时间，使用 Time.time 或 Time.unscaledTime)
        /// </summary>
        RecordFromGameStart,

        /// <summary>
        /// 从工具初始化时开始记录 (相对时间，从初始化时刻开始计时)
        /// </summary>
        RecordFromInit
    }

    // ==========================================
    // 2. 核心接口定义 (Interfaces)
    // ==========================================

    // 注意：原 ISnapshotDataSerializer 已被移除，现在统一使用 AxuFrameKit.Common.Utility.Serializer.ISerializer

    /// <summary>
    /// 快照回放输出回调接口
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public interface ISnapshotPlaybackHandler<T>
    {
        /// <summary>
        /// 当一个快照数据被输出时调用
        /// </summary>
        /// <param name="data">快照数据</param>
        /// <param name="timestamp">记录时的时间戳</param>
        void OnSnapshotOutput(T data, float timestamp);

        /// <summary>
        /// 当回放完成时调用
        /// </summary>
        void OnPlaybackComplete();
    }

    // ==========================================
    // 3. 数据结构定义 (Data Structures)
    // ==========================================

    /// <summary>
    /// 时间戳快照 - 包装泛型数据与时间信息 (内存中使用)
    /// </summary>
    [Serializable]
    public class TimestampedSnapshot<T>
    {
        /// <summary>
        /// 记录的时间戳
        /// </summary>
        public float Timestamp;

        /// <summary>
        /// 快照数据
        /// </summary>
        public T Data;

        public TimestampedSnapshot() { }

        public TimestampedSnapshot(T data, float timestamp)
        {
            Data = data;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// 快照元数据 - 非泛型，可直接序列化
    /// </summary>
    [Serializable]
    public class SnapshotMetadata
    {
        /// <summary>
        /// 时间缩放模式
        /// </summary>
        public int ScaleMode;

        /// <summary>
        /// 时间记录模式
        /// </summary>
        public int RecordMode;

        /// <summary>
        /// 记录创建时间 (UTC)
        /// </summary>
        public string CreatedAtUtc;

        /// <summary>
        /// 记录器初始化时的游戏时间
        /// </summary>
        public float InitTime;

        /// <summary>
        /// 快照数量
        /// </summary>
        public int SnapshotCount;
    }

    /// <summary>
    /// 已序列化的快照 - 时间戳 + 序列化后的数据字符串
    /// </summary>
    [Serializable]
    public class SerializedSnapshot
    {
        /// <summary>
        /// 记录的时间戳
        /// </summary>
        public float Timestamp;

        /// <summary>
        /// 序列化后的数据字符串
        /// </summary>
        public string Data;

        public SerializedSnapshot() { }

        public SerializedSnapshot(float timestamp, string data)
        {
            Timestamp = timestamp;
            Data = data;
        }
    }

    /// <summary>
    /// 快照序列化数据 - 最终序列化格式，非泛型
    /// </summary>
    [Serializable]
    public class SnapshotSerializedData
    {
        /// <summary>
        /// 元数据
        /// </summary>
        public SnapshotMetadata Metadata;

        /// <summary>
        /// 所有已序列化的快照
        /// </summary>
        public List<SerializedSnapshot> Snapshots;

        public SnapshotSerializedData()
        {
            Metadata = new SnapshotMetadata();
            Snapshots = new List<SerializedSnapshot>();
        }
    }

    /// <summary>
    /// 快照记录数据容器 - 用于内存中操作
    /// </summary>
    [Serializable]
    public class SnapshotData<T>
    {
        /// <summary>
        /// 时间缩放模式
        /// </summary>
        public TimeScaleMode ScaleMode;

        /// <summary>
        /// 时间记录模式
        /// </summary>
        public TimeRecordMode RecordMode;

        /// <summary>
        /// 所有快照数据
        /// </summary>
        public List<TimestampedSnapshot<T>> Snapshots;

        /// <summary>
        /// 记录创建时间 (UTC)
        /// </summary>
        public string CreatedAtUtc;

        /// <summary>
        /// 记录器初始化时的游戏时间
        /// </summary>
        public float InitTime;

        public SnapshotData()
        {
            Snapshots = new List<TimestampedSnapshot<T>>();
            CreatedAtUtc = DateTime.UtcNow.ToString("O");
        }
    }

    // ==========================================
    // 4. 快照记录器 (Snapshot Recorder)
    // ==========================================

    /// <summary>
    /// 快照记录器 - 用于记录带时间戳的快照数据
    /// </summary>
    /// <typeparam name="T">要记录的数据类型</typeparam>
    public class SnapshotRecorder<T>
    {
        private readonly TimeScaleMode _scaleMode;
        private readonly TimeRecordMode _recordMode;
        private readonly List<TimestampedSnapshot<T>> _snapshots;
        private readonly float _initTime;
        private bool _isRecording;

        /// <summary>
        /// 当前时间缩放模式
        /// </summary>
        public TimeScaleMode ScaleMode => _scaleMode;

        /// <summary>
        /// 当前时间记录模式
        /// </summary>
        public TimeRecordMode RecordMode => _recordMode;

        /// <summary>
        /// 已记录的快照数量
        /// </summary>
        public int SnapshotCount => _snapshots.Count;

        /// <summary>
        /// 是否正在记录
        /// </summary>
        public bool IsRecording => _isRecording;

        /// <summary>
        /// 初始化时间
        /// </summary>
        public float InitTime => _initTime;

        /// <summary>
        /// 创建快照记录器
        /// </summary>
        /// <param name="scaleMode">时间缩放模式</param>
        /// <param name="recordMode">时间记录模式</param>
        /// <param name="initialCapacity">初始容量</param>
        public SnapshotRecorder(
            TimeScaleMode scaleMode = TimeScaleMode.Scaled,
            TimeRecordMode recordMode = TimeRecordMode.RecordFromInit,
            int initialCapacity = 16)
        {
            _scaleMode = scaleMode;
            _recordMode = recordMode;
            _snapshots = new List<TimestampedSnapshot<T>>(initialCapacity);
            _initTime = GetCurrentTime();
            _isRecording = true;
        }

        /// <summary>
        /// 获取当前时间 (根据时间缩放模式)
        /// </summary>
        private float GetCurrentTime()
        {
            return _scaleMode == TimeScaleMode.Scaled ? Time.time : Time.unscaledTime;
        }

        /// <summary>
        /// 获取用于记录的时间戳
        /// </summary>
        private float GetRecordTimestamp()
        {
            float currentTime = GetCurrentTime();

            return _recordMode == TimeRecordMode.RecordFromGameStart
                ? currentTime
                : currentTime - _initTime;
        }

        /// <summary>
        /// 记录一个快照
        /// </summary>
        /// <param name="data">要记录的数据</param>
        public void Record(T data)
        {
            if (!_isRecording)
            {
                Debug.LogWarning("[SnapshotRecorder] Recording is stopped. Call StartRecording() to resume.");
                return;
            }

            var snapshot = new TimestampedSnapshot<T>(data, GetRecordTimestamp());
            _snapshots.Add(snapshot);
        }

        /// <summary>
        /// 记录一个快照并指定自定义时间戳
        /// </summary>
        /// <param name="data">要记录的数据</param>
        /// <param name="customTimestamp">自定义时间戳</param>
        public void RecordWithTimestamp(T data, float customTimestamp)
        {
            if (!_isRecording)
            {
                Debug.LogWarning("[SnapshotRecorder] Recording is stopped. Call StartRecording() to resume.");
                return;
            }

            var snapshot = new TimestampedSnapshot<T>(data, customTimestamp);
            _snapshots.Add(snapshot);
        }

        /// <summary>
        /// 停止记录
        /// </summary>
        public void StopRecording()
        {
            _isRecording = false;
        }

        /// <summary>
        /// 恢复记录
        /// </summary>
        public void StartRecording()
        {
            _isRecording = true;
        }

        /// <summary>
        /// 清空所有快照
        /// </summary>
        public void Clear()
        {
            _snapshots.Clear();
        }

        /// <summary>
        /// 获取所有快照 (只读)
        /// </summary>
        public IReadOnlyList<TimestampedSnapshot<T>> GetSnapshots()
        {
            return _snapshots.AsReadOnly();
        }

        /// <summary>
        /// 获取指定时间范围内的快照
        /// </summary>
        /// <param name="startTime">开始时间 (包含)</param>
        /// <param name="endTime">结束时间 (包含)</param>
        public List<TimestampedSnapshot<T>> GetSnapshotsInRange(float startTime, float endTime)
        {
            var result = new List<TimestampedSnapshot<T>>();
            foreach (var snapshot in _snapshots)
            {
                if (snapshot.Timestamp >= startTime && snapshot.Timestamp <= endTime)
                {
                    result.Add(snapshot);
                }
            }
            return result;
        }

        /// <summary>
        /// 导出为可序列化的数据结构
        /// </summary>
        public SnapshotData<T> ExportData()
        {
            return new SnapshotData<T>
            {
                ScaleMode = _scaleMode,
                RecordMode = _recordMode,
                Snapshots = new List<TimestampedSnapshot<T>>(_snapshots),
                CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                InitTime = _initTime
            };
        }

        /// <summary>
        /// 序列化为 JSON 字符串
        /// 元数据由框架内部自动处理，序列化器只负责序列化用户数据类型 T
        /// </summary>
        /// <param name="dataSerializer">用户数据序列化器 (只需处理 T 类型)</param>
        public string Serialize(ISerializer dataSerializer)
        {
            if (dataSerializer == null)
            {
                throw new ArgumentNullException(nameof(dataSerializer));
            }

            // 创建序列化数据结构
            var serializedData = new SnapshotSerializedData
            {
                Metadata = new SnapshotMetadata
                {
                    ScaleMode = (int)_scaleMode,
                    RecordMode = (int)_recordMode,
                    CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                    InitTime = _initTime,
                    SnapshotCount = _snapshots.Count
                },
                Snapshots = new List<SerializedSnapshot>(_snapshots.Count)
            };

            // 使用用户提供的序列化器序列化每个数据项
            foreach (var snapshot in _snapshots)
            {
                var serializedItem = dataSerializer.Serialize(snapshot.Data);
                serializedData.Snapshots.Add(new SerializedSnapshot(snapshot.Timestamp, serializedItem));
            }

            // 使用 Unity JsonUtility 序列化整个结构 (非泛型，可直接序列化)
            return JsonUtility.ToJson(serializedData, true);
        }
    }
}
