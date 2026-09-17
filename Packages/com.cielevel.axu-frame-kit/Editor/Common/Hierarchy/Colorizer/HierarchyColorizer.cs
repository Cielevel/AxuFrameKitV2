using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.Hierarchy
{
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

            // 配置加载/刷新时预解析 Layer 规则的层名 → 层数值，
            // 避免 Hierarchy 每次绘制、每个物体、每条规则都调用 LayerMask.NameToLayer
            if (config?.rules != null)
            {
                foreach (var rule in config.rules)
                {
                    rule.cachedLayer = string.IsNullOrEmpty(rule.pattern) ? -1 : LayerMask.NameToLayer(rule.pattern);
                }
            }
        }

        private static void HandleHierarchyWindowItemOnGUI(int selectionID, Rect selectionRect)
        {
            if (config == null) return;
            // 仅在 Repaint 时执行匹配与绘制（与 HierarchyIconDrawer 一致），避免 Layout 等事件重复跑规则
            if (Event.current.type != EventType.Repaint) return;

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
                            // 使用 RefreshConfig 时预解析的层数值缓存，不再逐帧调用 NameToLayer
                            if (rule.cachedLayer != -1)
                                matched = (obj.layer == rule.cachedLayer);
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
}
