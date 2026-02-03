using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.ListView.Editor
{
    /// <summary>
    /// UIListView自定义Editor
    /// 提供更友好的Inspector界面和快速设置工具
    /// </summary>
    [CustomEditor(typeof(UIListView))]
    public class UIListViewEditor : UnityEditor.Editor
    {
        private SerializedProperty _settingsProp;
        private SerializedProperty _selectionSettingsProp;

        private void OnEnable()
        {
            _settingsProp = serializedObject.FindProperty("settings");
            _selectionSettingsProp = serializedObject.FindProperty("selectionSettings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawTitle();
            EditorGUILayout.Space(5);

            DrawSettings();
            EditorGUILayout.Space(5);

            DrawSelectionSettings();
            EditorGUILayout.Space(5);

            DrawQuickActions();
            EditorGUILayout.Space(5);

            DrawInfo();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTitle()
        {
            EditorGUILayout.LabelField("UI ListView", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "现代化ListView组件\n" +
                "特性：虚拟化渲染、对象池、多布局模式、选中管理\n" +
                "支持垂直、水平、网格三种布局",
                MessageType.Info);
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("布局设置", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_settingsProp.FindPropertyRelative("layoutMode"), new GUIContent("布局模式"));
            EditorGUILayout.PropertyField(_settingsProp.FindPropertyRelative("itemSize"), new GUIContent("Item大小"));
            EditorGUILayout.PropertyField(_settingsProp.FindPropertyRelative("spacing"), new GUIContent("间距"));

            // 只在Grid模式下显示
            var layoutMode = (UIListView.LayoutMode)_settingsProp.FindPropertyRelative("layoutMode").enumValueIndex;
            if (layoutMode == UIListView.LayoutMode.Grid)
            {
                EditorGUILayout.PropertyField(_settingsProp.FindPropertyRelative("gridConstraintCount"), new GUIContent("每行数量"));
            }

            // 边距设置
            EditorGUILayout.LabelField("边距", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_settingsProp.FindPropertyRelative("paddingTop"), new GUIContent("上"));
            EditorGUILayout.PropertyField(_settingsProp.FindPropertyRelative("paddingBottom"), new GUIContent("下"));
            EditorGUILayout.PropertyField(_settingsProp.FindPropertyRelative("paddingLeft"), new GUIContent("左"));
            EditorGUILayout.PropertyField(_settingsProp.FindPropertyRelative("paddingRight"), new GUIContent("右"));
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(_settingsProp.FindPropertyRelative("bufferCount"), new GUIContent("缓冲数量"));
            EditorGUILayout.PropertyField(_settingsProp.FindPropertyRelative("usePooling"), new GUIContent("启用对象池"));
            EditorGUILayout.PropertyField(_settingsProp.FindPropertyRelative("itemPrefab"), new GUIContent("Item预制体"));
            EditorGUILayout.PropertyField(_settingsProp.FindPropertyRelative("controlChildSize"), new GUIContent("控制子物体大小"));

            EditorGUI.indentLevel--;
        }

        private void DrawSelectionSettings()
        {
            EditorGUILayout.LabelField("选中设置", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_selectionSettingsProp.FindPropertyRelative("enableSelection"), new GUIContent("启用选中"));

            // 只有启用选中时才显示其他选项
            if (_selectionSettingsProp.FindPropertyRelative("enableSelection").boolValue)
            {
                EditorGUILayout.PropertyField(_selectionSettingsProp.FindPropertyRelative("allowMultipleSelection"), new GUIContent("允许多选"));
                EditorGUILayout.PropertyField(_selectionSettingsProp.FindPropertyRelative("allowDeselect"), new GUIContent("允许取消选中"));
            }

            EditorGUI.indentLevel--;
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("快速操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("验证设置", GUILayout.Height(25)))
            {
                ValidateSettings();
            }

            if (GUILayout.Button("创建Item模板", GUILayout.Height(25)))
            {
                CreateItemTemplate();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawInfo()
        {
            if (!Application.isPlaying)
                return;

            EditorGUILayout.LabelField("运行时信息", EditorStyles.boldLabel);

            var listView = target as UIListView;
            if (listView == null)
                return;

            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.LabelField("选中数量", listView.GetSelectedIndices().Count.ToString());

            EditorGUI.EndDisabledGroup();
        }

        private void ValidateSettings()
        {
            var listView = target as UIListView;
            bool hasErrors = false;

            // 验证Item预制体
            var itemPrefab = _settingsProp.FindPropertyRelative("itemPrefab").objectReferenceValue as GameObject;
            if (itemPrefab == null)
            {
                Debug.LogError("[UIListView] Item预制体未设置！");
                hasErrors = true;
            }
            else
            {
                var itemComponent = itemPrefab.GetComponent<UIListViewItem>();
                if (itemComponent == null)
                {
                    Debug.LogWarning($"[UIListView] Item预制体 '{itemPrefab.name}' 没有UIListViewItem组件，将在运行时自动添加。");
                }
            }

            // 验证ScrollRect
            var scrollRect = listView.GetComponent<UnityEngine.UI.ScrollRect>();
            if (scrollRect == null)
            {
                Debug.LogError("[UIListView] 缺少ScrollRect组件！");
                hasErrors = true;
            }
            else if (scrollRect.content == null)
            {
                Debug.LogError("[UIListView] ScrollRect的Content未设置！");
                hasErrors = true;
            }

            // 验证数值
            var itemSize = _settingsProp.FindPropertyRelative("itemSize").floatValue;
            if (itemSize <= 0)
            {
                Debug.LogError("[UIListView] Item大小必须大于0！");
                hasErrors = true;
            }

            var gridConstraint = _settingsProp.FindPropertyRelative("gridConstraintCount").intValue;
            if (gridConstraint <= 0)
            {
                Debug.LogError("[UIListView] 网格约束数量必须大于0！");
                hasErrors = true;
            }

            if (!hasErrors)
            {
                Debug.Log("[UIListView] 设置验证通过！");
            }
        }

        private void CreateItemTemplate()
        {
            // 这里可以实现创建Item模板的逻辑
            // 例如在场景中创建一个基础的Item GameObject
            Debug.Log("创建Item模板功能待实现");

            EditorUtility.DisplayDialog(
                "创建Item模板",
                "该功能将帮助您快速创建一个ListView Item预制体模板。\n" +
                "请确保已经准备好UI元素（Text, Image等）。",
                "确定"
            );
        }
    }
}
