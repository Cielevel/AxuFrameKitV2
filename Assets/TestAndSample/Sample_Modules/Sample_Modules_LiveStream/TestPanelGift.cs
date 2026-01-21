using System.Collections.Generic;
using AxuFrameKit.Modules.LiveStream;
using UnityEngine;

namespace TestAndSample.Sample_Modules.Sample_Modules_LiveStream
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