using UnityEngine;

// Optional helper: find all GameEventChannelBase assets under Resources/EventChannels
// and optionally log them. Useful as a startup sanity check in large projects.
namespace AxuExpiremental.EventSystem
{
    public class AutoRegisterEventChannels : MonoBehaviour
    {
        [Tooltip("If true, channels under Resources/EventChannels will be logged on Awake")]
        public bool logFoundChannels = true;

        void Awake()
        {
            // Resources.LoadAll requires the assets to be inside a Resources folder.
            var channels = Resources.LoadAll<GameEventChannelBase>("EventChannels");
            if (logFoundChannels)
            {
                Debug.Log($"[AutoRegisterEventChannels] Found {channels.Length} channels.");
                foreach (var c in channels)
                    Debug.Log($" - {c.name} ({c.GetType().Name})");
            }

            // Nothing else to do: channels forward to EventBus when raised.
        }
    }
}