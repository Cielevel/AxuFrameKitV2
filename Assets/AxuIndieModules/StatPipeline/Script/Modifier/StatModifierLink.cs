using System.Collections.Generic;
using UnityEngine;


namespace AxuIndieModules.StatPipeline
{
    /// <summary>
    /// 通用StatModifier，计算时，依赖外部的Stat；（Stat不代表自己的数据）
    /// </summary>
    [CreateAssetMenu(fileName = "StatModifierLink", menuName = "AxuIndieModules/StatPipeline/StatModifierLink")]
    public class StatModifierLink : StatModifierBase
    {
        // 系数
        [field: SerializeField]
        public double Coefficient { get; private set; } = 1.0;

        [SerializeReference]
        public LinkStatModifierCalculator LinkCalculator;

        public List<string> Tags => Stat != null ? Stat.Tags : null;

        public override double Apply(double value)
        {
            if (Stat == null)
            {
                Debug.LogWarning($"StatModifierLink {ID} has no Stat", this);
                return value;
            }

            if (LinkCalculator == null)
            {
                Debug.LogWarning($"StatModifierLink {ID} has no LinkCalculator", this);
                return value;
            }

            return LinkCalculator.Calculate(value, Stat.Value, Coefficient);
        }

        public override StatModifierBase Clone()
        {
            var modifier = ScriptableObject.CreateInstance<StatModifierLink>();
            modifier.Index = Index;
            modifier.Count = Count;
            modifier.Weight = Weight;
            modifier.StageTag = StageTag;
            modifier.Duration = Duration;
            modifier.Active = Active;
            modifier.CountType = CountType;
            modifier.LifeType = LifeType;
            modifier.CombineParamType = CombineParamType;
            modifier.CombineCountType = CombineCountType;
            modifier.ConditionType = ConditionType;
            modifier.Stat = Stat;
            modifier.Coefficient = Coefficient;
            modifier.LinkCalculator = LinkCalculator;
            return modifier;
        }

        // ----- ModifierLink的独立方法 -----

        public void RegisterStat(StatBase stat) => Stat = stat;
    }
}