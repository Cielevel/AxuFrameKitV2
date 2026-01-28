namespace AxuIndieModules.StatPipeline
{
    public class SubCalculator : StatModifierCalculator
    {
        public override double Calculate(double targetValue, double modifierValue) => targetValue - modifierValue;
    }
}
