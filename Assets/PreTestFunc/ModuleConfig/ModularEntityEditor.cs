#if UNITY_EDITOR
// ============================================
// 4. 自定义Editor实现
// ============================================
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace PreTestFunc.ModuleConfig
{
    [CustomEditor(typeof(ModularEntity))]
    public class ModularEntityEditor : Editor
    {
        private ReorderableList reorderableList;
    
        // 定义可添加的模块类型
        private static readonly System.Type[] AvailableModuleTypes = new System.Type[]
        {
            typeof(HealthModule),
            typeof(MovementModule),
            typeof(AttackModule),
            typeof(ShieldModule)
        };
    
        void OnEnable()
        {
            SetupReorderableList();
        }
    
        void SetupReorderableList()
        {
            var modulesProperty = serializedObject.FindProperty("modules");
        
            reorderableList = new ReorderableList(serializedObject, modulesProperty, true, true, false, true);
        
            // 绘制列表头部
            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Modules");
            };
        
            // 绘制每个元素
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = modulesProperty.GetArrayElementAtIndex(index);
                rect.y      += 2;
                rect.height =  EditorGUIUtility.singleLineHeight;
            
                if (element.managedReferenceValue != null)
                {
                    var module = element.managedReferenceValue as ModuleBase;
                
                    // 显示模块名称为折叠标题
                    element.isExpanded = EditorGUI.Foldout(
                                                           new Rect(rect.x, rect.y, rect.width, rect.height),
                                                           element.isExpanded,
                                                           module.GetModuleName(),
                                                           true
                                                          );
                
                    if (element.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        rect.y += EditorGUIUtility.singleLineHeight + 2;
                    
                        // 绘制模块的所有字段
                        SerializedProperty prop        = element.Copy();
                        SerializedProperty endProperty = prop.GetEndProperty();
                        prop.NextVisible(true); // 跳过第一个（类型信息）
                    
                        while (prop.NextVisible(false) && !SerializedProperty.EqualContents(prop, endProperty))
                        {
                            rect.height = EditorGUI.GetPropertyHeight(prop, true);
                            EditorGUI.PropertyField(rect, prop, true);
                            rect.y += rect.height + 2;
                        }
                    
                        EditorGUI.indentLevel--;
                    }
                }
            };
        
            // 计算元素高度
            reorderableList.elementHeightCallback = (int index) =>
            {
                var element = modulesProperty.GetArrayElementAtIndex(index);
            
                if (!element.isExpanded)
                {
                    return EditorGUIUtility.singleLineHeight + 4;
                }
            
                float height = EditorGUIUtility.singleLineHeight + 4;
            
                SerializedProperty prop        = element.Copy();
                SerializedProperty endProperty = prop.GetEndProperty();
                prop.NextVisible(true);
            
                while (prop.NextVisible(false) && !SerializedProperty.EqualContents(prop, endProperty))
                {
                    height += EditorGUI.GetPropertyHeight(prop, true) + 2;
                }
            
                return height;
            };
        }
    
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
        
            // 绘制默认的脚本字段
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((ModularEntity)target), typeof(ModularEntity), false);
            GUI.enabled = true;
        
            EditorGUILayout.Space();
        
            // 绘制可重新排序的列表
            reorderableList.DoLayoutList();
        
            EditorGUILayout.Space();
        
            // 关键：添加模块的下拉菜单按钮
            if (GUILayout.Button("Add Module", GUILayout.Height(30)))
            {
                ShowModuleMenu();
            }
        
            serializedObject.ApplyModifiedProperties();
        }
    
        void ShowModuleMenu()
        {
            GenericMenu menu = new GenericMenu();
        
            var modulesProperty = serializedObject.FindProperty("modules");
            var existingModules = new HashSet<System.Type>();
        
            // 收集已添加的模块类型
            for (int i = 0; i < modulesProperty.arraySize; i++)
            {
                var element = modulesProperty.GetArrayElementAtIndex(i);
                if (element.managedReferenceValue != null)
                {
                    existingModules.Add(element.managedReferenceValue.GetType());
                }
            }
        
            // 为每个可用模块添加菜单项
            foreach (var moduleType in AvailableModuleTypes)
            {
                // 如果已存在该模块，显示为禁用
                bool alreadyExists = existingModules.Contains(moduleType);
            
                string menuPath = moduleType.Name.Replace("Module", "");
            
                if (alreadyExists)
                {
                    menu.AddDisabledItem(new GUIContent(menuPath));
                }
                else
                {
                    menu.AddItem(new GUIContent(menuPath), false, () => AddModule(moduleType));
                }
            }
        
            menu.ShowAsContext();
        }
    
        void AddModule(System.Type moduleType)
        {
            var modulesProperty = serializedObject.FindProperty("modules");
        
            serializedObject.Update();
        
            modulesProperty.arraySize++;
            var newElement = modulesProperty.GetArrayElementAtIndex(modulesProperty.arraySize - 1);
            newElement.managedReferenceValue = System.Activator.CreateInstance(moduleType);
        
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif