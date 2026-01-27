using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using AxuIndieModules.StatPipeline.Modifier;

namespace AxuIndieModules.StatPipeline.Stat
{
    public enum StatRefreshType
    {
        Auto, // 自动刷新
        Manual, // 手动刷新
        Dirty, // 脏值刷新
    }

    // 定义这一层是为了让 Modifier 可以引用任何类型的 Stat
    public abstract class StatBase : ScriptableObject
    {
        public abstract double Value { get; }

        [field: SerializeField]
        public string ID { get; set; }

        [field: SerializeField]
        public StatRefreshType RefreshType { get; private set; }

        [field: SerializeField]
        public List<string> Tags { get; protected set; }

        [field: SerializeField]
        public bool IsDirty { get; protected set; }

        [field: SerializeField]
        public List<StatModifierBase> Modifiers { get; protected set; }
    }

    public abstract class Stat<T> : StatBase where T : struct // 统一用double数值，子类负责裁切，提供具体数值
    {
        protected T _baseValue;
        protected T _cachedValue;

        public override double Value => FastConvert(GetValue());

        public T GetValue()
        {
            // 脏标记刷新模式下，如果不为脏，返回。如果是手动或自动，也返回。
            if (RefreshType is StatRefreshType.Dirty || !IsDirty || RefreshType is StatRefreshType.Manual or StatRefreshType.Auto)
                return _cachedValue;

            double finalValue = FastConvert(_baseValue);

            // 进行计算
            for (int i = 0; i < Modifiers.Count; i++) // TODO 此处目前只是简单的顺序变量，未增加SortLayer与OrderInLayer
            {
                var modifier = Modifiers[i];
                finalValue = modifier.Apply(finalValue);
            }

            _cachedValue = FromDouble(finalValue);

            IsDirty = false;

            return _cachedValue;
        }

        /// <summary>
        /// 提升转换性能
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FastConvert<T1>(T1 value) where T1 : struct
        {
            // 利用 typeof 静态比较，JIT 会在特化时优化掉不相关的分支（此处应该不会发生boxing，unboxing）
            if (typeof(T1) == typeof(double)) return (double)(object)value;
            if (typeof(T1) == typeof(int)) return (int)(object)value;
            if (typeof(T1) == typeof(float)) return (float)(object)value;
            if (typeof(T1) == typeof(long)) return (long)(object)value;

            return Convert.ToDouble(value);
        }
        
        // 必须由子类实现的“裁切”逻辑
        protected abstract T FromDouble(double value);
    }
}
