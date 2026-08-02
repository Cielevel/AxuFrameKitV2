using UnityEngine;

namespace AxuFrameKit.Common.Utility.Trigger
{
    public abstract class AbsTriggerable : MonoBehaviour
    {
        [SerializeField] private bool _onlyTrigerOnce;

        private bool _triggered;

        public void Execute()
        {
            if (_onlyTrigerOnce && _triggered)
                return;

            if (_onlyTrigerOnce)
                _triggered = true;

            OnExecute();
        }

        protected abstract void OnExecute();
    }
}