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

            // 收集逻辑统一收敛到 EnsureCollected：Awake 首次填充与后续惰性补齐共用同一份实现
            EnsureCollected();
        }

        /// <summary>
        /// 惰性收集：任一公开 API 入口调用时若列表尚未填充（例如外部早于本组件 Awake 调用），按 Awake 同款方式补齐。
        /// 修复缺陷：原实现仅在 Awake 填充一次，早于 Awake 调用会因列表为 null 抛 NRE。
        /// </summary>
        private void EnsureCollected()
        {
            if (_uiPageList == null)
                _uiPageList = transform.GetComponentsInChildren<UIBasePage>(_includeInactive).ToList();
            if (_uiAreaList == null)
                _uiAreaList = transform.GetComponentsInChildren<UIBaseArea>(_includeInactive).ToList();
        }

        #region 私有查找核心

        /// <summary>
        /// 页面查找核心（全部页面公开 API 统一委托至此）：
        /// 存活判定（已销毁组件经 Unity 假 null 判定剔除，其 is T1 仍为 true，必须显式判活）+ is T1 + id 为 null 或相等。
        /// 不加约束以同时服务无 where 约束的 IsPageShow&lt;T1&gt;(string id) 重载。
        /// </summary>
        private UIBasePage FindPage<T1>(string id)
        {
            EnsureCollected();
            return _uiPageList.Find(e => e != null && e is T1 && (id == null || e.ID == id));
        }

        /// <summary>
        /// 区域查找核心（全部区域公开 API 统一委托至此），过滤规则同 FindPage。
        /// </summary>
        private UIBaseArea FindArea<T1>(string id)
        {
            EnsureCollected();
            return _uiAreaList.Find(e => e != null && e is T1 && (id == null || e.ID == id));
        }

        #endregion

        #region PAGE

        // toggle page
        public void TogglePage<T1>(params object[] p) where T1 : UIBasePage => FindPage<T1>(null)?.Toggle(p);
        public void TogglePage<T1>(string id, params object[] p) where T1 : UIBasePage => FindPage<T1>(id)?.Toggle(p);

        // show page
        public void ShowPage<T1>() where T1 : UIBasePage => FindPage<T1>(null)?.Show();
        public void ShowPage<T1>(string id) where T1 : UIBasePage => FindPage<T1>(id)?.Show();
        public void ShowPage<T1>(params object[] p) where T1 : UIBasePage => FindPage<T1>(null)?.Show(p);
        public void ShowPage<T1>(string id, params object[] p) where T1 : UIBasePage => FindPage<T1>(id)?.Show(p);

        // hide page
        public void HidePage<T1>() where T1 : UIBasePage => FindPage<T1>(null)?.Hide();
        public void HidePage<T1>(string id) where T1 : UIBasePage => FindPage<T1>(id)?.Hide();
        public void HidePage<T1>(params object[] p) where T1 : UIBasePage => FindPage<T1>(null)?.Hide(p);
        public void HidePage<T1>(string id, params object[] p) where T1 : UIBasePage => FindPage<T1>(id)?.Hide(p);

        // is page show
        public bool IsPageShow<T1>() where T1 : UIBasePage => FindPage<T1>(null)?.Content?.activeSelf ?? false;

        // 行为修正：原实现 id 重载仅按 id 匹配、未过滤 T1（与无参重载语义不一致），现统一走 FindPage<T1> 补上类型过滤
        public bool IsPageShow<T1>(string id) => FindPage<T1>(id)?.Content?.activeSelf ?? false;

        #endregion

        #region AREA

        // toggle area
        public void ToggleArea<T1>(params object[] p) where T1 : UIBaseArea => FindArea<T1>(null)?.Toggle(p);
        public void ToggleArea<T1>(string id, params object[] p) where T1 : UIBaseArea => FindArea<T1>(id)?.Toggle(p);

        // show area
        public void ShowArea<T1>() where T1 : UIBaseArea => FindArea<T1>(null)?.Show();
        public void ShowArea<T1>(string id) where T1 : UIBaseArea => FindArea<T1>(id)?.Show();
        public void ShowArea<T1>(params object[] p) where T1 : UIBaseArea => FindArea<T1>(null)?.Show(p);
        public void ShowArea<T1>(string id, params object[] p) where T1 : UIBaseArea => FindArea<T1>(id)?.Show(p);

        // hide area
        public void HideArea<T1>() where T1 : UIBaseArea => FindArea<T1>(null)?.Hide();
        public void HideArea<T1>(string id) where T1 : UIBaseArea => FindArea<T1>(id)?.Hide();
        public void HideArea<T1>(params object[] p) where T1 : UIBaseArea => FindArea<T1>(null)?.Hide(p);
        public void HideArea<T1>(string id, params object[] p) where T1 : UIBaseArea => FindArea<T1>(id)?.Hide(p);

        // is area show
        public bool IsAreaShow<T1>() where T1 : UIBaseArea => FindArea<T1>(null)?.Content?.activeSelf ?? false;

        // 行为修正：原实现 id 重载仅按 id 匹配、未过滤 T1（与无参重载语义不一致），现统一走 FindArea<T1> 补上类型过滤
        public bool IsAreaShow<T1>(string id) => FindArea<T1>(id)?.Content?.activeSelf ?? false;

        #endregion

        #region 注册/注销（供动态实例化的页面/区域显式登记）

        /// <summary>
        /// 登记动态实例化（非 Manager 子物体）的页面，登记后各页面 API 即可命中；重复登记与已销毁/null 引用会被跳过。
        /// </summary>
        public void RegisterPage(UIBasePage page)
        {
            if (page == null) return; // 过滤 null 与已销毁组件（Unity 假 null）
            EnsureCollected();
            if (_uiPageList.Contains(page)) return; // 判重跳过，避免重复登记
            _uiPageList.Add(page);
        }

        /// <summary> 注销页面：注销后各页面 API 不再命中（对 Manager 子物体同样生效）。 </summary>
        public void UnregisterPage(UIBasePage page)
        {
            if (page == null) return;
            EnsureCollected();
            _uiPageList.Remove(page);
        }

        /// <summary>
        /// 登记动态实例化（非 Manager 子物体）的区域，登记后各区域 API 即可命中；重复登记与已销毁/null 引用会被跳过。
        /// </summary>
        public void RegisterArea(UIBaseArea area)
        {
            if (area == null) return;
            EnsureCollected();
            if (_uiAreaList.Contains(area)) return; // 判重跳过，避免重复登记
            _uiAreaList.Add(area);
        }

        /// <summary> 注销区域：注销后各区域 API 不再命中（对 Manager 子物体同样生效）。 </summary>
        public void UnregisterArea(UIBaseArea area)
        {
            if (area == null) return;
            EnsureCollected();
            _uiAreaList.Remove(area);
        }

        #endregion

        public T1 GetPage<T1>() where T1 : UIBasePage => FindPage<T1>(null) as T1;

        public T1 GetPage<T1>(string id) where T1 : UIBasePage => FindPage<T1>(id) as T1;

        public T1 GetArea<T1>() where T1 : UIBaseArea => FindArea<T1>(null) as T1;

        public T1 GetArea<T1>(string id) where T1 : UIBaseArea => FindArea<T1>(id) as T1;
    }
}
