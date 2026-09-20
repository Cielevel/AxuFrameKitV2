using System;
using UnityEditor;
using UnityEngine;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Common.Editor.CustomAttribute
{
    // odin插件未装载在项目时，该attribute不会生效，也不会报错
    [CustomEditor(typeof(CustomScriptableObject), true)]
    public class CustomScriptableObjectAttributeEditor : UnityEditor.Editor
    {
        // 静态缓存的标题样式，避免每帧 new GUIStyle；字号随被检视类型原地更新
        private static GUIStyle _titleStyle;

        public override void OnInspectorGUI()
        {
            var att0 = (InspectorTitleAttribute)Attribute.GetCustomAttribute(target.GetType(), typeof(InspectorTitleAttribute));
            if (att0 != null)
            {
                if (_titleStyle == null)
                    _titleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                if (_titleStyle.fontSize != att0.FontSize)
                    _titleStyle.fontSize = att0.FontSize;
                GUILayout.Label(att0.Title, _titleStyle);

                // 绘制分割线
                Rect rect = EditorGUILayout.GetControlRect(false, 2);
                EditorGUI.DrawRect(rect, Color.gray);
            }

            var att1 = (InspectorIntroAttribute)Attribute.GetCustomAttribute(target.GetType(), typeof(InspectorIntroAttribute));
            if (att1 != null)
            {
                EditorGUILayout.LabelField(att1.Description, EditorStyles.wordWrappedLabel);
                Rect rect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(rect, Color.gray);
            }

            DrawDefaultInspector();
        }
    }
}
