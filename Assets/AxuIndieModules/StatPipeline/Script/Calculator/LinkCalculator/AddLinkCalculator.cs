namespace AxuIndieModules.StatPipeline
{
    public class AddLinkCalculator : LinkStatModifierCalculator // 加法
    {
        public override double Calculate(double targetValue, double modifierValue, double coefficient) => targetValue + modifierValue * coefficient;
    }
}
