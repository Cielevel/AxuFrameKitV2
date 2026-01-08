using System;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream
{
    [CreateAssetMenu(fileName = "TriggerLikeConfigSo", menuName = "AxuFrameKit/LiveStream/TriggerLikeConfigSo")]
    public class TriggerLikeConfigSo : ScriptableObject
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
                TriggerLikeConfigUnitList[0].TriggerContent.Trigger(unit, unit.LikeCount);
        }

        private void OnValidate()
        {
            foreach (var item in TriggerLikeConfigUnitList)
                item.triggerRemark = $"点赞x{item.LikeCount}:{item.TriggerContent?.Remark}";
        }
    }

    [Serializable]
    public class TriggerLikeConfigUnit
    {
        [HideInInspector]
        public string triggerRemark;

        [Header("Like Setting")]

        [SerializeField]
        private int _likeCount;
        public int LikeCount => _likeCount;

        [Header("Trigger")]

        [SerializeField]
        private LiveTriggerLikeBase _trigger;
        public LiveTriggerLikeBase TriggerContent => _trigger;
    }
}