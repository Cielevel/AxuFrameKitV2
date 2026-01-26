using System.Collections.Generic;
using AxuFrameKit.Modules.LiveStream;
using UnityEngine;

namespace TestAndSample.Sample_Modules.LiveStream
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