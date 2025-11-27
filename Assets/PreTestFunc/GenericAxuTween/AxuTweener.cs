// using System;
// using AxuFrameKit._Core.Utility.Math.Interpolation;
//
// namespace PreTestFunc.GenericAxuTween
// {
//     public enum TweenerState
//     {
//         Running, Paused, Completed
//     }
//
//     public interface IAxuTweener
//     {
//         TweenerState State { get; }
//         
//         float Duration { get; }
//         float RunTime { get; } // 0~Duration
//         float Progress { get; } // 0~1
//
//         void Startup();
//
//         void Tik(float deltaTime);
//
//         void SetDuration(float duration);
//
//         void SetProgress(float progress);
//
//         #region State
//
//         void Replay();
//
//         void Pause();
//
//         void Resume();
//
//         void Kill();
//
//         #endregion
//     }
//     
//     // TSelf是作用目标类型（Transform，Image）；TTarget是作用值的类型（Vec3，float）
//     public abstract class AxuTweener<TSelf, TTarget> : IAxuTweener
//     {
//         // --public--
//         public float Duration { get; private set; }
//         public float RunTime { get; private set; }  // 0~Duration
//         public float Progress { get; private set; } // 0~1
//         public TweenerState State { get; private set; } = TweenerState.Paused;
//         
//         // --protected--
//         protected float _t;
//         
//         // --private--
//         private TSelf _self;
//         private TTarget _start, _target;
//         private EaseType _easeType;
//         private Action _onComplete;
//         private IEase _ease = EaseFactory.Get(EaseType.InCubic);
//         
//         // --inner public--
//         public AxuTweener<TSelf, TTarget> SetEase(EaseType easeType)
//         {
//             _easeType = easeType;
//
//             _ease = EaseFactory.Get(_easeType);
//
//             return this;
//         }
//         
//         public void SetTarget(TTarget target) => _target = target;
//         
//         // --inner private--
//         private static float Clamp01(float value) => System.Math.Clamp(value, 0, 1);
//         private void ApplyCurrentProgress()
//         {
//             if (Duration <= 0) // 避免除以零
//                 return;
//
//             Progress = Clamp01(RunTime / Duration);
//
//             _t = _ease.Evaluate(Progress);
//             
//             Action();
//         }
//         
//         // --interface--
//
//         void IAxuTweener.Startup() => OnStartup();
//
//         void IAxuTweener.Tik(float deltaTime)
//         {
//             if (State != TweenerState.Running)
//                 return;
//
//             RunTime += deltaTime;
//
//             if (0 < Duration && Duration <= RunTime) // 检查是否完成
//             {
//                 RunTime = Duration;
//                 ApplyCurrentProgress();
//
//                 State = TweenerState.Completed;
//                 _onComplete?.Invoke();
//                 return;
//             }
//             
//             ApplyCurrentProgress();
//         }
//
//         void IAxuTweener.SetProgress(float progress)
//         {
//             progress = Clamp01(progress);
//             
//             RunTime = progress * Duration;
//             
//             Progress = progress;
//             
//             if (0 < Duration && Duration <= RunTime) // 检查是否完成
//             {
//                 RunTime = Duration; 
//                 ApplyCurrentProgress();
//
//                 State = TweenerState.Completed;
//                 _onComplete?.Invoke();
//             }
//             else
//                 ApplyCurrentProgress();
//         }
//
//         public void SetDuration(float duration)
//         {
//             Duration = duration;
//         }
//
//         // --abstract--
//         protected abstract void Action();
//         protected abstract void OnStartup(); // Start时，Replay时
//     }
// }