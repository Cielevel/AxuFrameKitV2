using System.IO;

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
            return char.ToUpperInvariant(self[start]) + self.Substring(start + 1); // ToUpperInvariant文化不敏感，使用英语环境
        }
        
        /// <summary>
        /// 为null的字符串亦可调用，判断是否为空或者内容为空
        /// </summary>
        public static bool IsNullOrEmpty(this string self) => self == null || string.IsNullOrEmpty(self);
        
        /// <summary>
        /// 为null的字符串亦可调用，判断是否为空或者内容为空
        /// </summary>
        public static bool IsNotNullOrEmpty(this string self) => !self.IsNullOrEmpty();

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
    }
}