using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AxuIndieModules.StatPipeline.Modifier.Editor
{
    public class StatCalculatorGeneratorWindow : EditorWindow
    {
        private string _targetFolder = "Assets/AxuIndieModules/StatPipeline/Modifier/Calculators";
        private Vector2 _scrollPos;
        private List<Type> _calculatorTypes = new List<Type>();

        private const string PREFS_KEY = "StatCalculatorGenerator_Folder";

        [MenuItem("AxuIndieModules/StatPipeline/Calculator-Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<StatCalculatorGeneratorWindow>("Calculator-Generator");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            _targetFolder = EditorPrefs.GetString(PREFS_KEY, _targetFolder);
            RefreshTypes();
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(PREFS_KEY, _targetFolder);
        }

        private void RefreshTypes()
        {
            _calculatorTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(StatModifierCalculator).IsAssignableFrom(p) && !p.IsAbstract && p != typeof(StatModifierCalculator))
                .ToList();
        }

        private void OnGUI()
        {
            DrawHeader();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(5);
                DrawFolderSelection();
                GUILayout.Space(5);
            }

            GUILayout.Space(10);
            DrawTypeList();

            GUILayout.FlexibleSpace();
            DrawFooter();
        }

        private void DrawHeader()
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 40
            };
            headerStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Stat Modifier Calculator Generator", headerStyle);
            GUILayout.Space(5);

            // 下划线
            Rect rect = GUILayoutUtility.GetLastRect();
            Handles.BeginGUI();
            Handles.color = new Color(0.3f, 0.7f, 1f, 0.5f);
            Handles.DrawLine(new Vector2(20, rect.yMax + 5), new Vector2(position.width - 20, rect.yMax + 5));
            Handles.EndGUI();
            GUILayout.Space(15);
        }

        private void DrawFolderSelection()
        {
            EditorGUILayout.BeginHorizontal();
            _targetFolder = EditorGUILayout.TextField("Target Folder", _targetFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Target Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        _targetFolder = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Folder must be within the project Assets directory.", "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTypeList()
        {
            EditorGUILayout.LabelField("Available Calculator Types", EditorStyles.boldLabel);
            GUILayout.Space(5);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var type in _calculatorTypes)
            {
                DrawTypeRow(type);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTypeRow(Type type)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var labelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft
                };

                // 图标模拟
                GUILayout.Box(EditorGUIUtility.IconContent("ScriptableObject Icon"), GUIStyle.none, GUILayout.Width(20), GUILayout.Height(20));

                EditorGUILayout.LabelField(type.Name, labelStyle);

                GUILayout.FlexibleSpace();

                GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
                if (GUILayout.Button("Create Asset", GUILayout.Width(100), GUILayout.Height(22)))
                {
                    CreateAsset(type);
                }
                GUI.backgroundColor = Color.white;
            }
        }

        private void CreateAsset(Type type)
        {
            if (!Directory.Exists(_targetFolder))
            {
                Directory.CreateDirectory(_targetFolder);
                AssetDatabase.Refresh();
            }

            string assetName = type.Name;
            string fullPath = Path.Combine(_targetFolder, $"{assetName}.asset");
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

            var asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            Debug.Log($"<color=green>[Calculator Generator]</color> Created {type.Name} at {fullPath}");
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (GUILayout.Button("Refresh Classes", GUILayout.Height(25)))
            {
                RefreshTypes();
            }
            if (GUILayout.Button("Clean Up Empty Folders", GUILayout.Height(25)))
            {
                // 可选功能
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.LabelField("v1.0 - AxuIndieModules", EditorStyles.centeredGreyMiniLabel);
        }
    }
}
