using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.Utility
{
    public static class TitleDrawerEditor
    {
        // 静态缓存的标题样式，避免每次绘制都 new GUIStyle（textColor 每次调用时原地更新）
        private static GUIStyle _titleStyle;

        private static GUIStyle TitleStyle
        {
            get
            {
                if (_titleStyle == null)
                {
                    _titleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 18,
                    };
                }

                return _titleStyle;
            }
        }

        public static void DrawTitle(string title, Color textColor, int height = 30)
        {
            // 保存调用方进入时的 backgroundColor，结束时恢复原值，避免硬复位为白色覆盖调用方上下文
            Color prevBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.black;
            try
            {
                EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true)); // 固定宽度

                TitleStyle.normal.textColor = textColor;
                GUILayout.Label(title, TitleStyle, GUILayout.ExpandWidth(true), GUILayout.Height(height));

                EditorGUILayout.EndVertical();
            }
            finally
            {
                // 恢复调用方的 backgroundColor（而非硬编码复位为白色）
                GUI.backgroundColor = prevBackgroundColor;
            }
        }
    }
}
