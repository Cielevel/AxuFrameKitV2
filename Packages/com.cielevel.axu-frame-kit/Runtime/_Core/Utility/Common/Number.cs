
namespace AxuFrameKit._Core.Utility.Common
{
    /// <summary>
    /// 数值常量与随机工具类
    /// </summary>
    public static class Number
    {
        private static readonly System.Random Rd = new System.Random();

        public const int Int0 = 0;
        public const int Int1 = 1;
        public const int Int2 = 2;
        public const int Int3 = 3;
        public const int Int4 = 4;
        public const int Int5 = 5;
        public const int Int6 = 6;
        public const int Int7 = 7;
        public const int Int8 = 8;
        public const int Int9 = 9;
        public const int Int10 = 10;

        public const float Half = 0.5f;

        public const float FloatDot0 = 0.0f;
        public const float FloatDot1 = 0.1f;
        public const float FloatDot2 = 0.2f;
        public const float FloatDot3 = 0.3f;
        public const float FloatDot4 = 0.4f;
        public const float FloatDot5 = 0.5f;
        public const float FloatDot6 = 0.6f;
        public const float FloatDot7 = 0.7f;
        public const float FloatDot8 = 0.8f;
        public const float FloatDot9 = 0.9f;

        /// <summary> 一分钟的秒数 </summary>
        public const int MinuteSec = 60;
        /// <summary> 一小时的秒数 </summary>
        public const int HourSec = 60 * 60;
        /// <summary> 一天的秒数 </summary>
        public const int DaySec = 60 * 60 * 24;
        /// <summary> 一周的秒数 </summary>
        public const int WeekSec = 60 * 60 * 24 * 7;
        /// <summary> 一个月（30天）的秒数 </summary>
        public const int MonthSec = 60 * 60 * 24 * 30;
        /// <summary> 一年（365天）的秒数 </summary>
        public const int YearSec = 60 * 60 * 24 * 365;

        /// <summary> 一周的天数 </summary>
        public const int WeekDay = 7;
        /// <summary> 一个月的天数 </summary>
        public const int MonthDay = 30;
        /// <summary> 一年的天数 </summary>
        public const int YearDay = 365;

        /// <summary> 生成指定范围内的随机浮点数 </summary>
        public static float Random(float min, float max) => min + (float)Rd.NextDouble() * (max - min);

        /// <summary> 生成指定范围内的随机整数 (包含 min, 不含 max) </summary>
        public static int Random(int min, int max) => Rd.Next(min, max);

        /// <summary> 生成 [0, max) 范围内的随机浮点数 </summary>
        public static float Random0(float max) => Random(0f, max);

        /// <summary> 生成 [0, max) 范围内的随机整数 </summary>
        public static int Random0(int max) => Rd.Next(max);
    }
}