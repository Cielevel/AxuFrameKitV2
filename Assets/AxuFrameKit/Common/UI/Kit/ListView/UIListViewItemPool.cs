using System;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.ListView
{
    /// <summary>
    /// ListView Item对象池
    /// 职责：
    /// - 管理Item对象的创建和复用
    /// - 减少频繁的Instantiate和Destroy操作
    /// - 提升ListView性能
    /// </summary>
    public class UIListViewItemPool : IDisposable
    {
        private readonly Stack<UIListViewItem> _pool = new Stack<UIListViewItem>();
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly List<UIListViewItem> _allCreatedItems = new List<UIListViewItem>();

        /// <summary>
        /// 对象池中可用的Item数量
        /// </summary>
        public int Count => _pool.Count;

        /// <summary>
        /// 总共创建的Item数量（包括正在使用的）
        /// </summary>
        public int TotalCreated => _allCreatedItems.Count;

        public UIListViewItemPool(GameObject prefab, Transform parent)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            _prefab = prefab;
            _parent = parent;
        }

        /// <summary>
        /// 从对象池获取一个Item
        /// </summary>
        public UIListViewItem Get()
        {
            UIListViewItem item;
            bool isNewlyCreated = false;

            if (_pool.Count > 0)
            {
                item = _pool.Pop();
                item.gameObject.SetActive(true);
            }
            else
            {
                var itemGo = UnityEngine.Object.Instantiate(_prefab, _parent);
                item = itemGo.GetComponent<UIListViewItem>();

                if (item == null)
                {
                    Debug.LogError($"Prefab '{_prefab.name}' must have a UIListViewItem component!");
                    item = itemGo.AddComponent<UIListViewItem>();
                }

                _allCreatedItems.Add(item);
                isNewlyCreated = true;
            }

            // 触发显示生命周期回调
            item.OnShowInternal();
            
            // 新创建的 Item 返回标记
            if (isNewlyCreated)
                item.IsNewlyCreated = true;

            return item;
        }

        /// <summary>
        /// 将Item归还到对象池
        /// </summary>
        public void Return(UIListViewItem item)
        {
            if (item == null)
                return;

            // 触发隐藏生命周期回调
            item.OnHideInternal();
            
            // 重置创建标记
            item.IsNewlyCreated = false;
            
            item.gameObject.SetActive(false);
            _pool.Push(item);
        }

        /// <summary>
        /// 预创建指定数量的Item
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = Get();
                Return(item);
            }
        }

        /// <summary>
        /// 清空对象池并销毁所有Item
        /// </summary>
        public void Clear()
        {
            // 销毁所有创建的Item
            foreach (var item in _allCreatedItems)
            {
                if (item != null && item.gameObject != null)
                    UnityEngine.Object.Destroy(item.gameObject);
            }

            _pool.Clear();
            _allCreatedItems.Clear();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Clear();
        }
    }
}
