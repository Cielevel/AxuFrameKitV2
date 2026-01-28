namespace AxuIndieModules.StatPipeline
{
    public class NoneCalculator : StatModifierCalculator // 不进行任何操作
    {
        public override double Calculate(double targetValue, double modifierValue) => targetValue;
    }
}
