using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit.Common.Utility.UseMono;
using AxuFrameKit.Common.Utility.Analysis;
using AxuFrameKit.Common.Utility.Static;

namespace AxuFrameKit.Common.Utility.Auto.AutoSequence
{
    // 使用IUseMonoBehaviour
    // 简易的序列操作

    /// <summary>
    /// 简易的时序操作；适用于静态构建，动态运行。
    /// 目前也支持动态构建，动态运行，但不推荐。
    /// 支持运行时动态添加操作，新增操作会在当前操作完成后依次执行
    /// </summary>
    public class SimpleSequence : IUseMonoBehaviourSeparate
    {
        private enum SequenceUnitType { Action, Wait, CustomWait, Enumerator }

        private bool _autoDestroy = false;

        private bool _isLoop = false;

        /// <summary>
        /// 统一的操作单元，自包含类型和对应数据
        /// </summary>
        private struct SequenceUnit
        {
            public SequenceUnitType Type;
            public Action Action;
            public YieldInstruction YieldInstruction;
            public CustomYieldInstruction CustomYieldInstruction;
            public IEnumerator Enumerator;
        }

        private List<SequenceUnit> _sequenceUnits;

        private Coroutine _coroutine;
        private Coroutine _loopCoroutine;

        public SimpleSequence(int initialCapacity = 32)
        {
            _sequenceUnits = new List<SequenceUnit>(initialCapacity);
        }

        public SimpleSequence Do(Action action) // 对于可能需要参数的委托，尝试使用闭包来替代
        {
            _sequenceUnits.Add(new SequenceUnit
            {
                Type = SequenceUnitType.Action,
                Action = action
            });
            return this;
        }

        public SimpleSequence Debug(string message)
        {
            _sequenceUnits.Add(new SequenceUnit
            {
                Type = SequenceUnitType.Action,
                Action = () => Debugger.Log(message)
            });
            return this;
        }

        public SimpleSequence WaitSeconds(float seconds)
        {
            _sequenceUnits.Add(new SequenceUnit
            {
                Type = SequenceUnitType.Wait,
                YieldInstruction = new WaitForSeconds(seconds)
            });
            return this;
        }

        public SimpleSequence WaitRealSeconds(float seconds)
        {
            _sequenceUnits.Add(new SequenceUnit
            {
                Type = SequenceUnitType.CustomWait,
                CustomYieldInstruction = new WaitForSecondsRealtime(seconds)
            });
            return this;
        }

        public SimpleSequence WaitFixedUpdate()
        {
            _sequenceUnits.Add(new SequenceUnit
            {
                Type = SequenceUnitType.Wait,
                YieldInstruction = new WaitForFixedUpdate()
            });
            return this;
        }

        public SimpleSequence WaitAFrame()
        {
            _sequenceUnits.Add(new SequenceUnit
            {
                Type = SequenceUnitType.Wait,
                YieldInstruction = null
            });
            return this;
        }

        public SimpleSequence WaitFrameEnd()
        {
            _sequenceUnits.Add(new SequenceUnit
            {
                Type = SequenceUnitType.Wait,
                YieldInstruction = EnumeratorWait.EndOfFrame
            });
            return this;
        }

        public SimpleSequence WaitWhile(Func<bool> condition)
        {
            _sequenceUnits.Add(new SequenceUnit
            {
                Type = SequenceUnitType.CustomWait,
                CustomYieldInstruction = new WaitWhile(condition)
            });
            return this;
        }

        public SimpleSequence WaitUntil(Func<bool> condition)
        {
            _sequenceUnits.Add(new SequenceUnit
            {
                Type = SequenceUnitType.CustomWait,
                CustomYieldInstruction = new WaitUntil(condition)
            });
            return this;
        }

        public SimpleSequence WaitEnumerator(IEnumerator enumerator)
        {
            _sequenceUnits.Add(new SequenceUnit
            {
                Type = SequenceUnitType.Enumerator,
                Enumerator = enumerator
            });
            return this;
        }

        public SimpleSequence AutoDestroy()
        {
            _autoDestroy = true;
            return this;
        }

        public void Stop()
        {
            if (_coroutine != null)
            {
                this.StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }
        public void Run(bool forceRestart = false)
        {
            if (forceRestart)
            {
                StopLoop();
                Stop();
            }

            _isLoop = false;

            _coroutine = this.StartCoroutine(IESequence());
        }

        public void StopLoop()
        {
            if (_loopCoroutine != null)
            {
                this.StopCoroutine(_loopCoroutine);
                _loopCoroutine = null;
            }
        }
        public void Loop(float interval, bool forceRestart = false)
        {
            if (forceRestart)
            {
                StopLoop();
                Stop();
            }

            _isLoop = true;

            _loopCoroutine = this.StartCoroutine(LoopAction());
            return;

            IEnumerator LoopAction()
            {
                WaitForSeconds wait = new(interval);
                while (true)
                {
                    yield return this.StartCoroutine(IESequence());
                    yield return wait;
                }
            }
        }

        private IEnumerator IESequence()
        {
            // 使用索引遍历，支持运行时动态添加新操作
            // 新添加的操作会追加到列表末尾，索引 i 始终指向正确的 SequenceUnit
            for (int i = 0; i < _sequenceUnits.Count; i++)
            {
                var unit = _sequenceUnits[i];
                switch (unit.Type)
                {
                    case SequenceUnitType.Action:
                        unit.Action?.Invoke();
                        break;
                    case SequenceUnitType.Wait:
                        yield return unit.YieldInstruction;
                        break;
                    case SequenceUnitType.CustomWait:
                        yield return unit.CustomYieldInstruction;
                        break;
                    case SequenceUnitType.Enumerator:
                        yield return this.StartCoroutine(unit.Enumerator);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (!_isLoop && _autoDestroy)
                this.Destroy();
            else if (!_isLoop)
                _coroutine = null;
        }

        // TODO inverseRun的可能性？
    }
}