using System;
using UnityEngine;

namespace AxuIndieModules.StatPipeline.Stat
{
    [CreateAssetMenu(fileName = "StatInt", menuName = "AxuIndieModules/StatPipeline/Stat/StatInt")]
    public class StatInt : Stat<int>
    {
        protected override int FromDouble(double value)
        {
            // 可以定义你的舍入策略：Floor，Ceil，Round
            return (int)Math.Round(value);
        }
    }
}