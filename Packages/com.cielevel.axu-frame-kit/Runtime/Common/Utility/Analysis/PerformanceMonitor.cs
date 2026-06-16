using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;
using UnityEngine.Profiling;

namespace AxuFrameKit.Common.Utility.Analysis
{
    /// <summary>
    /// 性能数据容器
    /// </summary>
    public struct PerfData
    {
        // FPS 相关
        public float fps;         // 实时帧率
        public float frameTimeMs; // 帧耗时(毫秒)

        // 内存 相关 (单位: MB)
        public float totalReservedMem;  // Unity向系统申请的总内存 (物理内存)
        public float totalAllocatedMem; // Unity内部实际使用的内存
        public float monoUsedMem;       // C# 脚本堆内存使用量
        public float monoHeapMem;       // C# 脚本堆内存预留量

        // GC 相关
        public int gcCount;          // GC 触发总次数
        public long gcAllocPerFrame; // 每帧产生的 GC 垃圾 (Bytes)
    }

    /// <summary>
    /// 一个轻量级、安全且标准的 Unity 性能监控工具。
    /// 可以直接用于 IL2CPP 打包，并且支持多平台。
    /// </summary>
    public class PerformanceMonitor : SingletonMonoCrDs<PerformanceMonitor>
    {
        #region 配置
        [Header("设置")]
        [Tooltip("数据更新频率 (秒)")]
        public float updateInterval = 0.5f;
        #endregion

        #region 公开数据
        /// <summary>
        /// UI层可以直接读取此属性获取最新性能数据
        /// </summary>
        public PerfData CurrentData { get; private set; }
        #endregion

        #region 私有变量
        private float _accumDeltaTime = 0f;
        private int _frames = 0;
        private float _timeLeft;

        // 用于计算每帧 GC Alloc
        private long _lastFrameMonoUsed;
        #endregion

        private void Start()
        {
            _timeLeft = updateInterval;
            _lastFrameMonoUsed = Profiler.GetMonoUsedSizeLong();
        }

        private void Update()
        {
            // 1. 计算 FPS 核心逻辑 (累积法，避免每帧跳变)
            _timeLeft -= Time.unscaledDeltaTime;
            _accumDeltaTime += Time.unscaledDeltaTime; // 如果使用了TimeScale，建议用 unscaled
            _frames++;

            // 2. 计算每帧的 GC Allocation (核心逻辑：当前帧内存 - 上一帧内存)
            // 注意：如果刚好发生 GC，这个值可能是负数，需要过滤
            long currentMonoUsed = Profiler.GetMonoUsedSizeLong();
            long allocDelta = currentMonoUsed - _lastFrameMonoUsed;
            _lastFrameMonoUsed = currentMonoUsed;

            // 只有当内存增加时，才视为这一帧产生了垃圾分配
            // 如果发生GC，diff 会是负数，我们归零处理
            long frameGCAlloc = allocDelta > 0 ? allocDelta : 0;

            // 3. 到了更新间隔，刷新数据到 CurrentData
            if (_timeLeft <= 0.0f)
            {
                float fps = _frames / (updateInterval - _timeLeft); // 修正时间偏差
                float ms = 1000.0f / fps;

                UpdatePerfData(fps, ms, frameGCAlloc);

                // 重置计数器
                _timeLeft = updateInterval;
                _frames = 0;
            }
        }

        /// <summary>
        /// 采集并打包数据
        /// </summary>
        private void UpdatePerfData(float fps, float ms, long frameGCAlloc)
        {
            PerfData data = new PerfData
            {
                // --- FPS ---
                fps = fps,
                frameTimeMs = ms
            };

            // --- 内存 (转换为 MB) ---
            const float byteToMb = 1.0f / (1024 * 1024);

            // Unity 向系统申请的总物理内存
            data.totalReservedMem = Profiler.GetTotalReservedMemoryLong() * byteToMb;

            // Unity 引擎（包括资源、Native对象）实际使用的内存
            data.totalAllocatedMem = Profiler.GetTotalAllocatedMemoryLong() * byteToMb;

            // Mono/IL2CPP 托管堆实际使用的内存 (C# 对象)
            data.monoUsedMem = Profiler.GetMonoUsedSizeLong() * byteToMb;

            // Mono/IL2CPP 托管堆保留的内存 (由于内存碎片或GC策略，通常大于 Used)
            data.monoHeapMem = Profiler.GetMonoHeapSizeLong() * byteToMb;

            // --- GC ---
            // 获取第0代 GC 的回收次数 (通常足以监控卡顿)
            data.gcCount = System.GC.CollectionCount(0);
            data.gcAllocPerFrame = frameGCAlloc; // 保持 Bytes 单位，因为每帧分配通常较小

            // 赋值给公开属性
            CurrentData = data;
        }
    }
}