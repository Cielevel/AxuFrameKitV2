using System;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream
{
    [CreateAssetMenu(fileName = "TriggerCommentConfigSo", menuName = "AxuFrameKit/LiveStream/TriggerCommentConfigSo")]
    public class TriggerCommentConfigSo : ScriptableObject
    {
        [field: SerializeField]
        public List<TriggerCommentConfigUnit> TriggerCommentConfigUnitList { get; private set; } = new();

        public bool IsDefault { get; private set; } = false;
        public bool IsSelected { get; private set; } = false;

        public void Select() => IsSelected = true;
        public void UnSelect() => IsSelected = false;

        public void PushComment(UserDataComment unit) // TODO 点赞目前只会处理第一个指令
        {
            if (TriggerCommentConfigUnitList is { Count: > 0 })
                TriggerCommentConfigUnitList[0].TriggerContent.Trigger(unit, unit.Comment);
        }
    }

    [Serializable]
    public class TriggerCommentConfigUnit
    {
        [HideInInspector]
        public string Id => $"{Comment}:{TriggerContent?.Remark}";

        [Header("Comment Setting")]

        [SerializeField]
        private string _comment;

        [Header("Regex")]

        [SerializeField]
        private bool _useRegex;
        public bool UseRegex => _useRegex;

        [SerializeField]
        private string _regexComment;

        public string Comment => _useRegex ? _regexComment : _comment;

        [Header("Trigger")]

        [SerializeField]
        private LiveTriggerCommentBase _trigger;

        public LiveTriggerCommentBase TriggerContent => _trigger;
    }
}