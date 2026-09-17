using System.Collections.Generic;
using AxuFrameKit.Common.UI.Kit.DataBind;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.ListView
{
    /// <summary>
    /// UIListView 扩展方法
    /// 提供更便捷的数据绑定和刷新功能
    /// </summary>
    public static class UIListViewExtensions
    {
        /// <summary>
        /// 设置适配器并自动监听数据变更
        /// </summary>
        public static void SetAdapter(this UIListView listView, UIListViewAdapter adapter)
        {
            if (adapter == null)
            {
                // 解绑旧适配器订阅并清空列表
                listView.UnbindAdapter();
                listView.Clear();
                return;
            }

            // 设置初始数据
            listView.SetDataSource(adapter.GetDataList());

            // 订阅数据变更：委托缓存在 listView 内部（BindAdapter），
            // 保证同一适配器重复 SetAdapter 不叠加订阅，listView 销毁时自动退订
            listView.BindAdapter(adapter, () => OnAdapterDataChanged(listView, adapter));
        }

        private static void OnAdapterDataChanged(UIListView listView, UIListViewAdapter adapter)
        {
            listView.SetDataSource(adapter.GetDataList(), false);
        }

        /// <summary>
        /// 设置泛型适配器
        /// </summary>
        public static void SetAdapter<T>(this UIListView listView, UIListViewAdapter<T> adapter)
        {
            SetAdapter(listView, (UIListViewAdapter)adapter);
        }

        /// <summary>
        /// 快速设置数据绑定模式的数据（用于 UIListViewDataBindItem）
        /// </summary>
        public static void SetBindData(this UIListView listView, IList<IUIDataUnit[]> dataList)
        {
            var objectList = new List<object>(dataList.Count);
            foreach (var units in dataList)
                objectList.Add(units);

            listView.SetDataSource(objectList);
        }

        /// <summary>
        /// 追加数据绑定模式的数据
        /// </summary>
        public static void AppendBindData(this UIListView listView, IList<IUIDataUnit[]> dataList)
        {
            var objectList = new List<object>(dataList.Count);
            foreach (var units in dataList)
                objectList.Add(units);

            listView.AppendData(objectList);
        }

        /// <summary>
        /// 滚动到指定项并居中显示
        /// </summary>
        public static void ScrollToItemCentered(this UIListView listView, int index, bool animated = true, float duration = 0.3f)
        {
            // 通过反射或扩展方式调用内部方法
            // 由于 ScrollToItem 已经在 UIListView 中实现，这里直接调用
            listView.ScrollToItem(index, animated, duration);
        }
    }
}
