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

        public void PushLike(UserDataLike unit)
        {
            if (TriggerLikeConfigUnitList is { Count: > 0 }) // 默认只取第一个
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