using System.IO;
using UnityEditor;
using UnityEngine;
using AxuFrameKit._Core.Utility.Noise;

namespace AxuFrameKit.Common.Editor.UtilityWindow.Noise
{
    public class NoiseEditorWindow : EditorWindow
    {
        // --- 设置参数 ---
        private int _resolution = 512;
        private float _scale = 10f;
        private int _seed = 12345;
    
        // 偏移量
        private Vector2 _offset = Vector2.zero;

        // FBM (分形) 参数
        private int _octaves = 4;
        private float _persistence = 0.5f;
        private float _lacunarity = 2.0f;

        // 噪声类型
        private enum NoiseType { StandardPerlin, FractalPerlin, WhiteNoise }
        private NoiseType _currentType = NoiseType.FractalPerlin;

        // --- 新增：Alpha 剔除参数 ---
        private bool _enableAlphaCulling = false;
        private float _alphaThreshold = 0.5f;

        // 预览
        private Texture2D _previewTexture;
        private bool _autoUpdate = true;
    
        // 滚动视图位置
        private Vector2 _scrollPos;

        [MenuItem("AxuFrameKit/Utility/Noise Generator")]
        public static void ShowWindow()
        {
            GetWindow<NoiseEditorWindow>("Noise Editor");
        }

        private void OnEnable()
        {
            // 窗口开启时生成一次
            GenerateTexture();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            GUILayout.Label("噪声生成设置 (Noise Settings)", EditorStyles.boldLabel);

            // --- UI 控件 ---
            EditorGUI.BeginChangeCheck();

            // 基础设置
            _resolution = EditorGUILayout.IntSlider("分辨率 (Resolution)", _resolution, 64, 2048);
            _scale      = EditorGUILayout.Slider("缩放 (Scale)", _scale, 0.1f, 100f);
            _offset     = EditorGUILayout.Vector2Field("偏移 (Offset)", _offset);
            _seed       = EditorGUILayout.IntField("随机种子 (Seed)", _seed);
        
            GUILayout.Space(10);
            _currentType = (NoiseType)EditorGUILayout.EnumPopup("噪声类型 (Type)", _currentType);

            // 仅在分形模式下显示的参数
            if (_currentType == NoiseType.FractalPerlin)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("分形详情 (Fractal Details)", EditorStyles.miniLabel);
                _octaves     = EditorGUILayout.IntSlider("层数 (Octaves)", _octaves, 1, 8);
                _persistence = EditorGUILayout.Slider("持久度 (Persistence)", _persistence, 0.1f, 1f);
                _lacunarity  = EditorGUILayout.Slider("空隙度 (Lacunarity)", _lacunarity, 1f, 4f);
                EditorGUI.indentLevel--;
            }

            // --- 新增：Alpha 剔除 UI ---
            GUILayout.Space(10);
            GUILayout.Label("后期处理 (Post Processing)", EditorStyles.boldLabel);
            _enableAlphaCulling = EditorGUILayout.Toggle("开启 Alpha 剔除", _enableAlphaCulling);
        
            if (_enableAlphaCulling)
            {
                EditorGUI.indentLevel++;
                _alphaThreshold = EditorGUILayout.Slider("剔除阈值 (Threshold)", _alphaThreshold, 0f, 1f);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);
            _autoUpdate = EditorGUILayout.Toggle("自动刷新预览", _autoUpdate);

            // 如果参数改变且开启了自动刷新，则重新生成
            if (EditorGUI.EndChangeCheck())
            {
                if (_autoUpdate) GenerateTexture();
            }

            GUILayout.Space(10);

            // 手动生成按钮
            if (GUILayout.Button("生成预览 (Generate Preview)", GUILayout.Height(30)))
            {
                GenerateTexture();
            }

            GUILayout.Space(10);
        
            // --- 预览区域 ---
            if (_previewTexture != null)
            {
                // 计算正方形尺寸：窗口宽度的 0.9 倍
                float size = position.width * 0.9f;

                // 使用水平布局 + FlexibleSpace 实现居中
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace(); // 左侧弹簧

                // 绘制占位框 (正方形)
                GUILayout.Box(GUIContent.none, GUILayout.Width(size), GUILayout.Height(size));
            
                // 获取上一个控件（Box）的矩形区域并在其中绘制纹理
                Rect lastRect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawPreviewTexture(lastRect, _previewTexture);

                GUILayout.FlexibleSpace(); // 右侧弹簧
                GUILayout.EndHorizontal();
            
                // 下方文字也居中
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label($"当前预览: {_previewTexture.width}x{_previewTexture.height}");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            // --- 保存功能 ---
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("保存图片 (Save Texture)", GUILayout.Height(40)))
            {
                SaveTexture();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }

        private void GenerateTexture()
        {
            // 1. 初始化纯C#的噪声生成器
            NoiseCreator.Init(_seed);

            // 2. 创建 Texture2D
            if (_previewTexture == null || _previewTexture.width != _resolution || _previewTexture.height != _resolution)
            {
                _previewTexture = new Texture2D(_resolution, _resolution, TextureFormat.RGB24, false)
                {
                    filterMode = FilterMode.Bilinear
                };
            }

            Color[] pixels = new Color[_resolution * _resolution];

            // 3. 遍历像素
            for (int y = 0; y < _resolution; y++)
            {
                for (int x = 0; x < _resolution; x++)
                {
                    // 计算采样坐标 (使用纯C#逻辑传参)
                    // 加上 0.1f 避免整数边界问题
                    double xCoord = (x / (double)_resolution) * _scale + _offset.x + 0.1f;
                    double yCoord = (y / (double)_resolution) * _scale + _offset.y + 0.1f;

                    double sample = _currentType switch
                    {
                        NoiseType.StandardPerlin => (NoiseCreator.Perlin(xCoord, yCoord) + 1.0) * 0.5, // 标准 Perlin 输出约为 -1~1，映射到 0~1
                        NoiseType.FractalPerlin => NoiseCreator.FractalNoise(xCoord, yCoord, _octaves, _persistence, _lacunarity), // 已经在 FractalNoise 内部映射到 0~1
                        NoiseType.WhiteNoise => NoiseCreator.WhiteNoise(xCoord, yCoord),
                        _ => 0
                    };

                    // 钳制到 0-1
                    float val = Mathf.Clamp01((float)sample);

                    // --- 新增：Alpha 剔除逻辑 ---
                    if (_enableAlphaCulling)
                    {
                        // 小于阈值为黑色(0)，大于等于阈值为白色(1)
                        val = val < _alphaThreshold ? 0f : 1f;
                    }

                    pixels[y * _resolution + x] = new Color(val, val, val, 1.0f);
                }
            }

            _previewTexture.SetPixels(pixels);
            _previewTexture.Apply();
        }

        private void SaveTexture()
        {
            if (_previewTexture == null) return;

            string path = EditorUtility.SaveFilePanel("保存噪声贴图", Application.dataPath, "NewNoise", "png");
        
            if (!string.IsNullOrEmpty(path))
            {
                byte[] bytes = _previewTexture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
            
                // 刷新 AssetDatabase 以便在 Unity 编辑器中立即看到文件
                AssetDatabase.Refresh();
            
                // 如果路径在 Assets 文件夹内，高亮显示
                if (path.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath));
                    Debug.Log($"噪声图片已保存至: {relativePath}");
                }
            }
        }
    
        // 窗口关闭时清理内存
        private void OnDestroy()
        {
            if (_previewTexture != null)
            {
                DestroyImmediate(_previewTexture);
            }
        }
    }
}