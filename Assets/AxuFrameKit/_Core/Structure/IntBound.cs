using System;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Structure
{
    /// <summary>
    /// 表示一个整数范围（含最小值和最大值）
    /// </summary>
    [Serializable]
    public class IntBound
    {
        /// <summary> 获取实际的最小值（考虑反转） </summary>
        public int Min => max < min ? max : min;

        /// <summary> 获取实际的最大值（考虑反转） </summary>
        public int Max => max < min ? min : max;

        /// <summary> 获取或设置下界 </summary>
        public int min;

        /// <summary> 获取或设置上界 </summary>
        public int max;

        /// <summary>
        /// 初始化整数范围
        /// </summary>
        /// <param name="min">初始最小值</param>
        /// <param name="max">初始最大值</param>
        public IntBound(int min, int max)
        {
            this.min = min;
            this.max = max;

            if (max < min) // 自动安全反转
            {
                this.min = max;
                this.max = min;
            }
        }

        public sealed override string ToString()
        {
            return $"IntBound(min: {min}, max: {max})";
        }
    }

    /// <summary>
    /// IntBound 范围扩展方法
    /// </summary>
    public static class IntBoundExtension
    {
        private static readonly Random Rd = new();

        /// <summary>
        /// 在指定范围内生成一个随机整数
        /// </summary>
        /// <param name="bound">范围对象</param>
        /// <param name="excludeMin">是否排除下界（若为 true 则从 min + 1 开始）</param>
        /// <param name="excludeMax">是否排除上界（若为 true 则包含 index 小于 max）</param>
        /// <returns>生成的随机整数</returns>
        public static int Random(this IntBound bound, bool excludeMin = false, bool excludeMax = false) =>
            Rd.Next(bound.Min + (excludeMin ? 1 : 0), bound.Max + (excludeMax ? 0 : 1));
    }
}