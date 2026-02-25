using System;

namespace AxuFrameKit._Core.Extension
{
    public static class DateTimeExtension
    {
        private static readonly DateTime UnixEpochUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 判断时间是否在 [start, end] 区间内（含边界）
        /// </summary>
        public static bool IsBetween(this DateTime self, DateTime start, DateTime end)
        {
            if (start > end)
                (start, end) = (end, start);

            return self >= start && self <= end;
        }

        /// <summary>
        /// 获取当天 00:00:00
        /// </summary>
        public static DateTime StartOfDay(this DateTime self) => self.Date;

        /// <summary>
        /// 获取当天 23:59:59.9999999
        /// </summary>
        public static DateTime EndOfDay(this DateTime self) => self.Date.AddDays(1).AddTicks(-1);

        /// <summary>
        /// 基于指定一周起始日，获取当前日期所在周的起始日期（时间归零）
        /// </summary>
        public static DateTime StartOfWeek(this DateTime self, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            int diff = (7 + (self.DayOfWeek - startOfWeek)) % 7;
            return self.Date.AddDays(-diff);
        }

        /// <summary>
        /// 判断是否为周末（周六或周日）
        /// </summary>
        public static bool IsWeekend(this DateTime self)
        {
            return self.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        }

        /// <summary>
        /// 转换为 Unix 时间戳（秒）
        /// </summary>
        public static long ToUnixTimeSeconds(this DateTime self)
        {
            DateTime utc = self.Kind == DateTimeKind.Utc ? self : self.ToUniversalTime();
            return (long)(utc - UnixEpochUtc).TotalSeconds;
        }

        /// <summary>
        /// 从 Unix 时间戳（秒）转换为 UTC 时间
        /// </summary>
        public static DateTime FromUnixTimeSeconds(this long seconds)
        {
            return UnixEpochUtc.AddSeconds(seconds);
        }
    }
}
