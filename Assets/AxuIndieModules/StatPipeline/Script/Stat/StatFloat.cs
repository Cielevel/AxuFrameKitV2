using UnityEngine;

namespace AxuIndieModules.StatPipeline
{
    [CreateAssetMenu(fileName = "StatFloat", menuName = "AxuIndieModules/StatPipeline/Stat/StatFloat")]
    public class StatFloat : Stat<float>
    {
        protected override float FromDouble(double value) => (float)value;
    }
}