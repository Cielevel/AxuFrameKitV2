using UnityEngine;
using UnityEngine.Events;
using AxuFrameKit.Common.Utility.Event;

namespace AxuFrameKit.Common.Utility.Auto
{
    public abstract class AutoEventChannelBase : MonoBehaviour
    {
        [SerializeField] protected EventChannelCore _eventChannel;

        [SerializeField] protected UnityEvent _onEventRaised;
    }
}
