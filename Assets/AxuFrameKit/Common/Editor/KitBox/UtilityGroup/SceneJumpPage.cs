using System.Collections.Generic;
using System.IO;
using System.Linq;
using AxuFrameKit.Common.Editor.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBox.UtilityGroup
{
    public class SceneJumpPage : IKitPage
    {
        public string Title => "场景跳转";

        private Vector2 _scrollPos;

        public void Show()
        {
            GUI.backgroundColor = Color.white;

            // 列数根据窗口宽度自适应（每列约120px）
            int cols = Mathf.Max(1, (int)(EditorGUIUtility.currentViewWidth / 120f));

            // in build scene
            TitleDrawerEditor.DrawTitle("构建场景", Color.white);
            
            var buildList = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToList();
            for (int i = 0; i < buildList.Count; i++)
            {
                if (i % cols == 0) EditorGUILayout.BeginHorizontal();
                var path = buildList[i].path;
                var name = Path.GetFileNameWithoutExtension(path);
                if (GUILayout.Button(name, GUILayout.Width(EditorGUIUtility.currentViewWidth / cols - 8), GUILayout.Height(24)))
                {
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                }

                if (i % cols == cols - 1 || i == buildList.Count - 1) EditorGUILayout.EndHorizontal();
            }

            if (buildList.Count == 0)
                EditorGUILayout.LabelField("无已加入构建的场景");

            ShapeDrawerEditor.DrawLineHorizontal(3, Color.white, 3);
            
            // asset scene
            TitleDrawerEditor.DrawTitle("所有场景资源", Color.white);
            
            var guids = AssetDatabase.FindAssets("t:Scene");
            var paths = new List<string>();
            foreach (var guid in guids)
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));

            int  count = 0;
            bool group = false;

            for (int i = 0; i < paths.Count; i++, count++)
            {
                count %= 2;

                if (count == 0 && group == false)
                {
                    EditorGUILayout.BeginHorizontal();
                    group = true;
                }

                var path = paths[i];
                var name = Path.GetFileNameWithoutExtension(path);
                if (GUILayout.Button(name, GUILayout.ExpandWidth(true), GUILayout.Height(24)))
                {
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                }
                
                if (count == 1 && group)
                {
                    EditorGUILayout.EndHorizontal();
                    group = false;
                }
            }

            EditorGUILayout.EndScrollView();

            if (paths.Count == 0)
                EditorGUILayout.LabelField("未找到场景资源");
        }

        public void Refresh() { }
    }
}