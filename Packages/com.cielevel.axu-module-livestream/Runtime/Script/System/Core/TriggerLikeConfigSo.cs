using System;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Modules.LiveStream
{
    /// <summary>
    /// 点赞（Like）触发器配置。
    /// <para>
    /// 设计说明：Like 不会像 Comment / Gift 那样存在"多种触发"的形态差异——
    /// Comment 有精确匹配 / 正则 / 是否透传消息等分支，Gift 有"直传数量"与"按数量切割"两种模式；
    /// 而 Like 的多样性取决于具体游戏（连击节奏、点赞阈值、防刷限流等），无法在配置层穷举。
    /// </para>
    /// <para>
    /// 因此本 config 只提供 Like 的触发入口：单一 unit 列表、默认只取第一个触发。
    /// 具体的多样性表现交由游戏侧在 <see cref="LiveTriggerLikeBase"/> 的实现中自行定义。
    /// </para>
    /// </summary>
    [CreatorEntry("直播互动/触发器/TriggerLikeConfigSo")]
    public class TriggerLikeConfigSo : ScriptableObject
    {
        [field: SerializeField]
        public List<TriggerLikeConfigUnit> TriggerLikeConfigUnitList { get; private set; } = new();

        /// <summary>
        /// 点赞触发入口。
        /// 只取列表第一个 unit 触发，不做遍历匹配——原因见类注释：
        /// Like 的多样性不体现在配置形态上，而取决于具体游戏的触发实现。
        /// </summary>
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

    /// <summary>
    /// 点赞触发单元。
    /// 仅承载"入口 + 点赞数"信息，不承担形态匹配职责（见 <see cref="TriggerLikeConfigSo"/> 类注释）。
    /// </summary>
    [Serializable]
    public class TriggerLikeConfigUnit
    {
        [HideInInspector]
        public string elementName;

        [Header("Like Setting")]

        [SerializeField]
        private int _likeCount;

        /// <summary>
        /// 点赞数参数，默认透传给触发实现。
        /// 具体语义由游戏侧决定（如阈值、连击段数、单次触发数量等）。
        /// </summary>
        public int LikeCount => _likeCount;

        [Header("Trigger")]

        [SerializeField]
        private LiveTriggerLikeBase _trigger;
        public LiveTriggerLikeBase TriggerContent => _trigger;
    }
}