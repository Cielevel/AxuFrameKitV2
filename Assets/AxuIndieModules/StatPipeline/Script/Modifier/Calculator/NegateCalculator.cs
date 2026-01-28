namespace AxuIndieModules.StatPipeline
{
    public class NegateCalculator : StatModifierCalculator
    {
        public override double Calculate(double targetValue, double modifierValue) => -targetValue;
    }
}
