namespace AxuIndieModules.StatPipeline
{
    public class MultCalculator : StatModifierCalculator
    {
        public override double Calculate(double targetValue, double modifierValue) => targetValue * modifierValue;
    }
}
