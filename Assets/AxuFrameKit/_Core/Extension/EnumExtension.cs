using System;

namespace AxuFrameKit._Core.Extension
{
    /// <summary>
    /// 为枚举类型提供扩展功能
    /// </summary>
    public static class EnumExtensions
    {
        private static readonly Random SystemRandom = new();

        /// <summary>
        /// 获取枚举类型的随机值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>随机选择的枚举值</returns>
        public static T GetRandomValue<T>() where T : System.Enum
        {
            Array values = System.Enum.GetValues(typeof(T));
            return (T)values.GetValue(SystemRandom.Next(values.Length));
        }

        /// <summary>
        /// 使用Unity随机数生成器获取枚举类型的随机值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>随机选择的枚举值</returns>
        public static T GetRandomValueUnity<T>() where T : System.Enum
        {
            Array values = System.Enum.GetValues(typeof(T));
            return (T)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        }

        /// <summary>
        /// 判断给定的值是否定义在枚举中
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">要检查的值</param>
        /// <returns>如果值在枚举中定义则返回true</returns>
        public static bool IsDefined<T>(this T value) where T : System.Enum
        {
            return System.Enum.IsDefined(typeof(T), value);
        }

        /// <summary>
        /// 获取枚举值的名称
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">枚举值</param>
        /// <returns>枚举值的名称</returns>
        public static string GetName<T>(this T value) where T : System.Enum
        {
            return System.Enum.GetName(typeof(T), value);
        }

        /// <summary>
        /// 获取枚举中的所有值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>包含所有枚举值的数组</returns>
        public static T[] GetAllValues<T>() where T : System.Enum
        {
            return (T[])System.Enum.GetValues(typeof(T));
        }
    }
}