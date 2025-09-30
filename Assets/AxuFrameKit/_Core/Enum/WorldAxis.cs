using System;

namespace AxuFrameKit._Core.Enum
{
    /// <summary>
    /// 世界轴（笛卡尔）
    /// </summary>
    [Flags]
    public enum WorldAxis
    {
        None = 0,

        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2,
        
        All = X | Y | Z,
    }
    
    public static class WorldAxisExtension
    {
        public static bool HasX(this WorldAxis axis) => axis.HasFlag(WorldAxis.X);
        public static bool HasY(this WorldAxis axis) => axis.HasFlag(WorldAxis.Y);
        public static bool HasZ(this WorldAxis axis) => axis.HasFlag(WorldAxis.Z);

        public static bool Contains(this WorldAxis axis, WorldAxis axisTarget) => axis.HasFlag(axisTarget);
    }
}