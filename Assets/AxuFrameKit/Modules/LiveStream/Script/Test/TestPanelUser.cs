using System;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit.Modules.LiveStream.Script.UI;
using AxuFrameKit.Modules.LiveStream.Script.System.Unit;

namespace AxuFrameKit.Modules.LiveStream.Script.Test
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