using UnityEngine;

namespace AxuIndieModules.StatPipeline
{
    /*
     * Combine：Param默认为累加的形式，即使是乘法也是累加
     */

    /*
     * 对于数量的情况：
     * 1.允许此Modifier存在多个，则不会存在合并的选项（多个Modifier同时起作用）
     * 2.不允许此Modifier存在多个，则给出合并的选项（也可以不合并，直接排除）
     */

    /// <summary>
    /// 对多个相同的Modifier存在的允许情况
    /// </summary>
    public enum StatModifierCountType
    {
        AllowMultiple, // 允许多个相同ID的ValueModifier存在
        ForbidMultiple, // 禁止多个相同ID的ValueModifier存在
    }

    /// <summary>
    /// 当前Modifier的时间限制情况
    /// </summary>
    public enum StatModifierLifeType
    {
        Unlimited, // 是否是不限时的ValueModifier
        Limited, // 是否是限时的ValueModifier
    }

    /// <summary>
    /// 参数合并的模式
    /// </summary>
    public enum StatModifierCombineParamType
    {
        None, // 不合并

        MultParamAtSameTags, // 在tags完全相同时合并Param，且是乘法的形式
        AddParamAtSameTags, // 在tags完全相同时合并Param，且是累加的形式

        MultParamAtSameID, // 在ID相同时合并Param，且是乘法的形式
        AddParamAtSameID, // 在ID相同时合并Param，且是累加的形式
    }

    /// <summary>
    /// 数量合并的模式
    /// </summary>
    public enum StatModifierCombineCountType
    {
        None, // 不合并

        AddCountAtSameTags, // 在tags完全相同时合并Count，且是加法的形式
        AddCountAtSameID, // 在ID相同时是否合并Count，且是加法的形式
    }

    public enum ValueModifierValidConditionType
    {
        Always = 0, // 永远有效
    }

    /// <summary>
    /// 计算的方式
    /// </summary>
    public enum StatModifierCalculateType
    {
        // normal计算域（无任何强制性）
        None = 0,
        Add = 1,
        Subtract = 2,
        Multiply = 3,
        Divide = 4,
        AddPercent = 5,      // 百分比加成
        SubtractPercent = 6, // 百分比减成

        // final计算域（将强制为最后计算）
        FinalMultiply = 7,      // 最终乘法
        FinalDivide = 8,        // 最终除法
        ClampMin = 9,           // 钳制最小值
        ClampMax = 10,          // 钳制最大值
        ClampMinByStat = 11,    // 通过某个属性钳制最小值
        ClampMaxByStat = 12,    // 通过某个属性钳制最大值
        Abs = 13,               // 取绝对值
        Negate = 14,            // 取相反数
        Override = 15,          // 覆盖值（直接覆盖数值）
        // ConditionalActive = 16, // 条件激活（根据某个条件决定是否激活该ValueModifier）

        // final计算域（将强制为最后计算，且有特殊的计算方式）
        OverrideByStat = 32, // 通过某个属性覆盖值
        ScaleByStat = 33,    // 按另一个属性缩放
    }

    public abstract class StatModifierBase : ScriptableObject
    {
        [field: SerializeField]
        public uint Index { get; protected set; }

        [field: SerializeField]
        public int Count { get; protected set; } = 1; // 数量

        [field: SerializeField]
        public int Weight { get; protected set; } // 排序用Weight

        [field: SerializeField]
        public string StageTag { get; protected set; } // 所属阶段的 Tag

        [field: SerializeField]
        public float Duration { get; protected set; }

        [field: SerializeField]
        public bool Active { get; protected set; }

        [field: SerializeField]
        public StatModifierCountType CountType { get; protected set; }

        [field: SerializeField]
        public StatModifierLifeType LifeType { get; protected set; }

        [field: SerializeField]
        public StatModifierCombineParamType CombineParamType { get; protected set; }

        [field: SerializeField]
        public StatModifierCombineCountType CombineCountType { get; protected set; }

        [field: SerializeField]
        public ValueModifierValidConditionType ConditionType { get; protected set; }

        [field: SerializeField]
        public StatBase Stat { get; protected set; } // StatModifier中使用自己的Stat；StatModifierLink中使用外部的Stat

        public virtual string ID => Stat != null ? Stat.ID : string.Empty;

        [field: SerializeField]
        public StatBase Owner { get; private set; }

        public void RegisterOwner(StatBase owner)
        {
            Owner = owner;
        }

        public void UnRegisterOwner(StatBase owner) // 只能由StatBase调用
        {
            if (Owner != owner)
            {
                Debug.LogWarning($"StatModifier {Index} is not owned by {owner}", this);
                return;
            }

            Owner = null;
        }

        public abstract double Apply(double value);

        /// <summary>
        /// 克隆（注意不会赋值Owner）
        /// </summary>
        public abstract StatModifierBase Clone();

        /// <summary>
        /// 有生命周期的ValueModifier，调用Tik方法进行时间流逝计算
        /// </summary>
        // public abstract void Tik();
    }
}