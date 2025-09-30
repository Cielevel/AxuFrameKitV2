using System.Collections.Generic;

namespace AxuFrameKit._Core.Advanced.ValueSystem
{
    /*
     * Combine：Param默认为累加的形式，即使是乘法也是累加
     */

    public enum ValueModifierCountType
    {
        AllowMultiple = 1, // 允许多个相同ID的ValueModifier存在
        ForbidMultiple = 2, // 禁止多个相同ID的ValueModifier存在
    }
    
    public enum ValueModifierLifeType
    {
        Continuos = 1, // 是否是持续的ValueModifier
        LimitedTime = 2, // 是否是限时的ValueModifier
    }
    
    public enum ValueModifierCombineParamType
    {
        None = 0, // 不合并
        
        MultParamAtSameTags = 1, // 在tags完全相同时合并Param，且是乘法的形式
        AddParamAtSameTags = 2, // 在tags完全相同时合并Param，且是累加的形式
        
        MultParamAtSameID = 3, // 在ID相同时合并Param，且是乘法的形式
        AddParamAtSameID = 4, // 在ID相同时合并Param，且是累加的形式
    }
    
    public enum ValueModifierCombineCountType
    {
        None = 0, // 不合并
        
        AddCountAtSameTags = 1, // 在tags完全相同时合并Count，且是加法的形式
        AddCountAtSameID = 2, // 在ID相同时是否合并Count，且是加法的形式
    }

    public enum ValueModifierCalculateType
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

    public enum ValueModifierValidConditionType
    {
        Always = 0, // 永远有效
    }
    
    public abstract class ValueModifierCore
    {
        public uint UniqueID { get; private set; }
        
        public ValueUnitBaseCore ParamUnit { get; private set; }
        
        public int Count { get; private set; }
        
        public int Weight { get; private set; }
        
        public ValueModifierCountType CountType { get; private set; }
        public ValueModifierLifeType LifeType { get; private set; }
        public ValueModifierCombineParamType CombineParamType { get; private set; }
        public ValueModifierCombineCountType CombineCountType { get; private set; }
        public ValueModifierCalculateType CalculateType { get; private set; }
        public ValueModifierValidConditionType ConditionType { get; private set; }
        
        public List<string> TagList { get; private set; }

        public ValueUnitBaseCore Owner { get; private set; }
        
        public float Duration { get; private set; }
        
        public bool Active { get; private set; }

        /// <summary>
        /// 有生命周期的ValueModifier，调用Tik方法进行时间流逝计算
        /// </summary>
        public abstract void Tik();
    }
}