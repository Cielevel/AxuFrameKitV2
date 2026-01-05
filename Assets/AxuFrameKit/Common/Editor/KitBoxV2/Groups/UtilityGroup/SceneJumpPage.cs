using System.Collections.Generic;
using System.IO;
using System.Linq;
using AxuFrameKit.Common.Editor.KitBoxV2.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBoxV2.Groups.UtilityGroup
{
    /// <summary>
    /// 场景快速跳转页面
    /// </summary>
    public class SceneJumpPage : IKitPageV2
    {
        public string Title => "场景跳转";
        
        private IKitBoxV2 _kitBox;
        private Vector2 _scrollPos;
        private List<string> _allScenePaths = new();
        private List<string> _buildScenePaths = new();
        
        public void Init(IKitBoxV2 kitBox)
        {
            _kitBox = kitBox;
            CollectScenes();
        }
        
        public void Draw()
        {
            // 刷新按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("🔄 刷新场景列表", GUILayout.Width(120)))
            {
                CollectScenes();
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            // 构建场景区域
            DrawSceneSection("🏗 构建场景", _buildScenePaths, new Color(0.3f, 0.6f, 0.4f));
            
            GUILayout.Space(15);
            
            // 所有场景区域
            DrawSceneSection("📁 所有场景资源", _allScenePaths, new Color(0.3f, 0.5f, 0.7f));
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawSceneSection(string title, List<string> scenePaths, Color headerColor)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 标题
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = headerColor;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.backgroundColor = originalBg;
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };
            EditorGUILayout.LabelField(title, titleStyle, GUILayout.Height(25));
            EditorGUILayout.LabelField($"({scenePaths.Count})", EditorStyles.miniLabel, GUILayout.Width(40));
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            if (scenePaths.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到场景", MessageType.Info);
            }
            else
            {
                // 计算列数
                int cols = Mathf.Max(1, (int)(EditorGUIUtility.currentViewWidth / 180f));
                
                for (int i = 0; i < scenePaths.Count; i++)
                {
                    if (i % cols == 0)
                        EditorGUILayout.BeginHorizontal();
                    
                    DrawSceneButton(scenePaths[i]);
                    
                    if (i % cols == cols - 1 || i == scenePaths.Count - 1)
                        EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSceneButton(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            bool isCurrentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == path;
            
            Color originalBg = GUI.backgroundColor;
            
            if (isCurrentScene)
            {
                GUI.backgroundColor = new Color(0.4f, 0.7f, 0.4f);
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            
            EditorGUILayout.BeginHorizontal();
            
            // 场景按钮
            if (GUILayout.Button(name, GUILayout.Height(26), GUILayout.ExpandWidth(true)))
            {
                OpenScene(path);
            }
            
            // 定位按钮
            if (GUILayout.Button("📍", GUILayout.Width(28), GUILayout.Height(26)))
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (sceneAsset != null)
                {
                    EditorGUIUtility.PingObject(sceneAsset);
                    Selection.activeObject = sceneAsset;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 显示当前场景标记
            if (isCurrentScene)
            {
                GUIStyle currentStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    normal = { textColor = new Color(0.3f, 0.8f, 0.3f) }
                };
                EditorGUILayout.LabelField("● 当前场景", currentStyle);
            }
            
            EditorGUILayout.EndVertical();
            
            GUI.backgroundColor = originalBg;
        }
        
        private void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
        }
        
        private void CollectScenes()
        {
            _allScenePaths.Clear();
            _buildScenePaths.Clear();
            
            // 获取所有场景
            var guids = AssetDatabase.FindAssets("t:Scene");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/"))
                {
                    _allScenePaths.Add(path);
                }
            }
            
            // 获取构建场景
            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.enabled && !string.IsNullOrEmpty(buildScene.path))
                {
                    _buildScenePaths.Add(buildScene.path);
                }
            }
            
            // 排序
            _allScenePaths.Sort();
            _buildScenePaths.Sort();
        }
        
        public void Refresh()
        {
            CollectScenes();
        }
    }
}
