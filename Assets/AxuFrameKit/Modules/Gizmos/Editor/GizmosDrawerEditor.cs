// using System;
// using System.Linq;
// using System.Reflection;
// using UnityEditor;
// using UnityEditorInternal;
// using UnityEngine;
//
// // 1. 解决 Multi-object editing not supported 问题
// namespace AxuFrameKit.Modules.Gizmos.Editor
// {
//     [CustomEditor(typeof(GizmosDrawer))]
//     [CanEditMultipleObjects] 
//     public class GizmosDrawerEditor : UnityEditor.Editor
//     {
//         private ReorderableList _reorderableList;
//         private SerializedProperty _drawersProp;
//
//         private void OnEnable()
//         {
//             _drawersProp = serializedObject.FindProperty("_drawers");
//
//             // 初始化列表
//             _reorderableList = new ReorderableList(serializedObject, _drawersProp, true, true, true, true);
//
//             // 2. 设置头部标题
//             _reorderableList.drawHeaderCallback = (Rect rect) =>
//             {
//                 EditorGUI.LabelField(rect, $"Drawers List ({_drawersProp.arraySize})");
//             };
//
//             // 3. 绘制元素 (核心修复：确保显示子类的所有属性)
//             _reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
//             {
//                 // 获取当前数组元素
//                 var element = _drawersProp.GetArrayElementAtIndex(index);
//             
//                 // 为了美观，给一点上下的 Padding
//                 rect.y      += 2;
//                 rect.height =  EditorGUIUtility.singleLineHeight;
//
//                 // 获取 managedReferenceValue 的类型名称用于显示标题
//                 string labelName = "Empty (Null)";
//                 // 注意：managedReferenceFullTypename 是 Unity 2019.3+ 的 API
//                 if (!string.IsNullOrEmpty(element.managedReferenceFullTypename))
//                 {
//                     // 提取类名，例如 "AxuFrameKit.Modules.Gizmos.GizmosDrawer+LineDrawerV3" -> "LineDrawerV3"
//                     var fullType = element.managedReferenceFullTypename;
//                     int dotIndex = fullType.LastIndexOf('.');
//                     // 处理嵌套类名称中的 '+' 号
//                     int plusIndex  = fullType.LastIndexOf('+'); 
//                     int splitIndex = Mathf.Max(dotIndex, plusIndex);
//                 
//                     if (splitIndex >= 0 && splitIndex < fullType.Length - 1)
//                         labelName = fullType.Substring(splitIndex + 1);
//                 }
//
//                 // 绘制属性字段，includeChildren = true 才能把子类里的 start/end 画出来
//                 EditorGUI.PropertyField(rect, element, new GUIContent(labelName), true);
//             };
//
//             // 4. 动态计算高度 (核心修复：防止内容重叠或显示不全)
//             _reorderableList.elementHeightCallback = (int index) =>
//             {
//                 var element = _drawersProp.GetArrayElementAtIndex(index);
//                 // 获取属性的完整高度（包含展开的子属性）
//                 float propertyHeight = EditorGUI.GetPropertyHeight(element, true);
//                 // 加上一点间距
//                 return propertyHeight + 6; 
//             };
//
//             // 5. 覆盖添加按钮逻辑 (核心修复：弹出菜单)
//             _reorderableList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
//             {
//                 var menu = new GenericMenu();
//             
//                 // 查找所有 BaseDrawer 的子类
//                 // 这里使用 TypeCache (Unity 2019.2+) 性能更好，或者是标准的反射
//                 var baseType = typeof(GizmosDrawer.BaseDrawer);
//                 var types = Assembly.GetAssembly(baseType).GetTypes()
//                                     .Where(t => t.IsSubclassOf(baseType) && !t.IsAbstract)
//                                     .OrderBy(t => t.Name);
//
//                 foreach (var type in types)
//                 {
//                     // menu.AddItem(显示名称, 是否勾选, 点击回调, 回调参数)
//                     menu.AddItem(new GUIContent(type.Name), false, OnAddDrawerMenuSelected, type);
//                 }
//             
//                 if (!types.Any())
//                 {
//                     menu.AddItem(new GUIContent("No Subclasses Found"), false, null);
//                 }
//
//                 menu.ShowAsContext();
//             };
//         }
//
//         // 菜单回调
//         private void OnAddDrawerMenuSelected(object targetTypeObj)
//         {
//             Type targetType = (Type)targetTypeObj;
//
//             // 必须先 Update 获取最新数据
//             serializedObject.Update();
//         
//             // 数组扩容
//             int index = _drawersProp.arraySize;
//             _drawersProp.arraySize++;
//         
//             // 获取新生成的元素
//             var newElement = _drawersProp.GetArrayElementAtIndex(index);
//         
//             // 实例化 C# 对象并赋值给 managedReferenceValue
//             var instance = Activator.CreateInstance(targetType);
//             newElement.managedReferenceValue = instance;
//         
//             // 必须 Apply 才能保存修改
//             serializedObject.ApplyModifiedProperties();
//         }
//
//         public override void OnInspectorGUI()
//         {
//             // 这一步不能省
//             serializedObject.Update();
//         
//             // 绘制列表
//             if (_reorderableList != null)
//             {
//                 _reorderableList.DoLayoutList();
//             }
//             else
//             {
//                 // Fallback 
//                 EditorGUILayout.PropertyField(_drawersProp);
//             }
//
//             // 绘制列表之外的其他属性（如果有）
//             DrawPropertiesExcluding(serializedObject, "_drawers", "m_Script");
//         
//             serializedObject.ApplyModifiedProperties();
//         }
//     }
// }