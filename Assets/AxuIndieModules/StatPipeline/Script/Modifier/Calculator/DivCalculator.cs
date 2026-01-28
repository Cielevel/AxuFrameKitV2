namespace AxuIndieModules.StatPipeline
{
    public class DivCalculator : StatModifierCalculator
    {
        public override double Calculate(double targetValue, double modifierValue) => targetValue / modifierValue;
    }
}