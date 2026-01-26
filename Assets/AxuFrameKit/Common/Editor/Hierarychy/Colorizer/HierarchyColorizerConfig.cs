using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public class ColorRule
{
    public bool enabled = true;
    public string pattern = "";
    public MatchType matchType = MatchType.Prefix;
    public Color color = new Color(0.3f, 0.8f, 0.3f, 0.3f);

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