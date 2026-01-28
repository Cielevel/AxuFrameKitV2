using UnityEngine;

namespace AxuIndieModules.StatPipeline
{
    public abstract class StatModifierCalculator : ScriptableObject
    {
        public abstract double Calculate(double targetValue, double modifierValue);
    }
}