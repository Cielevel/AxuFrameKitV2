namespace AxuIndieModules.StatPipeline
{
    public class OverrideLinkCalculator : LinkStatModifierCalculator // 覆盖（targetValue被modifierValue覆盖）
    {
        public override double Calculate(double targetValue, double modifierValue, double coefficient) => modifierValue * coefficient;
    }
}
