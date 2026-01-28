namespace AxuIndieModules.StatPipeline
{
    public class AddCalculator : StatModifierCalculator // 加法
    {
        public override double Calculate(double targetValue, double modifierValue) => targetValue + modifierValue;
    }
}
