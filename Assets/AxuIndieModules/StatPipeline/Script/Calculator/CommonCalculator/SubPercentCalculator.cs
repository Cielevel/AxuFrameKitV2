namespace AxuIndieModules.StatPipeline
{
    public class SubPercentCalculator : StatModifierCalculator // 减百分比
    {
        public override double Calculate(double targetValue, double modifierValue) => targetValue * (1.0 - modifierValue);
    }
}
