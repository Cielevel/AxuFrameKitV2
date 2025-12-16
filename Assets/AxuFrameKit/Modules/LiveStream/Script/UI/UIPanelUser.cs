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
        [SerializeField] private LoopVerticalScrollRect _loopVerticalScrollRect;

        [Space] 
        
        [SerializeField] private UIPanelComment _uiPanelComment;
        
        [Space] 
        
        [SerializeField] private string _bindUserNickname = "PLAYER_NICKNAME";
        [SerializeField] private string _bindUserAvatar = "USER_AVATAR";

        [Space]

        [SerializeField] private List<UserDataUnit> _userDataList;
        
        private Dictionary<string, UserDataUnit> _userDataDict;
        
        public void SetUserData(IEnumerable<UserDataUnit> userDataCollection)
        {
            _userDataList = userDataCollection.ToList();

            _userDataDict = _userDataList.ToDictionary(e => e.ID);
            
            _loopVerticalScrollRect.onUpdateItem += OnUpdateItem;

            _loopVerticalScrollRect.RefillCells(_userDataList.Count);
            
            _loopVerticalScrollRect.GetComponentsInChildren<Button>().Do(btn =>
            {
                btn.onClick.AddListener(() =>
                {
                    var id = btn.GetComponent<UIUnitUserID>().UserID;
                    if (_userDataDict.TryGetValue(id, out var userData))
                        _uiPanelComment.OnSelectUser(userData.Nickname);
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
