using System;
using UnityEngine.Events;

namespace AxuExpiremental.EventSystem
{
    [Serializable]
    public class GameEventUnityEvent<T> : UnityEvent<T> { }
}