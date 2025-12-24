using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AxuFrameKit.Modules.GizmosUtil.Editor
{
    [CustomEditor(typeof(GizmosDrawer))]
    public class GizmosDrawerEditor : UnityEditor.Editor
    {
        private ReorderableList _drawerList;
        private SerializedProperty _drawerListProperty;

        private void OnEnable()
        {
            _drawerListProperty = serializedObject.FindProperty("_drawerList");
            CreateReorderableList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 绘制默认的 Script 字段
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            EditorGUILayout.Space();

            // 绘制自定义的 ReorderableList
            if (_drawerList != null)
                _drawerList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void CreateReorderableList()
        {
            _drawerList = new ReorderableList(serializedObject, _drawerListProperty, 
                                              true, true, true, true)
            {
                // 绘制头部
                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, $"Drawer List (Size: {_drawerListProperty.arraySize})");
                },
                // 绘制每个元素
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var element = _drawerListProperty.GetArrayElementAtIndex(index);
                    rect.y += 2;

                    // 获取类型名称
                    string typeName = GetTypeName(element);

                    EditorGUI.PropertyField(rect, element, new GUIContent($"[{index}] {typeName}"), true);
                },
                // 元素高度
                elementHeightCallback = (int index) =>
                {
                    var element = _drawerListProperty.GetArrayElementAtIndex(index);
                    return EditorGUI.GetPropertyHeight(element, true) + 4;
                },
                // 🎯 关键：自定义添加按钮的下拉菜单
                onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
                {
                    var menu = new GenericMenu();

                    // 获取所有 BaseDrawer 的子类
                    var baseType = typeof(GizmosDrawer.BaseDrawer);
                    var derivedTypes = System.AppDomain.CurrentDomain.GetAssemblies()
                                             .SelectMany(assembly => assembly.GetTypes())
                                             .Where(t => t.IsSubclassOf(baseType) &&
                                                         !t.IsAbstract &&
                                                         t.IsSerializable)
                                             .OrderBy(t => t.Name)
                                             .ToList();

                    if (derivedTypes.Count == 0)
                    {
                        menu.AddDisabledItem(new GUIContent("没有可用的子类"));
                    }
                    else
                    {
                        foreach (var type in derivedTypes)
                        {
                            menu.AddItem(new GUIContent(type.Name), false, () => { AddElement(type); });
                        }
                    }

                    menu.ShowAsContext();
                }
            };
        }

        private void AddElement(Type type)
        {
            serializedObject.Update();

            // 增加数组大小
            int newIndex = _drawerListProperty.arraySize;
            _drawerListProperty.arraySize++;

            // 获取新元素
            var newElement = _drawerListProperty.GetArrayElementAtIndex(newIndex);

            // 创建实例并赋值
            newElement.managedReferenceValue = Activator.CreateInstance(type);
            newElement.isExpanded            = true;

            serializedObject.ApplyModifiedProperties();
        }

        private string GetTypeName(SerializedProperty property)
        {
            if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
                return "Null";

            var fullTypeName = property.managedReferenceFullTypename;
            var parts        = fullTypeName.Split(' ');
            if (parts.Length > 1)
            {
                var typeName      = parts[1];
                var lastDot       = typeName.LastIndexOf('.');
                var lastPlus      = typeName.LastIndexOf('+');
                var lastSeparator = Math.Max(lastDot, lastPlus);

                if (lastSeparator >= 0)
                    return typeName.Substring(lastSeparator + 1);

                return typeName;
            }

            return fullTypeName;
        }
    }
}
