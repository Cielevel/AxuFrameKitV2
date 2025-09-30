#if ODIN_INSPECTOR
using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace AxU_NextGen.Common.CustomAttribute.Editor
{
    // odin插件未装载在项目时，该attribute不会生效，也不会报错
    [CustomEditor(typeof(CustomScriptableObject), true)]
    public class CustomScriptableObjectAttributeEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            var att0 = (InspectorTitleAttribute)Attribute.GetCustomAttribute(target.GetType(), typeof(InspectorTitleAttribute));
            if (att0 != null)
            {
                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize  = att0.FontSize,
                    alignment = TextAnchor.MiddleCenter
                };
                GUILayout.Label(att0.Title, titleStyle);
                
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
#endif