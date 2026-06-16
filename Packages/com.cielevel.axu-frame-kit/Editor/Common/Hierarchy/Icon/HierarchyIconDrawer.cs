using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace AxuFrameKit.Common.Editor.Hierarchy
{
    [InitializeOnLoad]
    public class HierarchyIconDrawer
{
    private static HierarchyIconConfig config;
    private static Dictionary<string, Texture> iconCache = new Dictionary<string, Texture>();

    // Initial pool of built-in components to check
    // We don't hardcode logic here, just use what's in config.enabledBuiltInIcons

    static HierarchyIconDrawer()
    {
        RefreshConfig();
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    public static void RefreshConfig()
    {
        config = HierarchyIconConfig.Load();
        iconCache.Clear();
    }

    private static void HandleHierarchyWindowItemOnGUI(int selectionID, Rect selectionRect)
    {
        if (config == null) return;
        if (Event.current.type != EventType.Repaint) return;

        GameObject obj = EditorUtility.InstanceIDToObject(selectionID) as GameObject;
        if (obj == null) return;

        float currentX = selectionRect.xMax - 20; // Start from right edge

        // 1. Draw Built-in Icons first
        if (config.enabledBuiltInIcons != null)
        {
            foreach (var typeName in config.enabledBuiltInIcons)
            {
                DrawIconForType(obj, typeName, ref currentX, selectionRect.y);
            }
        }

        // 2. Draw Custom Rules
        if (config.customRules != null)
        {
            foreach (var rule in config.customRules)
            {
                if (!rule.enabled || string.IsNullOrEmpty(rule.componentName)) continue;

                // For custom rules, we support custom offset. 
                // However, to keep it simple with the stacking logic, we might just use the stack.
                // Or if rule.offsetX is specific, use it.

                if (rule.offsetX != 0)
                {
                    // absolute offset from right?
                    float x = selectionRect.xMax - 20 - rule.offsetX;
                    DrawIconForType(obj, rule.componentName, ref x, selectionRect.y, false);
                }
                else
                {
                    DrawIconForType(obj, rule.componentName, ref currentX, selectionRect.y);
                }
            }
        }
    }

    private static void DrawIconForType(GameObject obj, string typeName, ref float currentX, float y, bool shiftX = true)
    {
        Component comp = obj.GetComponent(typeName);
        if (comp != null)
        {
            Texture icon = GetIconForComponent(comp, typeName);
            if (icon != null)
            {
                Rect iconRect = new Rect(currentX, y, 16, 16);
                GUI.DrawTexture(iconRect, icon);

                if (shiftX)
                    currentX -= 18;
            }
        }
    }

    private static Texture GetIconForComponent(Component comp, string typeName)
    {
        if (iconCache.TryGetValue(typeName, out Texture cachedIcon))
        {
            if (cachedIcon != null) return cachedIcon;
        }

        Texture icon = EditorGUIUtility.ObjectContent(comp, comp.GetType()).image;

        if (icon == null)
        {
            var content = EditorGUIUtility.ObjectContent(null, comp.GetType());
            if (content != null) icon = content.image;
        }

        // Cache even if null to avoid repeated lookups
        iconCache[typeName] = icon;

        return icon;
    }
}
}
