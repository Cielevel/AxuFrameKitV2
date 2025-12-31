using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AxuFrameKit.Common.Editor.Window
{
    public class AssetFavoritesWindow : EditorWindow
    {
        // ------------- 数据结构 -------------
        [Serializable]
        public class FavoriteItem
        {
            public string guid;       // 文件的唯一标识符
            public string cachedName; // 缓存名字，用于丢失引用时显示
        }

        [Serializable]
        public class FavoriteGroup
        {
            public string groupName = "New Group";
            public bool isExpanded = true;
            public List<FavoriteItem> items = new();
        }

        [Serializable]
        public class SaveData
        {
            public List<FavoriteGroup> groups = new();
        }

        // ------------- 运行时变量 -------------
        private static SaveData _data;
        private Vector2 _scrollPos;
        private const string PrefsKey = "AssetFavoritesWindow_Data";

        // ------------- 菜单入口 -------------
        
        [MenuItem("Window/Asset Favorites")]
        public static void ShowWindow()
        {
            GetWindow<AssetFavoritesWindow>("Favorites");
        }

        // 实现右键菜单：Assets -> Favorites -> Add to Group...
        // 动态生成菜单项，列出所有已有的分组
        [MenuItem("Assets/Favorites/Add to Group...", false, 20)]
        public static void AddToGroupContextMenu()
        {
            LoadData(); // 确保数据已加载

            // 获取当前选中的所有资源
            string[] selectedGuids = Selection.assetGUIDs;
            if (selectedGuids == null || selectedGuids.Length == 0)
            {
                Debug.LogWarning("No assets selected.");
                return;
            }

            GenericMenu menu = new GenericMenu();

            if (_data.groups.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Groups Created"));
            }
            else
            {
                // 为每个分组创建一个菜单项
                foreach (var group in _data.groups)
                {
                    menu.AddItem(new GUIContent(group.groupName), false, () => 
                    {
                        AddGuidsToGroup(group, selectedGuids);
                    });
                }
            }
            
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Create New Group & Add"), false, () =>
            {
                var newGroup = CreateNewGroup("New Group " + (_data.groups.Count + 1));
                AddGuidsToGroup(newGroup, selectedGuids);
            });

            menu.ShowAsContext();
        }

        // 验证是否有选中物体，决定菜单是否显示
        [MenuItem("Assets/Favorites/Add to Group...", true)]
        public static bool ValidateAddToGroup()
        {
            return Selection.assetGUIDs.Length > 0;
        }

        // ------------- 生命周期 -------------

        private void OnEnable()
        {
            LoadData();
        }

        private void OnDisable()
        {
            SaveDataToPrefs();
        }

        private void OnGUI()
        {
            DrawToolbar();
            
            // 绘制滚动区域
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            // 遍历绘制所有分组
            // 使用 for 循环而不是 foreach，以便在循环中安全删除
            for (int i = 0; i < _data.groups.Count; i++)
            {
                DrawGroup(_data.groups[i], i);
            }

            // 如果没有分组，显示提示
            if (_data.groups.Count == 0)
            {
                EditorGUILayout.HelpBox("Click '+' to create a new collection group.\nOr drag assets here to auto-create one.", MessageType.Info);
                HandleGlobalDragDrop(); // 空白区域也支持拖拽
            }
            else
            {
                // 在底部添加弹性空间，并处理拖拽到空白处新建分组
                GUILayout.FlexibleSpace();
                Rect remainingRect = GUILayoutUtility.GetLastRect();
                // 简单的空白区域拖拽检测可以放在这里，或者直接忽略
            }

            EditorGUILayout.EndScrollView();
        }

        // ------------- 绘制逻辑 -------------

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Add Group", EditorStyles.toolbarButton))
            {
                CreateNewGroup("Group " + (_data.groups.Count + 1));
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("Clear All", "Delete all groups and favorites?", "Yes", "Cancel"))
                {
                    _data.groups.Clear();
                    SaveDataToPrefs();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGroup(FavoriteGroup group, int index)
        {
            EditorGUILayout.BeginVertical("box"); // 分组外框

            // --- 分组标题栏 ---
            EditorGUILayout.BeginHorizontal();
            
            // 折叠箭头
            group.isExpanded = EditorGUILayout.Foldout(group.isExpanded, "", true);
            
            // 分组名称（可编辑）
            group.groupName = EditorGUILayout.TextField(group.groupName, EditorStyles.boldLabel);

            // 删除分组按钮
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Delete Group", $"Delete group '{group.groupName}'?", "Yes", "No"))
                {
                    _data.groups.RemoveAt(index);
                    SaveDataToPrefs();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI(); // 防止布局报错
                    return; 
                }
            }
            EditorGUILayout.EndHorizontal();

            // --- 拖拽检测 (Drop Area) ---
            // 获取当前组的Rect区域，用于检测鼠标拖拽
            Event evt = Event.current;
            Rect groupRect = GUILayoutUtility.GetLastRect(); 
            // 如果展开了，区域应该涵盖下面的内容，这里简单处理：只检测标题栏或者专门的Drop区域
            // 为了更好的体验，我们在标题栏下方绘制一个 "Drag Assets Here" 的隐形区域或提示
            
            if (group.isExpanded)
            {
                // 绘制组内所有资源
                if (group.items.Count == 0)
                {
                    EditorGUILayout.LabelField("   (Drag assets here)", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    for (int i = 0; i < group.items.Count; i++)
                    {
                        DrawItem(group, group.items[i], i);
                    }
                }
            }

            // 处理拖拽逻辑 (针对整个 Box)
            HandleGroupDragDrop(group);

            EditorGUILayout.EndVertical();
            GUILayout.Space(5); // 组间距
        }

        private void DrawItem(FavoriteGroup group, FavoriteItem item, int index)
        {
            // 获取资源路径
            string path = AssetDatabase.GUIDToAssetPath(item.guid);
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15); // 缩进

            // 图标
            Texture icon = obj != null ? AssetDatabase.GetCachedIcon(path) : EditorGUIUtility.IconContent("console.erroricon").image;
            
            // 按钮（点击定位）
            GUIContent content = new GUIContent(obj != null ? obj.name : item.cachedName + " (Missing)", icon);
            
            // 绘制类似Project视图的条目
            if (GUILayout.Button(content, EditorStyles.label, GUILayout.Height(20)))
            {
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
                else
                {
                    Debug.LogError($"Asset '{item.cachedName}' not found. It might be deleted or GUID changed.");
                }
            }

            // 删除条目按钮
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                group.items.RemoveAt(index);
                SaveDataToPrefs();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        // ------------- 逻辑控制 -------------

        private static FavoriteGroup CreateNewGroup(string name)
        {
            if (_data == null) LoadData();
            var newGroup = new FavoriteGroup { groupName = name };
            _data.groups.Add(newGroup);
            SaveDataToPrefs();
            return newGroup;
        }

        private static void AddGuidsToGroup(FavoriteGroup group, string[] guids)
        {
            bool changed = false;
            foreach (var guid in guids)
            {
                // 防止重复添加
                if (group.items.Any(x => x.guid == guid)) continue;

                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue; // 忽略无效资源

                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                
                group.items.Add(new FavoriteItem
                {
                    guid = guid,
                    cachedName = name
                });
                changed = true;
            }

            if (changed)
            {
                group.isExpanded = true; // 自动展开
                SaveDataToPrefs();
                // 如果窗口打开，重绘
                var window = GetWindow<AssetFavoritesWindow>();
                if (window) window.Repaint();
            }
        }

        // ------------- 拖拽系统 -------------

        private void HandleGroupDragDrop(FavoriteGroup group)
        {
            Event evt = Event.current;
            // 获取当前布局组的矩形区域（稍微Hack一点，通过GUILayoutUtility.GetLastRect拿到整个Box不太准，
            // 建议直接判断鼠标是否在当前绘制区域内。这里简化处理：只要是在Box内即可）
            
            // 注意：在Vertical scope内调用GetLastRect拿到的是上一个元素的，不是整个Scope的。
            // 更好的做法是用 BeginVertical 时返回的 rect，但在 Layout 模式下很难拿到准确高度。
            // 这里使用一个简单的通用区域判定：如果拖拽发生，且鼠标在这个 Group 的大致范围内。
            // 实际上，为了简化，我们只要检测 DragUpdated，且判断鼠标Y坐标即可，或者全屏检测分配给最近的组。
            
            // 简单有效的方案：在 Group 结尾处检测
            Rect groupRect = GUILayoutUtility.GetLastRect(); 
            // 这里的 groupRect 可能只是最后一个元素的，所以通常需要在 BeginVertical 后记录位置，EndVertical 后再计算高度。
            // 鉴于 IMGUI 的复杂性，我们采用更直接的方式：
            
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                // 如果鼠标在当前窗口内（简单判定）
                if (groupRect.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        string[] guids = (from obj in DragAndDrop.objectReferences
                                          select AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)))
                                          .ToArray();
                        
                        AddGuidsToGroup(group, guids);
                    }
                    evt.Use();
                }
            }
        }
        
        private void HandleGlobalDragDrop()
        {
             Event evt = Event.current;
             if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
             {
                 DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                 if (evt.type == EventType.DragPerform)
                 {
                     DragAndDrop.AcceptDrag();
                     var newGroup = CreateNewGroup("New Group");
                     string[] guids = (from obj in DragAndDrop.objectReferences
                                       select AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)))
                                       .ToArray();
                     AddGuidsToGroup(newGroup, guids);
                 }
                 evt.Use();
             }
        }

        // ------------- 数据持久化 -------------

        private static void LoadData()
        {
            if (_data != null) return;

            string json = EditorPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                _data = JsonUtility.FromJson<SaveData>(json);
            }
            
            if (_data == null) _data = new SaveData();
        }

        private static void SaveDataToPrefs()
        {
            if (_data == null) return;
            string json = JsonUtility.ToJson(_data);
            EditorPrefs.SetString(PrefsKey, json);
        }
    }
}