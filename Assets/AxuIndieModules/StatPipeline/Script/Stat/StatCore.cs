using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AxuIndieModules.StatPipeline
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
        public bool Inited { get; protected set; }

        [field: SerializeField]
        public List<StatModifierBase> Modifiers { get; protected set; }

        [field: SerializeField]
        public StatPipelineStages PipelineStages { get; private set; }

        protected List<StatModifierBase>[] _stageModifiers;

        protected virtual void InitializeGroupedModifiers()
        {
            if (PipelineStages == null)
            {
                _stageModifiers = null;
                Debug.LogWarning($"Stat {ID} has no StatPipelineStages", this);
                return;
            }

            var tags = PipelineStages.StageTags;
            _stageModifiers = new List<StatModifierBase>[tags.Count];
            for (int i = 0; i < tags.Count; i++)
                _stageModifiers[i] = new List<StatModifierBase>();

            foreach (var mod in Modifiers)
            {
                int index = PipelineStages.GetStageIndex(mod.StageTag);
                if (index >= 0)
                    _stageModifiers[index].Add(mod);
                else
                    Debug.LogWarning($"Stat {ID} contains modifier {mod.name} with unknown StageTag: {mod.StageTag}", mod);
            }

            // Sort within each stage by Weight (ascending)
            foreach (var stageList in _stageModifiers)
                stageList.Sort((a, b) => a.Weight.CompareTo(b.Weight));
        }

        private void OnEnable() => Inited = false;
    }

    public abstract class Stat<T> : StatBase where T : struct // 统一用double数值，子类负责裁切，提供具体数值
    {
        [field: SerializeField]
        public T BaseValue { get; set; }

        protected T _cachedValue;

        public override double Value => FastConvert(GetValue());

        public T GetValue()
        {
            if (!Inited)
            {
                InitializeGroupedModifiers();
                RefreshCache();
                Inited = true;
                return _cachedValue;
            }

            // 脏标记刷新模式下，如果不为脏，返回。如果是手动或自动，也返回。
            if (RefreshType is StatRefreshType.Dirty || !IsDirty || RefreshType is StatRefreshType.Manual or StatRefreshType.Auto)
                return _cachedValue;

            RefreshCache();

            return _cachedValue;
        }

        private void RefreshCache()
        {
            double finalValue = FastConvert(BaseValue);

            if (_stageModifiers != null)
            {
                for (int i = 0; i < _stageModifiers.Length; i++)
                {
                    var stageList = _stageModifiers[i];
                    for (int j = 0; j < stageList.Count; j++)
                    {
                        finalValue = stageList[j].Apply(finalValue);
                    }
                }
            }
            else
            {
                // Fallback to simple sequential calculation if no stages defined
                for (int i = 0; i < Modifiers.Count; i++)
                {
                    finalValue = Modifiers[i].Apply(finalValue);
                }
            }

            _cachedValue = FromDouble(finalValue);
            IsDirty = false;
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
