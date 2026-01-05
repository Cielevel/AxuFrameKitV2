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
            public string guid;
            public string cachedName; // 缓存名字，也用于显示
        }

        [Serializable]
        public class FavoriteGroup
        {
            public string groupName = "New Group";
            public List<string> guids = new List<string>(); // 存储关联资源的GUID
        }

        [Serializable]
        public class SaveData
        {
            public List<FavoriteGroup> groups = new();
            public List<string> ungroupedGuids = new(); // 收藏但未分组的
            public List<string> starredGuids = new(); // 高亮的GUID
        }

        // ------------- 运行时变量 -------------
        private static SaveData _data;
        private Vector2 _scrollPos;
        private string _newGroupName = "";
        private const string PrefsKey = "AssetFavoritesWindow_Data_V2";

        [MenuItem("AxuFrameKit/AssetFavorites")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetFavoritesWindow>("Asset Favorites");
            window.minSize = new Vector2(350, 400);
        }

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
            EditorGUILayout.Space(5);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Asset Favorites", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear All", EditorStyles.miniButton, GUILayout.Width(70)))
                {
                    if (EditorUtility.DisplayDialog("Clear All", "Delete all favorites?", "Yes", "No"))
                    {
                        _data.groups.Clear();
                        _data.ungroupedGuids.Clear();
                        SaveDataToPrefs();
                    }
                }
            }
            EditorGUILayout.Space(5);

            // ---- 分组管理 ----
            DrawGroupManagement();

            EditorGUILayout.Space(5);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // 1. 绘制自定义分组
            foreach (var group in _data.groups.ToList())
            {
                DrawGroupSection(group);
                EditorGUILayout.Space(5);
            }

            // 2. 绘制未分组资源
            if (_data.ungroupedGuids.Count > 0)
            {
                DrawUngroupedSection();
            }

            // 底部拖拽区域提醒
            if (_data.groups.Count == 0 && _data.ungroupedGuids.Count == 0)
            {
                EditorGUILayout.HelpBox("Drag assets here to add to favorites.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();

            // 处理全局拖拽
            HandleGlobalDragDrop();
        }

        private void DrawGroupManagement()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                _newGroupName = EditorGUILayout.TextField(_newGroupName);
                if (GUILayout.Button("Create Group", GUILayout.Width(100)))
                {
                    if (!string.IsNullOrEmpty(_newGroupName) && !_data.groups.Any(g => g.groupName == _newGroupName))
                    {
                        _data.groups.Add(new FavoriteGroup { groupName = _newGroupName });
                        _newGroupName = "";
                        SaveDataToPrefs();
                    }
                }
            }
        }

        private void DrawGroupSection(FavoriteGroup group)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(group.groupName, EditorStyles.boldLabel);
                if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Delete Group", $"Delete group '{group.groupName}'? Assets will move to Ungrouped.", "Yes", "No"))
                    {
                        foreach (var guid in group.guids)
                        {
                            if (!_data.ungroupedGuids.Contains(guid)) _data.ungroupedGuids.Add(guid);
                        }
                        _data.groups.Remove(group);
                        SaveDataToPrefs();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            EditorGUILayout.Space(2);

            if (group.guids.Count == 0)
            {
                EditorGUILayout.LabelField("Drag assets here.", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var guid in group.guids.ToList())
                {
                    DrawAssetRow(guid, group);
                }
            }

            // 组内拖拽
            HandleBoxDragDrop(GUILayoutUtility.GetLastRect(), group);

            EditorGUILayout.EndVertical();
        }

        private void DrawUngroupedSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Ungrouped", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            foreach (var guid in _data.ungroupedGuids.ToList())
            {
                DrawAssetRow(guid, null);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawAssetRow(string guid, FavoriteGroup currentGroup)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            string name = obj != null ? obj.name : "(Missing Asset)";
            Texture icon = obj != null ? AssetDatabase.GetCachedIcon(path) : EditorGUIUtility.IconContent("console.erroricon").image;

            bool isStarred = _data.starredGuids.Contains(guid);

            // 设置背景色逻辑：如果被收藏（Star），整行背景变绿
            Color originalColor = GUI.backgroundColor;
            if (isStarred)
                GUI.backgroundColor = Color.green;

            // 使用 "box" 样式以确保背景色能显示出来
            // EditorGUILayout.BeginHorizontal(isStarred ? "box" : GUIStyle.none);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Star 按钮
            GUI.backgroundColor = isStarred ? Color.yellow : originalColor;
            if (GUILayout.Button(isStarred ? "★" : "☆", GUILayout.Width(30)))
            {
                ToggleStar(guid);
            }
            if (isStarred)
                GUI.backgroundColor = Color.green;
            else
                GUI.backgroundColor = originalColor; // 恢复为行背景色

            // 移除按钮 (✘)
            if (GUILayout.Button("✘", GUILayout.Width(30)))
            {
                RemoveFavorite(guid, currentGroup);
            }

            // 资源按钮
            GUIContent content = new GUIContent(" " + name, icon);
            if (GUILayout.Button(content, EditorStyles.label, GUILayout.Height(20), GUILayout.ExpandWidth(true)))
            {
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
            }

            // Group 按钮
            if (GUILayout.Button("Group", GUILayout.Width(60)))
            {
                ShowGroupMenu(guid, currentGroup);
            }

            // Find 按钮
            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = originalColor;
        }

        private void ToggleStar(string guid)
        {
            if (_data.starredGuids.Contains(guid))
                _data.starredGuids.Remove(guid);
            else
                _data.starredGuids.Add(guid);
            SaveDataToPrefs();
        }

        private void ShowGroupMenu(string guid, FavoriteGroup currentGroup)
        {
            GenericMenu menu = new GenericMenu();

            // 移动到其他分组
            foreach (var g in _data.groups)
            {
                bool isInThisGroup = g.guids.Contains(guid);
                menu.AddItem(new GUIContent(g.groupName), isInThisGroup, () => ToggleGroupMembership(guid, g));
            }

            if (currentGroup != null)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Move to Ungrouped"), false, () =>
                {
                    currentGroup.guids.Remove(guid);
                    if (!_data.ungroupedGuids.Contains(guid)) _data.ungroupedGuids.Add(guid);
                    SaveDataToPrefs();
                });
            }

            menu.ShowAsContext();
        }

        private void ToggleGroupMembership(string guid, FavoriteGroup targetGroup)
        {
            // 如果已在目标组，则移除
            if (targetGroup.guids.Contains(guid))
            {
                targetGroup.guids.Remove(guid);
                if (!IsGuidInAnyGroup(guid) && !_data.ungroupedGuids.Contains(guid))
                    _data.ungroupedGuids.Add(guid);
            }
            else
            {
                // 加入目标组
                targetGroup.guids.Add(guid);
                _data.ungroupedGuids.Remove(guid);
            }
            SaveDataToPrefs();
        }

        private bool IsGuidInAnyGroup(string guid)
        {
            return _data.groups.Any(g => g.guids.Contains(guid));
        }

        private void RemoveFavorite(string guid, FavoriteGroup group)
        {
            if (group != null) group.guids.Remove(guid);
            _data.ungroupedGuids.Remove(guid);

            // 检查是否还在其他组里
            foreach (var g in _data.groups) g.guids.Remove(guid);

            SaveDataToPrefs();
        }

        // ------------- 拖拽系统 -------------

        private void HandleBoxDragDrop(Rect rect, FavoriteGroup group)
        {
            Event evt = Event.current;
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && rect.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
                        if (!string.IsNullOrEmpty(guid) && !group.guids.Contains(guid))
                        {
                            group.guids.Add(guid);
                            _data.ungroupedGuids.Remove(guid);
                        }
                    }
                    SaveDataToPrefs();
                }
                evt.Use();
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
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
                        if (!string.IsNullOrEmpty(guid) && !IsGuidInAnyGroup(guid) && !_data.ungroupedGuids.Contains(guid))
                        {
                            _data.ungroupedGuids.Add(guid);
                        }
                    }
                    SaveDataToPrefs();
                }
                evt.Use();
            }
        }

        // ------------- 数据持久化 -------------

        private static void LoadData()
        {
            if (_data != null) return;
            string json = EditorPrefs.GetString(PrefsKey, "");
            _data = !string.IsNullOrEmpty(json) ? JsonUtility.FromJson<SaveData>(json) : new SaveData();
        }

        private static void SaveDataToPrefs()
        {
            if (_data == null) return;
            EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(_data));
        }

        [MenuItem("Assets/Favorites/Add to AssetFavorites", false, 20)]
        public static void AddSelectedToFavorites()
        {
            LoadData();
            foreach (var guid in Selection.assetGUIDs)
            {
                if (!_data.ungroupedGuids.Contains(guid) && !_data.groups.Any(g => g.guids.Contains(guid)))
                {
                    _data.ungroupedGuids.Add(guid);
                }
            }
            SaveDataToPrefs();
            GetWindow<AssetFavoritesWindow>().Repaint();
        }
    }
}
