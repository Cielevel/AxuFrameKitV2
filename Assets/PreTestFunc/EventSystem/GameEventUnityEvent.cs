using System;
using UnityEngine.Events;

namespace PreTestFunc.EventSystem
{
    [Serializable]
    public class GameEventUnityEvent<T> : UnityEvent<T> { }
}