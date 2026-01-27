using UnityEngine;

namespace AxuIndieModules.StatPipeline.Stat
{
    [CreateAssetMenu(fileName = "StatDouble", menuName = "AxuIndieModules/StatPipeline/Stat/StatDouble")]
    public class StatDouble : Stat<double>
    {
        protected override double FromDouble(double value) => value;
    }
}