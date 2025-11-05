using UnityEditor;
using UnityEngine;
using AxuFrameKit._Core.Structure;

namespace AxuFrameKit.Common.Editor.CoreInspector
{
    [CustomPropertyDrawer(typeof(IntBound))]
    public class IntBoundDrawer : PropertyDrawer
    {
        private const float LabelWidth = 30f;
        private const float ButtonWidth = 70f;
        private const float Spacing = 4f;
        private const float Padding = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 绘制外框 (helpBox风格)
            Rect boxRect = new(position.x, position.y, position.width, position.height);
            GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

            // 为内部内容留出内边距
            Rect innerRect = new(
                position.x + Padding,
                position.y + Padding,
                position.width - Padding * 2,
                position.height - Padding * 2
            );

            SerializedProperty minProp = property.FindPropertyRelative("min");
            SerializedProperty maxProp = property.FindPropertyRelative("max");

            // 绘制标题（折叠标签）
            innerRect = EditorGUI.PrefixLabel(innerRect, GUIUtility.GetControlID(FocusType.Passive), label);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float fieldWidth = (innerRect.width - LabelWidth * 2 - ButtonWidth - Spacing * 4) / 2f;

            Rect minLabelRect = new(innerRect.x, innerRect.y, LabelWidth, EditorGUIUtility.singleLineHeight);
            Rect minFieldRect = new(minLabelRect.xMax + Spacing, innerRect.y, fieldWidth, EditorGUIUtility.singleLineHeight);
            Rect maxLabelRect = new(minFieldRect.xMax + Spacing, innerRect.y, LabelWidth, EditorGUIUtility.singleLineHeight);
            Rect maxFieldRect = new(maxLabelRect.xMax + Spacing, innerRect.y, fieldWidth, EditorGUIUtility.singleLineHeight);
            Rect buttonRect = new(maxFieldRect.xMax + Spacing, innerRect.y, ButtonWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(minLabelRect, "Min");
            EditorGUI.PropertyField(minFieldRect, minProp, GUIContent.none);

            EditorGUI.LabelField(maxLabelRect, "Max");
            EditorGUI.PropertyField(maxFieldRect, maxProp, GUIContent.none);

            if (GUI.Button(buttonRect, "🎲 Random"))
            {
                int min = Mathf.Min(minProp.intValue, maxProp.intValue);
                int max = Mathf.Max(minProp.intValue, maxProp.intValue + 1);
                int randomValue = UnityEngine.Random.Range(min, max);

                Debug.Log($"Random value from IntBound({min}, {max - 1}) = {randomValue}");
                EditorGUIUtility.systemCopyBuffer = randomValue.ToString();
                ShowNotification($"Generated: {randomValue}");
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 稍微加一点高度给边框 padding
            return EditorGUIUtility.singleLineHeight + Padding * 2;
        }

        private void ShowNotification(string text)
        {
            if (EditorWindow.focusedWindow != null)
                EditorWindow.focusedWindow.ShowNotification(new GUIContent(text));
        }
    }
}
