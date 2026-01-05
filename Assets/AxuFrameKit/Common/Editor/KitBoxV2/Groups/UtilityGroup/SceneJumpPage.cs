using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AxuFrameKit.Common.Editor.KitBoxV2.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Editor.KitBoxV2.Groups.UtilityGroup
{
    /// <summary>
    /// 场景快速跳转页面 (集成 SceneJumperWindow 功能)
    /// </summary>
    public class SceneJumpPage : IKitPageV2
    {
        private const string FavoriteScenesKey = "SceneJumper_FavoriteScenes";
        private const string GroupsKey = "SceneJumper_CustomGroups";

        public string Title => "场景跳转";

        private IKitBoxV2 _kitBox;
        private List<SceneInfo> _allScenes = new();
        private List<string> _favoriteSceneGuids = new();
        private List<SceneGroup> _customGroups = new();
        private string _newGroupName = "";

        // ----------------- IKitPageV2 Implementation -----------------

        public void Init(IKitBoxV2 kitBox)
        {
            _kitBox = kitBox;
            LoadData();
            CollectScenes();

            // 订阅项目变化，实时刷新
            EditorApplication.projectChanged -= CollectScenes;
            EditorApplication.projectChanged += CollectScenes;
        }

        public void Draw()
        {
            // 顶部工具栏：搜索与刷新
            DrawTopToolbar();

            GUILayout.Space(10);

            // 分组创建区域
            DrawGroupCreationSelection();

            GUILayout.Space(10);

            // 1. 自定义分组区域
            if (_customGroups.Count > 0)
            {
                foreach (var group in _customGroups.ToList())
                {
                    var scenesInGroup = _allScenes.Where(s => group.SceneGuids.Contains(s.Guid)).ToList();
                    DrawSceneSection($"📂 分组: {group.Name}", scenesInGroup, new Color(0.4f, 0.4f, 0.6f), group);
                    GUILayout.Space(10);
                }
            }

            // 2. 构建场景区域
            var buildScenes = _allScenes.Where(s => s.IsInBuild).ToList();
            DrawSceneSection("🏗 构建场景", buildScenes, new Color(0.3f, 0.6f, 0.4f));

            GUILayout.Space(15);

            // 3. 所有场景区域 (排除在构建里的，或者保留全部，根据习惯)
            // 这里我们显示“其他场景”，即不在构建中的
            var otherScenes = _allScenes.Where(s => !s.IsInBuild).ToList();
            DrawSceneSection("📁 其他场景资源", otherScenes, new Color(0.3f, 0.5f, 0.7f));
        }

        public void Refresh()
        {
            CollectScenes();
        }

        // ----------------- Internal Logic -----------------

        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("场景管理面板", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🔄 刷新项目场景", GUILayout.Width(120)))
            {
                CollectScenes();
            }

            EditorGUILayout.EndHorizontal();
            KitBoxStyles.DrawHorizontalLine(1f, 2f, 2f);
        }

        private void DrawGroupCreationSelection()
        {
            KitBoxStyles.DrawColoredBox(new Color(0.2f, 0.2f, 0.2f, 0.5f), () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("新建分组:", GUILayout.Width(60));
                _newGroupName = EditorGUILayout.TextField(_newGroupName);
                if (GUILayout.Button("创建", GUILayout.Width(60)))
                {
                    if (!string.IsNullOrEmpty(_newGroupName) && !_customGroups.Any(g => g.Name == _newGroupName))
                    {
                        _customGroups.Add(new SceneGroup { Name = _newGroupName });
                        _newGroupName = "";
                        SaveGroups();
                    }
                }
                EditorGUILayout.EndHorizontal();
            });
        }

        private void DrawSceneSection(string title, List<SceneInfo> scenes, Color headerColor, SceneGroup group = null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题装饰项
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = headerColor;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.backgroundColor = originalBg;

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };
            EditorGUILayout.LabelField(title, titleStyle, GUILayout.Height(22));

            if (group != null)
            {
                if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("删除分组", $"是否确定删除分组 '{group.Name}'？", "是", "否"))
                    {
                        _customGroups.Remove(group);
                        SaveGroups();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (scenes.Count == 0)
            {
                if (group != null)
                    EditorGUILayout.LabelField("分组内暂无场景，点击场景右侧 [Group] 添加。", EditorStyles.miniLabel);
                else
                    EditorGUILayout.LabelField("未找到符合条件的场景。", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var scene in scenes)
                {
                    DrawSceneItem(scene);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSceneItem(SceneInfo scene)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // 1. 收藏星标
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = scene.IsFavorite ? Color.yellow : originalColor;
            if (GUILayout.Button(scene.IsFavorite ? "★" : "☆", GUILayout.Width(28), GUILayout.Height(24)))
            {
                ToggleFavorite(scene);
            }
            GUI.backgroundColor = originalColor;

            // 2. 场景跳转主按钮
            bool isCurrent = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == scene.Path;
            if (isCurrent) GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            else if (scene.IsFavorite) GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f, 0.5f);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 2, 2),
                fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal
            };

            if (GUILayout.Button(new GUIContent(scene.Name, scene.Path), btnStyle, GUILayout.Height(24), GUILayout.ExpandWidth(true)))
            {
                OpenScene(scene.Path);
            }
            GUI.backgroundColor = originalColor;

            // 3. 分组菜单
            if (GUILayout.Button("Group", GUILayout.Width(50), GUILayout.Height(24)))
            {
                ShowGroupMenu(scene);
            }

            // 4. 定位按钮
            if (GUILayout.Button("📍", GUILayout.Width(28), GUILayout.Height(24)))
            {
                Object sceneAsset = AssetDatabase.LoadAssetAtPath<Object>(scene.Path);
                if (sceneAsset != null)
                {
                    EditorGUIUtility.PingObject(sceneAsset);
                    Selection.activeObject = sceneAsset;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ShowGroupMenu(SceneInfo scene)
        {
            GenericMenu menu = new GenericMenu();
            if (_customGroups.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("请先创建分组"));
            }
            else
            {
                foreach (var group in _customGroups)
                {
                    bool isInGroup = group.SceneGuids.Contains(scene.Guid);
                    menu.AddItem(new GUIContent(group.Name), isInGroup, () => ToggleGroupMembership(scene, group));
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
        }

        private void ToggleFavorite(SceneInfo scene)
        {
            scene.IsFavorite = !scene.IsFavorite;
            if (scene.IsFavorite)
            {
                if (!_favoriteSceneGuids.Contains(scene.Guid))
                    _favoriteSceneGuids.Add(scene.Guid);
            }
            else
            {
                _favoriteSceneGuids.Remove(scene.Guid);
            }
            SaveFavoriteScenes();
            CollectScenes(); // 重新排序
        }

        private void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
        }

        private void CollectScenes()
        {
            _allScenes.Clear();
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");

            HashSet<string> inBuildScenePaths = new HashSet<string>();
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.enabled)
                    inBuildScenePaths.Add(buildScene.path);
            }

            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/"))
                {
                    bool isInBuild = inBuildScenePaths.Contains(path);
                    bool isFavorite = _favoriteSceneGuids.Contains(guid);
                    _allScenes.Add(new SceneInfo(guid, path, isInBuild, isFavorite));
                }
            }

            // 排序：收藏优先 > 构建优先 > 名称
            _allScenes = _allScenes.OrderByDescending(s => s.IsFavorite)
                                   .ThenByDescending(s => s.IsInBuild)
                                   .ThenBy(s => s.Name)
                                   .ToList();
        }

        private void LoadData()
        {
            // Favorites
            string favoritesJson = EditorPrefs.GetString(FavoriteScenesKey, "");
            if (!string.IsNullOrEmpty(favoritesJson))
                _favoriteSceneGuids = JsonUtility.FromJson<StringListWrapper>(favoritesJson).list;
            else
                _favoriteSceneGuids = new List<string>();

            // Groups
            string groupsJson = EditorPrefs.GetString(GroupsKey, "");
            if (!string.IsNullOrEmpty(groupsJson))
                _customGroups = JsonUtility.FromJson<GroupListWrapper>(groupsJson).groups;
            else
                _customGroups = new List<SceneGroup>();
        }

        private void SaveFavoriteScenes()
        {
            string json = JsonUtility.ToJson(new StringListWrapper { list = _favoriteSceneGuids });
            EditorPrefs.SetString(FavoriteScenesKey, json);
        }

        private void SaveGroups()
        {
            string json = JsonUtility.ToJson(new GroupListWrapper { groups = _customGroups });
            EditorPrefs.SetString(GroupsKey, json);
        }

        // ----------------- Helper Classes -----------------

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

        [Serializable]
        public class SceneGroup
        {
            public string Name;
            public List<string> SceneGuids = new List<string>();
        }

        [Serializable]
        private class StringListWrapper { public List<string> list; }

        [Serializable]
        private class GroupListWrapper { public List<SceneGroup> groups; }
    }
}
