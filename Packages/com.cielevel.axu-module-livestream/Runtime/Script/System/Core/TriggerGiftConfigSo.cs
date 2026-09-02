using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Modules.LiveStream
{
    [CreatorEntry("直播互动/触发器/TriggerGiftConfigSo")]
    public class TriggerGiftConfigSo : ScriptableObject
    {
        [field: SerializeField]
        public bool UseGiftIndex { get; private set; }

        [field: SerializeField]
        public List<TriggerGiftConfigUnit> TriggerGiftConfigUnitWithCountList { get; private set; } = new(); // 默认是开启DeliverCount

        [field: SerializeField]
        public List<TriggerGiftConfigUnit> TriggerGiftConfigUnitList { get; private set; } = new(); // 默认是关闭DeliverCount

        public bool IsDefault { get; private set; } = false;

        // Cache for performance optimization
        private Dictionary<int, List<TriggerGiftConfigUnit>> _directCacheByIndex;
        private Dictionary<string, List<TriggerGiftConfigUnit>> _directCacheByName;

        private Dictionary<int, List<TriggerGiftConfigUnit>> _splitCacheByIndex;
        private Dictionary<string, List<TriggerGiftConfigUnit>> _splitCacheByName;

        public void PushGift(UserDataGift unit)
        {
            EnsureCache();

            // 1. 处理 TriggerGiftConfigUnitWithCountList (直接传递参数触发)
            if (UseGiftIndex)
            {
                if (_directCacheByIndex != null && _directCacheByIndex.TryGetValue(unit.GiftIndex, out var list))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var configUnit = list[i];
                        configUnit.TriggerContent?.Trigger(unit, unit.GiftCount);
                    }
                }
            }
            else
            {
                if (_directCacheByName != null && _directCacheByName.TryGetValue(unit.GiftName, out var list))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var configUnit = list[i];
                        configUnit.TriggerContent?.Trigger(unit, unit.GiftCount);
                    }
                }
            }

            // 2. 处理 TriggerGiftConfigUnitList (切割数字触发)
            List<TriggerGiftConfigUnit> matchedUnits = null;
            if (UseGiftIndex)
            {
                if (_splitCacheByIndex != null)
                    _splitCacheByIndex.TryGetValue(unit.GiftIndex, out matchedUnits);
            }
            else
            {
                if (_splitCacheByName != null)
                    _splitCacheByName.TryGetValue(unit.GiftName, out matchedUnits);
            }

            if (matchedUnits != null && matchedUnits.Count > 0)
            {
                int remainingCount = unit.GiftCount;
                for (int i = 0; i < matchedUnits.Count; i++)
                {
                    var configUnit = matchedUnits[i];
                    if (configUnit.GiftCount <= 0) continue; // 防止除以零

                    int triggerTimes = remainingCount / configUnit.GiftCount;
                    if (triggerTimes > 0)
                    {
                        for (int t = 0; t < triggerTimes; t++)
                        {
                            configUnit.TriggerContent?.Trigger(unit, configUnit.GiftCount);
                        }
                        remainingCount %= configUnit.GiftCount;
                    }

                    if (remainingCount == 0) break;
                }
            }
        }

        #region Cache

        private void EnsureCache()
        {
            if (UseGiftIndex)
            {
                if (_directCacheByIndex == null)
                    _directCacheByIndex = BuildCacheByIndex(TriggerGiftConfigUnitWithCountList, false);

                if (_splitCacheByIndex == null)
                    _splitCacheByIndex = BuildCacheByIndex(TriggerGiftConfigUnitList, true);
            }
            else
            {
                if (_directCacheByName == null)
                    _directCacheByName = BuildCacheByName(TriggerGiftConfigUnitWithCountList, false);

                if (_splitCacheByName == null)
                    _splitCacheByName = BuildCacheByName(TriggerGiftConfigUnitList, true);
            }
        }

        private Dictionary<int, List<TriggerGiftConfigUnit>> BuildCacheByIndex(List<TriggerGiftConfigUnit> source, bool sortByCountDesc)
        {
            if (source == null) return new Dictionary<int, List<TriggerGiftConfigUnit>>();
            var dict = new Dictionary<int, List<TriggerGiftConfigUnit>>();
            foreach (var unit in source)
            {
                if (!dict.TryGetValue(unit.GiftIndex, out var list))
                {
                    list = new List<TriggerGiftConfigUnit>();
                    dict[unit.GiftIndex] = list;
                }
                list.Add(unit);
            }

            if (sortByCountDesc)
            {
                foreach (var list in dict.Values)
                {
                    list.Sort((a, b) => b.GiftCount.CompareTo(a.GiftCount));
                }
            }
            return dict;
        }

        private Dictionary<string, List<TriggerGiftConfigUnit>> BuildCacheByName(List<TriggerGiftConfigUnit> source, bool sortByCountDesc)
        {
            if (source == null) return new Dictionary<string, List<TriggerGiftConfigUnit>>();
            var dict = new Dictionary<string, List<TriggerGiftConfigUnit>>();
            foreach (var unit in source)
            {
                if (string.IsNullOrEmpty(unit.GiftName)) continue;
                if (!dict.TryGetValue(unit.GiftName, out var list))
                {
                    list = new List<TriggerGiftConfigUnit>();
                    dict[unit.GiftName] = list;
                }
                list.Add(unit);
            }

            if (sortByCountDesc)
            {
                foreach (var list in dict.Values)
                {
                    list.Sort((a, b) => b.GiftCount.CompareTo(a.GiftCount));
                }
            }
            return dict;
        }

        #endregion

        private void OnValidate()
        {
            // Clear cache when data changes in editor
            _directCacheByIndex = null;
            _splitCacheByIndex = null;
            _directCacheByName = null;
            _splitCacheByName = null;

            int index = 0;
            TriggerGiftConfigUnitWithCountList?.ForEach(unit => unit.elementName = $"[Count]({index++}){unit.GiftName}:{unit.TriggerContent?.Remark}");

            index = 0;
            TriggerGiftConfigUnitList?.ForEach(unit => unit.elementName = $"({index++}){unit.GiftName}*{unit.GiftCount}:{unit.TriggerContent?.Remark}");
        }
    }

    [Serializable]
    public class TriggerGiftConfigUnit
    {
        [HideInInspector]
        public string elementName;

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