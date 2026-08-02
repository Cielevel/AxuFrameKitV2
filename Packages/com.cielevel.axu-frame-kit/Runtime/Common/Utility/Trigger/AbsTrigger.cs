using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Trigger
{
    public abstract class AbsTrigger : MonoBehaviour
    {
        [SerializeField] private List<AbsTriggerable> triggerables;

        public void Trigger()
        {
            foreach (var triggerable in triggerables)
            {
                triggerable.Execute();
            }
        }
    }
}