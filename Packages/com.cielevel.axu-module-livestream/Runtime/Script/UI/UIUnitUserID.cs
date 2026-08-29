using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream
{
    public class UIUnitUserID : MonoBehaviour
    {
        public string UserID => _userID;

        [SerializeField] private string _userID;

        public void SetUserID(string userID) => _userID = userID;
    }
}