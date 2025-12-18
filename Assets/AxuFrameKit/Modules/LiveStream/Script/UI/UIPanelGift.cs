using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AxuFrameKit._Core.Extension;
using AxuFrameKit.Common.UI.Framework;
using AxuFrameKit.Common.UI.Kit.Scroll;
using AxuFrameKit.Common.UI.Kit.DataBind;
using AxuFrameKit.Modules.LiveStream.Script.System;

namespace AxuFrameKit.Modules.LiveStream.Script.UI
{
    public class UIPanelGift : UIBasePanel
    {
        public LoopVerticalScrollRect loopVerticalScrollRect;

        [Space] 
        
        [SerializeField] private string _bindGiftName = "GIFT_NAME";
        [SerializeField] private string _bindGiftImg = "GIFT_IMG";

        [Space]

        [SerializeField] private List<GiftDataUnit> _giftDataList;
        
        public void SetUserData(IEnumerable<GiftDataUnit> giftDataCollection)
        {
            _giftDataList = giftDataCollection.ToList();
            
            loopVerticalScrollRect.onUpdateItem += OnUpdateItem;

            loopVerticalScrollRect.RefillCells(_giftDataList.Count);
            
            loopVerticalScrollRect.GetComponentsInChildren<Button>().Do(btn =>
            {
                btn.onClick.AddListener(() =>
                {
                    
                });
            });
        }

        private void OnUpdateItem(Transform item, int index)
        {
            item.GetComponent<UIBindParent>().SetData(new UIDataUnit<string>(_bindGiftName, _giftDataList[index].Name), new UIDataUnit<Sprite>(_bindGiftImg, _giftDataList[index].Img));
        }
    }
}
