using System;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Trigger
{
    [Serializable]
    public class TriggerLimit
    {
        [Header("次数限制")]
        [Tooltip("允许触发的最大次数，-1 表示无限次")]
        [SerializeField] private int maxTriggerCount = -1;

        [Header("冷却/节流限制")]
        [Tooltip("两次触发之间的最小间隔时间（秒），0 表示无冷却")]
        [SerializeField] private float cooldownTime = 0f;

        // 运行时状态记录（无需序列化）
        private int currentTriggerCount = 0;
        private float lastTriggerTime = -999f;

        /// <summary>
        /// 检查当前是否允许触发
        /// </summary>
        public bool Pass()
        {
            // 1. 检查次数限制 (-1 为无限)
            if (maxTriggerCount >= 0 && currentTriggerCount >= maxTriggerCount)
            {
                return false;
            }

            // 2. 检查冷却时间限制 (节流)
            if (cooldownTime > 0f && (Time.time - lastTriggerTime) < cooldownTime)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 成功触发后，更新限制状态
        /// </summary>
        public void RecordTrigger()
        {
            currentTriggerCount++;
            lastTriggerTime = Time.time;
        }

        /// <summary>
        /// 重置限制状态（用于机关复位等需求）
        /// </summary>
        public void Reset()
        {
            currentTriggerCount = 0;
            lastTriggerTime = -999f;
        }

        // 常用属性暴露
        public bool IsMaxedOut => maxTriggerCount >= 0 && currentTriggerCount >= maxTriggerCount;
        public int RemainingCount => maxTriggerCount < 0 ? int.MaxValue : Mathf.Max(0, maxTriggerCount - currentTriggerCount);
    }
}
