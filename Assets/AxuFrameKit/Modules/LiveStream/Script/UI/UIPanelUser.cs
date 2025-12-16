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
    public class UIPanelUser : UIBasePanel
    {
        public LoopVerticalScrollRect loopVerticalScrollRect;

        [Space] 
        
        [SerializeField] private string _bindUserNickname = "PLAYER_NICKNAME";
        [SerializeField] private string _bindUserAvatar = "USER_AVATAR";

        [Space]

        [SerializeField] private List<UserDataUnit> _userDataList;
        
        public void SetUserData(IEnumerable<UserDataUnit> userDataCollection)
        {
            _userDataList = userDataCollection.ToList();
            
            loopVerticalScrollRect.onUpdateItem += OnUpdateItem;

            loopVerticalScrollRect.RefillCells(_userDataList.Count);
            
            loopVerticalScrollRect.GetComponentsInChildren<Button>().Do(btn =>
            {
                btn.onClick.AddListener(() =>
                {
                    Debug.Log("CLICK: " + btn.GetComponent<UIUnitUserID>().UserID);
                });
            });
        }

        private void OnUpdateItem(Transform item, int index)
        {
            item.GetComponent<UIBindParent>().SetData(new UIDataUnit<string>(_bindUserNickname, _userDataList[index].Nickname), new UIDataUnit<Sprite>(_bindUserAvatar, _userDataList[index].Avatar));
            
            item.GetComponent<UIUnitUserID>().SetUserID(_userDataList[index].ID);
        }
    }
}
