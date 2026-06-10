using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Multiton
{
    /// <summary>
    /// Manager 所管理的子项接口
    /// </summary>
    public interface IManagerItem
    {
        /// <summary>
        /// 是否已完成，为 true 时 Manager 将在当帧 Tick 后自动移除并调用 OnDispose
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// 被 Manager 接管时调用（Register 时入队，下帧正式接管前调用）
        /// </summary>
        void OnCreate();

        /// <summary>
        /// 每帧由 Manager 驱动
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// IsComplete 为 true 的当帧，Tick 之后调用
        /// 仅正常完成时触发，强制移除时不触发
        /// </summary>
        void OnComplete();

        /// <summary>
        /// 从 Manager 中移除时调用，无论正常完成还是强制移除
        /// 用于释放资源
        /// </summary>
        void OnDispose();
    }

    /// <summary>
    /// 单例 Manager 基类
    /// 统一调度一批 IManagerItem，自动驱动其生命周期
    /// </summary>
    /// <typeparam name="TManager">Manager 自身类型（CRTP）</typeparam>
    /// <typeparam name="TItem">被管理的子项类型，需实现 IManagerItem</typeparam>
    public abstract class Manager<TManager, TItem> : MonoBehaviour
        where TManager : Manager<TManager, TItem>
        where TItem : class, IManagerItem
    {
        // ─────────────────────────────────────────────────────────────
        // 单例
        // ─────────────────────────────────────────────────────────────

        private static TManager _instance;

        public static TManager Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                var go = new GameObject($"[{typeof(TManager).Name}]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<TManager>();
                _instance.OnManagerCreated();
                return _instance;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 子项管理
        // ─────────────────────────────────────────────────────────────

        private readonly List<TItem> _items = new(8);

        // Tick 遍历期间缓冲新注册的子项，避免修改正在遍历的列表
        private readonly Queue<TItem> _pendingAdd = new();

        private bool _isDestroyed;

        /// <summary>
        /// 注册一个子项
        /// 若在 Tick 期间调用，实际接管延迟到当帧 Tick 结束后
        /// </summary>
        public void Register(TItem item)
        {
            if (item == null)
            {
                Debug.LogError($"{typeof(TManager).Name}: Register item cannot be null.");
                return;
            }

            if (_isDestroyed)
            {
                Debug.LogWarning($"{typeof(TManager).Name}: Manager 已销毁，无法注册新子项。");
                return;
            }

            _pendingAdd.Enqueue(item);
        }

        /// <summary>
        /// 强制移除一个子项，触发 OnDispose，不触发 OnComplete
        /// </summary>
        public void Unregister(TItem item)
        {
            if (item == null) return;

            int index = _items.IndexOf(item);
            if (index < 0) return;

            item.OnDispose();
            _items.RemoveAt(index);
        }

        /// <summary>
        /// 强制移除所有子项，每项触发 OnDispose，不触发 OnComplete
        /// </summary>
        public void UnregisterAll()
        {
            DisposeAllItems();
        }

        /// <summary>
        /// 当前管理的子项数量
        /// </summary>
        public int ItemCount => _items.Count;

        // ─────────────────────────────────────────────────────────────
        // Unity 生命周期
        // ─────────────────────────────────────────────────────────────

        private void Update()
        {
            FlushPendingAdd();

            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];

                item.Tick(Time.deltaTime);

                if (item.IsComplete)
                {
                    item.OnComplete();
                    item.OnDispose();
                    _items.RemoveAt(i);
                }
            }

            OnManagerUpdate();
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            _instance = null;

            DisposeAllItems();
            OnManagerDestroyed();
        }

        // ─────────────────────────────────────────────────────────────
        // 内部工具
        // ─────────────────────────────────────────────────────────────

        private void FlushPendingAdd()
        {
            while (_pendingAdd.Count > 0)
            {
                var item = _pendingAdd.Dequeue();
                _items.Add(item);
                item.OnCreate();
            }
        }

        private void DisposeAllItems()
        {
            while (_pendingAdd.Count > 0)
                _pendingAdd.Dequeue().OnDispose();

            for (int i = _items.Count - 1; i >= 0; i--)
                _items[i].OnDispose();

            _items.Clear();
        }

        // ─────────────────────────────────────────────────────────────
        // 子类可覆写的虚方法
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Manager 实例首次创建时调用
        /// </summary>
        protected virtual void OnManagerCreated() { }

        /// <summary>
        /// 每帧所有子项 Tick 之后调用
        /// </summary>
        protected virtual void OnManagerUpdate() { }

        /// <summary>
        /// Manager 销毁时调用，此时所有子项已完成 OnDispose
        /// </summary>
        protected virtual void OnManagerDestroyed() { }
    }
}