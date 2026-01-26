using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public class IconRule
{
    public bool enabled = true;
    public string componentName = "";
    public HierarchyIconConfig.IconAlignment alignment = HierarchyIconConfig.IconAlignment.Right;
    public float offsetX = 0f;
}

[Serializable]
public class HierarchyIconConfig
{
    public enum IconAlignment { Right, Left }

    // Store names of enabled built-in components
    public List<string> enabledBuiltInIcons = new List<string>();

    // Custom rules (manually added by user)
    public List<IconRule> customRules = new List<IconRule>();

    private const string PREFS_KEY = "HierarchyIcon_Config";

    public void Save()
    {
        string json = JsonUtility.ToJson(this);
        EditorPrefs.SetString(PREFS_KEY, json);
    }

    public static HierarchyIconConfig Load()
    {
        if (EditorPrefs.HasKey(PREFS_KEY))
        {
            string json = EditorPrefs.GetString(PREFS_KEY);
            return JsonUtility.FromJson<HierarchyIconConfig>(json);
        }

        // Default config
        var config = new HierarchyIconConfig();
        config.enabledBuiltInIcons.Add("Camera");
        config.enabledBuiltInIcons.Add("Light");
        return config;
    }
}
