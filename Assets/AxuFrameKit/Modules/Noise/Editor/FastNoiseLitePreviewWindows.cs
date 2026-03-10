using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
        private bool _autoRefresh = true;
        private bool _useDomainWarp = false;

        // 预览与导出设置
        public int resX = 256;
        public int resY = 256;
        public FilterMode filterMode = FilterMode.Point;
        public bool useUVCoordinates = true;

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
        public float zCoord = 0.0f;

        // UI Elements
        private Image _previewImage;
        private Label _previewInfoLabel;
        private Button _saveBtn;
        private VisualElement _fractalSettings;
        private VisualElement _cellularSettings;
        private VisualElement _domainWarpSettings;
        private IntegerField _resXField;
        private IntegerField _resYField;

        [MenuItem("AxuFrameKit/Modules/Noise/FastNoiseLite Previewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<FastNoiseLitePreviewWindow>("Noise Preview");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            _noise = new Noise.FastNoiseLite.FastNoiseLite();
        }

        private void CreateGUI()
        {
            // Load USS
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var dirPath = System.IO.Path.GetDirectoryName(scriptPath);
            var dirPrePath = System.IO.Path.GetDirectoryName(dirPath);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dirPrePath}/UI/NoiseWindowStyles.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            rootVisualElement.AddToClassList("ue5-root");

            // Main split container
            var mainContainer = new VisualElement();
            mainContainer.AddToClassList("main-split-container");
            mainContainer.style.flexGrow = 1;
            rootVisualElement.Add(mainContainer);

            // Left Panel - Settings
            var settingsPanel = CreateSettingsPanel();
            mainContainer.Add(settingsPanel);

            // Right Panel - Preview
            var previewPanel = CreatePreviewPanel();
            mainContainer.Add(previewPanel);

            // Initial update
            rootVisualElement.schedule.Execute(() => UpdateTexture()).ExecuteLater(100);
        }

        private VisualElement CreateSettingsPanel()
        {
            var panel = new VisualElement();
            panel.AddToClassList("settings-panel");

            var header = new VisualElement();
            header.AddToClassList("panel-header");
            var headerText = new Label("NOISE SETTINGS");
            headerText.AddToClassList("panel-header-text");
            header.Add(headerText);
            panel.Add(header);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("settings-scroll");
            panel.Add(scroll);

            // === Output Settings ===
            scroll.Add(CreateSectionHeader("Output Settings"));

            // Resolution
            var resRow = new VisualElement();
            resRow.AddToClassList("field-row");
            resRow.Add(CreateFieldLabel("Resolution"));

            var resContainer = new VisualElement();
            resContainer.AddToClassList("resolution-row");
            resContainer.style.flexGrow = 1;

            var wLabel = new Label("W");
            wLabel.AddToClassList("resolution-label");
            resContainer.Add(wLabel);

            _resXField = new IntegerField();
            _resXField.value = resX;
            _resXField.AddToClassList("resolution-field");
            _resXField.RegisterValueChangedCallback(evt => { resX = Mathf.Clamp(evt.newValue, 32, 4096); OnParameterChanged(); });
            resContainer.Add(_resXField);

            var hLabel = new Label("H");
            hLabel.AddToClassList("resolution-label");
            resContainer.Add(hLabel);

            _resYField = new IntegerField();
            _resYField.value = resY;
            _resYField.AddToClassList("resolution-field");
            _resYField.RegisterValueChangedCallback(evt => { resY = Mathf.Clamp(evt.newValue, 32, 4096); OnParameterChanged(); });
            resContainer.Add(_resYField);

            // var btn256 = new Button(() => { resX = 256; resY = 256; _resXField.value = 256; _resYField.value = 256; OnParameterChanged(); }) { text = "256" };
            // btn256.AddToClassList("quick-btn");
            // resContainer.Add(btn256);

            // var btn512 = new Button(() => { resX = 512; resY = 512; _resXField.value = 512; _resYField.value = 512; OnParameterChanged(); }) { text = "512" };
            // btn512.AddToClassList("quick-btn");
            // resContainer.Add(btn512);

            resRow.Add(resContainer);
            scroll.Add(resRow);

            // Filter Mode
            scroll.Add(CreateEnumField("Filter Mode", filterMode, v => { filterMode = v; OnParameterChanged(); }));

            // Use UV Coordinates
            scroll.Add(CreateToggleField("Use UV Coords", useUVCoordinates, v => { useUVCoordinates = v; OnParameterChanged(); }));

            // Auto Refresh
            scroll.Add(CreateToggleField("Auto Refresh", _autoRefresh, v => _autoRefresh = v));

            // Z Coord
            scroll.Add(CreateSliderField("Z Coord (3D Slice)", zCoord, -100f, 100f, v => { zCoord = v; OnParameterChanged(); }));

            scroll.Add(CreateSeparator());

            // === General Settings ===
            scroll.Add(CreateSectionHeader("General Settings"));
            scroll.Add(CreateIntField("Seed", seed, v => { seed = v; OnParameterChanged(); }));
            scroll.Add(CreateFloatField("Frequency", frequency, v => { frequency = v; OnParameterChanged(); }));
            scroll.Add(CreateEnumField("Noise Type", noiseType, v => { noiseType = v; UpdateConditionalVisibility(); OnParameterChanged(); }));
            scroll.Add(CreateEnumField("Rotation 3D", rotationType3D, v => { rotationType3D = v; OnParameterChanged(); }));

            scroll.Add(CreateSeparator());

            // === Fractal Settings ===
            scroll.Add(CreateSectionHeader("Fractal Settings"));
            scroll.Add(CreateEnumField("Fractal Type", fractalType, v => { fractalType = v; UpdateConditionalVisibility(); OnParameterChanged(); }));

            _fractalSettings = new VisualElement();
            _fractalSettings.AddToClassList("indented-content");
            scroll.Add(_fractalSettings);

            _fractalSettings.Add(CreateSliderIntField("Octaves", fractalOctaves, 1, 20, v => { fractalOctaves = v; OnParameterChanged(); }));
            _fractalSettings.Add(CreateFloatField("Lacunarity", fractalLacunarity, v => { fractalLacunarity = v; OnParameterChanged(); }));
            _fractalSettings.Add(CreateFloatField("Gain", fractalGain, v => { fractalGain = v; OnParameterChanged(); }));
            _fractalSettings.Add(CreateSliderField("Weighted Strength", fractalWeightedStrength, 0f, 1f, v => { fractalWeightedStrength = v; OnParameterChanged(); }));
            _fractalSettings.Add(CreateFloatField("Ping Pong Strength", fractalPingPongStrength, v => { fractalPingPongStrength = v; OnParameterChanged(); }));

            scroll.Add(CreateSeparator());

            // === Cellular Settings ===
            _cellularSettings = new VisualElement();
            scroll.Add(_cellularSettings);

            _cellularSettings.Add(CreateSectionHeader("Cellular Settings"));
            var cellularContent = new VisualElement();
            cellularContent.AddToClassList("indented-content");
            _cellularSettings.Add(cellularContent);

            cellularContent.Add(CreateEnumField("Distance Func", cellularDistanceFunc, v => { cellularDistanceFunc = v; OnParameterChanged(); }));
            cellularContent.Add(CreateEnumField("Return Type", cellularReturnType, v => { cellularReturnType = v; OnParameterChanged(); }));
            cellularContent.Add(CreateSliderField("Jitter", cellularJitter, 0f, 1.5f, v => { cellularJitter = v; OnParameterChanged(); }));

            scroll.Add(CreateSeparator());

            // === Domain Warp ===
            scroll.Add(CreateSectionHeader("Domain Warp"));
            scroll.Add(CreateToggleField("Enable Domain Warp", _useDomainWarp, v => { _useDomainWarp = v; UpdateConditionalVisibility(); OnParameterChanged(); }));

            _domainWarpSettings = new VisualElement();
            _domainWarpSettings.AddToClassList("indented-content");
            scroll.Add(_domainWarpSettings);

            var warpHelpBox = new VisualElement();
            warpHelpBox.AddToClassList("ue5-help-box");
            var warpHelpText = new Label("Domain Warp applies distortion to coordinates before sampling noise.");
            warpHelpText.AddToClassList("ue5-help-box-text");
            warpHelpBox.Add(warpHelpText);
            _domainWarpSettings.Add(warpHelpBox);

            _domainWarpSettings.Add(CreateEnumField("Warp Type", domainWarpType, v => { domainWarpType = v; OnParameterChanged(); }));
            _domainWarpSettings.Add(CreateFloatField("Warp Amplitude", domainWarpAmp, v => { domainWarpAmp = v; OnParameterChanged(); }));

            scroll.Add(CreateSeparator());

            // === Buttons ===
            var refreshBtn = new Button(UpdateTexture) { text = "Force Refresh" };
            refreshBtn.AddToClassList("ue5-button");
            refreshBtn.AddToClassList("ue5-button-primary");
            scroll.Add(refreshBtn);

            _saveBtn = new Button(SaveTexture) { text = $"Save Texture ({resX}x{resY})" };
            _saveBtn.AddToClassList("ue5-button");
            _saveBtn.AddToClassList("ue5-button-success");
            scroll.Add(_saveBtn);

            UpdateConditionalVisibility();

            return panel;
        }

        private VisualElement CreatePreviewPanel()
        {
            var panel = new VisualElement();
            panel.AddToClassList("preview-panel");

            var header = new VisualElement();
            header.AddToClassList("panel-header");
            var headerText = new Label("PREVIEW OUTPUT");
            headerText.AddToClassList("panel-header-text");
            header.Add(headerText);
            panel.Add(header);

            // Container for the preview image
            var container = new VisualElement();
            container.AddToClassList("preview-container");
            container.style.flexGrow = 1;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            panel.Add(container);

            // Use Image control - ScaleToFit preserves aspect ratio without distortion
            _previewImage = new Image();
            _previewImage.AddToClassList("preview-image");
            _previewImage.scaleMode = ScaleMode.ScaleToFit;
            _previewImage.style.flexGrow = 1;
            _previewImage.style.alignSelf = Align.Stretch;
            container.Add(_previewImage);

            var infoBar = new VisualElement();
            infoBar.AddToClassList("preview-info");
            panel.Add(infoBar);

            _previewInfoLabel = new Label("Resolution: --");
            _previewInfoLabel.AddToClassList("preview-info-text");
            infoBar.Add(_previewInfoLabel);

            return panel;
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

        private VisualElement CreateFloatField(string label, float value, System.Action<float> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("field-row");
            row.Add(CreateFieldLabel(label));

            var field = new FloatField();
            field.value = value;
            field.AddToClassList("ue5-float-field");
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

        #endregion

        #region Logic

        private void OnParameterChanged()
        {
            if (_autoRefresh)
            {
                UpdateTexture();
            }
        }

        private void UpdateConditionalVisibility()
        {
            if (_fractalSettings != null)
            {
                _fractalSettings.style.display = fractalType != Noise.FastNoiseLite.FastNoiseLite.FractalType.None
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_cellularSettings != null)
            {
                _cellularSettings.style.display = noiseType == Noise.FastNoiseLite.FastNoiseLite.NoiseType.Cellular
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_domainWarpSettings != null)
            {
                _domainWarpSettings.style.display = _useDomainWarp ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void UpdateTexture()
        {
            if (_noise == null) _noise = new Noise.FastNoiseLite.FastNoiseLite();

            if (_previewTexture == null || _previewTexture.width != resX || _previewTexture.height != resY)
            {
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

            if (_previewTexture.filterMode != filterMode)
            {
                _previewTexture.filterMode = filterMode;
            }

            // Apply settings
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

            // Generate texture
            Color[] pixels = new Color[resX * resY];

            for (int y = 0; y < resY; y++)
            {
                for (int x = 0; x < resX; x++)
                {
                    float xPos, yPos;

                    if (useUVCoordinates)
                    {
                        // Use uniform sampling step based on the smaller dimension
                        // This ensures noise is not stretched/compressed regardless of aspect ratio
                        // Like cutting a rectangle from an infinite noise plane
                        float uniformScale = 100.0f / Mathf.Min(resX, resY);
                        xPos = x * uniformScale;
                        yPos = y * uniformScale;
                    }
                    else
                    {
                        xPos = x;
                        yPos = y;
                    }

                    float zPos = zCoord;

                    if (_useDomainWarp)
                    {
                        _noise.DomainWarp(ref xPos, ref yPos, ref zPos);
                    }

                    float noiseValue = _noise.GetNoise(xPos, yPos, zPos);
                    float normalized = Mathf.Clamp01((noiseValue + 1f) * 0.5f);
                    pixels[y * resX + x] = new Color(normalized, normalized, normalized, 1f);
                }
            }

            _previewTexture.SetPixels(pixels);
            _previewTexture.Apply();

            // Update UI
            if (_previewImage != null)
            {
                _previewImage.image = _previewTexture;
                _previewImage.MarkDirtyRepaint();
            }

            if (_previewInfoLabel != null)
            {
                float aspect = (float)resX / resY;
                _previewInfoLabel.text = $"Resolution: {resX}x{resY} | Ratio: {aspect:F2}";
            }

            if (_saveBtn != null)
            {
                _saveBtn.text = $"Save Texture ({resX}x{resY})";
            }
        }

        private void SaveTexture()
        {
            if (_previewTexture == null) return;

            byte[] bytes = _previewTexture.EncodeToPNG();
            string path = Application.dataPath + $"/FastNoiseOutput_{resX}x{resY}.png";
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            Debug.Log("Texture saved to: " + path);
        }

        #endregion
    }
}