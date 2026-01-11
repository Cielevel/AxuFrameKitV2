using System;

namespace AxuFrameKit._Core.Extension
{
    public static class IntExtension
    {
        /// <summary>
        /// 将数字拆分为两份（如果为奇数趋近相同（差一），如果为偶数则相同）
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static (int, int) SplitHalf(this int number)
        {
            int half = number >> 1;
            return (half, number - half);
        }

        public static string ToTime(this int self, string format) => TimeSpan.FromSeconds(self).ToString(format);
    }
}