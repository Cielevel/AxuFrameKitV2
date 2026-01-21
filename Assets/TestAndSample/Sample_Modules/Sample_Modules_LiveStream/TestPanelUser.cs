using System.Collections.Generic;
using AxuFrameKit.Modules.LiveStream;
using UnityEngine;

namespace TestAndSample.Sample_Modules.Sample_Modules_LiveStream
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