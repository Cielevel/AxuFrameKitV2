using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.Utility
{
    public static class TitleDrawerEditor
    {
        public static void DrawTitle(string title, Color textColor, int height = 30)
        {
            GUI.backgroundColor = Color.black;
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true)); // 固定宽度
            
            GUILayout.Label(title, new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 18,
                normal    = { textColor = textColor }
            }, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
        }
    }
}