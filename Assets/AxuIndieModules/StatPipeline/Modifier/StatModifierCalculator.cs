using UnityEngine;

namespace AxuIndieModules.StatPipeline.Modifier
{
    public abstract class StatModifierCalculator : ScriptableObject
    {
        public abstract double Calculate(double targetValue, double modifierValue);
    }

    public class AddCalculator : StatModifierCalculator
    {
        public override double Calculate(double targetValue, double modifierValue) => targetValue + modifierValue;
    }

    public class SubCalculator : StatModifierCalculator
    {
        public override double Calculate(double targetValue, double modifierValue) => targetValue - modifierValue;
    }

    public class MultCalculator : StatModifierCalculator
    {
        public override double Calculate(double targetValue, double modifierValue) => targetValue * modifierValue;
    }

    public class DivCalculator : StatModifierCalculator
    {
        public override double Calculate(double targetValue, double modifierValue) => targetValue / modifierValue;
    }
}