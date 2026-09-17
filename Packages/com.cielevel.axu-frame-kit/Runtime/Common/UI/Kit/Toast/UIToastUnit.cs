using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using AxuFrameKit.Common.UI.Framework;
using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.Utility.Math.Interpolation;

namespace AxuFrameKit.Common.UI.Kit.Toast
{
    // 定义一个状态，用于ToastUnit的生命周期管理
    public enum ToastState
    {
        In,
        Stay,
        Out,
        Recycled
    }

    [RequireComponent(typeof(CanvasGroup))]
    public class UIToastUnit : UIBaseUnit
    {
        [Header("Toast Unit 组件")]
        [SerializeField] private CanvasGroup _canvasGroup;
        
        private Coroutine _lifetimeCoroutine;

        // 外部配置参数，由 UISideToast 设置
        public float TweenInDuration { get; set; }
        public float TweenOutDuration { get; set; }
        public float StayDuration { get; set; }
        public EaseType TweenEaseType { get; set; }
        
        // 状态
        public ToastState CurrentState { get; private set; } = ToastState.Recycled;
        
        // 生命周期完成后的事件，用于通知 UISideToast 回收自身
        public UnityAction OnLifetimeCompleted { get; set; }

        private AxuTweener<RectTransform> _moveTweenerX;
        private AxuTweener<RectTransform> _moveTweenerY;
        private AxuTweener<CanvasGroup> _fadeTweener;

        // Unit 被从对象池取出时调用 (相当于 PoolOnGet)
        public void OnGet(Vector2 startPos, Vector2 endPos)
        {
            CurrentState = ToastState.In;

            // 直接控制物体激活（Content 可能为空，不能只依赖 Show/Hide 的 Content 逻辑）
            gameObject.SetActive(true);
            Show();

            // 确保 Unit 在起始位置，并且不透明
            RtTsf.anchoredPosition = startPos;
            _canvasGroup.alpha     = 1f;

            // 启动进场动画
            _moveTweenerX = RtTsf
                           .ToAnchoredPosX(endPos.x, TweenInDuration)
                           .SetEase(TweenEaseType)
                           .Start();

            // 启动生命周期管理协程
            _lifetimeCoroutine = StartCoroutine(ToastUnitLifetime());
        }
        
        // Unit 被释放到对象池时调用 (相当于 PoolOnRelease)
        public void OnRelease()
        {
            CurrentState = ToastState.Recycled;

            // 直接关闭物体激活：基类 Hide 只做 Content?.SetActive(false)，Content 为空时物体会保持激活
            gameObject.SetActive(false);

            // 停止所有正在进行的动画和协程
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
                _lifetimeCoroutine = null;
            }

            // 确保动画被停止 (防止在回收后继续运行)
            _moveTweenerX?.Kill(); 
            _moveTweenerY?.Kill(); 
            _fadeTweener?.Kill();
        }

        // 挤压移动：在其他 Unit 弹出时，被挤压的 Unit 调用此方法移动
        public void Push(Vector2 targetPos) // push方法可能短时间内会被调用多次，所以需要在使用前kill
        {
            _moveTweenerY?.Kill();
            // 只有在 In 或 Stay 状态才允许被挤压
            if (CurrentState == ToastState.In || CurrentState == ToastState.Stay)
            {
                _moveTweenerY = RtTsf
                                .ToAnchoredPosY(targetPos.y, TweenInDuration)
                                .SetEase(TweenEaseType)
                                .Start();
            }
        }
        
        // 强制退场：在达到最大显示数量时，最底下的 Unit 被强制调用此方法
        public void ForceRecycle()
        {
            // 停止当前的生命周期协程，直接启动退场
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
                _lifetimeCoroutine = null;
            }
            StartRecycle();
        }

        // Unit 的完整生命周期：进场 -> 停留 -> 退场 -> 回收
        private IEnumerator ToastUnitLifetime()
        {
            // 1. 等待进场动画完成
            yield return new WaitForSeconds(TweenInDuration);

            // 2. 停留时间
            CurrentState = ToastState.Stay;
            yield return new WaitForSeconds(StayDuration);

            // 3. 启动退场
            StartRecycle();
        }

        // 启动 Unit 的退场动画并通知外部回收
        private void StartRecycle()
        {
            CurrentState = ToastState.Out;

            _fadeTweener = _canvasGroup
                           .ToAlpha(0f, TweenOutDuration)
                           .SetEase(TweenEaseType)
                           .OnComplete(() =>
                           {
                               // 动画完成后，通知 UISideToast 回收自身
                               OnLifetimeCompleted?.Invoke();
                           })
                           .Start();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}