using System;
using System.Collections.Generic;
using System.Linq;
using AxuFrameKit._Core.Utility.Common;
using Random = System.Random;

namespace AxuFrameKit._Core.Extension
{
    public static class CollectionExtension
    {
        private const string LogNullError = "集合为空！";
        private const string LogCountError = "集合内无内容！";
        
        private static readonly Random Ran = new();

        /// <summary>
        /// 获取集合中的随机一个元素，适用于任何 IEnumerable（数组、List、HashSet 等）
        /// </summary>
        public static T GetRandom<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                Logger.Error(LogNullError);
                return default;
            }

            // 转换为数组或列表以提高性能（避免多次枚举）
            var list = source as IReadOnlyList<T> ?? source.ToArray();

            if (list.Count == 0)
            {
                Logger.Error(LogCountError);
                return default;
            }

            return list[Ran.Next(list.Count)];
        }

        #region Random without limit

        /// <summary>
        /// 获取集合中的随机一个元素，但避免获取到exclude元素，适用于任何 IEnumerable（数组、List、HashSet 等）
        /// </summary>
        public static T GetRandom<T>(this IEnumerable<T> source, T exclude)
        {
            if (source == null)
            {
                Logger.Error(LogNullError);
                return default;
            }
       
            // 转换为数组或列表以提高性能（避免多次枚举）
            var filteredList = (source as IReadOnlyList<T> ?? source.ToArray()).Except(new[] { exclude }).ToArray();
       
            if (filteredList.Length == 0)
            {
                Logger.Error(LogCountError);
                return default;
            }
       
            return filteredList[Ran.Next(filteredList.Length)];
        }
        
        /// <summary>
        /// 获取集合中的随机一个元素，但避免获取到exclude中的元素，适用于任何 IEnumerable（数组、List、HashSet 等）
        /// </summary>
        public static T GetRandom<T>(this IEnumerable<T> source, IEnumerable<T> exclude)
        {
            if (source == null)
            {
                Logger.Error(LogNullError);
                return default;
            }
       
            if (exclude == null)
            {
                exclude = Enumerable.Empty<T>();
            }
       
            // 转换为数组或列表以提高性能（避免多次枚举）
            var filteredList = (source as IReadOnlyList<T> ?? source.ToArray()).Except(exclude).ToArray();
       
            if (filteredList.Length == 0)
            {
                Logger.Error(LogCountError);
                return default;
            }
       
            return filteredList[Ran.Next(filteredList.Length)];
        }

        #endregion
        
        #region Random limit

        /// <summary>
        /// 获取集合中的随机一个元素，但避免获取到exclude元素，针对可进行等式判断的元素（继承IEquatable）
        /// </summary>
        public static T GetRandomEq<T>(this IEnumerable<T> source, Func<T, bool> excludeFunc)
        {
            if (source == null)
            {
                Logger.Error(LogNullError);
                return default;
            }

            var sourceArray = source.ToArray();

            if (sourceArray.All(excludeFunc.Invoke))
                return default;
       
            // 转换为数组或列表以提高性能（避免多次枚举）
            var filteredList = sourceArray.Where(e => !excludeFunc(e)).ToArray();
       
            if (filteredList.Length == 0)
            {
                Logger.Error(LogCountError);
                return default;
            }
       
            return filteredList[Ran.Next(filteredList.Length)];
        }
        
        /// <summary>
        /// 获取集合中的随机一个元素，但避免获取到exclude元素，针对可进行等式判断的元素（继承IEquatable）
        /// </summary>
        public static T GetRandomEq<T>(this IEnumerable<T> source, T exclude) where T : IEquatable<T>
        {
            if (source == null)
            {
                Logger.Error(LogNullError);
                return default;
            }

            var sourceArray = source.ToArray();

            if (sourceArray.All(e => e.Equals(exclude)))
                return exclude;
       
            // 转换为数组或列表以提高性能（避免多次枚举）
            var filteredList = (source as IReadOnlyList<T> ?? sourceArray).Except(new[] { exclude }).ToArray();
       
            if (filteredList.Length == 0)
            {
                Logger.Error(LogCountError);
                return default;
            }
       
            return filteredList[Ran.Next(filteredList.Length)];
        }
        
        /// <summary>
        /// 获取集合中的随机一个元素，但避免获取到exclude中的元素，针对可进行等式判断的元素（继承IEquatable）
        /// </summary>
        public static T GetRandomEq<T>(this IEnumerable<T> source, IEnumerable<T> exclude) where T : IEquatable<T>
        {
            if (source == null)
            {
                Logger.Error(LogNullError);
                return default;
            }
       
            if (exclude == null)
            {
                exclude = Enumerable.Empty<T>();
            }
       
            // 转换为数组或列表以提高性能（避免多次枚举）
            var filteredList = (source as IReadOnlyList<T> ?? source.ToArray()).Except(exclude).ToArray();
       
            if (filteredList.Length == 0)
            {
                Logger.Error(LogCountError);
                return default;
            }
       
            return filteredList[Ran.Next(filteredList.Length)];
        }

        #endregion

        /// <summary>
        /// 获取集合中的随机一个元素，适用于任何 IEnumerable（数组、List、HashSet 等）
        /// </summary>
        public static (T result, int index) GetRandomAndIndex<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                Logger.Error(LogNullError);
                return default;
            }

            // 转换为数组或列表以提高性能（避免多次枚举）
            var list = source as IReadOnlyList<T> ?? source.ToArray();

            if (list.Count == 0)
            {
                Logger.Error(LogCountError);
                return default;
            }

            int index = Ran.Next(list.Count);
            return (list[index], index);
        }

        /// <summary>
        /// 随机获取n个元素，其中元素可重复
        /// </summary>
        public static T[] GetRandom<T>(this IEnumerable<T> source, int count)
        {
            if (source == null)
            {
                Logger.Error(LogNullError);
                return null;
            }
            
            // 转换为数组或列表以提高性能（避免多次枚举）
            var list = source as IReadOnlyList<T> ?? source.ToArray();

            if (list.Count == 0)
            {
                Logger.Error(LogCountError);
                return null;
            }

            return Enumerable.Range(0, count).Select(_ => list[Ran.Next(list.Count)]).ToArray();
        }
        
        /// <summary>
        /// 获取n个不重复元素，如果count大于了集合大小，则返回集合本身（Array）；以Hash晒重
        /// </summary>
        public static T[] GetRandomUniqueItems<T>(this IEnumerable<T> source, int n)
        {
            if (source == null)
            {
                Logger.Error(LogNullError);
                return null;
            }

            var list = source as IReadOnlyList<T> ?? source.ToArray();
            
            if (n >= list.Count)
                return list.ToArray(); // 如果 n >= 集合大小，直接返回整个集合

            var random = new Random();
            var result = new HashSet<T>(); // 使用 HashSet 去重

            while (result.Count < n)
            {
                var randomIndex = random.Next(list.Count);
                result.Add(list[randomIndex]); // 自动去重
            }

            return result.ToArray();
        }
        
        /// <summary>
        /// 获取n个不重复元素，如果count大于了集合大小，则返回集合本身（Array）；算法为Fisher-Yates洗牌算法
        /// </summary>
        public static T[] GetRandomUniqueItemsAlgo<T>(this IEnumerable<T> source, int count)
        {
            if (source == null)
            {
                Logger.Error(LogNullError);
                return null;
            }

            // 转换为数组或列表以提高性能（避免多次枚举）
            var list = source as IReadOnlyList<T> ?? source.ToArray();
            
            if (count >= list.Count)
                return list.ToArray(); // 如果 n >= 集合大小，直接返回整个集合

            var random   = new Random();
            var shuffled = list.ToList(); // 复制一份避免修改原集合

            // Fisher-Yates 洗牌算法
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]); // 交换元素
            }

            return shuffled.Take(count).ToArray(); // 取前 n 个
        }

        /// <summary>
        /// 对每一个元素进行操作
        /// </summary>
        public static void Do<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null)
            {
                Logger.Error(LogNullError);
                return;
            }
            
            foreach (var item in source)
                action?.Invoke(item);
        }

        /// <summary>
        /// 对每一个元素进行操作，并传入递增index
        /// </summary>
        public static void Do<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            if (source == null)
            {
                Logger.Error(LogNullError);
                return;
            }

            int index = 0;
            foreach (var item in source)
                action?.Invoke(item, index++);
        }
        
        /// <summary>
        /// 从集合中就地移除满足指定条件的所有元素
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="list">要修改的集合</param>
        /// <param name="predicate">用于测试每个元素的条件</param>
        /// <returns>被移除的元素数量</returns>
        public static int RemoveWhere<T>(this IList<T> list, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
    
            int removedCount = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i]))
                {
                    list.RemoveAt(i);
                    removedCount++;
                }
            }
    
            return removedCount; // 返回移除数量，提供有用的反馈
        }
    }
}