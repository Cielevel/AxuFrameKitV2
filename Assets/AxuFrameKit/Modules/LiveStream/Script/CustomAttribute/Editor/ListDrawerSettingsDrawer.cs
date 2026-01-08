using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using AxuFrameKit.Modules.LiveStream;

namespace AxuFrameKit.Modules.LiveStream.Editor
{
    [CustomPropertyDrawer(typeof(ListDrawerSettingsAttribute))]
    public class ListDrawerSettingsDrawer : PropertyDrawer
    {
        private Dictionary<string, ReorderableList> _listCache = new Dictionary<string, ReorderableList>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isArray)
                return EditorGUI.GetPropertyHeight(property, label, true);

            InitList(property);
            var key = property.propertyPath;
            return _listCache[key].GetHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!property.isArray)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            InitList(property);
            var key = property.propertyPath;
            _listCache[key].DoList(position);
        }

        private void InitList(SerializedProperty property)
        {
            var key = property.propertyPath;
            if (_listCache.ContainsKey(key))
                return;

            var list = new ReorderableList(property.serializedObject, property, true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, property.displayName);
                },

                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    var settings = attribute as ListDrawerSettingsAttribute;
                    var labelName = settings?.ListElementLabelName;

                    var labelContent = new GUIContent($"Element {index}");

                    if (!string.IsNullOrEmpty(labelName))
                    {
                        var nameProp = element.FindPropertyRelative(labelName);
                        if (nameProp != null)
                        {
                            switch (nameProp.propertyType)
                            {
                                case SerializedPropertyType.String:
                                    labelContent.text = nameProp.stringValue;
                                    break;
                                case SerializedPropertyType.Integer:
                                    labelContent.text = nameProp.intValue.ToString();
                                    break;
                                default:
                                    labelContent.text = nameProp.displayName; // Fallback
                                    break;
                            }
                        }
                    }

                    rect.y += 2;
                    rect.height = EditorGUI.GetPropertyHeight(element, labelContent, true);
                    EditorGUI.PropertyField(rect, element, labelContent, true);
                },

                elementHeightCallback = index =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    return EditorGUI.GetPropertyHeight(element, GUIContent.none, true) + 6;
                }
            };

            _listCache.Add(key, list);
        }
    }
}