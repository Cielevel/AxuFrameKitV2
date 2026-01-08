using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream
{
    public class TestPanelUser : MonoBehaviour
    {
        public List<UserDataUnit> testUserDataList;

        public UIPanelUser panel;

        private void Start()
        {
            panel.SetUserData(testUserDataList);
        }
    }
}