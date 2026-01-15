using System;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream
{
    [CreateAssetMenu(fileName = "TriggerLikeConfigSo", menuName = "AxuFrameKit/LiveStream/TriggerLikeConfigSo")]
    public class TriggerLikeConfigSo : ScriptableObject
    {
        [field: SerializeField]
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
            int index = 0;
            TriggerLikeConfigUnitList?.ForEach(unit => unit.elementName = $"({index++})点赞*{unit.LikeCount}:{unit.TriggerContent?.Remark}");
        }
    }

    [Serializable]
    public class TriggerLikeConfigUnit
    {
        [HideInInspector]
        public string elementName;

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