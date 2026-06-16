using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.Utility
{
    public static class ShapeDrawerEditor
    {
        public static void DrawLineHorizontal(int height, Color color, int space = 0)
        {
            if (0 < space)
                EditorGUILayout.Space(space);
            
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.DrawRect(rect, color);
            
            if (0 < space)
                EditorGUILayout.Space(space);
        }
    }
}