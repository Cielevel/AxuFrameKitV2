using UnityEngine;
using AxuIndieModules.StatPipeline;

namespace AxuIndieModules.StatPipeline.Temp
{
    public class SampleStatPipeline : MonoBehaviour
    {
        [SerializeField] private StatInt _attack;
        [SerializeField] private StatDouble _health;

        [Space]

        [SerializeField] private StatModifierBase _atkModifier1;
        [SerializeField] private StatModifierBase _atkModifier2;
        [SerializeField] private StatModifierBase _healthModifier;

        [Space]

        [SerializeField] private StatModifierLink _atkLinkModifier;

        private void Start()
        {
            _attack = _attack.Clone();
            _health = _health.Clone();

            _attack.RegisterModifier(_atkModifier1.Clone());
            _health.RegisterModifier(_healthModifier.Clone());
            // _attack.RegisterModifier(_atkModifier2.Clone());

            _atkLinkModifier = _atkLinkModifier.Clone() as StatModifierLink;
            _atkLinkModifier.RegisterStat(_health);

            _attack.RegisterModifier(_atkLinkModifier);

            Debug.Log("Attack: " + _attack.Value);

            StatDebugger.LogCalculatePipeline(_health);
            StatDebugger.LogCalculatePipeline(_attack);
        }
    }
}