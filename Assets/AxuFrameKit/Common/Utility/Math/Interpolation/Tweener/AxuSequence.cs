using System.Collections.Generic;
using AxuFrameKit._Core.Utility.Math.Interpolation;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    public struct SequenceElement
    {
        public IAxuTweener TargetTween { get; set; } // 要执行的 Tweener
        public float StartTime { get; set; }       // 在 Sequence 时间轴上开始的绝对时间
        public float EndTime { get; set; }         // 在 Sequence 时间轴上结束的绝对时间
    }
    
    public class AxuSequence : AxuTweener
    {
        private List<SequenceElement> _elementList = new();
        private float _totalDuration; 

        public AxuSequence() : base(0f) // 初始 Duration 为 0
        {
        }

        // 核心组合方法
        public AxuSequence Append(IAxuTweener tween)
        {
            // 创建 Element 并记录时间
            _elementList.Add(new SequenceElement
            {
                TargetTween = tween,
                StartTime   = _totalDuration,
                EndTime     = _totalDuration + tween.Duration
            });
        
            _totalDuration = _elementList[^1].EndTime; // 更新总时长
            
            // 【修正点 1】更新基类的 Duration，解决 Duraton=0 导致的瞬间完成问题
            Duration = _totalDuration; 
            
            return this;
        }
    
        // 覆盖 Action()
        protected override void Action()
        {
            // 获取 Sequence 自身的运行时间 (已缓动并钳制后的 _t * TotalDuration)
            float currentSequenceTime = _t * _totalDuration; 
        
            // 遍历所有元素，并手动控制它们的进度
            for (int i = 0; i < _elementList.Count; i++)
            {
                SequenceElement element = _elementList[i];
                IAxuTweener tween = element.TargetTween;

                if (currentSequenceTime >= element.StartTime && currentSequenceTime <= element.EndTime)
                {
                    // 动画正在进行中
                    
                    // 计算元素在其时间轴上的绝对时间
                    float elementTime = currentSequenceTime - element.StartTime;
                    
                    // 计算元素在其时间轴上的归一化进度 (0~1)
                    float progress = elementTime / tween.Duration; 
                    
                    // 【修正点 2】直接通过 SetProgress 设置进度，而不是试图计算 deltaTime
                    // SetProgress 现在会自动调用 ApplyCurrentProgress() 来更新动画值
                    tween.SetProgress(progress);
                }
                else if (currentSequenceTime > element.EndTime)
                {
                    // 动画已完成
                    // 确保动画停在终点 (t=1)
                    tween.SetProgress(1f); 
                }
                else // currentSequenceTime < element.StartTime
                {
                    // 动画尚未开始
                    // 确保动画停在起点 (t=0)
                    tween.SetProgress(0f);
                }
            }
        }
    }
}