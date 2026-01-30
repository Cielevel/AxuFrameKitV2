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
    /// <summary>
    /// 使用Stat时请使用具体类型，不要使用StatBase与泛型Stat
    /// </summary>
    public abstract class StatBase : ScriptableObject
    {
        public abstract double Value { get; }

        [field: SerializeField]
        public string ID { get; set; }

        [field: SerializeField]
        public StatRefreshType RefreshType { get; protected set; }

        [field: SerializeField]
        public List<string> Tags { get; protected set; }

        [field: SerializeField]
        public bool IsDirty { get; protected set; }

        [field: SerializeField]
        public bool IsInit { get; protected set; }

        [field: SerializeField]
        public List<StatModifierBase> Modifiers { get; protected set; }

        [field: SerializeField]
        public StatStages Stages { get; protected set; }

        protected List<StatModifierBase>[] _stageModifiers;

        private HashSet<string> _stageTags;

        protected HashSet<string> StageTags
        {
            get
            {
                if (_stageTags == null)
                    _stageTags = new HashSet<string>(Stages.StageTags);
                return _stageTags;
            }
        }

        protected virtual void InitializeGroupedModifiers()
        {
            if (Stages == null)
            {
                _stageModifiers = null;
                Debug.LogWarning($"Stat {ID} has no StatPipelineStages", this);
                return;
            }

            _stageModifiers = new List<StatModifierBase>[StageTags.Count];
            for (int i = 0; i < _stageModifiers.Length; i++)
                _stageModifiers[i] = new List<StatModifierBase>();

            foreach (var mod in Modifiers)
            {
                int index = Stages.GetStageIndex(mod.StageTag);
                if (index >= 0)
                    _stageModifiers[index].Add(mod);
                else
                    Debug.LogWarning($"Stat {ID} contains modifier {mod.name} with unknown StageTag: {mod.StageTag}", mod);
            }

            // Sort within each stage by Weight (ascending)
            foreach (var stageList in _stageModifiers)
                stageList.Sort((a, b) => a.Weight.CompareTo(b.Weight));
        }

        private void OnEnable() => IsInit = false;

        public abstract void RefreshCache();

        protected abstract void TryAutoRefresh();

        public abstract void RegisterModifier(StatModifierBase modifier);
        public abstract void UnRegisterModifier(StatModifierBase modifier);

        protected void AddModifierToStage(StatModifierBase modifier)
        {
            if (_stageModifiers == null || Stages == null) return;
            int index = Stages.GetStageIndex(modifier.StageTag);
            if (index >= 0)
            {
                _stageModifiers[index].Add(modifier);
                _stageModifiers[index].Sort((a, b) => a.Weight.CompareTo(b.Weight));
            }
        }

        protected void RemoveModifierFromStage(StatModifierBase modifier)
        {
            if (_stageModifiers == null || Stages == null) return;
            int index = Stages.GetStageIndex(modifier.StageTag);
            if (index >= 0)
            {
                _stageModifiers[index].Remove(modifier);
            }
        }
    }

    /// <summary>
    /// 使用Stat时请使用具体类型，不要使用StatBase与泛型Stat
    /// </summary>
    public abstract class Stat<T> : StatBase where T : struct // 统一用double数值，子类负责裁切，提供具体数值
    {
        [field: SerializeField]
        public T BaseValue { get; set; }

        protected T _cachedValue;

        public override double Value => FastConvert(GetValue());

        public T GetValue()
        {
            if (!IsInit)
            {
                InitializeGroupedModifiers();
                RefreshCache();
                IsInit = true;
                return _cachedValue;
            }

            // 脏标记刷新模式下，如果不为脏，返回。如果是手动或自动，也返回。
            if (RefreshType is StatRefreshType.Dirty || !IsDirty || RefreshType is StatRefreshType.Manual or StatRefreshType.Auto)
                return _cachedValue;

            RefreshCache();

            return _cachedValue;
        }

        public override void RefreshCache()
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
                Debug.LogWarning($"Stat {ID} has no _stageModifiers", this);
                // Fallback to simple sequential calculation if no stages defined
                for (int i = 0; i < Modifiers.Count; i++)
                {
                    finalValue = Modifiers[i].Apply(finalValue);
                }
            }

            _cachedValue = FromDouble(finalValue);
            IsDirty = false;
        }

        protected override void TryAutoRefresh()
        {
            if (RefreshType is StatRefreshType.Auto)
                RefreshCache();

            if (RefreshType is StatRefreshType.Dirty)
                IsDirty = true;
        }

        public override void RegisterModifier(StatModifierBase modifier)
        {
            if (!StageTags.Contains(modifier.StageTag))
                Debug.LogWarning($"Stat {ID} has no stage tag: {modifier.StageTag}", this);

            Modifiers.Add(modifier);
            AddModifierToStage(modifier);
            modifier.RegisterOwner(this);
            TryAutoRefresh();
        }

        public override void UnRegisterModifier(StatModifierBase modifier)
        {
            Modifiers.Remove(modifier);
            RemoveModifierFromStage(modifier);
            modifier.UnRegisterOwner(this);
            TryAutoRefresh();
        }

        protected void CopyTo(Stat<T> other)
        {
            other.ID = ID;
            other.RefreshType = RefreshType;
            other.Tags = Tags != null ? new List<string>(Tags) : new List<string>();
            other.IsDirty = IsDirty;
            other.IsInit = false; // 确保克隆体在第一次 GetValue 时会重新执行 InitializeGroupedModifiers
            other.Modifiers = Modifiers != null ? new List<StatModifierBase>(Modifiers.Count) : new List<StatModifierBase>();
            if (Modifiers != null)
                foreach (var mod in Modifiers)
                    other.Modifiers.Add(mod.Clone());
            other.Stages = Stages; // Stages 通常是全局配置，保持同一个引用
            other.BaseValue = BaseValue;
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
