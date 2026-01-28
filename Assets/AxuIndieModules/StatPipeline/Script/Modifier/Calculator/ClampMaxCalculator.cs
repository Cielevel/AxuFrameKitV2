using System;

namespace AxuIndieModules.StatPipeline
{
    public class ClampMaxCalculator : StatModifierCalculator // 钳制最大值（targetValue不能超过modifierValue）
    {
        public override double Calculate(double targetValue, double modifierValue) => Math.Min(targetValue, modifierValue);
    }
}
