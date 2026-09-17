using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using AxuFrameKit.Common.Utility.Event;

namespace AxuFrameKit.Common.Utility.Auto.AutoSequence
{
    /// <summary>
    /// 时序工具 - 不依赖 Odin Inspector 的版本
    /// 可视化时序工具，通过配置事件顺序，实现自动化。
    /// 继承Item即可添加新的时序。
    /// </summary>
    public class SequencePlayer : MonoBehaviour
    {
        [SerializeReference]
        public List<Item> itemList = new List<Item>();

        [SerializeField] private int _currentIndex;

        [SerializeField] private PlayMode _playMode;

        [SerializeField] private EventChannelCore _eventChannel;

        private enum PlayMode { Manual, Auto, ByChannel }

        private void Awake()
        {
            if (_playMode == PlayMode.ByChannel)
            {
                if (_eventChannel == null)
                {
                    Debug.LogWarning($"{nameof(SequencePlayer)}: PlayMode 为 ByChannel 但 _eventChannel 未赋值，跳过事件注册。", this);
                    return;
                }

                _eventChannel.TryRegister(Play);
            }
        }

        private void Start()
        {
            if (_playMode == PlayMode.Auto)
                Play();
        }

        private void OnDestroy()
        {
            // 未赋值时 Awake 已警告过，拆除期不再重复刷日志
            if (_playMode == PlayMode.ByChannel && _eventChannel != null)
                _eventChannel.Unregister(Play);
        }

        /// <summary>
        /// 播放序列
        /// </summary>
        public void Play()
        {
            StartCoroutine(IEPlaySequence());
        }

        /// <summary>
        /// 播放序列的协程
        /// </summary>
        public IEnumerator IEPlaySequence()
        {
            for (int i = 0; i < itemList.Count; i++)
            {
                _currentIndex = i;
                if (itemList[i] != null)
                {
                    yield return itemList[i].Run();
                }
            }
        }

        /// <summary>
        /// 序列 Item 的抽象基类
        /// 所有自定义 Item 类型都应继承此类
        /// </summary>
        [Serializable]
        public abstract class Item
        {
            /// <summary>
            /// Item 的显示名称
            /// </summary>
            public virtual string Name => string.Empty;

            /// <summary>
            /// Item 在 Inspector 中的背景颜色
            /// </summary>
            public virtual Color Color => new Color(0.23f, 0.23f, 0.23f);

            /// <summary>
            /// 执行此 Item 的逻辑
            /// </summary>
            public virtual IEnumerator Run()
            {
                yield return null;
            }
        }

        #region 通用 Item 实现

        /// <summary>
        /// 评论 Item - 仅用于 Inspector 中的注释，不执行任何逻辑
        /// </summary>
        [Serializable]
        public class Comment : Item
        {
            public override string Name => string.IsNullOrEmpty(comment) ? "COMMENT" : comment;
            public override Color Color => new Color(0.23f, 0.4f, 0.23f);

            // [TextArea(2, 5)]
            public string comment;
        }

        /// <summary>
        /// 调试消息 Item - 输出日志消息
        /// </summary>
        [Serializable]
        public class DebugMessage : Item
        {
            public override string Name => "DEBUG MESSAGE";
            public override Color Color => new Color(0.23f, 0f, 0.23f);

            public string msg;

            public override IEnumerator Run()
            {
                Debug.Log(msg);
                yield return null;
            }
        }

        /// <summary>
        /// 等待时间 Item - 等待指定的秒数
        /// </summary>
        [Serializable]
        public class WaitTime : Item
        {
            public override string Name => $"WAIT TIME ({time}s)";

            public float time = 1.0f;

            public override IEnumerator Run()
            {
                yield return new WaitForSeconds(time);
            }
        }

        /// <summary>
        /// 触发事件 Item - 触发 UnityEvent
        /// </summary>
        [Serializable]
        public class TriggerEvent : Item
        {
            public override string Name => "TRIGGER EVENT";

            public UnityEvent unityEvent;

            public override IEnumerator Run()
            {
                unityEvent?.Invoke();
                yield return null;
            }
        }

        /// <summary>
        /// 等待条件 Item - 等待直到条件满足
        /// </summary>
        [Serializable]
        public class WaitUntil : Item
        {
            public override string Name => "WAIT UNTIL";
            public override Color Color => new Color(0.4f, 0.35f, 0.23f);

            [Tooltip("等待的条件，需要在运行时通过代码设置")]
            public string conditionDescription = "描述等待的条件";

            // 条件委托，需要通过代码设置
            [NonSerialized]
            public Func<bool> condition;

            public override IEnumerator Run()
            {
                if (condition != null)
                {
                    yield return new UnityEngine.WaitUntil(condition);
                }
                else
                {
                    Debug.LogWarning($"WaitUntil: condition is null, skipping. Description: {conditionDescription}");
                    yield return null;
                }
            }
        }

        /// <summary>
        /// 设置激活状态 Item - 设置 GameObject 的激活状态
        /// </summary>
        [Serializable]
        public class SetActive : Item
        {
            public override string Name => targetObject != null
                ? $"SET ACTIVE: {targetObject.name} = {active}"
                : "SET ACTIVE";
            public override Color Color => new Color(0.3f, 0.3f, 0.5f);

            public GameObject targetObject;
            public bool active = true;

            public override IEnumerator Run()
            {
                if (targetObject != null)
                {
                    targetObject.SetActive(active);
                }
                yield return null;
            }
        }

        /// <summary>
        /// 触发事件 Item - 触发事件频道
        /// </summary>
        [Serializable]
        public class RaiseEvent : Item
        {
            public override string Name => $"RAISE EVENT: {eventChannel?.name ?? "Null"}";
            public override Color Color => new Color(0.1f, 0.3f, 0.5f);

            public EventChannelCore eventChannel;

            public override IEnumerator Run()
            {
                if (eventChannel == null)
                    Debug.LogWarning("SequencePlayer.RaiseEvent: eventChannel 未赋值，跳过本次触发。");
                else
                    eventChannel.RaiseEvent();
                yield return null;
            }
        }

        /// <summary>
        /// 触发事件 Item - 触发事件频道的默认值
        /// </summary>
        [Serializable]
        public class RaiseEventDefaultValue : Item
        {
            public override string Name => $"RAISE EVENT DEFAULT VALUE: {eventChannel?.name ?? "Null"}";
            public override Color Color => new Color(0.1f, 0.3f, 0.5f);

            public EventChannelCore eventChannel;

            public override IEnumerator Run()
            {
                if (eventChannel == null)
                    Debug.LogWarning("SequencePlayer.RaiseEventDefaultValue: eventChannel 未赋值，跳过本次触发。");
                else
                    eventChannel.RaiseEventDefaultValue();
                yield return null;
            }
        }

        #endregion

        // 游戏内可按需定制 Item，通过继承 Item 类实现
    }
}