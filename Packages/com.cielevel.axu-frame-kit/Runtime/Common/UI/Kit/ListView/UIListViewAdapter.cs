using System;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.ListView
{
    /// <summary>
    /// ListView 数据适配器基类
    /// 封装数据管理和刷新逻辑，支持自定义模式或 DataBind 模式
    /// 
    /// 使用方式：
    /// 1. 继承此类并实现 GetData 方法
    /// 2. 在 Unity Inspector 中配置 UIListView
    /// 3. 调用 SetAdapter 方法设置适配器
    /// </summary>
    public abstract class UIListViewAdapter
    {
        /// <summary>
        /// 数据变更事件
        /// </summary>
        public event Action OnDataChanged;

        /// <summary>
        /// 获取指定索引的数据
        /// </summary>
        public abstract object GetData(int index);

        /// <summary>
        /// 获取数据总数
        /// </summary>
        public abstract int GetCount();

        /// <summary>
        /// 获取数据列表（用于批量设置）
        /// </summary>
        public abstract IList<object> GetDataList();

        /// <summary>
        /// 通知数据已变更，触发刷新
        /// </summary>
        protected void NotifyDataChanged()
        {
            OnDataChanged?.Invoke();
        }
    }

    /// <summary>
    /// 泛型 ListView 适配器
    /// </summary>
    public class UIListViewAdapter<T> : UIListViewAdapter
    {
        private IList<T> _dataList;

        /// <summary>
        /// 当前数据列表
        /// </summary>
        public IList<T> DataList => _dataList;

        public UIListViewAdapter(IList<T> dataList = null)
        {
            _dataList = dataList ?? new List<T>();
        }

        /// <summary>
        /// 设置数据列表
        /// </summary>
        public void SetData(IList<T> dataList)
        {
            _dataList = dataList ?? new List<T>();
            NotifyDataChanged();
        }

        /// <summary>
        /// 追加数据
        /// </summary>
        public void AppendData(IList<T> dataList)
        {
            if (dataList == null)
                return;

            foreach (var item in dataList)
                _dataList.Add(item);

            NotifyDataChanged();
        }

        /// <summary>
        /// 添加单个数据
        /// </summary>
        public void Add(T item)
        {
            _dataList.Add(item);
            NotifyDataChanged();
        }

        /// <summary>
        /// 移除数据
        /// </summary>
        public bool Remove(T item)
        {
            var result = _dataList.Remove(item);
            if (result)
                NotifyDataChanged();
            return result;
        }

        /// <summary>
        /// 在指定位置插入数据
        /// </summary>
        public void Insert(int index, T item)
        {
            _dataList.Insert(index, item);
            NotifyDataChanged();
        }

        /// <summary>
        /// 移除指定位置的数据
        /// </summary>
        public void RemoveAt(int index)
        {
            _dataList.RemoveAt(index);
            NotifyDataChanged();
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        public void Clear()
        {
            _dataList.Clear();
            NotifyDataChanged();
        }

        /// <summary>
        /// 更新指定位置的数据
        /// </summary>
        public void UpdateItem(int index, T item)
        {
            if (index < 0 || index >= _dataList.Count)
                return;

            _dataList[index] = item;
            NotifyDataChanged();
        }

        #region UIListViewAdapter Implementation

        public override object GetData(int index)
        {
            if (index < 0 || index >= _dataList.Count)
                return null;
            return _dataList[index];
        }

        public override int GetCount() => _dataList.Count;

        public override IList<object> GetDataList()
        {
            var result = new List<object>(_dataList.Count);
            foreach (var item in _dataList)
                result.Add(item);
            return result;
        }

        #endregion
    }
}
