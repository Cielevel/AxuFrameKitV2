using System;
using UnityEngine;

namespace AxuFrameKit.Common.CustomAttribute
{
    /// <summary>
    /// 自定义列表绘制设置，类似于 Odin 的 ListDrawerSettings
    /// 目前支持：ListElementLabelName (指定列表中元素的显示名称字段)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ListDrawerSettingsAttribute : PropertyAttribute
    {
        /// <summary>
        /// 元素中作为标签显示的字段名
        /// </summary>
        public string ListElementLabelName { get; set; }

        public ListDrawerSettingsAttribute()
        {
        }
    }
}
