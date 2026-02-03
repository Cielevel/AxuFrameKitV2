using System;
using UnityEngine;
using UnityEngine.Events;

namespace AxuExpiremental.EventSystem
{
    public abstract class GameEventChannelBase : ScriptableObject
    {
        // base marker for Resources load filtering and editor helpers
    }
    
    public abstract class GameEventChannel<T> : GameEventChannelBase where T : IEvent
    {
        [Header("Options")]
        public bool logOnRaise = false;

        // runtime C# listeners
        private event Action<T> _onRaised;

        [SerializeField]
        private GameEventUnityEvent<T> _onRaisedUnity = new GameEventUnityEvent<T>();

        // Raise from code
        public void Raise(T evt)
        {
            if (logOnRaise)
                Debug.Log($"[EventChannel] {name} Raise: {typeof(T).Name}");

            // Unity callbacks
            try { _onRaisedUnity?.Invoke(evt); } catch (Exception ex) { Debug.LogException(ex); }

            // C# listeners
            try { _onRaised?.Invoke(evt); } catch (Exception ex) { Debug.LogException(ex); }

            // forward to global bus
            EventBus.Publish(evt);
        }

        // C# registration
        public void Register(Action<T> listener) => _onRaised += listener;
        public void Unregister(Action<T> listener) => _onRaised -= listener;

        // UnityEvent registration
        public void RegisterUnity(UnityAction<T> listener) => _onRaisedUnity.AddListener(listener);
        public void UnregisterUnity(UnityAction<T> listener) => _onRaisedUnity.RemoveListener(listener);

        // Expose UnityEvent for Inspector read-only
        public GameEventUnityEvent<T> GetUnityEvent() => _onRaisedUnity;
    }
}