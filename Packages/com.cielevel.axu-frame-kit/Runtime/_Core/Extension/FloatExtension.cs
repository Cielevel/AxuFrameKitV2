using System;
using System.Globalization;
using System.Numerics;
using UnityEngine;

namespace AxuFrameKit._Core.Extension
{
    public static class FloatExtension
    {
        private const float Epsilon = 0.01f; // 取近值使用的误差值

        public static bool NearZero(this float self) => Mathf.Abs(self) <= Epsilon;
        public static bool Near(this float self, float target) => Mathf.Abs(self - target) <= Epsilon;

        public static float SelfMult(this float self) => self * self;

        // replace的原因：ToString(format)方法对格式字符串的严格转义要求，其中的":"需要转义为"\\:"
        public static string ToTime(this float self, string format) => TimeSpan.FromSeconds(self).ToString(format.Replace(":", "\\:"));
        
        /// <summary>
        /// 安全保留一部分小数位，返回string
        /// </summary>
        public static string TruncateToStringSafe(this float value, int decimalPlaces)
        {
            if (decimalPlaces < 0)
                throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "小数位数不能为负数");

            // 计算有效数字范围
            int integerDigits = (int)Math.Log10(Math.Abs(value)) + 1;
            int maxPrecision  = 7 - integerDigits; // float有效数字约7位

            // 自动限制小数位数
            int safeDecimalPlaces = Math.Min(decimalPlaces, Math.Max(maxPrecision, 0));

            string format = $"F{safeDecimalPlaces}";
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 精确且安全地获取float截断值（整型 as string）
        /// </summary>
        public static string TruncateToStringSafe0DecimalPlaces(this float self) =>
            BigInteger.Parse(self.ToString("F0")).ToString();
        
        /// <summary>
        /// 获取float截断值（as int），若数值超过了long.MaxValue，将触发科学计数法（超出这个范围后，精度会下降，无法精确表示数字，用科学计数法近似表示）
        /// </summary>
        public static string TruncateToString0DecimalPlaces(this float self) => self.ToString("F0");
    }
}