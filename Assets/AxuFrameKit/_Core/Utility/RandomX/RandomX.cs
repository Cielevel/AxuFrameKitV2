using System;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Utility.RandomX
{
    public static class RandomX // 注意 System.Random 左闭右开
    {
        #region Bool
        
        private static readonly Random RandomBool = new();
        public static bool GetBool() => RandomBool.Next(2) == 1;

        #endregion

        #region int

        private static readonly Random RandomInt = new();

        public static int GetInt(int minInclude, int max, bool maxInclude = false)
        {
            if (max < minInclude)
            {
                Logger.Error("max < minInclude!");
                return -1;
            }

            if (max == minInclude)
                return max;
            
            if (maxInclude)
                return RandomInt.Next(minInclude, max + 1);

            return RandomInt.Next(minInclude, max);
        }

        #endregion

        #region float

        

        #endregion

        #region double

        

        #endregion
    }
}