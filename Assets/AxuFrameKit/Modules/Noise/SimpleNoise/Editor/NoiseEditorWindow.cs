using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuFrameKit.Modules.Noise.SimpleNoise.Editor
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

        // --- Alpha 剔除参数 ---
        private bool _enableAlphaCulling = false;
        private float _alphaThreshold = 0.5f;

        // 预览
        private Texture2D _previewTexture;
        private bool _autoUpdate = true;

        // UI Elements
        private Image _previewImage;
        private Label _previewInfoLabel;
        private VisualElement _fractalSettings;
        private VisualElement _alphaCullingSettings;

        [MenuItem("AxuFrameKit/Utility/Noise Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<NoiseEditorWindow>("Noise Editor");
            window.minSize = new Vector2(450, 700);
        }

        private void OnEnable()
        {
            // 延迟生成纹理
        }

        private void CreateGUI()
        {
            // Load USS
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/AxuFrameKit/Modules/Noise/Editor/NoiseWindowStyles.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            rootVisualElement.AddToClassList("single-column-root");

            // Main panel
            var mainPanel = new VisualElement();
            mainPanel.AddToClassList("single-column-panel");
            mainPanel.style.flexGrow = 1;
            rootVisualElement.Add(mainPanel);

            // Title
            var title = new Label("NOISE GENERATOR");
            title.AddToClassList("window-title");
            mainPanel.Add(title);

            // Scroll View
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("single-column-scroll");
            mainPanel.Add(scroll);

            // === Noise Settings ===
            scroll.Add(CreateSectionHeader("Noise Settings"));

            scroll.Add(CreateSliderIntField("分辨率 (Resolution)", _resolution, 64, 2048, v => { _resolution = v; OnParameterChanged(); }));
            scroll.Add(CreateSliderField("缩放 (Scale)", _scale, 0.1f, 100f, v => { _scale = v; OnParameterChanged(); }));
            scroll.Add(CreateVector2Field("偏移 (Offset)", _offset, v => { _offset = v; OnParameterChanged(); }));
            scroll.Add(CreateIntField("随机种子 (Seed)", _seed, v => { _seed = v; OnParameterChanged(); }));

            scroll.Add(CreateSeparator());

            // Noise Type
            scroll.Add(CreateEnumField("噪声类型 (Type)", _currentType, v => { _currentType = v; UpdateConditionalVisibility(); OnParameterChanged(); }));

            // Fractal Settings (conditional)
            _fractalSettings = new VisualElement();
            _fractalSettings.AddToClassList("indented-content");
            scroll.Add(_fractalSettings);

            var fractalLabel = new Label("分形详情 (Fractal Details)");
            fractalLabel.style.fontSize = 10;
            fractalLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            fractalLabel.style.marginBottom = 4;
            _fractalSettings.Add(fractalLabel);

            _fractalSettings.Add(CreateSliderIntField("层数 (Octaves)", _octaves, 1, 8, v => { _octaves = v; OnParameterChanged(); }));
            _fractalSettings.Add(CreateSliderField("持久度 (Persistence)", _persistence, 0.1f, 1f, v => { _persistence = v; OnParameterChanged(); }));
            _fractalSettings.Add(CreateSliderField("空隙度 (Lacunarity)", _lacunarity, 1f, 4f, v => { _lacunarity = v; OnParameterChanged(); }));

            scroll.Add(CreateSeparator());

            // === Post Processing ===
            scroll.Add(CreateSectionHeader("后期处理 (Post Processing)"));

            scroll.Add(CreateToggleField("开启 Alpha 剔除", _enableAlphaCulling, v => { _enableAlphaCulling = v; UpdateConditionalVisibility(); OnParameterChanged(); }));

            _alphaCullingSettings = new VisualElement();
            _alphaCullingSettings.AddToClassList("indented-content");
            scroll.Add(_alphaCullingSettings);

            _alphaCullingSettings.Add(CreateSliderField("剔除阈值 (Threshold)", _alphaThreshold, 0f, 1f, v => { _alphaThreshold = v; OnParameterChanged(); }));

            scroll.Add(CreateSeparator());

            // Auto Update
            scroll.Add(CreateToggleField("自动刷新预览", _autoUpdate, v => _autoUpdate = v));

            scroll.Add(CreateSeparator());

            // Generate Button
            var generateBtn = new Button(GenerateTexture) { text = "生成预览 (Generate Preview)" };
            generateBtn.AddToClassList("ue5-button");
            generateBtn.AddToClassList("ue5-button-primary");
            generateBtn.style.height = 32;
            scroll.Add(generateBtn);

            // === Preview Area ===
            scroll.Add(CreateSeparator());
            scroll.Add(CreateSectionHeader("Preview"));

            _previewImage = new Image();
            _previewImage.AddToClassList("preview-box");
            _previewImage.scaleMode = ScaleMode.ScaleToFit;
            _previewImage.style.width = 400;
            _previewImage.style.height = 400;
            _previewImage.style.backgroundColor = new StyleColor(new Color(0.05f, 0.05f, 0.05f));
            scroll.Add(_previewImage);

            _previewInfoLabel = new Label("当前预览: --");
            _previewInfoLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _previewInfoLabel.style.marginTop = 8;
            _previewInfoLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            _previewInfoLabel.style.fontSize = 11;
            scroll.Add(_previewInfoLabel);

            scroll.Add(CreateSeparator());

            // Save Button
            var saveBtn = new Button(SaveTexture) { text = "保存图片 (Save Texture)" };
            saveBtn.AddToClassList("ue5-button");
            saveBtn.AddToClassList("ue5-button-success");
            saveBtn.style.height = 36;
            scroll.Add(saveBtn);

            // Initial state
            UpdateConditionalVisibility();

            // Generate initial texture
            rootVisualElement.schedule.Execute(() => GenerateTexture()).ExecuteLater(100);
        }

        #region UI Helpers

        private VisualElement CreateSectionHeader(string text)
        {
            var header = new VisualElement();
            header.AddToClassList("section-header");
            var label = new Label(text);
            label.AddToClassList("section-header-text");
            header.Add(label);
            return header;
        }

        private Label CreateFieldLabel(string text)
        {
            var label = new Label(text);
            label.AddToClassList("field-label");
            return label;
        }

        private VisualElement CreateSeparator()
        {
            var sep = new VisualElement();
            sep.AddToClassList("separator");
            return sep;
        }

        private VisualElement CreateIntField(string label, int value, System.Action<int> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("field-row");
            row.Add(CreateFieldLabel(label));

            var field = new IntegerField();
            field.value = value;
            field.AddToClassList("ue5-int-field");
            field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            row.Add(field);

            return row;
        }

        private VisualElement CreateSliderField(string label, float value, float min, float max, System.Action<float> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("field-row");
            row.Add(CreateFieldLabel(label));

            var container = new VisualElement();
            container.AddToClassList("slider-with-value");
            container.style.flexGrow = 1;

            var slider = new Slider(min, max);
            slider.value = value;
            slider.AddToClassList("ue5-slider");
            slider.style.flexGrow = 1;

            var valueLabel = new Label(value.ToString("F2"));
            valueLabel.AddToClassList("slider-value-label");

            slider.RegisterValueChangedCallback(evt =>
            {
                valueLabel.text = evt.newValue.ToString("F2");
                onChanged(evt.newValue);
            });

            container.Add(slider);
            container.Add(valueLabel);
            row.Add(container);

            return row;
        }

        private VisualElement CreateSliderIntField(string label, int value, int min, int max, System.Action<int> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("field-row");
            row.Add(CreateFieldLabel(label));

            var container = new VisualElement();
            container.AddToClassList("slider-with-value");
            container.style.flexGrow = 1;

            var slider = new SliderInt(min, max);
            slider.value = value;
            slider.AddToClassList("ue5-slider");
            slider.style.flexGrow = 1;

            var valueLabel = new Label(value.ToString());
            valueLabel.AddToClassList("slider-value-label");

            slider.RegisterValueChangedCallback(evt =>
            {
                valueLabel.text = evt.newValue.ToString();
                onChanged(evt.newValue);
            });

            container.Add(slider);
            container.Add(valueLabel);
            row.Add(container);

            return row;
        }

        private VisualElement CreateEnumField<T>(string label, T value, System.Action<T> onChanged) where T : System.Enum
        {
            var row = new VisualElement();
            row.AddToClassList("field-row");
            row.Add(CreateFieldLabel(label));

            var field = new EnumField(value);
            field.AddToClassList("ue5-enum-field");
            field.RegisterValueChangedCallback(evt => onChanged((T)evt.newValue));
            row.Add(field);

            return row;
        }

        private VisualElement CreateToggleField(string label, bool value, System.Action<bool> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("field-row");
            row.Add(CreateFieldLabel(label));

            var toggle = new Toggle();
            toggle.value = value;
            toggle.AddToClassList("ue5-toggle");
            toggle.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            row.Add(toggle);

            return row;
        }

        private VisualElement CreateVector2Field(string label, Vector2 value, System.Action<Vector2> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("field-row");
            row.Add(CreateFieldLabel(label));

            var field = new Vector2Field();
            field.value = value;
            field.AddToClassList("ue5-vector2-field");
            field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            row.Add(field);

            return row;
        }

        #endregion

        #region Logic

        private void OnParameterChanged()
        {
            if (_autoUpdate) GenerateTexture();
        }

        private void UpdateConditionalVisibility()
        {
            if (_fractalSettings != null)
            {
                _fractalSettings.style.display = _currentType == NoiseType.FractalPerlin
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_alphaCullingSettings != null)
            {
                _alphaCullingSettings.style.display = _enableAlphaCulling
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void GenerateTexture()
        {
            NoiseCreator.Init(_seed);

            if (_previewTexture == null || _previewTexture.width != _resolution || _previewTexture.height != _resolution)
            {
                _previewTexture = new Texture2D(_resolution, _resolution, TextureFormat.RGB24, false)
                {
                    filterMode = FilterMode.Bilinear
                };
            }

            Color[] pixels = new Color[_resolution * _resolution];

            for (int y = 0; y < _resolution; y++)
            {
                for (int x = 0; x < _resolution; x++)
                {
                    double xCoord = (x / (double)_resolution) * _scale + _offset.x + 0.1f;
                    double yCoord = (y / (double)_resolution) * _scale + _offset.y + 0.1f;

                    double sample = _currentType switch
                    {
                        NoiseType.StandardPerlin => (NoiseCreator.Perlin(xCoord, yCoord) + 1.0) * 0.5,
                        NoiseType.FractalPerlin => NoiseCreator.FractalNoise(xCoord, yCoord, _octaves, _persistence, _lacunarity),
                        NoiseType.WhiteNoise => NoiseCreator.WhiteNoise(xCoord, yCoord),
                        _ => 0
                    };

                    float val = Mathf.Clamp01((float)sample);

                    if (_enableAlphaCulling)
                    {
                        val = val < _alphaThreshold ? 0f : 1f;
                    }

                    pixels[y * _resolution + x] = new Color(val, val, val, 1.0f);
                }
            }

            _previewTexture.SetPixels(pixels);
            _previewTexture.Apply();

            // Update UI
            if (_previewImage != null)
            {
                _previewImage.image = _previewTexture;
            }

            if (_previewInfoLabel != null)
            {
                _previewInfoLabel.text = $"当前预览: {_previewTexture.width}x{_previewTexture.height}";
            }
        }

        private void SaveTexture()
        {
            if (_previewTexture == null) return;

            string path = EditorUtility.SaveFilePanel("保存噪声贴图", Application.dataPath, "NewNoise", "png");

            if (!string.IsNullOrEmpty(path))
            {
                byte[] bytes = _previewTexture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);

                AssetDatabase.Refresh();

                if (path.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath));
                    Debug.Log($"噪声图片已保存至: {relativePath}");
                }
            }
        }

        private void OnDestroy()
        {
            if (_previewTexture != null)
            {
                DestroyImmediate(_previewTexture);
            }
        }

        #endregion
    }
}