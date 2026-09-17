using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.Hierarchy
{
    [Serializable]
    public class ColorRule
{
        public bool enabled = true;
        public string pattern = "";
        public MatchType matchType = MatchType.Prefix;
        public Color color = new Color(0.3f, 0.8f, 0.3f, 0.3f);

        /// <summary>
        /// Layer 匹配模式下的层名预解析结果（-1 表示无效/未解析）。
        /// 不参与序列化，由 HierarchyColorizer.RefreshConfig 刷新，
        /// 避免 Hierarchy 逐帧绘制时对每个物体每条规则调用 LayerMask.NameToLayer。
        /// </summary>
        [NonSerialized] public int cachedLayer = -1;

    public enum MatchType
    {
        Prefix,      // 前缀匹配
        Suffix,      // 后缀匹配
        Contains,    // 包含匹配
        Tag,         // Tag匹配
        Layer        // Layer匹配
    }
}

[Serializable]
public class HierarchyColorizerConfig
{
    public List<ColorRule> rules = new List<ColorRule>();

    private const string PREFS_KEY = "HierarchyColorizer_Config";

    public void Save()
    {
        string json = JsonUtility.ToJson(this);
        EditorPrefs.SetString(PREFS_KEY, json);
    }

    public static HierarchyColorizerConfig Load()
    {
        if (EditorPrefs.HasKey(PREFS_KEY))
        {
            string json = EditorPrefs.GetString(PREFS_KEY);
            return JsonUtility.FromJson<HierarchyColorizerConfig>(json);
        }

        // 返回默认配置
        var config = new HierarchyColorizerConfig();
        config.rules.Add(new ColorRule
        {
            pattern = "@",
            matchType = ColorRule.MatchType.Prefix,
            color = new Color(0.3f, 0.8f, 0.3f, 0.3f)
        });
        config.rules.Add(new ColorRule
        {
            pattern = "#",
            matchType = ColorRule.MatchType.Prefix,
            color = new Color(0.8f, 0.3f, 0.3f, 0.3f)
        });
        return config;
    }
}
}