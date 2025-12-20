using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AxuFrameKit.Common.CustomAttribute.PolymorphicList.Editor
{
    [CustomPropertyDrawer(typeof(PolymorphicListAttribute))]
    public class PolymorphicListDrawer : PropertyDrawer
    {
        /// <summary>
        /// 每一个 SerializedProperty 对应一个 ReorderableList
        /// 解决 PropertyDrawer 复用问题
        /// </summary>
        private readonly Dictionary<string, ReorderableList> _lists
            = new Dictionary<string, ReorderableList>();

        #region Unity Callbacks

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // ⚠️ 只接管“字段本身”的 List，不接管其子属性
            if (!IsTargetListProperty(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var list = GetList(property, label);
            list.DoList(position);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!IsTargetListProperty(property))
                return EditorGUI.GetPropertyHeight(property, label, true);

            var list = GetList(property, label);
            return list.GetHeight();
        }

        #endregion

        #region List Creation

        private ReorderableList GetList(SerializedProperty property, GUIContent label)
        {
            var key = property.propertyPath;

            if (_lists.TryGetValue(key, out var list))
                return list;

            list = CreateList(property, label);
            _lists[key] = list;
            return list;
        }

        private ReorderableList CreateList(SerializedProperty property, GUIContent label)
        {
            var list = new ReorderableList(
                property.serializedObject,
                property,
                true, true, true, true);

            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, $"{label.text} ({property.arraySize})");
            };

            list.drawElementCallback = (rect, index, active, focused) =>
            {
                var element = property.GetArrayElementAtIndex(index);
                rect.y += 2;

                string typeName = GetManagedReferenceTypeName(element);
                EditorGUI.PropertyField(rect, element, new GUIContent(typeName), true);
            };

            list.elementHeightCallback = index =>
            {
                var element = property.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + 6f;
            };

            list.onAddDropdownCallback = (rect, l) =>
            {
                ShowAddMenu(property);
            };

            return list;
        }

        #endregion

        #region Add Menu

        private void ShowAddMenu(SerializedProperty listProperty)
        {
            var baseType = GetListElementType(listProperty);
            if (baseType == null)
                return;

            var menu = new GenericMenu();

            var types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t =>
                    !t.IsAbstract &&
                    !t.IsInterface &&
                    t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name);

            foreach (var type in types)
            {
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    AddNewElement(listProperty, type);
                });
            }

            if (!types.Any())
                menu.AddDisabledItem(new GUIContent("No valid sub types"));

            menu.ShowAsContext();
        }

        private void AddNewElement(SerializedProperty listProperty, Type type)
        {
            var so = listProperty.serializedObject;
            so.Update();

            listProperty.arraySize++;
            var element = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);

            // ⚠️ 必须先清空，再赋值
            element.managedReferenceValue = null;
            element.managedReferenceValue = Activator.CreateInstance(type);
            element.isExpanded = true;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(so.targetObject);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// 只允许作用在字段级 List / Array 上
        /// </summary>
        private static bool IsTargetListProperty(SerializedProperty property)
        {
            if (!property.isArray)
                return false;

            // Array.data[x] 属于子元素，直接排除
            if (property.propertyPath.Contains("Array.data"))
                return false;

            return true;
        }

        private static Type GetListElementType(SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;
            var field = target.GetType()
                .GetField(property.name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field == null)
                return null;

            if (field.FieldType.IsArray)
                return field.FieldType.GetElementType();

            if (field.FieldType.IsGenericType)
                return field.FieldType.GetGenericArguments()[0];

            return null;
        }

        private static string GetManagedReferenceTypeName(SerializedProperty property)
        {
            if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
                return "Null";

            var name = property.managedReferenceFullTypename;
            int plus = name.LastIndexOf('+');
            int dot  = name.LastIndexOf('.');
            int idx  = Mathf.Max(plus, dot);

            return idx >= 0 ? name.Substring(idx + 1) : name;
        }

        #endregion
    }
}
