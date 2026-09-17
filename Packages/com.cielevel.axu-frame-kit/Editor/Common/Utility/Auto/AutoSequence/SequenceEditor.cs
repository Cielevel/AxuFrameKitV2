#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Auto.AutoSequence
{
    /// <summary>
    /// 自定义 Sequence Inspector，用于在不依赖 Odin 的情况下，
    /// 实现对抽象类 List 的子类注入功能。
    /// </summary>
    [CustomEditor(typeof(SequencePlayer), true)]
    public class SequenceEditor : UnityEditor.Editor
    {
        private SerializedProperty _itemListProperty;
        private ReorderableList _reorderableList;

        // 缓存所有可用的 Item 子类类型
        private List<Type> _availableItemTypes;
        private string[] _itemTypeNames;

        // 列表元素头部样式与内容的静态缓存，避免 drawElementCallback 内每个元素每帧 new
        private static GUIStyle _headerStyle;
        private static GUIContent _headerContent;



        private void OnEnable()
        {
            _itemListProperty = serializedObject.FindProperty("itemList");

            // 查找所有 Item 的子类
            CacheAvailableItemTypes();

            // 创建 ReorderableList
            CreateReorderableList();
        }

        /// <summary>
        /// 缓存所有可用的 Item 子类类型
        /// </summary>
        private void CacheAvailableItemTypes()
        {
            _availableItemTypes = new List<Type>();

            // 获取所有程序集中继承自 SequenceNonOdin.Item 的非抽象类
            var baseType = typeof(SequencePlayer.Item);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t))
                        .ToList();

                    _availableItemTypes.AddRange(types);
                }
                catch (ReflectionTypeLoadException)
                {
                    // 忽略无法加载的程序集
                }
            }

            // 生成显示名称
            _itemTypeNames = _availableItemTypes.Select(t => GetDisplayName(t)).ToArray();
        }

        /// <summary>
        /// 获取类型的显示名称
        /// </summary>
        private string GetDisplayName(Type type)
        {
            // 如果是嵌套类，使用 ClassName.NestedName 的格式
            if (type.DeclaringType != null)
            {
                return $"{type.DeclaringType.Name}/{type.Name}";
            }
            return type.Name;
        }

        /// <summary>
        /// 创建可重排序列表
        /// </summary>
        private void CreateReorderableList()
        {
            _reorderableList = new ReorderableList(serializedObject, _itemListProperty, true, true, true, true);

            // 绘制列表头
            _reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Sequence Items", EditorStyles.boldLabel);
            };

            // 计算元素高度
            _reorderableList.elementHeightCallback = (int index) =>
            {
                if (index >= _itemListProperty.arraySize) return EditorGUIUtility.singleLineHeight;

                var element = _itemListProperty.GetArrayElementAtIndex(index);
                if (element.managedReferenceValue == null) return EditorGUIUtility.singleLineHeight + 4;

                // 使用 SerializedProperty.isExpanded 来控制折叠
                if (!element.isExpanded)
                    return EditorGUIUtility.singleLineHeight + 6;

                // 展开状态，计算所有属性的高度
                float height = EditorGUIUtility.singleLineHeight + 4; // 标题行

                var iterator = element.Copy();
                var endProperty = element.GetEndProperty();
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    enterChildren = false;
                    height += EditorGUI.GetPropertyHeight(iterator, true) + 2;
                }

                return height + 10;
            };

            // 绘制列表元素
            _reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index >= _itemListProperty.arraySize) return;

                var element = _itemListProperty.GetArrayElementAtIndex(index);

                if (element.managedReferenceValue == null)
                {
                    EditorGUI.LabelField(rect, $"[{index}] <null - click + to add>");
                    return;
                }

                var item = element.managedReferenceValue as SequencePlayer.Item;
                if (item == null) return;

                // 获取背景颜色
                Color bgColor = item.Color;
                EditorGUI.DrawRect(new Rect(rect.x - 20, rect.y, rect.width + 24, rect.height), bgColor);

                // 绘制折叠箭头和标题
                Rect foldoutRect = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);

                string itemName = string.IsNullOrEmpty(item.Name) ? item.GetType().Name : item.Name;
                string displayTitle = itemName;

                // 准备样式 - 使用普通 foldout 样式（透明无背景），但加上粗体和裁剪
                // 样式与内容均静态缓存复用，仅原地更新文本，避免每个元素每帧分配
                if (_headerStyle == null)
                {
                    _headerStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold,
                        fixedWidth = 0,
                        stretchWidth = true
                    };
                }
                if (_headerContent == null) _headerContent = new GUIContent();

                // 调整一下文字颜色，确保在深色自定义背景上清晰可见（可选，视情况而定，默认颜色通常没问题）
                // _headerStyle.normal.textColor = Color.white;

                // 使用 GUIContent 以便支持 Tooltip
                _headerContent.text = displayTitle;
                _headerContent.tooltip = displayTitle;

                // 直接使用 element.isExpanded 存储状态
                element.isExpanded = EditorGUI.Foldout(foldoutRect, element.isExpanded, _headerContent, true, _headerStyle);

                if (element.isExpanded)
                {
                    // 绘制所有属性
                    float yOffset = EditorGUIUtility.singleLineHeight + 4;

                    var iterator = element.Copy();
                    var endProperty = element.GetEndProperty();
                    bool enterChildren = true;

                    while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
                    {
                        enterChildren = false;

                        float propHeight = EditorGUI.GetPropertyHeight(iterator, true);
                        Rect propRect = new Rect(rect.x + 15, rect.y + yOffset, rect.width - 15, propHeight);

                        EditorGUI.PropertyField(propRect, iterator, true);
                        yOffset += propHeight + 2;
                    }
                }
            };

            // 添加元素下拉菜单
            _reorderableList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
            {
                var menu = new GenericMenu();

                for (int i = 0; i < _availableItemTypes.Count; i++)
                {
                    var type = _availableItemTypes[i];
                    menu.AddItem(new GUIContent(_itemTypeNames[i]), false, () => AddItemOfType(type));
                }

                menu.ShowAsContext();
            };

            // 删除元素时清理折叠状态
            _reorderableList.onRemoveCallback = (ReorderableList list) =>
            {
                {
                    _itemListProperty.DeleteArrayElementAtIndex(list.index);
                    serializedObject.ApplyModifiedProperties();
                }
            };

            // 重排序时更新折叠状态 - 自动跟随 property，无需处理
        }

        /// <summary>
        /// 添加指定类型的 Item
        /// </summary>
        private void AddItemOfType(Type type)
        {
            var newItem = Activator.CreateInstance(type);

            int newIndex = _itemListProperty.arraySize;
            _itemListProperty.InsertArrayElementAtIndex(newIndex);

            var newElement = _itemListProperty.GetArrayElementAtIndex(newIndex);
            newElement.managedReferenceValue = newItem;

            newElement.isExpanded = true; // 默认展开新添加的元素

            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 绘制默认的脚本引用
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            // 绘制标题和说明
            // EditorGUILayout.LabelField("Sequence (Non-Odin)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "可视化时序工具，通过配置事件顺序，实现自动化。\n" +
                "点击 + 按钮添加不同类型的 Item，可以拖拽排序。\n" +
                "注意⚠️：事件模式下，SequencePlayer如果添加了相同的事件，可能会循环触发，请确保顺序正确。",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            // 播放模式设置
            var playModeProp = serializedObject.FindProperty("_playMode");
            if (playModeProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Play Mode", GUILayout.Width(EditorGUIUtility.labelWidth));

                int currentIndex = playModeProp.enumValueIndex;
                // Manual=0, Auto=1, ByChannel=2
                string[] options = new string[] { "手动", "自动", "事件" };

                int newIndex = GUILayout.Toolbar(currentIndex, options);

                if (newIndex != currentIndex)
                {
                    playModeProp.enumValueIndex = newIndex;

                    // 如果切换至非事件模式，将 _eventChannel 置空
                    if (newIndex != 2)
                    {
                        var eventChannelProp = serializedObject.FindProperty("_eventChannel");
                        if (eventChannelProp != null)
                        {
                            eventChannelProp.objectReferenceValue = null;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                // 如果选择了 "事件" (ByChannel)，显示 EventChannel 字段
                if (newIndex == 2)
                {
                    var eventChannelProp = serializedObject.FindProperty("_eventChannel");
                    if (eventChannelProp != null)
                    {
                        EditorGUILayout.PropertyField(eventChannelProp);
                    }
                }
            }

            EditorGUILayout.Space(5);

            // 绘制运行时信息
            var currentIndexProp = serializedObject.FindProperty("_currentIndex");
            if (currentIndexProp != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(currentIndexProp);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // 绘制 Play 按钮
            if (Application.isPlaying)
            {
                if (GUILayout.Button("Play Sequence", GUILayout.Height(30)))
                {
                    var sequence = target as SequencePlayer;
                    if (sequence != null)
                    {
                        sequence.Play();
                    }
                }
                EditorGUILayout.Space(5);
            }

            // 绘制列表
            _reorderableList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
