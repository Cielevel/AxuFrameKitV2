using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using AxuFrameKit.Common.CustomAttribute;
using System.Reflection;
using System;
using System.Collections;

namespace AxuFrameKit.Common.CustomAttribute.Editor
{
    [CustomPropertyDrawer(typeof(ListDrawerSettingsAttribute))]
    public class ListDrawerSettingsDrawer : PropertyDrawer
    {
        private Dictionary<string, ReorderableList> _listCache = new Dictionary<string, ReorderableList>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!property.isArray)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            ReorderableList reorderableList = GetReorderableList(property);
            reorderableList.DoList(position);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isArray)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            ReorderableList reorderableList = GetReorderableList(property);
            return reorderableList.GetHeight();
        }

        private ReorderableList GetReorderableList(SerializedProperty property)
        {
            string key = property.serializedObject.targetObject.GetInstanceID() + property.propertyPath;

            if (!_listCache.TryGetValue(key, out ReorderableList list) || list.serializedProperty.serializedObject != property.serializedObject)
            {
                list = new ReorderableList(property.serializedObject, property, true, true, true, true);

                list.drawHeaderCallback = (Rect rect) =>
                {
                    string labelText = property.displayName;
                    if (property.isArray) labelText += $" [{property.arraySize}]";
                    EditorGUI.LabelField(rect, labelText, EditorStyles.boldLabel);
                };

                list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    if (index >= property.arraySize) return;

                    SerializedProperty element = property.GetArrayElementAtIndex(index);
                    ListDrawerSettingsAttribute settings = (ListDrawerSettingsAttribute)attribute;

                    GUIContent elementLabel = new GUIContent($"Item {index}");
                    if (!string.IsNullOrEmpty(settings.ListElementLabelName))
                    {
                        string labelValue = GetCustomLabel(element, settings.ListElementLabelName);
                        if (!string.IsNullOrEmpty(labelValue))
                        {
                            elementLabel.text = labelValue;
                        }
                    }

                    rect.y += 2;
                    float height = EditorGUI.GetPropertyHeight(element, elementLabel, true);
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, height), element, elementLabel, true);
                };

                list.elementHeightCallback = (int index) =>
                {
                    if (index >= property.arraySize) return 20f;

                    SerializedProperty element = property.GetArrayElementAtIndex(index);
                    ListDrawerSettingsAttribute settings = (ListDrawerSettingsAttribute)attribute;

                    GUIContent elementLabel = new GUIContent(" ");
                    if (!string.IsNullOrEmpty(settings.ListElementLabelName))
                    {
                        string labelValue = GetCustomLabel(element, settings.ListElementLabelName);
                        if (!string.IsNullOrEmpty(labelValue))
                        {
                            elementLabel.text = labelValue;
                        }
                    }

                    return EditorGUI.GetPropertyHeight(element, elementLabel, true) + 4;
                };

                _listCache[key] = list;
            }
            return list;
        }

        private string GetCustomLabel(SerializedProperty element, string fieldName)
        {
            // 1. Try to find as a serialized property first
            SerializedProperty labelProp = element.FindPropertyRelative(fieldName);

            // If not found directly, and it's an object reference, try looking inside the object
            if (labelProp == null && element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null)
            {
                SerializedObject so = new SerializedObject(element.objectReferenceValue);
                labelProp = so.FindProperty(fieldName);
            }

            if (labelProp != null)
            {
                return GetPropertyValueAsString(labelProp);
            }

            // 2. If not found as serialized property, try Reflection (to support C# properties/nested objects)
            object obj = GetTargetObjectOfProperty(element);
            if (obj != null)
            {
                object val = GetValue_Internal(obj, fieldName);
                if (val != null) return val.ToString();
            }

            // Fallback for object reference: just use the object name if the fieldName is "name"
            if (element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null)
            {
                if (fieldName.Equals("name", StringComparison.OrdinalIgnoreCase))
                    return element.objectReferenceValue.name;
            }

            return null;
        }

        private string GetPropertyValueAsString(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Integer:
                    return property.intValue.ToString();
                case SerializedPropertyType.Enum:
                    return property.enumDisplayNames[property.enumValueIndex];
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null ? property.objectReferenceValue.name : "None";
                case SerializedPropertyType.Float:
                    return property.floatValue.ToString("G");
                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();
                default:
                    return null;
            }
        }

        /// <summary>
        /// 获取 SerializedProperty 对应的实际对象实例
        /// </summary>
        private object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null) return null;

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');

            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Internal(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Internal(obj, element);
                }
            }
            return obj;
        }

        private object GetValue_Internal(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();

            while (type != null)
            {
                // Try to find as Field
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null) return f.GetValue(source);

                // Try to find as Property
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null) return p.GetValue(source, null);

                // If it's an auto-property, try matching the backing field name directly just in case the path uses it
                if (!name.StartsWith("<"))
                {
                    string backingFieldName = $"<{name}>k__BackingField";
                    var bf = type.GetField(backingFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (bf != null) return bf.GetValue(source);
                }

                type = type.BaseType;
            }
            return null;
        }

        private object GetValue_Internal(object source, string name, int index)
        {
            var collection = GetValue_Internal(source, name);
            if (collection == null) return null;

            if (collection is IList list)
            {
                if (index < list.Count) return list[index];
            }
            else if (collection is IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();
                for (int i = 0; i <= index; i++)
                {
                    if (!enumerator.MoveNext()) return null;
                }
                return enumerator.Current;
            }
            return null;
        }
    }
}
