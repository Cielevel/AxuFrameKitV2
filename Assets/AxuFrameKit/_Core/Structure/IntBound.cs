using System;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Structure
{
    [Serializable]
    public class IntBound
    {
        public int Min => max < min ? max : min;
        public int Max => max < min ? min : max;
        
        public int min;
        public int max;

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

    public static class IntBoundExtension
    {
        private static readonly Random Rd = new();

        public static int Random(this IntBound bound, bool excludeMin = false, bool excludeMax = false) =>
            Rd.Next(bound.Min + (excludeMin ? 1 : 0), bound.Max + (excludeMax ? 0 : 1));
    }
}