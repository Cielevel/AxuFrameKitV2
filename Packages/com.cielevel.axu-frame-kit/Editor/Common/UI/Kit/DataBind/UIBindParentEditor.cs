using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal; // 引入 ReorderableList 的命名空间
using AxuFrameKit.Common.UI.Kit.DataBind.Component;
using AxuFrameKit.Common.UI.Kit.DataBind;

namespace AxuFrameKit.Common.Editor.UI.Kit.DataBind
{
    [CustomEditor(typeof(UIBindParent))]
    public class UIBindParentEditor : UnityEditor.Editor
    {
        private ReorderableList _reorderableList;
        private SerializedProperty _bindTypeProp;
        private SerializedProperty _listProp;
        private string _generatedCode = "";

        // 每帧绘制复用的 GUI 内容/样式静态缓存，避免 OnInspectorGUI 与列表元素逐帧 new
        private static GUIContent _elementLabelContent;
        private static GUIStyle _greenButtonStyle;

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
                    rect.y += 2;
                    rect.height = EditorGUIUtility.singleLineHeight;

                    // 绘制属性字段，并将我们计算出的 labelName 传进去覆盖默认的 "Element X"
                    // 复用静态 GUIContent，仅原地更新 text，避免每个元素每帧分配
                    if (_elementLabelContent == null) _elementLabelContent = new GUIContent();
                    _elementLabelContent.text = labelName;
                    EditorGUI.PropertyField(rect, element, _elementLabelContent);
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

            // 惰性创建并静态缓存按钮样式（GUI.skin 需在 GUI 上下文中访问）
            if (_greenButtonStyle == null)
            {
                _greenButtonStyle = new GUIStyle(GUI.skin.button);
                _greenButtonStyle.normal.textColor = Color.green;
            }

            // 2. 绘制我们自定义的列表
            _reorderableList.DoLayoutList();

            // 3. 添加一个辅助按钮，手动触发 OnValidate 逻辑 (可选)
            // 因为列表是自动获取的，有时候修改了子物体结构，手动点一下刷新很方便
            if (GUILayout.Button("Refresh List from Children"))
                RefreshList();

            if (GUILayout.Button("CheckIdOrIndex"))
                CheckIdOrIndex();

            if (GUILayout.Button("FillIdOrIndex"))
                FillIdOrIndex();

            if (GUILayout.Button("Generate SetData Code", _greenButtonStyle))
                GenerateSetterCode();

            // 显示生成的代码
            if (!string.IsNullOrEmpty(_generatedCode))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Generated Code:", EditorStyles.boldLabel);

                // 使用TextArea显示代码，可以选择和复制
                EditorGUILayout.SelectableLabel(_generatedCode, EditorStyles.textArea, GUILayout.Height(60));
            }

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

        private void CheckIdOrIndex()
        {
            int bindTypeIndex = _bindTypeProp.enumValueIndex;

            if (bindTypeIndex == 0) // ID 模式
            {
                HashSet<string> hs = new HashSet<string>(_listProp.arraySize);
                for (int i = 0; i < _listProp.arraySize; i++)
                {
                    var element = _listProp.GetArrayElementAtIndex(i);

                    var componentRef = element.objectReferenceValue as UIBindComponent;
                    if (componentRef != null && string.IsNullOrEmpty(componentRef.ID))
                        Debug.LogWarning($"UIBindComponent at index {i} has an empty ID.");

                    if (componentRef == null)
                    {
                        Debug.LogError($"UIBindComponent at {i} reference is null.");
                        continue;
                    }

                    if (hs.Add(componentRef.ID) == false)
                        Debug.LogWarning($"UIBindComponent.ID '{componentRef.ID}' is duplicated. At {i}");
                }
            }
            else if (bindTypeIndex == 1) // Index 模式
            {
                HashSet<int> hs = new HashSet<int>(_listProp.arraySize);
                for (int i = 0; i < _listProp.arraySize; i++)
                {
                    var element = _listProp.GetArrayElementAtIndex(i);

                    var componentRef = element.objectReferenceValue as UIBindComponent;
                    if (componentRef != null && string.IsNullOrEmpty(componentRef.ID))
                        Debug.LogWarning($"UIBindComponent at index {i} has an empty ID.");

                    if (componentRef == null)
                    {
                        Debug.LogError($"UIBindComponent at {i} reference is null.");
                        continue;
                    }

                    if (hs.Add(componentRef.Index) == false)
                        Debug.LogWarning($"UIBindComponent.Index '{componentRef.Index}' is duplicated. At {i}");
                }
            }

            Debug.Log("Check Completed.");
        }

        private void FillIdOrIndex()
        {
            int bindTypeIndex = _bindTypeProp.enumValueIndex;

            if (bindTypeIndex == 0) // ID 模式
            {
                for (int i = 0; i < _listProp.arraySize; i++)
                {
                    var element = _listProp.GetArrayElementAtIndex(i);

                    var componentRef = element.objectReferenceValue as UIBindComponent;

                    if (componentRef == null)
                    {
                        Debug.LogError($"UIBindComponent at {i} reference is null.");
                        continue;
                    }

                    // 获取泛型类型后缀
                    string typeSuffix = GetGenericTypeSuffix(componentRef);
                    string baseName = componentRef.name.ToLower().Replace(' ', '_');
                    string finalId = string.IsNullOrEmpty(typeSuffix) ? baseName : $"{baseName}_{typeSuffix}";

                    // 对场景中子组件的改动需记录撤销并标脏，否则改动丢失且不可撤销（参照 RefreshList 的做法）
                    Undo.RecordObject(componentRef, "Fill Bind ID");
                    ((IUIBindComponent)componentRef).SetID(finalId);
                    EditorUtility.SetDirty(componentRef);
                }
            }
            else if (bindTypeIndex == 1) // Index 模式
            {
                for (int i = 0; i < _listProp.arraySize; i++)
                {
                    var element = _listProp.GetArrayElementAtIndex(i);

                    var componentRef = element.objectReferenceValue as UIBindComponent;

                    if (componentRef == null)
                    {
                        Debug.LogError($"UIBindComponent at {i} reference is null.");
                        continue;
                    }

                    // 同上：记录撤销并标脏
                    Undo.RecordObject(componentRef, "Fill Bind Index");
                    ((IUIBindComponent)componentRef).SetIndex(i);
                    EditorUtility.SetDirty(componentRef);
                }
            }

            Debug.Log("Fill Completed.");
        }

        /// <summary>
        /// 获取UIBindComponent的泛型类型后缀（小写）
        /// </summary>
        private string GetGenericTypeSuffix(UIBindComponent component)
        {
            var componentType = component.GetType();

            // 查找 UIBindComponentGeneric<T> 基类
            var baseType = componentType.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition().Name == "UIBindComponentGeneric`1")
                {
                    // 获取泛型参数 T
                    var genericArgument = baseType.GetGenericArguments()[0];
                    return GetFriendlyTypeName(genericArgument).ToLower();
                }
                baseType = baseType.BaseType;
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取类型的友好名称（如 int 而非 Int32）
        /// </summary>
        private string GetFriendlyTypeName(System.Type type)
        {
            // C# 基本类型别名
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(double)) return "double";
            if (type == typeof(long)) return "long";
            if (type == typeof(short)) return "short";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(char)) return "char";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(decimal)) return "decimal";

            // 使用类型的简短名称
            return type.Name;
        }

        private void GenerateSetterCode()
        {
            if (_listProp.arraySize == 0)
            {
                Debug.LogWarning("No UIBindComponents found!");
                return;
            }

            var codeBuilder = new System.Text.StringBuilder();
            codeBuilder.Append("SetData(");

            int validCount = 0;
            for (int i = 0; i < _listProp.arraySize; i++)
            {
                var element = _listProp.GetArrayElementAtIndex(i);
                var component = element.objectReferenceValue as UIBindComponent;

                if (component == null) continue;

                if (validCount > 0)
                    codeBuilder.Append(", ");

                // 获取组件类型信息和默认值
                var (typeName, defaultValue) = GetComponentTypeInfo(component);

                // 根据绑定类型生成代码，包含泛型类型
                int bindTypeIndex = _bindTypeProp.enumValueIndex;
                string methodCall = bindTypeIndex == 0 // ID 模式
                    ? $"UIData.Id<{typeName}>(\"{component.ID}\", "
                    : $"UIData.Index<{typeName}>({component.Index}, ";

                codeBuilder.Append(methodCall);
                codeBuilder.Append(defaultValue);
                codeBuilder.Append(")");

                validCount++;
            }

            codeBuilder.Append(");");

            string result = codeBuilder.ToString();

            // 保存生成的代码到字段中
            _generatedCode = result;

            // 刷新Inspector显示
            Repaint();

            // 同时输出到Console
            Debug.Log($"<color=cyan>Generated SetData Code:</color>\n{result}");
        }


        private (string typeName, string defaultValue) GetComponentTypeInfo(UIBindComponent component)
        {
            var componentType = component.GetType();

            // 查找 UIBindComponentGeneric<T> 基类
            var baseType = componentType.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition().Name == "UIBindComponentGeneric`1")
                {
                    // 获取泛型参数 T
                    var genericArgument = baseType.GetGenericArguments()[0];
                    return GetTypeNameAndDefault(genericArgument);
                }
                baseType = baseType.BaseType;
            }

            // 如果找不到泛型基类，返回默认值
            return ("object", "default");
        }

        private (string typeName, string defaultValue) GetTypeNameAndDefault(System.Type type)
        {
            // 使用类型的简短名称
            string typeName = type.Name;

            // 为常见类型提供友好的默认值
            // 基本类型
            if (type == typeof(string))
                return ("string", "\"default\"");
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
                return ("int", "0");
            if (type == typeof(float))
                return ("float", "0f");
            if (type == typeof(double))
                return ("double", "0.0");
            if (type == typeof(bool))
                return ("bool", "false");

            // Unity常用类型的默认值
            if (type == typeof(UnityEngine.Color))
                return ("Color", "Color.white");
            if (type == typeof(UnityEngine.Vector2))
                return ("Vector2", "Vector2.zero");
            if (type == typeof(UnityEngine.Vector3))
                return ("Vector3", "Vector3.zero");
            if (type == typeof(UnityEngine.Vector4))
                return ("Vector4", "Vector4.zero");
            if (type == typeof(UnityEngine.Quaternion))
                return ("Quaternion", "Quaternion.identity");

            // 引用类型默认为 null
            if (!type.IsValueType)
                return (typeName, "null");

            // 值类型默认为 default
            return (typeName, "default");
        }
    }
}