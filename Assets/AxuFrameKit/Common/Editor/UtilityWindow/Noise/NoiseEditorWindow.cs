using System.IO;
using AxuFrameKit._Core.Utility.Noise;
using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.UtilityWindow.Noise
{
    public class NoiseEditorWindow : EditorWindow
    {
        // --- 设置参数 ---
        private int resolution = 512;
        private float scale = 10f;
        private int seed = 12345;
    
        // 偏移量
        private Vector2 offset = Vector2.zero;

        // FBM (分形) 参数
        private int octaves = 4;
        private float persistence = 0.5f;
        private float lacunarity = 2.0f;

        // 噪声类型
        public enum NoiseType { StandardPerlin, FractalPerlin, WhiteNoise }
        private NoiseType currentType = NoiseType.FractalPerlin;

        // --- 新增：Alpha 剔除参数 ---
        private bool enableAlphaCulling = false;
        private float alphaThreshold = 0.5f;

        // 预览
        private Texture2D previewTexture;
        private bool autoUpdate = true;
    
        // 滚动视图位置
        private Vector2 scrollPos;

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
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("噪声生成设置 (Noise Settings)", EditorStyles.boldLabel);

            // --- UI 控件 ---
            EditorGUI.BeginChangeCheck();

            // 基础设置
            resolution = EditorGUILayout.IntSlider("分辨率 (Resolution)", resolution, 64, 2048);
            scale      = EditorGUILayout.Slider("缩放 (Scale)", scale, 0.1f, 100f);
            offset     = EditorGUILayout.Vector2Field("偏移 (Offset)", offset);
            seed       = EditorGUILayout.IntField("随机种子 (Seed)", seed);
        
            GUILayout.Space(10);
            currentType = (NoiseType)EditorGUILayout.EnumPopup("噪声类型 (Type)", currentType);

            // 仅在分形模式下显示的参数
            if (currentType == NoiseType.FractalPerlin)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("分形详情 (Fractal Details)", EditorStyles.miniLabel);
                octaves     = EditorGUILayout.IntSlider("层数 (Octaves)", octaves, 1, 8);
                persistence = EditorGUILayout.Slider("持久度 (Persistence)", persistence, 0.1f, 1f);
                lacunarity  = EditorGUILayout.Slider("空隙度 (Lacunarity)", lacunarity, 1f, 4f);
                EditorGUI.indentLevel--;
            }

            // --- 新增：Alpha 剔除 UI ---
            GUILayout.Space(10);
            GUILayout.Label("后期处理 (Post Processing)", EditorStyles.boldLabel);
            enableAlphaCulling = EditorGUILayout.Toggle("开启 Alpha 剔除", enableAlphaCulling);
        
            if (enableAlphaCulling)
            {
                EditorGUI.indentLevel++;
                alphaThreshold = EditorGUILayout.Slider("剔除阈值 (Threshold)", alphaThreshold, 0f, 1f);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);
            autoUpdate = EditorGUILayout.Toggle("自动刷新预览", autoUpdate);

            // 如果参数改变且开启了自动刷新，则重新生成
            if (EditorGUI.EndChangeCheck())
            {
                if (autoUpdate) GenerateTexture();
            }

            GUILayout.Space(10);

            // 手动生成按钮
            if (GUILayout.Button("生成预览 (Generate Preview)", GUILayout.Height(30)))
            {
                GenerateTexture();
            }

            GUILayout.Space(10);
        
            // --- 预览区域 ---
            if (previewTexture != null)
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
                EditorGUI.DrawPreviewTexture(lastRect, previewTexture);

                GUILayout.FlexibleSpace(); // 右侧弹簧
                GUILayout.EndHorizontal();
            
                // 下方文字也居中
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label($"当前预览: {previewTexture.width}x{previewTexture.height}");
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
            NoiseCreator.Init(seed);

            // 2. 创建 Texture2D
            if (previewTexture == null || previewTexture.width != resolution || previewTexture.height != resolution)
            {
                previewTexture            = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
                previewTexture.filterMode = FilterMode.Bilinear;
            }

            Color[] pixels = new Color[resolution * resolution];

            // 3. 遍历像素
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // 计算采样坐标 (使用纯C#逻辑传参)
                    // 加上 0.1f 避免整数边界问题
                    double xCoord = (x / (double)resolution) * scale + offset.x + 0.1f;
                    double yCoord = (y / (double)resolution) * scale + offset.y + 0.1f;

                    double sample = 0;

                    switch (currentType)
                    {
                        case NoiseType.StandardPerlin:
                            // 标准 Perlin 输出约为 -1~1，映射到 0~1
                            sample = (NoiseCreator.Perlin(xCoord, yCoord) + 1.0) * 0.5;
                            break;
                        case NoiseType.FractalPerlin:
                            // 已经在 FractalNoise 内部映射到 0~1
                            sample = NoiseCreator.FractalNoise(xCoord, yCoord, octaves, persistence, lacunarity);
                            break;
                        case NoiseType.WhiteNoise:
                            sample = NoiseCreator.WhiteNoise(xCoord, yCoord);
                            break;
                    }

                    // 钳制到 0-1
                    float val = Mathf.Clamp01((float)sample);

                    // --- 新增：Alpha 剔除逻辑 ---
                    if (enableAlphaCulling)
                    {
                        // 小于阈值为黑色(0)，大于等于阈值为白色(1)
                        val = val < alphaThreshold ? 0f : 1f;
                    }

                    pixels[y * resolution + x] = new Color(val, val, val, 1.0f);
                }
            }

            previewTexture.SetPixels(pixels);
            previewTexture.Apply();
        }

        private void SaveTexture()
        {
            if (previewTexture == null) return;

            string path = EditorUtility.SaveFilePanel("保存噪声贴图", Application.dataPath, "NewNoise", "png");
        
            if (!string.IsNullOrEmpty(path))
            {
                byte[] bytes = previewTexture.EncodeToPNG();
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
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
            }
        }
    }
}