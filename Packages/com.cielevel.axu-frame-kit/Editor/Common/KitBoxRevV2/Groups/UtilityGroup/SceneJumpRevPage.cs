using System;
using System.Collections.Generic;
using System.Linq;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.UtilityGroup
{
    public class SceneJumpRevPage : IKitPageRevV2
    {
        private const string FavoriteScenesKey = "SceneJumper_FavoriteScenes_Rev";
        private const string GroupsKey = "SceneJumper_CustomGroups_Rev";

        public string Title => "场景跳转";
        private IKitBoxRevV2 _kitBox;

        private List<SceneInfo> allScenes = new List<SceneInfo>();
        private List<string> favoriteSceneGuids = new List<string>();
        private List<SceneGroup> customGroups = new List<SceneGroup>();

        // UI Elements
        private TextField _newGroupNameField;
        private ScrollView _contentScrollView;
        private VisualElement _root;

        public void Init(IKitBoxRevV2 kitBox)
        {
            _kitBox = kitBox;
            LoadFavoriteScenes();
            LoadGroups();
            CollectScenes();
            EditorApplication.projectChanged += OnProjectChanged;
        }

        public VisualElement CreateContent()
        {
            _root = new VisualElement();

            // Load CommonWindowStyles.uss — find by asset name instead of relative path (robust across directory moves)
            string[] ussGuids = AssetDatabase.FindAssets("CommonWindowStyles t:StyleSheet");
            if (ussGuids.Length > 0)
            {
                var styleSheetPath = AssetDatabase.GUIDToAssetPath(ussGuids[0]);
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);
                if (styleSheet != null)
                {
                    _root.styleSheets.Add(styleSheet);
                }
            }

            _root.AddToClassList("ue5-root");

            // Toolbar Panel
            var toolbarPanel = new VisualElement();
            toolbarPanel.AddToClassList("toolbar-panel");
            _root.Add(toolbarPanel);

            var createLabel = new Label("新建分组");
            createLabel.AddToClassList("toolbar-label");
            toolbarPanel.Add(createLabel);

            _newGroupNameField = new TextField();
            _newGroupNameField.AddToClassList("ue5-text-field");
            toolbarPanel.Add(_newGroupNameField);

            var createBtn = new Button(OnCreateGroupClicked) { text = "创建" };
            createBtn.AddToClassList("primary-btn");
            toolbarPanel.Add(createBtn);

            // Content ScrollView
            _contentScrollView = new ScrollView(ScrollViewMode.Vertical);
            _contentScrollView.AddToClassList("content-scroll");
            _root.Add(_contentScrollView);

            // Initial refresh
            RefreshContent();

            return _root;
        }

        private void OnProjectChanged()
        {
            CollectScenes();
            RefreshContent();
        }

        private void CollectScenes()
        {
            allScenes.Clear();
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");

            HashSet<string> inBuildScenePaths = new HashSet<string>();
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.enabled)
                {
                    inBuildScenePaths.Add(buildScene.path);
                }
            }

            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/"))
                {
                    bool isInBuild = inBuildScenePaths.Contains(path);
                    bool isFavorite = favoriteSceneGuids.Contains(guid);
                    allScenes.Add(new SceneInfo(guid, path, isInBuild, isFavorite));
                }
            }

            allScenes = allScenes.OrderByDescending(s => s.IsFavorite)
                                 .ThenByDescending(s => s.IsInBuild)
                                 .ThenBy(s => s.Name)
                                 .ToList();
        }

        private void RefreshContent()
        {
            if (_contentScrollView == null) return;
            _contentScrollView.Clear();

            // 1. Custom Groups
            foreach (var group in customGroups.ToList())
            {
                var scenesInGroup = allScenes.Where(s => group.SceneGuids.Contains(s.Guid)).ToList();
                var groupContainer = CreateSceneCategory($"分组：{group.Name}", scenesInGroup, group);
                _contentScrollView.Add(groupContainer);
            }

            // 2. In Build Scenes
            var buildScenes = allScenes.Where(s => s.IsInBuild).ToList();
            var buildContainer = CreateSceneCategory("构建场景", buildScenes, null, true);
            _contentScrollView.Add(buildContainer);

            // 3. Other Scenes
            var otherScenes = allScenes.Where(s => !s.IsInBuild).ToList();
            var otherContainer = CreateSceneCategory("其他场景资源", otherScenes, null);
            _contentScrollView.Add(otherContainer);
        }

        private VisualElement CreateSceneCategory(string categoryName, List<SceneInfo> scenesToDraw, SceneGroup group, bool isBuildCategory = false)
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");

            // Header
            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label(categoryName);
            title.AddToClassList("group-title");
            header.Add(title);

            if (isBuildCategory)
            {
                var badge = new Label($"{scenesToDraw.Count}");
                badge.AddToClassList("badge");
                badge.AddToClassList("badge-build");
                header.Add(badge);
            }

            if (group != null)
            {
                var deleteBtn = new Button(() => OnDeleteGroupClicked(group)) { text = "×" };
                deleteBtn.AddToClassList("delete-group-btn");
                header.Add(deleteBtn);
            }

            // Content
            var content = new VisualElement();
            content.AddToClassList("group-content");
            container.Add(content);

            if (scenesToDraw.Count == 0)
            {
                var emptyState = new VisualElement();
                emptyState.AddToClassList("empty-state");
                var emptyText = new Label(group != null ? "分组为空：在场景行点击「分组」添加。" : "未找到场景。");
                emptyText.AddToClassList("empty-state-text");
                emptyState.Add(emptyText);
                content.Add(emptyState);
            }
            else
            {
                foreach (var scene in scenesToDraw)
                {
                    var itemRow = CreateSceneRow(scene);
                    content.Add(itemRow);
                }
            }

            return container;
        }

        private VisualElement CreateSceneRow(SceneInfo scene)
        {
            string currentScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            bool isCurrent = currentScenePath == scene.Path;

            var row = new VisualElement();
            row.AddToClassList("item-row");
            if (isCurrent) row.AddToClassList("item-row-current");
            row.tooltip = scene.Path;

            // Star button
            var starBtn = new Button(() => OnToggleFavorite(scene)) { text = scene.IsFavorite ? "★" : "☆" };
            starBtn.AddToClassList("star-btn");
            if (scene.IsFavorite) starBtn.AddToClassList("star-btn-active");
            row.Add(starBtn);

            // Scene icon
            var iconElement = new VisualElement();
            iconElement.AddToClassList("item-icon");
            var sceneIcon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            if (sceneIcon != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(sceneIcon);
            }
            row.Add(iconElement);

            // Scene name button
            var nameBtn = new Button(() => OnOpenScene(scene.Path)) { text = scene.Name };
            nameBtn.AddToClassList("item-name");
            if (scene.IsFavorite) nameBtn.AddToClassList("item-name-starred");
            nameBtn.style.unityTextAlign = TextAnchor.MiddleLeft;
            nameBtn.style.backgroundColor = Color.clear;
            nameBtn.style.borderLeftWidth = 0;
            nameBtn.style.borderRightWidth = 0;
            nameBtn.style.borderTopWidth = 0;
            nameBtn.style.borderBottomWidth = 0;
            if (isCurrent)
            {
                nameBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            row.Add(nameBtn);

            // Current badge
            if (isCurrent)
            {
                var currentBadge = new Label("当前");
                currentBadge.AddToClassList("badge");
                currentBadge.AddToClassList("badge-current");
                row.Add(currentBadge);
            }

            // In Build badge
            if (scene.IsInBuild && !isCurrent)
            {
                var buildBadge = new Label("构建");
                buildBadge.AddToClassList("badge");
                buildBadge.AddToClassList("badge-build");
                row.Add(buildBadge);
            }

            // Group button
            var groupBtn = new Button(() => ShowGroupMenu(scene)) { text = "分组" };
            groupBtn.AddToClassList("action-btn");
            row.Add(groupBtn);

            // Locate button
            var locateBtn = new Button(() => OnLocateScene(scene)) { text = "定位" };
            locateBtn.AddToClassList("action-btn");
            row.Add(locateBtn);

            return row;
        }

        #region Event Handlers

        private void OnCreateGroupClicked()
        {
            string newName = _newGroupNameField?.value?.Trim();
            if (!string.IsNullOrEmpty(newName) && !customGroups.Any(g => g.Name == newName))
            {
                customGroups.Add(new SceneGroup { Name = newName });
                _newGroupNameField.value = "";
                SaveGroups();
                RefreshContent();
            }
        }

        private void OnDeleteGroupClicked(SceneGroup group)
        {
            if (EditorUtility.DisplayDialog("Delete Group", $"Delete group '{group.Name}'?", "Yes", "No"))
            {
                customGroups.Remove(group);
                SaveGroups();
                RefreshContent();
            }
        }

        private void OnToggleFavorite(SceneInfo scene)
        {
            scene.IsFavorite = !scene.IsFavorite;
            if (scene.IsFavorite)
            {
                if (!favoriteSceneGuids.Contains(scene.Guid))
                {
                    favoriteSceneGuids.Add(scene.Guid);
                }
            }
            else
            {
                favoriteSceneGuids.Remove(scene.Guid);
            }
            SaveFavoriteScenes();
            CollectScenes();
            RefreshContent();
        }

        private void OnOpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
                RefreshContent();
            }
        }

        private void OnLocateScene(SceneInfo scene)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scene.Path);
            if (sceneAsset != null)
            {
                EditorGUIUtility.PingObject(sceneAsset);
                Selection.activeObject = sceneAsset;
            }
        }

        private void ShowGroupMenu(SceneInfo scene)
        {
            GenericMenu menu = new GenericMenu();
            if (customGroups.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("暂无分组，请先创建"));
            }
            else
            {
                foreach (var group in customGroups)
                {
                    bool isInGroup = group.SceneGuids.Contains(scene.Guid);
                    var targetGroup = group;
                    menu.AddItem(new GUIContent(group.Name), isInGroup, () => ToggleGroupMembership(scene, targetGroup));
                }
            }
            menu.ShowAsContext();
        }

        private void ToggleGroupMembership(SceneInfo scene, SceneGroup group)
        {
            if (group.SceneGuids.Contains(scene.Guid))
                group.SceneGuids.Remove(scene.Guid);
            else
                group.SceneGuids.Add(scene.Guid);
            SaveGroups();
            RefreshContent();
        }

        #endregion

        #region Data Persistence

        private void LoadFavoriteScenes()
        {
            string favoritesJson = EditorPrefs.GetString(FavoriteScenesKey, "");
            if (!string.IsNullOrEmpty(favoritesJson))
            {
                favoriteSceneGuids = JsonUtility.FromJson<StringListWrapper>(favoritesJson).list;
            }
            else
            {
                favoriteSceneGuids = new List<string>();
            }
        }

        private void SaveFavoriteScenes()
        {
            StringListWrapper wrapper = new StringListWrapper { list = favoriteSceneGuids };
            string favoritesJson = JsonUtility.ToJson(wrapper);
            EditorPrefs.SetString(FavoriteScenesKey, favoritesJson);
        }

        private void LoadGroups()
        {
            string groupsJson = EditorPrefs.GetString(GroupsKey, "");
            if (!string.IsNullOrEmpty(groupsJson))
            {
                customGroups = JsonUtility.FromJson<GroupListWrapper>(groupsJson).groups;
            }
            else
            {
                customGroups = new List<SceneGroup>();
            }
        }

        private void SaveGroups()
        {
            GroupListWrapper wrapper = new GroupListWrapper { groups = customGroups };
            string groupsJson = JsonUtility.ToJson(wrapper);
            EditorPrefs.SetString(GroupsKey, groupsJson);
        }

        #endregion

        #region Data Classes

        private class SceneInfo
        {
            public string Guid;
            public string Path;
            public string Name;
            public bool IsInBuild;
            public bool IsFavorite;

            public SceneInfo(string guid, string path, bool isInBuild, bool isFavorite)
            {
                Guid = guid;
                Path = path;
                Name = System.IO.Path.GetFileNameWithoutExtension(path);
                IsInBuild = isInBuild;
                IsFavorite = isFavorite;
            }
        }

        [System.Serializable]
        public class SceneGroup
        {
            public string Name;
            public List<string> SceneGuids = new List<string>();
        }

        [System.Serializable]
        private class StringListWrapper
        {
            public List<string> list;
        }

        [System.Serializable]
        private class GroupListWrapper
        {
            public List<SceneGroup> groups;
        }

        #endregion

        public void Refresh() { CollectScenes(); RefreshContent(); }
        public void DrawIMGUI() { }
    }
}
