using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AxuIndieModules.StatPipeline
{
    public class StatCalculatorGeneratorWindow : EditorWindow
    {
        private string _targetFolder = "Assets/AxuIndieModules/StatPipeline/Modifier/Calculator";
        private Vector2 _scrollPos;
        private List<Type> _calculatorTypes = new List<Type>();
        private List<Type> _linkCalculatorTypes = new List<Type>();

        private bool _enableChinese;
        private const string PREFS_KEY = "StatCalculatorGenerator_Folder";
        private const string PREFS_KEY_CHINESE = "StatCalculatorGenerator_EnableChinese";

        [MenuItem("AxuIndieModules/StatPipeline/Calculator-Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<StatCalculatorGeneratorWindow>("Calculator-Generator");
            window.minSize = new Vector2(700, 500);
        }

        private void OnEnable()
        {
            _targetFolder = EditorPrefs.GetString(PREFS_KEY, _targetFolder);
            _enableChinese = EditorPrefs.GetBool(PREFS_KEY_CHINESE, false);
            RefreshTypes();
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(PREFS_KEY, _targetFolder);
            EditorPrefs.SetBool(PREFS_KEY_CHINESE, _enableChinese);
        }

        private void RefreshTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var allTypes = assemblies.SelectMany(s => s.GetTypes()).ToList();

            _calculatorTypes = allTypes
                .Where(p => typeof(StatModifierCalculator).IsAssignableFrom(p) && !p.IsAbstract && p != typeof(StatModifierCalculator))
                .ToList();

            _linkCalculatorTypes = allTypes
                .Where(p => typeof(LinkStatModifierCalculator).IsAssignableFrom(p) && !p.IsAbstract && p != typeof(LinkStatModifierCalculator))
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
            headerStyle.normal.textColor = new Color(1, 0.5843137f, 0, 1);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Stat Modifier Calculator Generator", headerStyle);
            GUILayout.Space(10);

            // 下划线
            Rect rect = GUILayoutUtility.GetLastRect();
            Handles.BeginGUI();
            Handles.color = new Color(1, 0.5843137f, 0, 1);
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

            _enableChinese = EditorGUILayout.Toggle("Enable Chinese Name", _enableChinese);

            // Drag and drop area
            GUILayout.Space(2);
            Rect dropArea = GUILayoutUtility.GetRect(0f, 25f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Folder Here to Set Path", EditorStyles.helpBox);
            HandleDragAndDrop(dropArea);
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition)) return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        // Check if only one object is dragged
                        if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.paths.Length == 1)
                        {
                            string path = DragAndDrop.paths[0];
                            // Check if it's a folder
                            if (AssetDatabase.IsValidFolder(path))
                            {
                                _targetFolder = path;
                                Repaint();
                            }
                        }
                    }
                    evt.Use();
                    break;
            }
        }

        private void DrawTypeList()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.BeginHorizontal();

            // Column 1: Calculator
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Calculator Types", EditorStyles.boldLabel);
            GUILayout.Space(5);
            foreach (var type in _calculatorTypes)
            {
                DrawTypeRow(type);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // Column 2: Link Calculator
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Link Calculator Types", EditorStyles.boldLabel);
            GUILayout.Space(5);
            foreach (var type in _linkCalculatorTypes)
            {
                DrawTypeRow(type);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

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

                EditorGUILayout.LabelField(GetAssetName(type), labelStyle);

                GUILayout.FlexibleSpace();

                GUI.backgroundColor = new Color(1, 0.5843137f, 0, 1);
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

            string assetName = GetAssetName(type);
            string fullPath = Path.Combine(_targetFolder, $"{assetName}.asset");
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

            var asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            Debug.Log($"<color=green>[Calculator Generator]</color> Created {type.Name} at {fullPath}");
        }

        private string GetAssetName(Type type)
        {
            bool isLink = typeof(LinkStatModifierCalculator).IsAssignableFrom(type);
            string typeName = type.Name;

            if (typeName.EndsWith("Calculator"))
                typeName = typeName.Substring(0, typeName.Length - "Calculator".Length);

            if (isLink && typeName.EndsWith("Link"))
                typeName = typeName.Substring(0, typeName.Length - "Link".Length);

            if (_enableChinese && _chineseNames.ContainsKey(typeName))
            {
                typeName = _chineseNames[typeName];
            }

            return isLink ? $"LinkCalculator-{typeName}" : $"Calculator-{typeName}";
        }

        private readonly Dictionary<string, string> _chineseNames = new Dictionary<string, string>()
        {
            { "Add", "加法" },
            { "Subtract", "减法" },
            { "Sub", "减法" },
            { "Multiply", "乘法" },
            { "Mult", "乘法" },
            { "Divide", "除法" },
            { "Div", "除法" },
            { "AddPercent", "加百分比" },
            { "SubtractPercent", "减百分比" },
            { "SubPercent", "减百分比" },
            { "ClampMin", "钳制最小值" },
            { "ClampMax", "钳制最大值" },
            { "Abs", "取绝对值" },
            { "Negate", "取相反数" },
            { "Override", "覆盖" },
            { "None", "不进行任何操作" },
            { "OverrideByStat", "通过属性覆盖" },
            { "ScaleByStat", "按属性缩放" },
            { "FinalMultiply", "最终乘法" },
            { "FinalDivide", "最终除法" },
            { "ClampMinByStat", "按属性钳制最小值" },
            { "ClampMaxByStat", "按属性钳制最大值" },
        };

        private void CreateAll()
        {
            if (!Directory.Exists(_targetFolder))
            {
                Directory.CreateDirectory(_targetFolder);
                AssetDatabase.Refresh();
            }

            ProcessTypes(_calculatorTypes);
            ProcessTypes(_linkCalculatorTypes);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[Calculator Generator]</color> Create All & Clean Up completed.");
        }

        private void ProcessTypes(List<Type> types)
        {
            if (types == null || types.Count == 0) return;

            // 1. Get all assets in target folder of the specific base type
            Type baseType = typeof(StatModifierCalculator).IsAssignableFrom(types[0]) ? typeof(StatModifierCalculator) : typeof(LinkStatModifierCalculator);
            string filter = baseType == typeof(StatModifierCalculator) ? "t:StatModifierCalculator" : "t:LinkStatModifierCalculator";

            var existingAssets = AssetDatabase.FindAssets(filter, new[] { _targetFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => new { Path = p, Asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(p) })
                .Where(x => x.Asset != null && types.Contains(x.Asset.GetType()))
                .ToList();

            // 2. Map existing assets by type
            var typeToPaths = new Dictionary<Type, List<string>>();
            foreach (var item in existingAssets)
            {
                Type t = item.Asset.GetType();
                if (!typeToPaths.ContainsKey(t)) typeToPaths[t] = new List<string>();
                typeToPaths[t].Add(item.Path);
            }

            // 3. Process each available type
            foreach (var type in types)
            {
                string assetName = GetAssetName(type);
                string standardPath = Path.Combine(_targetFolder, $"{assetName}.asset").Replace("\\", "/");

                if (typeToPaths.TryGetValue(type, out var paths))
                {
                    // Ensure standard path is used and others are deleted
                    bool standardExists = paths.Any(p => p.Replace("\\", "/") == standardPath);
                    string keepPath = standardExists ? standardPath : paths[0].Replace("\\", "/");

                    foreach (var p in paths)
                    {
                        string normalizedP = p.Replace("\\", "/");
                        if (normalizedP != keepPath)
                        {
                            AssetDatabase.DeleteAsset(normalizedP);
                        }
                    }

                    if (keepPath != standardPath)
                    {
                        AssetDatabase.MoveAsset(keepPath, standardPath);
                    }
                }
                else
                {
                    // Create new
                    var asset = ScriptableObject.CreateInstance(type);
                    AssetDatabase.CreateAsset(asset, standardPath);
                }
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create All & Clean", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Confirm", "This will create all missing calculators and delete redundant ones in the target folder. Continue?", "Yes", "No"))
                {
                    CreateAll();
                }
            }
            if (GUILayout.Button("Refresh Classes", GUILayout.Height(25)))
                RefreshTypes();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            EditorGUILayout.LabelField("v1.0 - AxuIndieModules", EditorStyles.centeredGreyMiniLabel);
        }
    }
}
