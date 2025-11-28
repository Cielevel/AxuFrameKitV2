using UnityEngine;

namespace AxuFrameKit.Common.UI.Framework
{
    public class UIBaseComponent : MonoBehaviour
    {
        [field: SerializeField]
        public string Name { get; set; }
        
        [field: SerializeField]
        public bool HideContentOnStart { get; private set; }
        
        [field: SerializeField]
        public GameObject Content { get; private set; }

        protected virtual void Start() => HideOnStartIfNeeded();
    
        protected void HideOnStartIfNeeded() => Content?.SetActive(!HideContentOnStart);
        
        public virtual void Show() => Content?.SetActive(true);
        public virtual void Show(params object[] objs) => Content?.SetActive(true);

        public virtual void Hide() => Content?.SetActive(false);
        public virtual void Hide(params object[] objs) => Content?.SetActive(false);
        
        public RectTransform RtTsf
        {
            get
            {
                if (_rtTsf == null) 
                    _rtTsf = GetComponent<RectTransform>();

                return _rtTsf;
            }
        }

        private RectTransform _rtTsf;
        
        public virtual void Toggle()
        {
            if (Content.activeSelf)
                Hide();
            else
                Show();
        }
        
        public virtual void Toggle(params object[] objs)
        {
            if (Content.activeSelf)
                Hide(objs);
            else
                Show(objs);
        }
        
        protected virtual void OnValidate()
        {
            Name = transform.name.Split(' ')[^1];
            
            Content = transform.Find("Content")?.gameObject;

            if (Content == null)
            {
                Content = new GameObject("Content");
                Content.transform.SetParent(transform);
                Content.transform.localPosition = Vector3.zero;
                Content.transform.localRotation = Quaternion.identity;
                Content.transform.localScale = Vector3.one;
            }
        }
    }
}