using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AxuFrameKit.Common.Utility.Singleton;

namespace AxuFrameKit.Common.UI.Framework
{
    public class UIBaseManager<T> : SingletonMono<T> where T : UIBaseManager<T>
    {
        [SerializeField] private bool _includeInactive = true;

        private List<UIBasePage> _uiPageList;

        private List<UIBaseArea> _uiAreaList;

        protected override void Awake()
        {
            base.Awake();
            
            _uiPageList = transform.GetComponentsInChildren<UIBasePage>(_includeInactive).ToList();
            _uiAreaList = transform.GetComponentsInChildren<UIBaseArea>(_includeInactive).ToList();
        }

        #region PAGE

        // toggle page
        public void TogglePage<T1>(params object[] p) where T1 : UIBasePage => _uiPageList.Find(e => e is T1)?.Toggle(p);
        public void TogglePage<T1>(string id, params object[] p) where T1 : UIBasePage => _uiPageList.Find(e => e is T1 && e.ID == id)?.Toggle(p);

        // show page
        public void ShowPage<T1>() where T1 : UIBasePage => _uiPageList.Find(e => e is T1)?.Show();
        public void ShowPage<T1>(string id) where T1 : UIBasePage => _uiPageList.Find(e => e is T1 && e.ID == id)?.Show();
        public void ShowPage<T1>(params object[] p) where T1 : UIBasePage => _uiPageList.Find(e => e is T1)?.Show(p);
        public void ShowPage<T1>(string id, params object[] p) where T1 : UIBasePage => _uiPageList.Find(e => e is T1 && e.ID == id)?.Show(p);

        // hide page
        public void HidePage<T1>() where T1 : UIBasePage => _uiPageList.Find(e => e is T1)?.Hide();
        public void HidePage<T1>(string id) where T1 : UIBasePage => _uiPageList.Find(e => e is T1 && e.ID == id)?.Hide();
        public void HidePage<T1>(params object[] p) where T1 : UIBasePage => _uiPageList.Find(e => e is T1)?.Hide(p);
        public void HidePage<T1>(string id, params object[] p) where T1 : UIBasePage => _uiPageList.Find(e => e is T1 && e.ID == id)?.Hide(p);

        // is page show
        public bool IsPageShow<T1>() where T1 : UIBasePage => _uiPageList.Find(e => e is T1)?.Content.activeSelf ?? false;
        public bool IsPageShow<T1>(string id) => _uiPageList.Find(e => e.ID == id)?.Content.activeSelf ?? false;

        #endregion

        #region AREA

        // toggle area
        public void ToggleArea<T1>(params object[] p) where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1)?.Toggle(p);
        public void ToggleArea<T1>(string id, params object[] p) where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1 && e.ID == id)?.Toggle(p);

        // show area
        public void ShowArea<T1>() where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1)?.Show();
        public void ShowArea<T1>(string id) where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1 && e.ID == id)?.Show();
        public void ShowArea<T1>(params object[] p) where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1)?.Show(p);
        public void ShowArea<T1>(string id, params object[] p) where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1 && e.ID == id)?.Show(p);

        // hide area
        public void HideArea<T1>() where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1)?.Hide();
        public void HideArea<T1>(string id) where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1 && e.ID == id)?.Hide();
        public void HideArea<T1>(params object[] p) where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1)?.Hide(p);
        public void HideArea<T1>(string id, params object[] p) where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1 && e.ID == id)?.Hide(p);

        // is area show
        public bool IsAreaShow<T1>() where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1)?.Content.activeSelf ?? false;
        public bool IsAreaShow<T1>(string id) where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1 && e.ID == id)?.Content.activeSelf ?? false;

        #endregion
        
        public T1 GetPage<T1>() where T1 : UIBasePage
        {
            var area = _uiPageList.Find(e => e is T1);
            if (area != null)
                return area as T1;
            return null;
        }
        
        public T1 GetPage<T1>(string id) where T1 : UIBasePage
        {
            var area = _uiPageList.Find(e => e is T1 && e.ID == id);
            if (area != null)
                return area as T1;
            return null;
        }
        
        public T1 GetArea<T1>() where T1 : UIBaseArea
        {
            var area = _uiAreaList.Find(e => e is T1);
            if (area != null)
                return area as T1;
            return null;
        }
        
        public T1 GetArea<T1>(string id) where T1 : UIBaseArea
        {
            var area = _uiAreaList.Find(e => e is T1 && e.ID == id);
            if (area != null)
                return area as T1;
            return null;
        }
    }
}