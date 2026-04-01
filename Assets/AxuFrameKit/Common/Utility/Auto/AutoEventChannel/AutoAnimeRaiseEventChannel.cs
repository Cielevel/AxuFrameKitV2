using UnityEngine;
using AxuFrameKit.Common.Utility.Event;

namespace AxuFrameKit.Common.Utility.Auto
{
    /// <summary>
    /// 适用于Unity-Animation事件的自动Channel
    /// </summary>
    public class AutoAnimeRaiseEventChannel : MonoBehaviour
    {
        [SerializeField] protected EventChannelCore _eventChannel;

        public void RaiseEvent() => _eventChannel.RaiseEvent();
    }
}