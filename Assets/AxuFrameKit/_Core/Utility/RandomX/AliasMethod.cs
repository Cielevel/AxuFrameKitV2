using System.Collections.Generic;
using System.Linq;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Utility.RandomX
{
    /// <summary>
    /// 别名法。
    /// 这是一个高效的加权随机抽样算法，预处理时间O(n)，抽样时间O(1)
    /// 适用场景：高频率抽样；权重数量极多的抽样；不需要经常更改权重的抽样；
    /// 不适用场景：经常需要更改权重的抽样
    /// </summary>
    public class AliasMethod
    {
        private readonly double[] _probabilities;
        private readonly int[] _aliases;
        private readonly System.Random _random;
        private readonly int _n;

        public AliasMethod(double[] weights)
        {
            if (weights == null || weights.Length == 0)
            {
                Logger.Error("权重数组不能为空");
                return;
            }

            if (weights.Any(w => w < 0))
            {
                Logger.Error("权重不能为负数");
                return;
            }

            _n             = weights.Length;
            _probabilities = new double[_n];
            _aliases       = new int[_n];
            _random        = new System.Random();

            BuildAliasStructure(weights);
        }

        private void BuildAliasStructure(double[] weights)
        {
            double totalWeight = weights.Sum();
            if (totalWeight <= 0)
            {
                {
                    Logger.Error("总权重必须大于0");
                    return;
                }
            }
            
            double[] scaledWeights = weights.Select(w => w * _n / totalWeight).ToArray();

            var small = new Queue<int>();
            var large = new Queue<int>();

            for (int i = 0; i < _n; i++)
            {
                if (scaledWeights[i] < 1.0)
                    small.Enqueue(i);
                else
                    large.Enqueue(i);
            }

            while (small.Count > 0 && large.Count > 0)
            {
                int smallIndex = small.Dequeue();
                int largeIndex = large.Dequeue();

                _probabilities[smallIndex] = scaledWeights[smallIndex];
                _aliases[smallIndex]       = largeIndex;

                scaledWeights[largeIndex] = scaledWeights[largeIndex] + scaledWeights[smallIndex] - 1.0;

                if (scaledWeights[largeIndex] < 1.0)
                    small.Enqueue(largeIndex);
                else
                    large.Enqueue(largeIndex);
            }

            while (large.Count > 0)
            {
                int index = large.Dequeue();
                _probabilities[index] = 1.0;
            }

            while (small.Count > 0)
            {
                int index = small.Dequeue();
                _probabilities[index] = 1.0;
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public int Sample()
        {
            int    bucketIndex = _random.Next(_n);
            double randomValue = _random.NextDouble();

            if (randomValue < _probabilities[bucketIndex])
                return bucketIndex;
            else
                return _aliases[bucketIndex];
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public int[] Sample(int count)
        {
            int[] results = new int[count];
            for (int i = 0; i < count; i++)
            {
                results[i] = Sample();
            }

            return results;
        }

        /// <summary>
        /// 以相同概率，在给定的数组中随机抽取1个元素
        /// </summary>
        public static T Sample<T>(T[] items)
        {
            if (items == null || items.Length == 0)
            {
                Logger.Error("采样集合不能为空");
                return default;
            }

            double[] weights = new double[items.Length];
            for (int i = 0; i < items.Length; i++)
                weights[i] = 1.0; // 默认权重为1.0

            var aliasMethod = new AliasMethod(weights);

            int indices = aliasMethod.Sample();

            return items[indices];
        }
        
        /// <summary>
        /// 以相同概率，在给定的数组中随机抽取n个元素
        /// </summary>
        public static T[] Sample<T>(T[] items, int count)
        {
            if (items == null || items.Length == 0)
            {
                Logger.Error("采样集合不能为空");
                return null;
            }

            double[] weights = new double[items.Length];
            for (int i = 0; i < items.Length; i++)
                weights[i] = 1.0; // 默认权重为1.0

            var aliasMethod = new AliasMethod(weights);

            var indices = aliasMethod.Sample(count);
            
            var results = new T[count];
            for (int i = 0; i < count; i++)
                results[i] = items[indices[i]];
            return results;
        }
    }
}