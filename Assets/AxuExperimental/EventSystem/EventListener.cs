using System;
using UnityEngine;
using UnityEngine.Events;

// A reusable MonoBehaviour that binds to a GameEventChannel<T> in inspector
// and calls a UnityEvent<T> when the channel raises. Allows one-shot and
// auto-unregister on disable.
namespace AxuExperimental.EventSystem
{
    public class EventListener<T, TChannel> : MonoBehaviour where T : struct, IEvent where TChannel : GameEventChannel<T>
    {
        [Tooltip("Drop the EventChannel asset here (ScriptableObject)")]
        public TChannel channel;

        [Tooltip("Unity event to invoke when the channel raises")]
        public UnityEvent<T> onEvent = new UnityEvent<T>();

        [Tooltip("If true, this listener will automatically unregister when disabled")]
        public bool autoUnregisterOnDisable = true;

        [Tooltip("If true, this listener will run once and auto-unregister after firing once")]
        public bool oneShot = false;

        private Action<T> _handler;

        protected virtual void OnEnable()
        {
            if (channel == null) return;
            _handler = (T evt) =>
            {
                try { onEvent?.Invoke(evt); }
                catch (Exception ex) { Debug.LogException(ex); }

                if (oneShot)
                {
                    channel.Unregister(_handler);
                }
            };

            channel.Register(_handler);
        }

        protected virtual void OnDisable()
        {
            if (channel != null && autoUnregisterOnDisable && _handler != null)
                channel.Unregister(_handler);
        }
    }
}