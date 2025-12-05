using UnityEngine;
using UnityEditor;
using UnityEditorInternal; // 引入 ReorderableList 的命名空间
using AxuFrameKit.Common.UI.Kit.DataBind.Component;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Editor
{
    [CustomEditor(typeof(UIBindParent))]
    public class UIBindParentEditor : UnityEditor.Editor
    {
        private ReorderableList _reorderableList;
        private SerializedProperty _bindTypeProp;
        private SerializedProperty _listProp;

        private void OnEnable()
        {
            // 获取属性引用
            _bindTypeProp = serializedObject.FindProperty("_bindType");
            _listProp = serializedObject.FindProperty("_uiBindComponentList");

            // 初始化 ReorderableList
            // 参数: serializedObject, elementsProperty, draggable, displayHeader, displayAddButton, displayRemoveButton
            _reorderableList = new ReorderableList(serializedObject, _listProp, true, true, true, true)
            {
                // 1. 自定义列表头部
                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, $"Bind Components ({_listProp.arraySize})");
                },
                // 2. 自定义列表元素的绘制 (核心逻辑)
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    // 获取当前列表元素的 SerializedProperty
                    var element = _listProp.GetArrayElementAtIndex(index);

                    // 获取实际引用的组件对象，以便读取它的 ID 或 Index
                    var componentRef = element.objectReferenceValue as UIBindComponent;

                    // 准备显示的 Label 名字
                    string labelName = $"Element {index}"; // 默认兜底

                    if (componentRef != null)
                    {
                        // 获取当前的 BindType (0 = ID, 1 = Index)
                        int bindTypeIndex = _bindTypeProp.enumValueIndex;

                        if (bindTypeIndex == 0) // ID 模式
                        {
                            string id = componentRef.ID;
                            labelName = string.IsNullOrEmpty(id) ? "<No ID>" : id;
                        }
                        else if (bindTypeIndex == 1) // Index 模式
                        {
                            labelName = $"Index {componentRef.Index}";
                        }
                    }
                    else
                    {
                        labelName = "<Empty/Null>";
                    }

                    // 调整一下高度，让显示更居中
                    rect.y      += 2;
                    rect.height =  EditorGUIUtility.singleLineHeight;

                    // 绘制属性字段，并将我们计算出的 labelName 传进去覆盖默认的 "Element X"
                    EditorGUI.PropertyField(rect, element, new GUIContent(labelName));
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1. 绘制 BindType 下拉框
            // 监听变化，如果变化了需要刷新列表显示（虽然 OnGUI 会自动刷新，但这样更保险）
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_bindTypeProp);
            if (EditorGUI.EndChangeCheck())
            {
                // 如果切换了模式，强制重绘 Inspector 以便立即更新列表的 Label
                Repaint();
            }

            EditorGUILayout.Space(5);

            // 2. 绘制我们自定义的列表
            _reorderableList.DoLayoutList();

            // 3. 添加一个辅助按钮，手动触发 OnValidate 逻辑 (可选)
            // 因为列表是自动获取的，有时候修改了子物体结构，手动点一下刷新很方便
            if (GUILayout.Button("Refresh List from Children"))
                RefreshList();

            serializedObject.ApplyModifiedProperties();
        }

        private void RefreshList()
        {
            // 通过反射或 SendMessage 调用目标脚本的 OnValidate，或者手动实现查找逻辑
            // 这里为了简单直接复用目标逻辑，我们手动查找并赋值
            var script = (UIBindParent)target;
            var components = script.GetComponentsInChildren<UIBindComponent>(true);
            
            // 需要记录撤销操作
            Undo.RecordObject(script, "Refresh Bind Components");
            
            // 注意：由于 _uiBindComponentList 是 private，不能直接赋值
            // 我们可以利用 SerializedProperty 来修改它，这样更安全且支持撤销
            _listProp.ClearArray();
            for (int i = 0; i < components.Length; i++)
            {
                _listProp.InsertArrayElementAtIndex(i);
                _listProp.GetArrayElementAtIndex(i).objectReferenceValue = components[i];
            }
        }
    }
}