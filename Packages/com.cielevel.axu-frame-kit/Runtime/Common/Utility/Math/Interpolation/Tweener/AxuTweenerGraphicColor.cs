using UnityEngine;
using UnityEngine.UI;
using AxuFrameKit._Core.Utility.Math.Interpolation; // 确保引用了基础命名空间

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    public class AxuTweenerGraphicColor : AxuTweener<Graphic>
    {
        private Color _startColor;
        private Color _endColor;

        public AxuTweenerGraphicColor(float duration, Graphic target, Color endValue) : base(duration)
        {
            _target   = target;
            _endColor = endValue;
        }

        public void SetEnd(Color endValue) => _endColor = endValue;

        protected override void OnStartup()
        {
            if (_target != null)
                _startColor = _target.color;
        }

        protected override void Action()
        {
            if (_target == null) return;

            // 使用 Color.Lerp 进行插值，_t 是经过 Ease 曲线处理的进度
            Color currentColor = Color.Lerp(_startColor, _endColor, _t);

            // 赋值到 Graphic.color
            _target.color = currentColor;
        }
    }
}