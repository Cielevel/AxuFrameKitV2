using UnityEngine;

namespace AxuExpiremental.EventSystem
{
    [CreateAssetMenu(menuName = "Events/Player Died Event", fileName = "PlayerDiedEventChannel")]
    public class PlayerDiedEventChannel : GameEventChannel<PlayerDiedEvent> { }

    public struct PlayerDiedEvent : IEvent
    {
        public int playerId;
        public Vector3 worldPosition;

        public PlayerDiedEvent(int id, Vector3 pos)
        {
            playerId      = id;
            worldPosition = pos;
        }
    }
}