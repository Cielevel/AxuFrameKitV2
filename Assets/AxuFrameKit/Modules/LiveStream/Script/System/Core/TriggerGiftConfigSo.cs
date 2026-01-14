using System;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream
{
    [CreateAssetMenu(fileName = "TriggerGiftConfigSo", menuName = "AxuFrameKit/LiveStream/TriggerGiftConfigSo")]
    public class TriggerGiftConfigSo : ScriptableObject
    {
        [field: SerializeField]
        public List<TriggerGiftConfigUnit> TriggerGiftConfigUnitList { get; private set; } = new();

        public bool IsDefault { get; private set; } = false;
        public bool IsSelected { get; private set; } = false;

        public void Select() => IsSelected = true;
        public void UnSelect() => IsSelected = false;

        public void PushGift(UserDataGift unit) // TODO 点赞目前只会处理第一个指令
        {
            if (TriggerGiftConfigUnitList is { Count: > 0 })
                TriggerGiftConfigUnitList[0].TriggerContent.Trigger(unit, unit.GiftCount);
        }
    }

    [Serializable]
    public class TriggerGiftConfigUnit
    {
        [HideInInspector]
        public string Id => $"{GiftName}*{GiftCount}:{TriggerContent?.Remark}";

        [Header("Gift Setting")]

        [SerializeField]
        private string _giftName;
        public string GiftName => _giftName;

        [SerializeField]
        private int _giftIndex;
        public int GiftIndex => _giftIndex;

        [SerializeField]
        private int _giftCount;
        public int GiftCount => _giftCount;

        [Header("Trigger")]

        [SerializeField]
        private LiveTriggerGiftBase _trigger;

        public LiveTriggerGiftBase TriggerContent => _trigger;
    }
}