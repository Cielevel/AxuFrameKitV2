using UnityEngine;
using AxuIndieModules.StatPipeline;

namespace AxuIndieModules.StatPipeline.Temp
{
    public class SampleStatPipeline : MonoBehaviour
    {
        [SerializeField] private StatInt _attack;

        private void Start()
        {
            Debug.Log(_attack.Value);
        }
    }
}
