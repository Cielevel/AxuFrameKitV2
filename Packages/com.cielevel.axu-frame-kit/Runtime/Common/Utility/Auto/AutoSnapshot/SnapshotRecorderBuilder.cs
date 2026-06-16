using System;
using AxuFrameKit.Common.Utility.Serializer;

namespace AxuFrameKit.Common.Utility.Auto.AutoSnapshot
{
    /// <summary>
    /// 快照记录器构建器 - 使用 Builder 模式创建记录器
    /// </summary>
    /// <typeparam name="T">要记录的数据类型</typeparam>
    public class SnapshotRecorderBuilder<T>
    {
        private TimeScaleMode _scaleMode = TimeScaleMode.Scaled;
        private TimeRecordMode _recordMode = TimeRecordMode.RecordFromInit;
        private int _initialCapacity = 16;

        /// <summary>
        /// 设置时间缩放模式
        /// </summary>
        public SnapshotRecorderBuilder<T> WithScaleMode(TimeScaleMode mode)
        {
            _scaleMode = mode;
            return this;
        }

        /// <summary>
        /// 使用受缩放影响的时间 (Time.time)
        /// </summary>
        public SnapshotRecorderBuilder<T> UseScaledTime()
        {
            _scaleMode = TimeScaleMode.Scaled;
            return this;
        }

        /// <summary>
        /// 使用不受缩放影响的时间 (Time.unscaledTime)
        /// </summary>
        public SnapshotRecorderBuilder<T> UseUnscaledTime()
        {
            _scaleMode = TimeScaleMode.Unscaled;
            return this;
        }

        /// <summary>
        /// 设置时间记录模式
        /// </summary>
        public SnapshotRecorderBuilder<T> WithRecordMode(TimeRecordMode mode)
        {
            _recordMode = mode;
            return this;
        }

        /// <summary>
        /// 从游戏启动开始记录时间 (绝对时间)
        /// </summary>
        public SnapshotRecorderBuilder<T> RecordFromGameStart()
        {
            _recordMode = TimeRecordMode.RecordFromGameStart;
            return this;
        }

        /// <summary>
        /// 从记录器初始化时开始记录时间 (相对时间)
        /// </summary>
        public SnapshotRecorderBuilder<T> RecordFromInit()
        {
            _recordMode = TimeRecordMode.RecordFromInit;
            return this;
        }

        /// <summary>
        /// 设置初始容量
        /// </summary>
        public SnapshotRecorderBuilder<T> WithCapacity(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0.");
            }
            _initialCapacity = capacity;
            return this;
        }

        /// <summary>
        /// 构建记录器实例
        /// </summary>
        public SnapshotRecorder<T> Build()
        {
            return new SnapshotRecorder<T>(_scaleMode, _recordMode, _initialCapacity);
        }
    }

    /// <summary>
    /// 快照工厂 - 提供快捷创建方法
    /// </summary>
    public static class SnapshotFactory
    {
        /// <summary>
        /// 创建记录器构建器
        /// </summary>
        public static SnapshotRecorderBuilder<T> CreateRecorder<T>()
        {
            return new SnapshotRecorderBuilder<T>();
        }

        /// <summary>
        /// 快速创建使用默认设置的记录器
        /// </summary>
        public static SnapshotRecorder<T> CreateDefaultRecorder<T>()
        {
            return new SnapshotRecorder<T>();
        }

        /// <summary>
        /// 快速创建使用缩放时间、游戏启动时间的记录器
        /// </summary>
        public static SnapshotRecorder<T> CreateAbsoluteTimeRecorder<T>()
        {
            return new SnapshotRecorder<T>(TimeScaleMode.Scaled, TimeRecordMode.RecordFromGameStart);
        }

        /// <summary>
        /// 快速创建使用不缩放时间、相对时间的记录器
        /// </summary>
        public static SnapshotRecorder<T> CreateRelativeTimeRecorder<T>()
        {
            return new SnapshotRecorder<T>(TimeScaleMode.Unscaled, TimeRecordMode.RecordFromInit);
        }

        /// <summary>
        /// 从 JSON 创建播放器
        /// </summary>
        public static SnapshotPlayer<T> CreatePlayerFromJson<T>(string json, ISerializer dataSerializer)
        {
            return SnapshotPlayer<T>.FromJson(json, dataSerializer);
        }

        /// <summary>
        /// 从 SnapshotData 创建播放器
        /// </summary>
        public static SnapshotPlayer<T> CreatePlayer<T>(SnapshotData<T> data)
        {
            return new SnapshotPlayer<T>(data);
        }
    }
}
