using System;

namespace AxuIndieModules.StatPipeline
{
    public class AbsCalculator : StatModifierCalculator // 取绝对值
    {
        public override double Calculate(double targetValue, double modifierValue) => Math.Abs(targetValue);
    }
}
