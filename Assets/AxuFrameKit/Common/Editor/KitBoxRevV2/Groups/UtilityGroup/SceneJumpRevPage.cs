using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
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

        private List<SceneInfo> _displayScenes = new List<SceneInfo>();
        private List<string> _favoriteGuids = new List<string>();
        private List<SceneGroup> _customGroups = new List<SceneGroup>();

        private ListView _sceneListView;
        private TextField _searchField;
        private TextField _groupNameField;
        private VisualElement _groupContainer;

        public void Init(IKitBoxRevV2 kitBox)
        {
            _kitBox = kitBox;
            LoadData();
            CollectScenes();
            EditorApplication.projectChanged += CollectScenes;
        }

        public VisualElement CreateContent()
        {
            var root = new VisualElement();
            root.style.flexGrow = 1;

            // --- Toolbar ---
            var toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar");
            root.Add(toolbar);

            _searchField = new TextField { name = "search-field" };
            _searchField.style.flexGrow = 1;
            _searchField.RegisterValueChangedCallback(_ => FilterScenes());
            toolbar.Add(_searchField);

            var refreshBtn = new Button(CollectScenes) { text = "🔄" };
            refreshBtn.tooltip = "刷新项目场景";
            toolbar.Add(refreshBtn);

            // --- Group Creation ---
            var groupBar = new VisualElement();
            groupBar.AddToClassList("toolbar");
            root.Add(groupBar);

            var groupLabel = new Label("新建分组:");
            groupLabel.style.marginRight = 8;
            groupBar.Add(groupLabel);

            _groupNameField = new TextField { name = "new-group-name" };
            _groupNameField.style.flexGrow = 1;
            _groupNameField.SetValueWithoutNotify("新分组");
            groupBar.Add(_groupNameField);

            var addGroupBtn = new Button(AddGroup) { text = "➕ 添加分组" };
            groupBar.Add(addGroupBtn);

            // --- Groups Container ---
            _groupContainer = new ScrollView();
            _groupContainer.style.flexGrow = 1;
            root.Add(_groupContainer);

            // --- All Scenes List (在分组下方) ---
            var allScenesFoldout = new Foldout { text = "📋 所有场景", value = true };
            allScenesFoldout.style.marginTop = 10;

            _sceneListView = new ListView(_displayScenes, 32, MakeSceneItem, BindSceneItem);
            _sceneListView.selectionType = SelectionType.None;
            _sceneListView.style.flexGrow = 1;
            allScenesFoldout.Add(_sceneListView);

            root.Add(allScenesFoldout);

            RefreshUI();

            return root;
        }

        private void AddGroup()
        {
            string name = _groupNameField.value.Trim();
            if (string.IsNullOrEmpty(name)) return;
            if (_customGroups.Any(g => g.Name == name))
            {
                EditorUtility.DisplayDialog("提示", $"分组 '{name}' 已存在！", "确定");
                return;
            }

            _customGroups.Add(new SceneGroup { Name = name });
            _groupNameField.value = "";
            SaveData();
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_groupContainer == null) return;
            _groupContainer.Clear();

            // Render Custom Groups as Foldouts
            foreach (var group in _customGroups.ToList())
            {
                var groupBox = new VisualElement();
                groupBox.style.marginBottom = 8;
                groupBox.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                groupBox.style.borderTopLeftRadius = 4;
                groupBox.style.borderTopRightRadius = 4;
                groupBox.style.borderBottomLeftRadius = 4;
                groupBox.style.borderBottomRightRadius = 4;
                groupBox.style.paddingTop = 4;
                groupBox.style.paddingBottom = 4;

                var headerRow = new VisualElement();
                headerRow.style.flexDirection = FlexDirection.Row;
                headerRow.style.alignItems = Align.Center;
                headerRow.style.paddingLeft = 10;
                headerRow.style.paddingRight = 10;
                headerRow.style.paddingTop = 4;
                headerRow.style.paddingBottom = 4;

                var foldout = new Foldout { text = $"📂 {group.Name}", value = true };
                foldout.style.flexGrow = 1;
                headerRow.Add(foldout);

                var deleteBtn = new Button(() => DeleteGroup(group)) { text = "🗑️ 删除" };
                deleteBtn.style.height = 24;
                deleteBtn.tooltip = $"删除分组 '{group.Name}'";
                headerRow.Add(deleteBtn);

                groupBox.Add(headerRow);

                var groupScenes = _displayScenes.Where(s => group.SceneGuids.Contains(s.Guid)).ToList();
                if (groupScenes.Count == 0)
                {
                    var emptyLabel = new Label("  (空分组 - 右键场景选择分组)");
                    emptyLabel.style.color = Color.gray;
                    emptyLabel.style.fontSize = 11;
                    emptyLabel.style.paddingLeft = 20;
                    emptyLabel.style.paddingTop = 4;
                    emptyLabel.style.paddingBottom = 4;
                    foldout.Add(emptyLabel);
                }
                else
                {
                    foreach (var scene in groupScenes)
                    {
                        foldout.Add(CreateSceneRow(scene));
                    }
                }

                _groupContainer.Add(groupBox);
            }

            FilterScenes();
        }

        private void DeleteGroup(SceneGroup group)
        {
            if (EditorUtility.DisplayDialog("删除分组", $"确定删除分组 '{group.Name}'?\n\n场景不会被删除，只是移出分组。", "删除", "取消"))
            {
                _customGroups.Remove(group);
                SaveData();
                RefreshUI();
            }
        }

        private VisualElement MakeSceneItem() => CreateSceneRow(null);

        private void BindSceneItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _displayScenes.Count) return;
            var scene = _displayScenes[index];
            UpdateSceneRow(element, scene);
        }

        private VisualElement CreateSceneRow(SceneInfo scene)
        {
            var row = new VisualElement();
            row.AddToClassList("scene-row");

            // 收藏按钮
            var favBtn = new Button { name = "fav-btn" };
            favBtn.AddToClassList("btn-small");
            row.Add(favBtn);

            // 主按钮（场景名称）
            var mainBtn = new Button { name = "main-btn" };
            mainBtn.style.flexGrow = 1;
            mainBtn.style.unityTextAlign = TextAnchor.MiddleLeft;
            mainBtn.AddToClassList("btn-secondary");
            row.Add(mainBtn);

            // 分组菜单按钮
            var menuBtn = new Button { name = "menu-btn", text = "📁" };
            menuBtn.AddToClassList("btn-small");
            menuBtn.tooltip = "添加到分组";
            row.Add(menuBtn);

            // Ping 按钮
            var pingBtn = new Button { name = "ping-btn", text = "📍" };
            pingBtn.AddToClassList("btn-small");
            pingBtn.tooltip = "在 Project 窗口中定位";
            row.Add(pingBtn);

            if (scene != null)
            {
                UpdateSceneRow(row, scene);
            }

            return row;
        }

        private void UpdateSceneRow(VisualElement row, SceneInfo scene)
        {
            if (scene == null) return;

            // 收藏状态的整行样式
            if (scene.IsFavorite)
            {
                row.style.backgroundColor = new Color(0.2f, 0.5f, 0.3f, 0.5f); // 绿色半透明背景
            }
            else
            {
                row.style.backgroundColor = new StyleColor(StyleKeyword.Null); // 重置为默认
            }

            var favBtn = row.Q<Button>("fav-btn");
            if (favBtn != null)
            {
                favBtn.text = scene.IsFavorite ? "★" : "☆";
                favBtn.style.color = scene.IsFavorite ? Color.yellow : Color.gray;

                // 使用 userData 存储场景信息，避免重复订阅
                if (favBtn.userData == null)
                {
                    favBtn.clicked += () =>
                    {
                        var sceneInfo = favBtn.userData as SceneInfo;
                        if (sceneInfo != null) ToggleFavorite(sceneInfo);
                    };
                }
                favBtn.userData = scene;
            }

            var mainBtn = row.Q<Button>("main-btn");
            if (mainBtn != null)
            {
                mainBtn.text = scene.Name;
                mainBtn.tooltip = scene.Path;

                bool isCurrent = EditorSceneManager.GetActiveScene().path == scene.Path;

                // 设置字体样式
                mainBtn.style.unityFontStyleAndWeight = isCurrent ? FontStyle.Bold : FontStyle.Normal;

                // 设置颜色优先级：当前场景 > 收藏场景 > 默认
                if (isCurrent)
                {
                    // 当前场景：亮绿色
                    mainBtn.style.color = new Color(0.4f, 1f, 0.4f);
                }
                else if (scene.IsFavorite)
                {
                    // 收藏场景：白色
                    mainBtn.style.color = Color.white;
                }
                else
                {
                    // 普通场景：默认颜色
                    mainBtn.style.color = new StyleColor(StyleKeyword.Null);
                }

                // 使用 userData 存储场景路径，避免重复订阅
                if (mainBtn.userData == null)
                {
                    mainBtn.clicked += () =>
                    {
                        var scenePath = mainBtn.userData as string;
                        if (!string.IsNullOrEmpty(scenePath)) OpenScene(scenePath);
                    };
                }
                mainBtn.userData = scene.Path;
            }

            var menuBtn = row.Q<Button>("menu-btn");
            if (menuBtn != null)
            {
                // 收藏场景的按钮文字也设为白色
                if (scene.IsFavorite)
                {
                    menuBtn.style.color = Color.white;
                }
                else
                {
                    menuBtn.style.color = new StyleColor(StyleKeyword.Null);
                }

                // 使用 userData 存储场景信息，避免重复订阅
                if (menuBtn.userData == null)
                {
                    menuBtn.clicked += () =>
                    {
                        var sceneInfo = menuBtn.userData as SceneInfo;
                        if (sceneInfo != null) ShowGroupMenu(sceneInfo, menuBtn);
                    };
                }
                menuBtn.userData = scene;
            }

            var pingBtn = row.Q<Button>("ping-btn");
            if (pingBtn != null)
            {
                // 收藏场景的按钮文字也设为白色
                if (scene.IsFavorite)
                {
                    pingBtn.style.color = Color.white;
                }
                else
                {
                    pingBtn.style.color = new StyleColor(StyleKeyword.Null);
                }

                // 使用 userData 存储场景信息，避免重复订阅
                if (pingBtn.userData == null)
                {
                    pingBtn.clicked += () =>
                    {
                        var sceneInfo = pingBtn.userData as SceneInfo;
                        if (sceneInfo != null)
                        {
                            var asset = AssetDatabase.LoadAssetAtPath<Object>(sceneInfo.Path);
                            if (asset)
                            {
                                EditorGUIUtility.PingObject(asset);
                                Selection.activeObject = asset;
                            }
                        }
                    };
                }
                pingBtn.userData = scene;
            }
        }

        private void ShowGroupMenu(SceneInfo scene, VisualElement anchor)
        {
            var menu = new GenericMenu();
            if (_customGroups.Count == 0) menu.AddDisabledItem(new GUIContent("请先创建分组"));
            else
            {
                foreach (var g in _customGroups)
                {
                    bool isIn = g.SceneGuids.Contains(scene.Guid);
                    menu.AddItem(new GUIContent(g.Name), isIn, () =>
                    {
                        if (isIn) g.SceneGuids.Remove(scene.Guid);
                        else g.SceneGuids.Add(scene.Guid);
                        SaveData();
                        RefreshUI();
                    });
                }
            }
            menu.DropDown(anchor.worldBound);
        }

        private void ToggleFavorite(SceneInfo scene)
        {
            scene.IsFavorite = !scene.IsFavorite;
            if (scene.IsFavorite) { if (!_favoriteGuids.Contains(scene.Guid)) _favoriteGuids.Add(scene.Guid); }
            else _favoriteGuids.Remove(scene.Guid);
            SaveData();
            CollectScenes();
        }

        private void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
        }

        private void CollectScenes()
        {
            var all = new List<SceneInfo>();
            string[] guids = AssetDatabase.FindAssets("t:Scene");
            var buildPaths = new HashSet<string>(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path));

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/")) continue;
                all.Add(new SceneInfo(guid, path, buildPaths.Contains(path), _favoriteGuids.Contains(guid)));
            }

            _displayScenes = all.OrderByDescending(s => s.IsFavorite)
                               .ThenByDescending(s => s.IsInBuild)
                               .ThenBy(s => s.Name)
                               .ToList();

            FilterScenes();
        }

        private void FilterScenes()
        {
            string query = _searchField?.value?.ToLower() ?? "";
            var filtered = string.IsNullOrEmpty(query)
                ? _displayScenes
                : _displayScenes.Where(s => s.Name.ToLower().Contains(query) || s.Path.ToLower().Contains(query)).ToList();

            if (_sceneListView != null)
            {
                _sceneListView.itemsSource = filtered;
                _sceneListView.RefreshItems();
            }
        }

        private void LoadData()
        {
            _favoriteGuids = EditorPrefs.GetString(FavoriteScenesKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            string groupsJson = EditorPrefs.GetString(GroupsKey, "");
            if (!string.IsNullOrEmpty(groupsJson)) _customGroups = JsonUtility.FromJson<GroupListWrapper>(groupsJson).groups;
        }

        private void SaveData()
        {
            EditorPrefs.SetString(FavoriteScenesKey, string.Join(",", _favoriteGuids));
            EditorPrefs.SetString(GroupsKey, JsonUtility.ToJson(new GroupListWrapper { groups = _customGroups }));
        }

        private class SceneInfo
        {
            public string Guid, Path, Name;
            public bool IsInBuild, IsFavorite;
            public SceneInfo(string guid, string path, bool inBuild, bool fav)
            {
                Guid = guid; Path = path; Name = System.IO.Path.GetFileNameWithoutExtension(path);
                IsInBuild = inBuild; IsFavorite = fav;
            }
        }

        [Serializable] public class SceneGroup { public string Name; public List<string> SceneGuids = new List<string>(); }
        [Serializable] private class GroupListWrapper { public List<SceneGroup> groups; }

        public void Refresh() => CollectScenes();
        public void DrawIMGUI() { }
    }
}
