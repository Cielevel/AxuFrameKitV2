using UnityEngine;
using UnityEngine.Events;
using AxuFrameKit.Common.Utility.Event;

namespace AxuFrameKit.Common.Utility.Auto
{
    /// <summary>
    /// 自动事件频道基类
    /// </summary>
    public abstract class AutoEventChannelBase : MonoBehaviour
    {
        [SerializeField] protected EventChannelCore _eventChannel;

        [SerializeField] protected UnityEvent _onEventRaised;
    }
}
