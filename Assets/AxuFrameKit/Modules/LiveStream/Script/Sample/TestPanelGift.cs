using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream
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