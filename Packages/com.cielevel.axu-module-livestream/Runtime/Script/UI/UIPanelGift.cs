using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AxuFrameKit._Core.Extension;
using AxuFrameKit.Common.UI.Framework;
using AxuFrameKit.Common.UI.Kit.Scroll;
using AxuFrameKit.Common.UI.Kit.DataBind;

namespace AxuFrameKit.Modules.LiveStream
{
    public class UIPanelGift : UIBasePanel
    {
        public UILoopVerticalScrollRect uiLoopVerticalScrollRect;

        [Space]

        [SerializeField] private string _bindGiftName = "GIFT_NAME";
        [SerializeField] private string _bindGiftImg = "GIFT_IMG";

        [Space]

        [SerializeField] private List<GiftDataUnit> _giftDataList;

        public void SetUserData(IEnumerable<GiftDataUnit> giftDataCollection)
        {
            _giftDataList = giftDataCollection.ToList();

            uiLoopVerticalScrollRect.onUpdateItem += OnUpdateItem;

            uiLoopVerticalScrollRect.RefillCells(_giftDataList.Count);

            uiLoopVerticalScrollRect.GetComponentsInChildren<Button>().Do(btn =>
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
