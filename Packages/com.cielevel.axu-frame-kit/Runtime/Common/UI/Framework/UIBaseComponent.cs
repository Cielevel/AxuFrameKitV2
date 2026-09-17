using UnityEngine;

namespace AxuFrameKit.Common.UI.Framework
{
    public class UIBaseComponent : MonoBehaviour
    {
        [SerializeField] private bool _autoValidate;
        
        [field: SerializeField]
        public string Name { get; set; }
        
        [field: SerializeField]
        public string ID { get; set; }
        
        [field: SerializeField]
        public bool HideContentOnStart { get; private set; }
        
        [field: SerializeField]
        public GameObject Content { get; private set; }

        /// <summary> 是否已发生过 Show/Hide/Toggle 交互；为 true 时 Start 不再执行开局隐藏，避免覆盖此前手动调用 </summary>
        private bool _interacted;

        // 修复缺陷：原实现无条件隐藏，会覆盖实例化后、Start 执行前的手动 Show/Hide/Toggle 结果
        protected virtual void Start()
        {
            if (_interacted == false)
                HideOnStartIfNeeded();
        }
    
        protected void HideOnStartIfNeeded() => Content?.SetActive(!HideContentOnStart);
        
        public virtual void Show()
        {
            _interacted = true;
            Content?.SetActive(true);
            OnShow();
        }

        public virtual void Show(params object[] objs)
        {
            _interacted = true;
            Content?.SetActive(true);

            // 空参数回落到无参回调：Manager 的 ShowPage<T>() 会传入空数组，
            // 若只重写了 OnShow() 的子类将收不到回调（与 Show() 行为不一致）
            if (objs == null || objs.Length == 0)
                OnShow();
            else
                OnShow(objs);
        }

        protected virtual void OnShow() { }
        protected virtual void OnShow(params object[] objs) { }

        public virtual void Hide()
        {
            _interacted = true;
            Content?.SetActive(false);
            OnHide();
        }

        public virtual void Hide(params object[] objs)
        {
            _interacted = true;
            Content?.SetActive(false);

            // 同 Show(params)：空参数回落到无参回调
            if (objs == null || objs.Length == 0)
                OnHide();
            else
                OnHide(objs);
        }

        protected virtual void OnHide() { }
        protected virtual void OnHide(params object[] objs) { }
        
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
            _interacted = true;
            if (Content != null && Content.activeSelf)
                Hide();
            else
                Show();
        }

        public virtual void Toggle(params object[] objs)
        {
            _interacted = true;
            if (Content != null && Content.activeSelf)
                Hide(objs);
            else
                Show(objs);
        }
        
        protected virtual void OnValidate()
        {
            if (_autoValidate == false)
                return;

            // 修复缺陷：仅空缺时自动填充，绝不覆写手工赋值的 Name/ID/Content
            if (string.IsNullOrEmpty(Name))
                Name = transform.name.Split(' ')[^1];

            if (string.IsNullOrEmpty(ID))
                ID = transform.name.ToLower().Replace(' ', '_');

            // Content 仅未赋值时查找/新建，避免每次校验都重置手工连好的引用
            if (Content == null)
            {
                Content = transform.Find("Content")?.gameObject;

                if (Content == null)
                {
                    Content = new GameObject("Content");
                    Content.transform.SetParent(transform);
                    Content.AddComponent<RectTransform>();
                    Content.transform.localPosition = Vector3.zero;
                    Content.transform.localRotation = Quaternion.identity;
                    Content.transform.localScale = Vector3.one;
                }
            }
        }
    }
}