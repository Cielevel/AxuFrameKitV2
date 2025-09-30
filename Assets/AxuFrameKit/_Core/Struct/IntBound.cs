using System;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Struct
{
    [Serializable]
    public struct IntBound // struct类型在Unity Inspector上的可能存在问题。
    {
        public int min;
        public int max;

        public IntBound(int min, int max, bool safeInverse)
        {
            this.min = min;
            this.max = max;

            if (max < min)
            {
                Logger.Error("IntBound中最小值大于了最大值:" + ToString() + ", 安全反转: " + safeInverse);
                if (!safeInverse) 
                    return;
                
                this.min = max;
                this.max = min;
            }
        }

        public override string ToString()
        {
            return $"IntBound(min: {min}, max: {max})";
        }
    }

    public static class IntBoundExtension
    {
        private static readonly Random Rd = new();

        public static int Random(this IntBound bound, bool excludeMax = false) =>
            Rd.Next(bound.min, bound.max + (excludeMax ? 0 : 1));
    }
}