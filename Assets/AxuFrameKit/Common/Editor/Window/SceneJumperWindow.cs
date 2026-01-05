using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace AxuFrameKit.Common.Editor.Window
{
    public class SceneJumperWindow : EditorWindow
    {
        private const string FavoriteScenesKey = "SceneJumper_FavoriteScenes"; // 用于EditorPrefs存储收藏夹
        private const string GroupsKey = "SceneJumper_CustomGroups"; // 用于EditorPrefs存储自定义分组

        private List<SceneInfo> allScenes = new List<SceneInfo>();
        private List<string> favoriteSceneGuids = new List<string>(); // 存储收藏场景的GUID
        private List<SceneGroup> customGroups = new List<SceneGroup>(); // 存储自定义分组

        private Vector2 scrollPosition; // 滚动视图的位置
        private string newGroupName = ""; // 新分组名称输入

        [MenuItem("AxuFrameKit/SceneJumper")]
        public static void ShowWindow()
        {
            GetWindow<SceneJumperWindow>("Scene Jumper");
        }

        private void OnEnable()
        {
            LoadFavoriteScenes();
            LoadGroups();
            CollectScenes();
            // 订阅项目发生变化事件，以便在场景文件增删改时刷新列表
            EditorApplication.projectChanged += CollectScenes;
        }

        private void OnDisable()
        {
            // 取消订阅，避免内存泄露
            EditorApplication.projectChanged -= CollectScenes;
            SaveGroups();
        }

        private void OnFocus()
        {
            // 窗口获得焦点时重新收集，以防在关闭期间项目发生变化
            CollectScenes();
        }

        // 收集所有场景文件并分类
        private void CollectScenes()
        {
            allScenes.Clear();
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene"); // 查找所有场景资产的GUID

            // 获取Build Settings中的场景路径
            HashSet<string> inBuildScenePaths = new HashSet<string>();
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.enabled) // 只考虑启用的场景
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
                    bool isFavorite = favoriteSceneGuids.Contains(guid); // 检查是否已收藏
                    allScenes.Add(new SceneInfo(guid, path, isInBuild, isFavorite));
                }
            }

            // 排序，收藏的优先，然后按名称排序
            allScenes = allScenes.OrderByDescending(s => s.IsFavorite)
                                 .ThenByDescending(s => s.IsInBuild) // InBuild的场景也优先
                                 .ThenBy(s => s.Name)
                                 .ToList();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Scene Jumper", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Group Creation Header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Create Group:", GUILayout.Width(90));
            newGroupName = EditorGUILayout.TextField(newGroupName);
            if (GUILayout.Button("Create", GUILayout.Width(70)))
            {
                if (!string.IsNullOrEmpty(newGroupName) && !customGroups.Any(g => g.Name == newGroupName))
                {
                    customGroups.Add(new SceneGroup { Name = newGroupName });
                    newGroupName = "";
                    SaveGroups();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 1. Custom Groups
            foreach (var group in customGroups.ToList())
            {
                var scenesInGroup = allScenes.Where(s => group.SceneGuids.Contains(s.Guid)).ToList();
                DrawSceneCategory($"📂 分组: {group.Name}", scenesInGroup, new Color(0.4f, 0.4f, 0.6f), group);
                EditorGUILayout.Space(10);
            }

            // 2. In Build Scenes
            DrawSceneCategory("🏗 构建场景", allScenes.Where(s => s.IsInBuild).ToList(), new Color(0.3f, 0.6f, 0.4f));
            EditorGUILayout.Space(15);

            // 3. Other Scenes
            DrawSceneCategory("📁 其他场景资源", allScenes.Where(s => !s.IsInBuild).ToList(), new Color(0.3f, 0.5f, 0.7f));

            EditorGUILayout.EndScrollView();
        }

        // 绘制单个场景分类
        private void DrawSceneCategory(string categoryName, List<SceneInfo> scenesToDraw, Color headerColor, SceneGroup group = null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题背景色装饰
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = headerColor;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.backgroundColor = originalBg;

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };
            EditorGUILayout.LabelField(categoryName, titleStyle, GUILayout.Height(22));

            if (group != null)
            {
                if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Delete Group", $"Delete group '{categoryName}'?", "Yes", "No"))
                    {
                        customGroups.Remove(group);
                        SaveGroups();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (scenesToDraw.Count == 0)
            {
                if (group != null)
                    EditorGUILayout.LabelField("Group is empty. Click [Group] on a scene to add.", EditorStyles.miniLabel);
                else
                    EditorGUILayout.HelpBox($"No {categoryName} found.", MessageType.Info);
            }
            else
            {
                foreach (SceneInfo scene in scenesToDraw)
                {
                    DrawSceneButton(scene);
                }
            }
            EditorGUILayout.EndVertical();
        }

        // 绘制单个场景的按钮和收藏星标
        private void DrawSceneButton(SceneInfo scene)
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

            // 2. 场景跳转按钮
            bool isCurrent = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == scene.Path;
            if (isCurrent) GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            else if (scene.IsFavorite) GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f, 0.5f);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 2, 2),
                fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal
            };

            if (GUILayout.Button(new GUIContent(scene.Name, scene.Path), btnStyle, GUILayout.Height(24)))
            {
                OpenScene(scene.Path);
            }
            GUI.backgroundColor = originalColor;

            // 3. Group Button
            if (GUILayout.Button("Group", GUILayout.Width(55), GUILayout.Height(24)))
            {
                ShowGroupMenu(scene);
            }

            // 4. 定位按钮
            if (GUILayout.Button("📍", GUILayout.Width(28), GUILayout.Height(24)))
            {
                UnityEngine.Object sceneAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scene.Path);
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
            if (customGroups.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Groups Found. Create one first."));
            }
            else
            {
                foreach (var group in customGroups)
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

        // 切换收藏状态
        private void ToggleFavorite(SceneInfo scene)
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
            SaveFavoriteScenes(); // 保存到EditorPrefs
            CollectScenes(); // 重新收集并排序
        }

        // 打开场景
        private void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
        }

        // 加载收藏的场景GUID
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

        // 保存收藏的场景GUID
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

        // 场景信息辅助类
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

        // 用于JsonUtility序列化List<string>的辅助类
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
    }
}
