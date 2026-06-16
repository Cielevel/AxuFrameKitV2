using System;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Utility.RandomX
{
    public class RandomX // 注意 System.Random 左闭右开
    {
        private readonly Random Ran;

        public RandomX()
        {
            Ran = new Random();
        }

        public RandomX(int seed)
        {
            Ran = new Random(seed);
        }

        // ===Bool===

        public bool GetBool() => Ran.Next(2) == 1;

        // ===Int===

        /// <summary>
        /// 获取随机整数
        /// </summary>
        /// <param name="minInclude">包含最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="maxInclude">是否包含最大值</param>
        public int GetInt(int minInclude, int max, bool maxInclude = false)
        {
            if (max < minInclude)
            {
                Logger.Error("max < minInclude!");
                return -1;
            }

            if (max == minInclude)
                return max;

            if (maxInclude)
                return Ran.Next(minInclude, max + 1);

            return Ran.Next(minInclude, max);
        }

        // ===Seed===

        /// <summary>
        /// 获取随机字符串
        /// </summary>
        /// <param name="seedLen">字符串长度</param>
        /// <returns></returns>
        public string Seed(int seedLen)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] result = new char[seedLen];
            for (int i = 0; i < seedLen; i++)
                result[i] = chars[Ran.Next(chars.Length)];
            return new string(result);
        }

        /// <summary>
        /// 获取长度为len的随机整数
        /// </summary>
        /// <param name="len">整数长度</param>
        public int SeedInt(int len)
        {
            if (len <= 0 || len > 9)
            {
                Logger.Error($"SeedInt length {len} is out of range (1-9)!");
                return -1;
            }
            int min = (len == 1) ? 0 : (int)System.Math.Pow(10, len - 1);
            int max = (int)System.Math.Pow(10, len);
            return Ran.Next(min, max);
        }
    }
}