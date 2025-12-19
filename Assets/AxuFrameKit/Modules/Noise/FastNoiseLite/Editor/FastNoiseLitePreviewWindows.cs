using System.IO;
using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Modules.Noise.FastNoiseLite.Editor
{
    public class FastNoiseLitePreviewWindow : EditorWindow
    {
        // ========================================================================
        // 状态变量
        // ========================================================================
        private Noise.FastNoiseLite.FastNoiseLite _noise;
        private Texture2D _previewTexture;
    
        // UI 状态
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private bool _useDomainWarp = false;
    
        // 预览与导出设置
        public int resX = 256; // 宽
        public int resY = 256; // 高
        public FilterMode filterMode = FilterMode.Point;
        public bool useUVCoordinates = true; // 是否使用UV坐标(0-1)而不是像素坐标

        // ========================================================================
        // FastNoiseLite 参数映射
        // ========================================================================
        // General
        public int seed = 1337;
        public float frequency = 0.01f;
        public Noise.FastNoiseLite.FastNoiseLite.NoiseType noiseType = Noise.FastNoiseLite.FastNoiseLite.NoiseType.OpenSimplex2;
        public Noise.FastNoiseLite.FastNoiseLite.RotationType3D rotationType3D = Noise.FastNoiseLite.FastNoiseLite.RotationType3D.None;

        // Fractal
        public Noise.FastNoiseLite.FastNoiseLite.FractalType fractalType = Noise.FastNoiseLite.FastNoiseLite.FractalType.None;
        public int fractalOctaves = 3;
        public float fractalLacunarity = 2.0f;
        public float fractalGain = 0.5f;
        public float fractalWeightedStrength = 0.0f;
        public float fractalPingPongStrength = 2.0f;

        // Cellular
        public Noise.FastNoiseLite.FastNoiseLite.CellularDistanceFunction cellularDistanceFunc = Noise.FastNoiseLite.FastNoiseLite.CellularDistanceFunction.EuclideanSq;
        public Noise.FastNoiseLite.FastNoiseLite.CellularReturnType cellularReturnType = Noise.FastNoiseLite.FastNoiseLite.CellularReturnType.Distance;
        public float cellularJitter = 1.0f;

        // Domain Warp
        public Noise.FastNoiseLite.FastNoiseLite.DomainWarpType domainWarpType = Noise.FastNoiseLite.FastNoiseLite.DomainWarpType.OpenSimplex2;
        public float domainWarpAmp = 1.0f;

        // Preview Settings
        public float zCoord = 0.0f; // 用于 3D 噪声切片预览

        [MenuItem("AxuFrameKit/Utility/FastNoiseLite Previewer")]
        public static void ShowWindow()
        {
            GetWindow<FastNoiseLitePreviewWindow>("Noise Preview");
        }

        private void OnEnable()
        {
            _noise = new Noise.FastNoiseLite.FastNoiseLite();
            // 初始化时不立即生成，等待UpdateTexture检查
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // -------------------------
            // 左侧：参数设置面板
            // -------------------------
            EditorGUILayout.BeginVertical(GUILayout.Width(350));
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader("Output Settings");
            EditorGUI.BeginChangeCheck();
        
            // 分辨率设置 (XY 分离)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Resolution", GUILayout.Width(70));
            EditorGUILayout.LabelField("W", GUILayout.Width(15));
            resX = EditorGUILayout.IntField(resX, GUILayout.Width(50));
            EditorGUILayout.LabelField("H", GUILayout.Width(15));
            resY = EditorGUILayout.IntField(resY, GUILayout.Width(50));
        
            // 快捷按钮 (同时设置宽高)
            if (GUILayout.Button("256", GUILayout.Width(40))) { resX = 256; resY = 256; }
            if (GUILayout.Button("512", GUILayout.Width(40))) { resX = 512; resY = 512; }
            EditorGUILayout.EndHorizontal();

            // 限制分辨率范围
            resX = Mathf.Clamp(resX, 32, 4096);
            resY = Mathf.Clamp(resY, 32, 4096);

            filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", filterMode);
        
            // UV 坐标模式说明
            useUVCoordinates = EditorGUILayout.Toggle(new GUIContent("Use UV Coords", "If Checked: Noise maps to 0-1 UV space (Resolution independent).\nIf Unchecked: Noise maps to Pixel space (Changing resolution changes visible area)."), useUVCoordinates);
            if (useUVCoordinates)
            {
                EditorGUILayout.HelpBox("Mapping: (x / width, y / height) * frequency\nAspect ratio affects noise stretch.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Mapping: (x, y) * frequency\nPixel perfect noise scale.", MessageType.None);
            }
        
            EditorGUILayout.Space(5);
            bool tempAuto = EditorGUILayout.Toggle("Auto Refresh", _autoRefresh);
            if (tempAuto != _autoRefresh) { _autoRefresh = tempAuto; }
        
            zCoord = EditorGUILayout.Slider("Z Coord (3D Slice)", zCoord, -100f, 100f);

            // General
            DrawHeader("General Settings");
            seed           = EditorGUILayout.IntField("Seed", seed);
            frequency      = EditorGUILayout.FloatField("Frequency", frequency);
            noiseType      = (Noise.FastNoiseLite.FastNoiseLite.NoiseType)EditorGUILayout.EnumPopup("Noise Type", noiseType);
            rotationType3D = (Noise.FastNoiseLite.FastNoiseLite.RotationType3D)EditorGUILayout.EnumPopup("Rotation Type 3D", rotationType3D);

            // Fractal
            DrawHeader("Fractal Settings");
            fractalType = (Noise.FastNoiseLite.FastNoiseLite.FractalType)EditorGUILayout.EnumPopup("Fractal Type", fractalType);
            if (fractalType != Noise.FastNoiseLite.FastNoiseLite.FractalType.None)
            {
                EditorGUI.indentLevel++;
                fractalOctaves          = EditorGUILayout.IntSlider("Octaves", fractalOctaves, 1, 20);
                fractalLacunarity       = EditorGUILayout.FloatField("Lacunarity", fractalLacunarity);
                fractalGain             = EditorGUILayout.FloatField("Gain", fractalGain);
                fractalWeightedStrength = EditorGUILayout.Slider("Weighted Strength", fractalWeightedStrength, 0f, 1f);
                if (fractalType == Noise.FastNoiseLite.FastNoiseLite.FractalType.PingPong)
                {
                    fractalPingPongStrength = EditorGUILayout.FloatField("Ping Pong Strength", fractalPingPongStrength);
                }
                EditorGUI.indentLevel--;
            }

            // Cellular
            if (noiseType == Noise.FastNoiseLite.FastNoiseLite.NoiseType.Cellular)
            {
                DrawHeader("Cellular Settings");
                EditorGUI.indentLevel++;
                cellularDistanceFunc = (Noise.FastNoiseLite.FastNoiseLite.CellularDistanceFunction)EditorGUILayout.EnumPopup("Distance Function", cellularDistanceFunc);
                cellularReturnType   = (Noise.FastNoiseLite.FastNoiseLite.CellularReturnType)EditorGUILayout.EnumPopup("Return Type", cellularReturnType);
                cellularJitter       = EditorGUILayout.Slider("Jitter", cellularJitter, 0f, 1.5f);
                EditorGUI.indentLevel--;
            }

            // Domain Warp
            DrawHeader("Domain Warp (Experimental)");
            _useDomainWarp = EditorGUILayout.Toggle("Enable Domain Warp", _useDomainWarp);
            if (_useDomainWarp)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Domain Warp applies a distortion to the coordinates BEFORE sampling the noise type selected above.", MessageType.Info);
                domainWarpType = (Noise.FastNoiseLite.FastNoiseLite.DomainWarpType)EditorGUILayout.EnumPopup("Warp Type", domainWarpType);
                domainWarpAmp  = EditorGUILayout.FloatField("Warp Amplitude", domainWarpAmp);
                EditorGUI.indentLevel--;
            }

            bool paramsChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Force Refresh", GUILayout.Height(30)))
            {
                UpdateTexture();
            }
        
            if (GUILayout.Button($"Save Texture ({resX}x{resY})", GUILayout.Height(30)))
            {
                SaveTexture();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // -------------------------
            // 右侧：预览图
            // -------------------------
            EditorGUILayout.BeginVertical();
            DrawHeader("Preview Output");
        
            // 即使没有 Texture 也要占位，避免布局跳动
            Rect rect = GUILayoutUtility.GetRect(100, 1000, 100, 1000);
        
            if (_previewTexture != null)
            {
                // 计算预览区域，保持正确的长宽比
                float aspect = (float)resX / (float)resY;
                float width  = rect.width;
                float height = width / aspect;
            
                // 如果高度超出了窗口高度，基于高度反算宽度
                if (height > rect.height)
                {
                    height = rect.height;
                    width  = height * aspect;
                }
            
                // 居中显示
                float offsetX = (rect.width - width) * 0.5f;
                float offsetY = (rect.height - height) * 0.5f;

                Rect drawRect = new Rect(rect.x + offsetX, rect.y + offsetY, width, height);

                // 绘制纹理背景（棋盘格），方便查看透明通道
                EditorGUI.DrawTextureTransparent(drawRect, _previewTexture, ScaleMode.ScaleToFit);
            
                // 显示当前信息
                GUILayout.Label($"Resolution: {_previewTexture.width}x{_previewTexture.height} | Ratio: {aspect:F2}", EditorStyles.miniLabel);
            }
            else
            {
                if (Event.current.type == EventType.Repaint) UpdateTexture();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // 逻辑处理
            if (paramsChanged && _autoRefresh)
            {
                UpdateTexture();
            }
        }

        private void UpdateTexture()
        {
            if (_noise == null) _noise = new Noise.FastNoiseLite.FastNoiseLite();

            // 检查纹理是否需要重新初始化（分辨率改变或为空）
            if (_previewTexture == null || _previewTexture.width != resX || _previewTexture.height != resY)
            {
                // Reinitialize 可以保留 Texture 对象引用，只是改变大小和格式
                if (_previewTexture == null)
                {
                    _previewTexture = new Texture2D(resX, resY, TextureFormat.RGBA32, false);
                }
                else
                {
                    _previewTexture.Reinitialize(resX, resY);
                }
                _previewTexture.wrapMode = TextureWrapMode.Clamp;
            }

            // 应用过滤模式
            if (_previewTexture.filterMode != filterMode)
            {
                _previewTexture.filterMode = filterMode;
            }

            // 1. 应用设置到 FastNoiseLite 实例
            _noise.SetSeed(seed);
            _noise.SetFrequency(frequency);
            _noise.SetNoiseType(noiseType);
            _noise.SetRotationType3D(rotationType3D);

            _noise.SetFractalType(fractalType);
            _noise.SetFractalOctaves(fractalOctaves);
            _noise.SetFractalLacunarity(fractalLacunarity);
            _noise.SetFractalGain(fractalGain);
            _noise.SetFractalWeightedStrength(fractalWeightedStrength);
            _noise.SetFractalPingPongStrength(fractalPingPongStrength);

            _noise.SetCellularDistanceFunction(cellularDistanceFunc);
            _noise.SetCellularReturnType(cellularReturnType);
            _noise.SetCellularJitter(cellularJitter);

            _noise.SetDomainWarpType(domainWarpType);
            _noise.SetDomainWarpAmp(domainWarpAmp);

            // 2. 生成纹理数据
            Color[] pixels = new Color[resX * resY];
        
            // 预计算倒数，优化循环内计算
            float invResX = 1.0f / resX;
            float invResY = 1.0f / resY;

            for (int y = 0; y < resY; y++)
            {
                for (int x = 0; x < resX; x++)
                {
                    // 坐标计算
                    float xPos, yPos;

                    if (useUVCoordinates)
                    {
                        // 模式 A: UV 坐标 (0 到 1)，乘以 Frequency * 100
                        // 注意：如果宽高比不同，这里的 UV 也是 0-1，所以噪声形状会随图片比例拉伸
                        xPos = x * invResX * 100.0f; 
                        yPos = y * invResY * 100.0f;
                    }
                    else
                    {
                        // 模式 B: 像素坐标，直接传入 x, y
                        // 改变分辨率会改变视野 (Zoom out)，不拉伸
                        xPos = x;
                        yPos = y;
                    }

                    float zPos = zCoord; 

                    // 如果启用了 Domain Warp
                    if (_useDomainWarp)
                    {
                        _noise.DomainWarp(ref xPos, ref yPos, ref zPos);
                    }

                    // 获取噪声值 (-1 到 1)
                    float noiseValue = _noise.GetNoise(xPos, yPos, zPos);

                    // 归一化到 0-1
                    float normalized = Mathf.Clamp01((noiseValue + 1f) * 0.5f);

                    pixels[y * resX + x] = new Color(normalized, normalized, normalized, 1f);
                }
            }

            _previewTexture.SetPixels(pixels);
            _previewTexture.Apply();
        }

        private void SaveTexture()
        {
            if (_previewTexture == null) return;

            byte[] bytes = _previewTexture.EncodeToPNG();
            string path  = Application.dataPath + $"/FastNoiseOutput_{resX}x{resY}.png";
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            Debug.Log("Texture saved to: " + path);
        }

        // 辅助绘制样式
        private void DrawHeader(string label)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
        }
    }
}