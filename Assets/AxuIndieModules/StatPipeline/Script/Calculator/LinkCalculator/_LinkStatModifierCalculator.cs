using UnityEngine;

namespace AxuIndieModules.StatPipeline
{
    public abstract class LinkStatModifierCalculator : ScriptableObject
    {
        public abstract double Calculate(double targetValue, double modifierValue, double coefficient);
    }
}