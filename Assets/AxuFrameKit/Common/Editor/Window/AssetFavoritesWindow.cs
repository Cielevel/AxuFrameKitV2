using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

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
        private const string PrefsKey = "AssetFavoritesWindow_Data_V2";

        // UI Elements
        private TextField _newGroupNameField;
        private ScrollView _contentScrollView;

        [MenuItem("AxuFrameKit/Utility/Asset Favorites")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetFavoritesWindow>("Asset Favorites");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            LoadData();
        }

        private void OnDisable()
        {
            SaveDataToPrefs();
        }

        private void CreateGUI()
        {
            LoadData();

            // Load and add USS
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var dirPath = System.IO.Path.GetDirectoryName(scriptPath);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dirPath}/CommonWindowStyles.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            rootVisualElement.AddToClassList("ue5-root");

            // Window Title
            var titleLabel = new Label("Asset Favorites");
            titleLabel.AddToClassList("window-title");
            rootVisualElement.Add(titleLabel);

            // Toolbar Panel
            var toolbarPanel = new VisualElement();
            toolbarPanel.AddToClassList("toolbar-panel");
            rootVisualElement.Add(toolbarPanel);

            var createLabel = new Label("Create Group:");
            createLabel.AddToClassList("toolbar-label");
            toolbarPanel.Add(createLabel);

            _newGroupNameField = new TextField();
            _newGroupNameField.AddToClassList("ue5-text-field");
            toolbarPanel.Add(_newGroupNameField);

            var createBtn = new Button(OnCreateGroupClicked) { text = "Create" };
            createBtn.AddToClassList("primary-btn");
            toolbarPanel.Add(createBtn);

            var clearAllBtn = new Button(OnClearAllClicked) { text = "Clear All" };
            clearAllBtn.AddToClassList("danger-btn");
            clearAllBtn.style.marginLeft = 10;
            toolbarPanel.Add(clearAllBtn);

            // Content ScrollView
            _contentScrollView = new ScrollView(ScrollViewMode.Vertical);
            _contentScrollView.AddToClassList("content-scroll");
            rootVisualElement.Add(_contentScrollView);

            // Setup drag and drop
            SetupDragAndDrop();

            // Initial refresh
            RefreshContent();
        }

        private void SetupDragAndDrop()
        {
            rootVisualElement.RegisterCallback<DragEnterEvent>(evt =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            });

            rootVisualElement.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            });

            rootVisualElement.RegisterCallback<DragPerformEvent>(evt =>
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
                RefreshContent();
            });
        }

        private void RefreshContent()
        {
            if (_contentScrollView == null) return;
            _contentScrollView.Clear();

            // 1. Draw custom groups
            foreach (var group in _data.groups.ToList())
            {
                var groupContainer = CreateGroupSection(group);
                _contentScrollView.Add(groupContainer);
            }

            // 2. Draw ungrouped assets
            if (_data.ungroupedGuids.Count > 0)
            {
                var ungroupedContainer = CreateUngroupedSection();
                _contentScrollView.Add(ungroupedContainer);
            }

            // 3. Empty state
            if (_data.groups.Count == 0 && _data.ungroupedGuids.Count == 0)
            {
                var helpBox = new VisualElement();
                helpBox.AddToClassList("help-box");

                var helpText = new Label("Drag assets here to add to favorites.\nOr right-click assets in Project and select 'Add to AssetFavorites'.");
                helpText.AddToClassList("help-box-text");
                helpBox.Add(helpText);

                _contentScrollView.Add(helpBox);
            }
        }

        private VisualElement CreateGroupSection(FavoriteGroup group)
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");

            // Header
            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label($"📂 分组: {group.groupName}");
            title.AddToClassList("group-title");
            header.Add(title);

            var deleteBtn = new Button(() => OnDeleteGroupClicked(group)) { text = "×" };
            deleteBtn.AddToClassList("delete-group-btn");
            header.Add(deleteBtn);

            // Content
            var content = new VisualElement();
            content.AddToClassList("group-content");
            container.Add(content);

            if (group.guids.Count == 0)
            {
                var emptyState = new VisualElement();
                emptyState.AddToClassList("empty-state");
                var emptyText = new Label("Drag assets here or click [Group] to add.");
                emptyText.AddToClassList("empty-state-text");
                emptyState.Add(emptyText);
                content.Add(emptyState);
            }
            else
            {
                foreach (var guid in group.guids.ToList())
                {
                    var itemRow = CreateAssetRow(guid, group);
                    content.Add(itemRow);
                }
            }

            // Setup drag and drop for group
            SetupGroupDragAndDrop(container, group);

            return container;
        }

        private VisualElement CreateUngroupedSection()
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");

            // Header
            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("📦 未分组资源 (Ungrouped)");
            title.AddToClassList("group-title");
            header.Add(title);

            // Content
            var content = new VisualElement();
            content.AddToClassList("group-content");
            container.Add(content);

            foreach (var guid in _data.ungroupedGuids.ToList())
            {
                var itemRow = CreateAssetRow(guid, null);
                content.Add(itemRow);
            }

            return container;
        }

        private VisualElement CreateAssetRow(string guid, FavoriteGroup currentGroup)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            string name = obj != null ? obj.name : $"(Missing: {guid.Substring(0, 8)}...)";
            Texture icon = obj != null ? AssetDatabase.GetCachedIcon(path) : EditorGUIUtility.IconContent("console.erroricon").image;

            bool isStarred = _data.starredGuids.Contains(guid);

            var row = new VisualElement();
            row.AddToClassList("item-row");
            row.tooltip = path;

            // Star button
            var starBtn = new Button(() => OnToggleStar(guid)) { text = isStarred ? "★" : "☆" };
            starBtn.AddToClassList("star-btn");
            if (isStarred) starBtn.AddToClassList("star-btn-active");
            row.Add(starBtn);

            // Remove button
            var removeBtn = new Button(() => OnRemoveFavorite(guid, currentGroup)) { text = "✘" };
            removeBtn.AddToClassList("remove-btn");
            row.Add(removeBtn);

            // Icon
            var iconElement = new VisualElement();
            iconElement.AddToClassList("item-icon");
            if (icon != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(icon as Texture2D);
            }
            row.Add(iconElement);

            // Name/Button
            var nameBtn = new Button(() => OnAssetClicked(obj)) { text = name };
            nameBtn.AddToClassList("item-name");
            if (isStarred) nameBtn.AddToClassList("item-name-starred");
            nameBtn.style.unityTextAlign = TextAnchor.MiddleLeft;
            nameBtn.style.backgroundColor = Color.clear;
            nameBtn.style.borderLeftWidth = 0;
            nameBtn.style.borderRightWidth = 0;
            nameBtn.style.borderTopWidth = 0;
            nameBtn.style.borderBottomWidth = 0;
            row.Add(nameBtn);

            // Group button
            var groupBtn = new Button(() => ShowGroupMenu(guid, currentGroup, row)) { text = "Group" };
            groupBtn.AddToClassList("action-btn");
            row.Add(groupBtn);

            // Locate button
            var locateBtn = new Button(() => OnAssetClicked(obj)) { text = "📍" };
            locateBtn.AddToClassList("action-btn");
            row.Add(locateBtn);

            return row;
        }

        private void SetupGroupDragAndDrop(VisualElement container, FavoriteGroup group)
        {
            container.RegisterCallback<DragEnterEvent>(evt =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            });

            container.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            });

            container.RegisterCallback<DragPerformEvent>(evt =>
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
                RefreshContent();
                evt.StopPropagation();
            });
        }

        #region Event Handlers

        private void OnCreateGroupClicked()
        {
            string newName = _newGroupNameField?.value?.Trim();
            if (!string.IsNullOrEmpty(newName) && !_data.groups.Any(g => g.groupName == newName))
            {
                _data.groups.Add(new FavoriteGroup { groupName = newName });
                _newGroupNameField.value = "";
                SaveDataToPrefs();
                RefreshContent();
            }
        }

        private void OnClearAllClicked()
        {
            if (EditorUtility.DisplayDialog("Clear All", "Delete all favorites?", "Yes", "No"))
            {
                _data.groups.Clear();
                _data.ungroupedGuids.Clear();
                _data.starredGuids.Clear();
                SaveDataToPrefs();
                RefreshContent();
            }
        }

        private void OnDeleteGroupClicked(FavoriteGroup group)
        {
            if (EditorUtility.DisplayDialog("Delete Group", $"Delete group '{group.groupName}'? Assets will move to Ungrouped.", "Yes", "No"))
            {
                foreach (var guid in group.guids)
                {
                    if (!_data.ungroupedGuids.Contains(guid)) _data.ungroupedGuids.Add(guid);
                }
                _data.groups.Remove(group);
                SaveDataToPrefs();
                RefreshContent();
            }
        }

        private void OnToggleStar(string guid)
        {
            if (_data.starredGuids.Contains(guid))
                _data.starredGuids.Remove(guid);
            else
                _data.starredGuids.Add(guid);
            SaveDataToPrefs();
            RefreshContent();
        }

        private void OnRemoveFavorite(string guid, FavoriteGroup group)
        {
            if (group != null) group.guids.Remove(guid);
            _data.ungroupedGuids.Remove(guid);

            // 检查是否还在其他组里
            foreach (var g in _data.groups) g.guids.Remove(guid);

            SaveDataToPrefs();
            RefreshContent();
        }

        private void OnAssetClicked(UnityEngine.Object obj)
        {
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
        }

        private void ShowGroupMenu(string guid, FavoriteGroup currentGroup, VisualElement anchor)
        {
            GenericMenu menu = new GenericMenu();

            // 移动到其他分组
            foreach (var g in _data.groups)
            {
                bool isInThisGroup = g.guids.Contains(guid);
                var targetGroup = g; // Capture for closure
                menu.AddItem(new GUIContent(g.groupName), isInThisGroup, () => ToggleGroupMembership(guid, targetGroup));
            }

            if (currentGroup != null)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Move to Ungrouped"), false, () =>
                {
                    currentGroup.guids.Remove(guid);
                    if (!_data.ungroupedGuids.Contains(guid)) _data.ungroupedGuids.Add(guid);
                    SaveDataToPrefs();
                    RefreshContent();
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
            RefreshContent();
        }

        #endregion

        #region Helpers

        private bool IsGuidInAnyGroup(string guid)
        {
            return _data.groups.Any(g => g.guids.Contains(guid));
        }

        #endregion

        #region Data Persistence

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

        #endregion

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
            if (HasOpenInstances<AssetFavoritesWindow>())
            {
                GetWindow<AssetFavoritesWindow>().RefreshContent();
            }
        }
    }
}
