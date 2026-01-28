namespace AxuIndieModules.StatPipeline
{
    public class OverrideCalculator : StatModifierCalculator // 覆盖（targetValue被modifierValue覆盖）
    {
        public override double Calculate(double targetValue, double modifierValue) => modifierValue;
    }
}
