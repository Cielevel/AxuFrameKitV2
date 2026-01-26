using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HierarchyColorizer
{
    private static HierarchyColorizerConfig config;

    static HierarchyColorizer()
    {
        RefreshConfig();
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    public static void RefreshConfig()
    {
        config = HierarchyColorizerConfig.Load();
    }

    private static void HandleHierarchyWindowItemOnGUI(int selectionID, Rect selectionRect)
    {
        if (config == null) return;
        
        GameObject obj = EditorUtility.InstanceIDToObject(selectionID) as GameObject;
        if (obj == null) return;

        foreach (var rule in config.rules)
        {
            if (!rule.enabled) continue;

            bool matched = false;
            try
            {
                switch (rule.matchType)
                {
                    case ColorRule.MatchType.Prefix:
                        matched = !string.IsNullOrEmpty(rule.pattern) && obj.name.StartsWith(rule.pattern);
                        break;
                    case ColorRule.MatchType.Suffix:
                        matched = !string.IsNullOrEmpty(rule.pattern) && obj.name.EndsWith(rule.pattern);
                        break;
                    case ColorRule.MatchType.Contains:
                        matched = !string.IsNullOrEmpty(rule.pattern) && obj.name.Contains(rule.pattern);
                        break;
                    case ColorRule.MatchType.Tag:
                         if (!string.IsNullOrEmpty(rule.pattern))
                            matched = obj.CompareTag(rule.pattern);
                        break;
                    case ColorRule.MatchType.Layer:
                         if (!string.IsNullOrEmpty(rule.pattern))
                         {
                            int layer = LayerMask.NameToLayer(rule.pattern);
                            if (layer != -1)
                                matched = (obj.layer == layer);
                         }
                        break;
                }
            }
            catch
            {
                matched = false;
            }

            if (matched)
            {
                Rect bgRect = new Rect(selectionRect);
                bgRect.x = 0;
                bgRect.width = selectionRect.xMax; 

                // Draw the color
                EditorGUI.DrawRect(bgRect, rule.color);
                
                // If you want to ensure the label is visible even with opaque colors, 
                // you would need to redraw the label here. 
                // However, doing so properly (matching Unity's style) is non-trivial.
                // We rely on the user using transparent colors (alpha < 1).
                
                break;
            }
        }
    }
}
