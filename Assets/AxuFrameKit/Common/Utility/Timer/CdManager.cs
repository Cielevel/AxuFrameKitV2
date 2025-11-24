using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Advanced.ObjectPool;
using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Timer
{
    /// <summary>
    /// CountDownManager，倒计时管理器；倒计时数量多时推荐使用。
    /// </summary>
    public class CdManager : SingletonMonoCrDs<CdManager>
    {
        private SimpleObjPool<ICdUnit> _cdPool;

        private List<ICdUnit> _cdRunningList;

        private int RunningCount => _cdRunningList.Count;
        private int LastCount => RunningCount - 1;
        
        private void Update()
        {
            for (int i = 0; i < _cdRunningList.Count; i++)
            {
                _cdRunningList[i].Tik(Time.deltaTime);

                if (_cdRunningList[i].IsCompleted())
                    OnCdComplete(i);
            }
        }

        public void StartTimer(Action onComplete)
        {
            var unit = _cdPool.Get();
            
            unit.OnComplete += onComplete;
            
            _cdRunningList.Add(unit);
        }
        
        public void StartTimer(Action<float> onTik, Action onComplete)
        {
            var unit = _cdPool.Get();
            
            unit.OnTik      += onTik;
            unit.OnComplete += onComplete;
            
            _cdRunningList.Add(unit);
        }
        
        private void OnCdComplete(int index)
        {
            if (index != LastCount)
                (_cdRunningList[index], _cdRunningList[RunningCount - 1]) = (_cdRunningList[RunningCount - 1], _cdRunningList[index]);

            _cdRunningList[LastCount].SetComplete();
            _cdRunningList.RemoveAt(LastCount);
        }

        private interface ICdUnit
        {
            event Action<float> OnTik;
            event Action OnComplete;

            void Tik(float deltaTime);
            
            bool IsCompleted();

            void SetComplete();
        }
        
        private class CdUnit : ICdUnit
        {
            public event Action<float> OnTik;

            public event Action OnComplete;

            public float Time { get; private set; }

            public void Tik(float deltaTime)
            {
                Time -= deltaTime;
                
                OnTik?.Invoke(Time);
            }

            public bool IsCompleted() => Time <= 0f;

            public void SetComplete()
            {
                OnComplete?.Invoke();

                OnTik      = delegate { };
                OnComplete = delegate { };
            }
        }
    }
}