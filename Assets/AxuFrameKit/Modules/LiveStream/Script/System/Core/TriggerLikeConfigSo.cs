using System;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit.Common.CustomAttribute;
using AxuFrameKit.Modules.LiveStream.Script.System.Unit;

namespace AxuFrameKit.Modules.LiveStream.Script.System.Base
{
    [CreateAssetMenu(fileName = "TriggerLikeConfigUnitSo", menuName = "AxuFrameKit/LiveStream/TriggerLikeConfigUnitSo")]
    public class TriggerLikeConfigUnitSo : ScriptableObject
    {
        [field: SerializeField, ListDrawerSettings(ListElementLabelName = "TriggerRemark")]
        public List<TriggerLikeConfigUnit> TriggerLikeConfigUnitList { get; private set; } = new();

        public bool IsDefault { get; private set; } = false;
        public bool IsSelected { get; private set; } = false;

        public void Select() => IsSelected = true;
        public void UnSelect() => IsSelected = false;

        public void PushLike(UserDataLike unit) // TODO 点赞目前只会处理第一个指令
        {
            if (TriggerLikeConfigUnitList is { Count: > 0 })
                TriggerLikeConfigUnitList[0].LiveTriggerLike.Trigger(unit, unit.LikeCount);
        }

        private void OnValidate()
        {
            foreach (var item in TriggerLikeConfigUnitList)
            {
                item.triggerRemark = $"点赞x{item.LikeCount}:{item.LiveTriggerLike?.Remark}";
            }
        }
    }

    [Serializable]
    public class TriggerLikeConfigUnit
    {
        [HideInInspector]
        public string triggerRemark;

        [field: SerializeField]
        public int LikeCount { get; private set; } = 1;

        [field: SerializeField]
        public LiveTriggerLikeBase LiveTriggerLike { get; private set; }
    }
}