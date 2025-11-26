using System.Collections.Generic;
using AxuFrameKit._Core.Utility.Math.Interpolation;
using UnityEngine;

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
        private const int DefaultCapacity = 8;
        
        private Queue<SequenceElement> _elementQStart = new(DefaultCapacity); // 未开始队列
        private Queue<SequenceElement> _elementQEnd = new(DefaultCapacity); // 已完成队列
        private List<SequenceElement> _currentElementList = new(DefaultCapacity);
        private Stack<SequenceElement> _elementReStack; // 反转栈
        private float _totalDuration;

        public AxuSequence() : base(0f) { } // 初始 Duration 为 0

        protected override void OnStartup()
        {
            while (0 < _elementQEnd.Count)
                _elementQStart.Enqueue(_elementQEnd.Dequeue());
            
            _currentElementList.Clear();
            
            Debug.Log($"OnStart, Q:{_elementQStart.Count},S:{_elementQEnd.Count}");
        }

        // 核心组合方法
        public AxuSequence Append(IAxuTweener tween)
        {
            // 创建 Element 并记录时间
            _elementQStart.Enqueue(new SequenceElement
            {
                TargetTween = tween,
                StartTime   = _totalDuration,
                EndTime     = _totalDuration + tween.Duration
            });
        
            _totalDuration += tween.Duration; // 更新总时长
            
            Duration = _totalDuration;
            
            return this;
        }
    
        // 覆盖 Action()
        protected override void Action() // TODO 这样的结构似乎无法实现SetProgress功能（2025-11-26，暂时关闭了SetProgress功能开放）
        {
            // 此处逻辑还是有问题：一个Tweener的End与Start，在Sequence的生命周期里，应该只会调用一次。
            if (_elementQStart.TryPeek(out var element))
            {
                var tween = element.TargetTween;

                if (element.StartTime <= RunTime && RunTime <= element.EndTime) // 区间内
                {
                    tween.Startup();
                    _currentElementList.Add(element);
                    _elementQEnd.Enqueue(element);
                    _elementQStart.Dequeue();
                }
            }

            for (int i = _currentElementList.Count - 1; i >= 0; i--)
            {
                var currentElement = _currentElementList[i];

                if (currentElement.EndTime < RunTime)
                {
                    currentElement.TargetTween.SetProgress(1f);
                    _currentElementList.RemoveAt(i);
                    continue;
                }
                
                var elementTime = RunTime - currentElement.StartTime;
                var progress    = elementTime / currentElement.TargetTween.Duration; 
                
                currentElement.TargetTween.SetProgress(progress);
            }
        }
    }
}