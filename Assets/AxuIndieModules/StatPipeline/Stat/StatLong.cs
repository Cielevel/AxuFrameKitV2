using UnityEngine;

namespace AxuIndieModules.StatPipeline.Stat
{
    [CreateAssetMenu(fileName = "StatLong", menuName = "AxuIndieModules/StatPipeline/Stat/StatLong")]
    public class StatLong : Stat<long>
    {
        /// <summary>
        /// 如果业务逻辑保证不超过 2^53，这里是安全的
        /// </summary>
        protected override long FromDouble(double value) => (long)value;
    }
}