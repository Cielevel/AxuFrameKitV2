using System.Collections.Generic;
using AxuFrameKit._Core.Advanced.ObjectPool;
using AxuFrameKit._Core.Structure;
using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.UI.Framework;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.Toast
{
    public enum PushDirection
    {
        Up,
        Down
    }

    /// <summary>
    /// UI侧边弹出（左右），非侵入式提示（Toast），T为弹出的UI-Unit
    /// </summary>
    public abstract class UISideToast<T> : UIBasePanel where T : UIToastUnit // T 约束为 UIToastUnit
    {
        private const int DefaultCapacity = 16;
        
        [Header("Unit配置")]
        [SerializeField] private T _prefabUnit;
        [SerializeField] private Vector2 _unitSize;

        [Header("位置配置")] 
        [SerializeField] private Transform _parentGroup;
        [SerializeField] private Vector2 _startAnchoredPos; // Unit 进场的起始位置
        [SerializeField] private Vector2 _endAnchoredPos;   // Unit 进场的最终位置

        [Header("纵向挤压配置")] 
        [SerializeField] private float _verticalInterval; // 纵向挤压时，Unit之间的间隔
        [SerializeField] private int _maxShowingCount = 2; // 纵向上最多同时显示几个Unit
        [SerializeField] private PushDirection _pushDirection;

        [Header("Tween设置")]
        [SerializeField] private EaseType _easeType = EaseType.OutQuad;
        [SerializeField] private float _tweenInDuration = 0.3f;
        [SerializeField] private float _tweenOutDuration = 0.3f;
        [SerializeField] private float _stayDuration = 2.0f;

        private SimpleObjPool<T> _toastPool;

        // 用于跟踪当前显示的 Unit 列表
        private readonly IndexedList<T> _showingUnitList = new(DefaultCapacity);

        protected override void Start()
        {
            base.Start();

            // PoolOnGet 和 PoolOnRelease 不再进行动画操作，只负责通知 Unit 状态改变
            _toastPool = new SimpleObjPool<T>(PoolConstructor, PoolOnGet, PoolOnRelease);
        }

        private T PoolConstructor()
        {
            var unit = Instantiate(_prefabUnit, _parentGroup);

            // 继承 UIBaseComponent 的 Unit 属性设置
            unit.RtTsf.anchoredPosition = _startAnchoredPos;
            unit.RtTsf.sizeDelta        = _unitSize;

            // 配置 Unit 的动画参数
            unit.TweenInDuration  = _tweenInDuration;
            unit.TweenOutDuration = _tweenOutDuration;
            unit.StayDuration     = _stayDuration;
            unit.TweenEaseType    = _easeType;

            return unit;
        }

        private void PoolOnGet(T unit)
        {
            // 在 Unit 生命周期完成后，它会通过 OnLifetimeCompleted 事件调用这个方法，从而实现回收
            unit.OnLifetimeCompleted = () => ReleaseUnit(unit);

            // 通知 Unit 启动生命周期，并告诉它进场位置
            unit.OnGet(_startAnchoredPos, _endAnchoredPos);
        }

        private void PoolOnRelease(T unit)
        {
            // 清除事件，防止回收的 Unit 再次触发 Release
            unit.OnLifetimeCompleted = null;

            // 通知 Unit 进入回收状态
            unit.OnRelease();
        }

        /// <summary>
        /// 弹出提示
        /// </summary>
        public void Toast()
        {
            // 1. 处理挤压：移动已存在的 Unit 并强制回收超出最大数量的 Unit
            PushShowingUnits();

            // 2. 从对象池获取新的 Unit
            var newUnit = _toastPool.Get();

            // 3. 将新 Unit 添加到跟踪列表的最前面 (索引 0)
            _showingUnitList.Insert(0, newUnit);

            OnToast(newUnit);
        }

        /// <summary>
        /// 移动当前显示的 Unit，并回收超出限制的 Unit
        /// </summary>
        private void PushShowingUnits()
        {
            // 1. 计算需要立即回收的超出数量（预留一个位置给即将弹出的新 Unit）
            var allowedBeforeInsert = _maxShowingCount - 1;
            var overflow = Mathf.Max(0, _showingUnitList.Count - allowedBeforeInsert);
        
            // 2. 先移动所有当前显示的 Unit（每个 Unit 都需要被挤压到对应位置）
            for (var i = 0; i < _showingUnitList.Count; i++)
            {
                var unit = _showingUnitList[i];
                var targetPos = GetTargetAnchoredPos(i + 1);
        
                // 通知 Unit 移动
                unit.Push(targetPos);
            }
        
            // 3. 对于超出的 Unit，立即触发回收并从显示列表中移除（它们仍然会执行移动动画）
            for (var r = 0; r < overflow; r++)
            {
                var idx = _showingUnitList.Count - 1;
                var unitToRecycle = _showingUnitList[idx];
        
                // 强制 Unit 启动退场并通知自身回收
                unitToRecycle.ForceRecycle();
        
                // 从列表中移除，实际回收由 Unit 的 OnLifetimeCompleted 事件处理
                _showingUnitList.RemoveAt(idx);
            }
        }

        /// <summary>
        /// 根据列表索引获取 Unit 的锚点位置
        /// 索引 0 是新弹出的 Unit (_endAnchoredPos)，索引 1 开始是被挤压的 Unit
        /// </summary>
        private Vector2 GetTargetAnchoredPos(int index)
        {
            // 计算纵向偏移量: (Unit 高度 + 间隔) * 索引 (index=1 为第一个被挤压的 Unit)
            var offsetY = (_unitSize.y + _verticalInterval) * index;

            var targetPos = _endAnchoredPos;

            // 根据 PushDirection 调整纵向位置
            if (_pushDirection == PushDirection.Up)
            {
                // 向下挤压：Unit 向上移动
                targetPos.y += offsetY;
            }
            else // PushDirection.Down
            {
                // 向上挤压：Unit 向下移动
                targetPos.y -= offsetY;
            }

            return targetPos;
        }

        /// <summary>
        /// Unit 退场动画完成后调用的回收方法
        /// </summary>
        private void ReleaseUnit(T unit)
        {
            // 如果 Unit 还在显示列表中，说明它是被 PushShowingUnits 移除的，则这里不需要再次移除
            if (_showingUnitList.Contains(unit))
                _showingUnitList.Remove(unit);

            // 将 Unit 释放回对象池
            _toastPool.Release(unit);
        }

        protected abstract void OnToast(T unit);
    }
}
