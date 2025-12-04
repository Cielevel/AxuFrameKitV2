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
        /// 将路径中的反斜杠替换为正斜杠
        /// </summary>
        public static string MakeSureFilePath(this string self) =>
            self.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        
        /// <summary>
        /// 将路径中的正斜杠替换为反斜杠
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
        /// 将驼峰命名转换为下划线命名 (e.g., "CamelCase" -> "camel_case")
        /// </summary>
        public static string ToSnakeCase(this string self)
        {
            if (string.IsNullOrEmpty(self)) return self;

            return string.Concat(self.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }
    }
}
