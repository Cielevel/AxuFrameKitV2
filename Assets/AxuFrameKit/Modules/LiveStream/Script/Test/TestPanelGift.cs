using System;
using System.Collections.Generic;
using AxuFrameKit.Modules.LiveStream.Script.System;
using AxuFrameKit.Modules.LiveStream.Script.UI;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream.Script.Test
{
    public class TestPanelGift : MonoBehaviour
    {
        public List<GiftDataUnit> testGiftDataList;

        public UIPanelGift panel;

        private void Start()
        {
            panel.SetUserData(testGiftDataList);
        }
    }
}