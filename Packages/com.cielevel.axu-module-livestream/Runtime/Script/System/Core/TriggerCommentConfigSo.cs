using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Modules.LiveStream
{
    [CreatorEntry("直播互动/触发器/TriggerCommentConfigSo")]
    public class TriggerCommentConfigSo : ScriptableObject
    {
        [field: SerializeField]
        public List<TriggerCommentConfigUnit> TriggerCommentConfigUnitList { get; private set; } = new();

        public void PushComment(UserDataComment unit)
        {
            if (TriggerCommentConfigUnitList == null) return;

            foreach (var configUnit in TriggerCommentConfigUnitList)
            {
                if (configUnit.UseRegex)
                {
                    var match = Regex.Match(unit.Comment, configUnit.Comment);
                    if (match.Success)
                    {
                        configUnit.TriggerContent?.Trigger(unit, match);
                    }
                }
                else if (unit.Comment == configUnit.Comment)
                {
                    if (configUnit.DeliverMessage)
                    {
                        configUnit.TriggerContent?.Trigger(unit, unit.Comment);
                    }
                    else
                    {
                        configUnit.TriggerContent?.Trigger(unit);
                    }
                }
            }
        }

        private void OnValidate()
        {
            int index = 0;
            TriggerCommentConfigUnitList?.ForEach(unit => unit.elementName = $"({index++}){unit.Comment}:{unit.TriggerContent?.Remark}");
        }
    }

    [Serializable]
    public class TriggerCommentConfigUnit
    {
        [HideInInspector]
        public string elementName;

        [Header("Comment Setting")]

        [SerializeField]
        private string _comment;

        [Header("Regex")]

        [SerializeField]
        private bool _useRegex;
        public bool UseRegex => _useRegex;

        [SerializeField]
        private bool _deliverMessage;
        public bool DeliverMessage => _deliverMessage;

        [SerializeField]
        private string _regexComment;

        public string Comment => _useRegex ? _regexComment : _comment;

        [Header("Trigger")]

        [SerializeField]
        private LiveTriggerCommentBase _trigger;

        public LiveTriggerCommentBase TriggerContent => _trigger;
    }
}