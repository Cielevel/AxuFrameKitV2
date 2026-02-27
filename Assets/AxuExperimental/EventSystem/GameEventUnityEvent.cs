using System;
using UnityEngine.Events;

namespace AxuExperimental.EventSystem
{
    [Serializable]
    public class GameEventUnityEvent<T> : UnityEvent<T> { }
}