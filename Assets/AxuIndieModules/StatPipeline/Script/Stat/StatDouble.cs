using UnityEngine;

namespace AxuIndieModules.StatPipeline
{
    [CreateAssetMenu(fileName = "StatDouble", menuName = "AxuIndieModules/StatPipeline/Stat/StatDouble")]
    public class StatDouble : Stat<double>
    {
        protected override double FromDouble(double value) => value;
    }
}