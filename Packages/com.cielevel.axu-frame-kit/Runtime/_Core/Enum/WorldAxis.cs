using System;

namespace AxuFrameKit._Core.Enum
{
    /// <summary>
    /// 世界轴（笛卡尔坐标系）枚举，支持位运算组合
    /// </summary>
    [Flags]
    public enum WorldAxis
    {
        /// <summary> 无轴 </summary>
        None = 0,

        /// <summary> X轴 </summary>
        X = 1 << 0,

        /// <summary> Y轴 </summary>
        Y = 1 << 1,

        /// <summary> Z轴 </summary>
        Z = 1 << 2,

        /// <summary> 所有轴 (X | Y | Z) </summary>
        All = X | Y | Z,
    }

    /// <summary>
    /// WorldAxis 枚举的扩展方法
    /// </summary>
    public static class WorldAxisExtension
    {
        /// <summary> 检查是否包含 X 轴 </summary>
        public static bool HasX(this WorldAxis axis) => axis.HasFlag(WorldAxis.X);

        /// <summary> 检查是否包含 Y 轴 </summary>
        public static bool HasY(this WorldAxis axis) => axis.HasFlag(WorldAxis.Y);

        /// <summary> 检查是否包含 Z 轴 </summary>
        public static bool HasZ(this WorldAxis axis) => axis.HasFlag(WorldAxis.Z);

        /// <summary> 检查是否包含指定的目标轴 </summary>
        public static bool Contains(this WorldAxis axis, WorldAxis axisTarget) => axis.HasFlag(axisTarget);
    }
}