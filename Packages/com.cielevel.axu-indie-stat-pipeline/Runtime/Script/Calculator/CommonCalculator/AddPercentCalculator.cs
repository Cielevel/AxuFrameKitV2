namespace AxuIndieModules.StatPipeline
{
    public class AddPercentCalculator : StatModifierCalculator // 加百分比
    {
        public override double Calculate(double targetValue, double modifierValue) => targetValue * (1.0 + modifierValue);
    }
}
