using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Advanced.ObjectPool;
using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Timer
{
    public interface ICdManager
    {
        void StartCd(float time, Action onComplete);
        void StartCd(float time, Action<float> onTik, Action onComplete);
    }
    
    /// <summary>
    /// CountDownManager，倒计时管理器；倒计时数量多时推荐使用。
    /// </summary>
    public class CdManager : SingletonMonoCrDs<CdManager>, ICdManager
    {
        private const int DefaultCapacity = 32;
        
        private SimpleObjPool<ICdUnit> _cdPool;

        private List<ICdUnit> _cdRunningList;

        private int RunningCount => _cdRunningList.Count;
        private int LastCount => RunningCount - 1;

        protected override void Awake()
        {
            base.Awake();

            _cdPool        = new SimpleObjPool<ICdUnit>(Constructor, OnGet, OnRelease, DefaultCapacity);
            _cdRunningList = new List<ICdUnit>(DefaultCapacity);
        }

        private void Update()
        {
            for (int i = 0; i < _cdRunningList.Count; i++)
            {
                _cdRunningList[i].Tik(Time.deltaTime);

                if (_cdRunningList[i].IsCompleted())
                    OnCdComplete(i);
            }
        }

        #region Pool Method

        private ICdUnit Constructor() => new CdUnit();

        private void OnGet(ICdUnit cdUnit) => cdUnit.Reset();

        private void OnRelease(ICdUnit cdUnit) => cdUnit.Reset();

        #endregion

        void ICdManager.StartCd(float time, Action onComplete)
        {
            var unit = _cdPool.Get();
            
            unit.OnComplete += onComplete;
            
            unit.SetTime(time);
            
            _cdRunningList.Add(unit);
        }
        
        void ICdManager.StartCd(float time, Action<float> onTik, Action onComplete)
        {
            var unit = _cdPool.Get();
            
            unit.OnTik      += onTik;
            unit.OnComplete += onComplete;
            
            unit.SetTime(time);
            
            _cdRunningList.Add(unit);
        }
        
        private void OnCdComplete(int index)
        {
            if (index != LastCount)
                (_cdRunningList[index], _cdRunningList[LastCount]) = (_cdRunningList[LastCount], _cdRunningList[index]);

            _cdRunningList[LastCount].SetComplete();
            
            _cdPool.Release(_cdRunningList[LastCount]);
            
            _cdRunningList.RemoveAt(LastCount);
        }

        private interface ICdUnit
        {
            event Action<float> OnTik;
            event Action OnComplete;

            void SetTime(float time);
            void Tik(float deltaTime);
            
            bool IsCompleted();

            void SetComplete();

            void Reset();
        }
        
        private class CdUnit : ICdUnit
        {
            public event Action<float> OnTik;

            public event Action OnComplete;

            public float Time { get; private set; }

            public void SetTime(float time) => Time = time;

            public void Tik(float deltaTime)
            {
                Time -= deltaTime;
                
                OnTik?.Invoke(Time);
            }

            public bool IsCompleted() => Time <= 0f;

            public void SetComplete()
            {
                OnComplete?.Invoke();

                Time = 0f;
                
                OnTik      = delegate { };
                OnComplete = delegate { };
            }

            public void Reset()
            {
                Time = 0f;
                
                OnTik      = delegate { };
                OnComplete = delegate { };
            }
        }
    }
}