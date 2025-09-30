using System.Collections.Generic;
using System.Linq;
using AxuFrameKit.Common.Utility.Singleton;

namespace AxuFrameKit.Common.UI.Framework
{
    public class UIBaseManager<T> : SingletonMono<T> where T : UIBaseManager<T>
    {
        private List<UIBasePage> _uiPageList;

        private List<UIBaseArea> _uiAreaList;
        
        protected override void Awake()
        {
            base.Awake();
            
            _uiPageList = transform.GetComponentsInChildren<UIBasePage>().ToList();
            _uiAreaList = transform.GetComponentsInChildren<UIBaseArea>().ToList();
        }
        
        public void TogglePage<T1>(params object[] p) where T1 : UIBasePage => _uiPageList.Find(e => e is T1).Toggle(p);
        public void ShowPage<T1>() where T1 : UIBasePage => _uiPageList.Find(e => e is T1).Show();
        public void ShowPage<T1>(params object[] p) where T1 : UIBasePage => _uiPageList.Find(e => e is T1).Show(p);
        public void HidePage<T1>() where T1 : UIBasePage => _uiPageList.Find(e => e is T1).Hide();
        public void HidePage<T1>(params object[] p) where T1 : UIBasePage => _uiPageList.Find(e => e is T1).Hide(p);
        public bool IsPageShow<T1>() where T1 : UIBasePage => _uiPageList.Find(e => e is T1).Content.activeSelf;
        
        public void ToggleArea<T1>(params object[] p) where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1).Toggle(p);
        public void ShowArea<T1>() where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1).Show();
        public void ShowArea<T1>(params object[] p) where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1).Show(p);
        public void HideArea<T1>() where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1).Hide();
        public void HideArea<T1>(params object[] p) where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1).Hide(p);
        public bool IsAreaShow<T1>() where T1 : UIBaseArea => _uiAreaList.Find(e => e is T1).Content.activeSelf;
        
        public T1 GetPage<T1>() where T1 : UIBasePage
        {
            var area = _uiPageList.Find(e => e is T1);
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
    }
}