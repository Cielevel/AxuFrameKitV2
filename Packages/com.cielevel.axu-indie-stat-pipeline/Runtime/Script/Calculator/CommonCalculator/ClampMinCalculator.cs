using System;

namespace AxuIndieModules.StatPipeline
{
    public class ClampMinCalculator : StatModifierCalculator // 钳制最小值（targetValue不能低于modifierValue）
    {
        public override double Calculate(double targetValue, double modifierValue) => Math.Max(targetValue, modifierValue);
    }
}
