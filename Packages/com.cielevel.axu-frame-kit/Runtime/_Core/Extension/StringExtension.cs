using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AxuFrameKit._Core.Extension
{
    public static class StringExtension
    {
        public static string PrivateNameToUpper(this string self) // 将小写开头的成员名改为大写，如果小写成员开头为 _，则剔除后修改
        {
            if (string.IsNullOrEmpty(self))
                return null;

            // FirstCharacterToUpper()也是使用的字符串拼接
            int start = self[0].Equals('_') ? 1 : 0; // 开头是 _，忽略掉
            if (self.Length <= start) return string.Empty;

            return char.ToUpperInvariant(self[start]) + self.Substring(start + 1); // ToUpperInvariant文化不敏感，使用英语环境
        }
        
        /// <summary>
        /// 为null的字符串亦可调用，判断是否为空或者内容为空
        /// </summary>
        public static bool IsNullOrEmpty(this string self) => string.IsNullOrEmpty(self);
        
        /// <summary>
        /// 为null的字符串亦可调用，判断是否为空或者内容为空
        /// </summary>
        public static bool IsNotNullOrEmpty(this string self) => !string.IsNullOrEmpty(self);

        /// <summary>
        /// 判断字符串是否为空、null或仅包含空白字符
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string self) => string.IsNullOrWhiteSpace(self);

        /// <summary>
        /// 如果字符串为 null/空白，返回 null；否则返回原值
        /// </summary>
        public static string NullIfWhiteSpace(this string self) => string.IsNullOrWhiteSpace(self) ? null : self;

        /// <summary>
        /// 将路径中的备用分隔符替换为系统主分隔符（Windows 上为 '/' → '\'；Unix 上两种分隔符相同，实际无变化）
        /// </summary>
        public static string MakeSureFilePath(this string self) =>
            self.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        /// <summary>
        /// 将路径中的系统主分隔符替换为备用分隔符（Windows 上为 '\' → '/'；Unix 上两种分隔符相同，实际无变化）
        /// </summary>
        public static string MakeSureFilePathInverse(this string self) =>
            self.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        /// <summary>
        /// 截断字符串，如果超过maxLength，则截断并（可选）添加后缀
        /// </summary>
        public static string Truncate(this string self, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(self) || self.Length <= maxLength)
                return self;

            return self.Substring(0, maxLength) + suffix;
        }

        /// <summary>
        /// string.Format 的语法糖
        /// </summary>
        public static string Format(this string self, params object[] args)
        {
            return string.Format(self, args);
        }

        /// <summary>
        /// 指定字符串比较规则的 Contains（兼容旧版本 .NET）
        /// </summary>
        public static bool Contains(this string self, string value, StringComparison comparisonType)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return self.IndexOf(value, comparisonType) >= 0;
        }

        /// <summary>
        /// 按 Ordinal 比较两个字符串
        /// </summary>
        public static bool EqualsOrdinal(this string self, string other)
        {
            return string.Equals(self, other, StringComparison.Ordinal);
        }

        /// <summary>
        /// 按 OrdinalIgnoreCase 比较两个字符串
        /// </summary>
        public static bool EqualsOrdinalIgnoreCase(this string self, string other)
        {
            return string.Equals(self, other, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 尝试将字符串转换为枚举
        /// </summary>
        public static T ToEnum<T>(this string self, bool ignoreCase = true) where T : struct, System.Enum
        {
            if (string.IsNullOrEmpty(self))
                throw new ArgumentException("Value cannot be null or empty.", nameof(self));

            return (T)System.Enum.Parse(typeof(T), self, ignoreCase);
        }

        /// <summary>
        /// 尝试将字符串转换为枚举，失败返回默认值
        /// </summary>
        public static T ToEnumOrDefault<T>(this string self, T defaultValue = default, bool ignoreCase = true) where T : struct, System.Enum
        {
            if (string.IsNullOrEmpty(self))
                return defaultValue;

            return System.Enum.TryParse(self, ignoreCase, out T result) ? result : defaultValue;
        }

        /// <summary>
        /// 重复字符串n次
        /// </summary>
        public static string Repeat(this string self, int count)
        {
            if (string.IsNullOrEmpty(self)) return self;
            if (count <= 0) return string.Empty;

            StringBuilder builder = new StringBuilder(self.Length * count);
            for (int i = 0; i < count; i++) builder.Append(self);
            return builder.ToString();
        }

        /// <summary>
        /// 移除所有空白字符
        /// </summary>
        public static string RemoveWhitespace(this string self)
        {
            if (string.IsNullOrEmpty(self)) return self;
            return new string(self.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        /// <summary>
        /// 确保字符串以前缀开头，不满足则拼接
        /// </summary>
        public static string EnsureStartsWith(this string self, string prefix, StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (string.IsNullOrEmpty(prefix)) return self;

            return self.StartsWith(prefix, comparisonType) ? self : prefix + self;
        }

        /// <summary>
        /// 确保字符串以后缀结尾，不满足则拼接
        /// </summary>
        public static string EnsureEndsWith(this string self, string suffix, StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (string.IsNullOrEmpty(suffix)) return self;

            return self.EndsWith(suffix, comparisonType) ? self : self + suffix;
        }

        /// <summary>
        /// 将驼峰命名转换为下划线命名 (e.g., "CamelCase" -> "camel_case")
        /// </summary>
        public static string ToSnakeCase(this string self)
        {
            if (string.IsNullOrEmpty(self)) return self;

            return string.Concat(self.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }
    }
}
