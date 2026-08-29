using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace AxuFrameKit.Modules.AITools
{
    public class AIScriptGeneratorWindow : EditorWindow
    {
        // UXML (and its USS, referenced from the UXML) sit next to this script; resolve the
        // path relative to the script so the window works under Assets or as a package.
        private string UXML_PATH =>
            System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)))
                .Replace('\\', '/') + "/AIScriptGeneratorWindow.uxml";
        private const string PREF_KEY_CONFIG = "AIScriptGenerator_LastConfig";
        private const string PREF_KEY_OUTPUT_PATH = "AIScriptGenerator_OutputPath";
        private const string DEFAULT_OUTPUT_PATH = "Assets/Scripts/Generated";
        private const string PREF_KEY_NAMESPACE = "AIScriptGenerator_Namespace";

        private AIConfig _currentConfig;
        private string _outputPath = DEFAULT_OUTPUT_PATH;
        private string _namespace = "";

        // UI Elements
        private Button _configBtn;
        private Label _configLabel;
        private ObjectField _parentField;
        private TextField _scriptNameField;
        private TextField _namespaceField;
        private EnumField _fieldStyleField;
        private EnumField _namingConventionField;
        private TextField _outputPathField;
        private Button _browsePathBtn;
        private Button _generateBtn;
        private Label _statusLabel;

        // Data
        private GameObject _selectedParent;
        private string _scriptName = "UIController";
        private FieldStyle _fieldStyle = FieldStyle.SerializeFieldPrivate;
        private NamingConvention _namingConvention = NamingConvention.CamelCase;
        private bool _isGenerating = false;

        public enum FieldStyle
        {
            PublicField,              // public Type name
            SerializeFieldPrivate     // [SerializeField] private Type _name
        }

        public enum NamingConvention
        {
            CamelCase,    // myButton
            PascalCase    // MyButton
        }

        [MenuItem("AxuFrameKit/Modules/AI Tools/Script Generator")]
        public static void ShowWindow()
        {
            AIScriptGeneratorWindow wnd = GetWindow<AIScriptGeneratorWindow>();
            wnd.titleContent = new GUIContent("AI Script Generator");
            wnd.minSize = new Vector2(400, 600);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            // Load UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);
            if (visualTree == null)
            {
                root.Add(new Label($"Error: Could not load UXML at {UXML_PATH}"));
                return;
            }

            // Use Instantiate() as in AIDialogueWindow
            VisualElement labelFromUXML = visualTree.Instantiate();
            labelFromUXML.style.flexGrow = 1;
            root.Add(labelFromUXML);

            // Setup interactions
            SetupInteractions(root);

            // Load saved preferences
            LoadSavedPreferences();
        }

        private void SetupInteractions(VisualElement root)
        {
            _configBtn = root.Q<Button>("ConfigButton");
            _configLabel = root.Q<Label>("CurrentConfigLabel");
            _scriptNameField = root.Q<TextField>("ScriptNameField");
            _generateBtn = root.Q<Button>("GenerateButton");
            _statusLabel = root.Q<Label>("StatusLabel");

            // Manually create complex fields to avoid UXML serialization issues
            var parentContainer = root.Q<VisualElement>("ParentFieldContainer");
            if (parentContainer != null)
            {
                _parentField = new ObjectField("Parent GameObject")
                {
                    objectType = typeof(GameObject),
                    allowSceneObjects = true
                };
                parentContainer.Add(_parentField);

                _parentField.RegisterValueChangedCallback(evt =>
                {
                    _selectedParent = evt.newValue as GameObject;
                    UpdatePreview();
                    ValidateState();
                });
            }

            var fieldStyleContainer = root.Q<VisualElement>("FieldStyleContainer");
            if (fieldStyleContainer != null)
            {
                _fieldStyleField = new EnumField("Field Style", _fieldStyle);
                fieldStyleContainer.Add(_fieldStyleField);

                _fieldStyleField.RegisterValueChangedCallback(evt => _fieldStyle = (FieldStyle)evt.newValue);
            }

            var namingContainer = root.Q<VisualElement>("NamingConventionContainer");
            if (namingContainer != null)
            {
                _namingConventionField = new EnumField("Naming Convention", _namingConvention);
                namingContainer.Add(_namingConventionField);

                _namingConventionField.RegisterValueChangedCallback(evt => _namingConvention = (NamingConvention)evt.newValue);
            }

            // Config Selection
            if (_configBtn != null) _configBtn.clicked += OnSelectConfigClicked;

            if (_scriptNameField != null)
            {
                _scriptNameField.RegisterValueChangedCallback(evt =>
                {
                    _scriptName = evt.newValue;
                    ValidateState();
                });

                // Add Namespace field after Script Name
                _namespaceField = new TextField("Namespace");
                _namespaceField.value = _namespace;
                _namespaceField.RegisterValueChangedCallback(evt =>
                {
                    _namespace = evt.newValue;
                    EditorPrefs.SetString(PREF_KEY_NAMESPACE, _namespace);
                });

                if (_scriptNameField.parent != null)
                {
                    int index = _scriptNameField.parent.IndexOf(_scriptNameField);
                    _scriptNameField.parent.Insert(index + 1, _namespaceField);
                }
            }

            // Output Path
            _outputPathField = root.Q<TextField>("OutputPathField");
            _browsePathBtn = root.Q<Button>("BrowsePathButton");

            if (_outputPathField != null)
            {
                _outputPathField.value = _outputPath;
                _outputPathField.RegisterValueChangedCallback(evt =>
                {
                    _outputPath = evt.newValue;
                    EditorPrefs.SetString(PREF_KEY_OUTPUT_PATH, _outputPath);
                });
            }

            if (_browsePathBtn != null)
            {
                _browsePathBtn.clicked += OnBrowsePathClicked;
            }

            // Generate
            if (_generateBtn != null)
            {
                _generateBtn.clicked += OnGenerateClicked;
            }

            ValidateState();
        }

        private void OnBrowsePathClicked()
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Convert absolute path to relative path
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }

                _outputPath = selectedPath;
                if (_outputPathField != null)
                {
                    _outputPathField.value = _outputPath;
                }
                EditorPrefs.SetString(PREF_KEY_OUTPUT_PATH, _outputPath);
            }
        }

        private void LoadSavedPreferences()
        {
            // Load saved namespace
            if (EditorPrefs.HasKey(PREF_KEY_NAMESPACE))
            {
                _namespace = EditorPrefs.GetString(PREF_KEY_NAMESPACE);
                if (_namespaceField != null)
                {
                    _namespaceField.value = _namespace;
                }
            }
            // Load saved output path
            if (EditorPrefs.HasKey(PREF_KEY_OUTPUT_PATH))
            {
                _outputPath = EditorPrefs.GetString(PREF_KEY_OUTPUT_PATH, DEFAULT_OUTPUT_PATH);
                if (_outputPathField != null)
                {
                    _outputPathField.value = _outputPath;
                }
            }

            // Load saved config
            if (EditorPrefs.HasKey(PREF_KEY_CONFIG))
            {
                string configPath = EditorPrefs.GetString(PREF_KEY_CONFIG);
                if (!string.IsNullOrEmpty(configPath))
                {
                    var config = AssetDatabase.LoadAssetAtPath<AIConfig>(configPath);
                    if (config != null)
                    {
                        SelectConfig(config);
                        return;
                    }
                }
            }

            // Fallback to default config
            LoadDefaultConfig();
        }

        private void ValidateState()
        {
            if (_generateBtn == null) return;

            bool isValid = _selectedParent != null && !string.IsNullOrEmpty(_scriptName) && !_isGenerating && _currentConfig != null;
            _generateBtn.SetEnabled(isValid);

            if (_isGenerating)
            {
                _generateBtn.text = "Generating...";
            }
            else
            {
                _generateBtn.text = "Generate Script";
            }
        }

        private ScrollView _previewScrollView;
        private List<ChildObjectInfo> _currentChildrenInfo;

        private void UpdatePreview()
        {
            if (_previewScrollView == null)
            {
                _previewScrollView = rootVisualElement.Q<ScrollView>("PreviewScrollView");
            }

            if (_previewScrollView == null) return;

            _previewScrollView.Clear();

            if (_selectedParent == null)
            {
                var emptyLabel = new Label("Select a Parent GameObject to see children...");
                emptyLabel.AddToClassList("preview-empty-label");
                _previewScrollView.Add(emptyLabel);
                _currentChildrenInfo = null;
                return;
            }

            _currentChildrenInfo = GetChildrenInfo(_selectedParent);

            if (_currentChildrenInfo.Count == 0)
            {
                var emptyLabel = new Label("No child objects found.");
                emptyLabel.AddToClassList("preview-empty-label");
                _previewScrollView.Add(emptyLabel);
                return;
            }

            // 添加标题
            var headerContainer = new VisualElement();
            headerContainer.AddToClassList("preview-header");

            var headerLabel = new Label($"Found {_currentChildrenInfo.Count} child object(s)");
            headerLabel.AddToClassList("preview-header-label");
            headerContainer.Add(headerLabel);

            _previewScrollView.Add(headerContainer);

            // 为每个子物体创建UI
            foreach (var childInfo in _currentChildrenInfo)
            {
                CreateChildObjectUI(childInfo);
            }
        }

        private void CreateChildObjectUI(ChildObjectInfo childInfo)
        {
            // 子物体容器
            var childContainer = new VisualElement();
            childContainer.AddToClassList("child-object-container");

            // 子物体头部（名称 + 展开/折叠按钮）
            var childHeader = new VisualElement();
            childHeader.AddToClassList("child-object-header");

            // 展开/折叠按钮
            var expandBtn = new Button();
            expandBtn.text = childInfo.isExpanded ? "▼" : "▶";
            expandBtn.AddToClassList("expand-button");

            // 子物体名称
            var nameLabel = new Label(childInfo.name);
            nameLabel.AddToClassList("child-object-name");

            // 组件数量标签
            int selectedCount = childInfo.components.Count(c => c.isSelected);
            var componentCountLabel = new Label($"({selectedCount}/{childInfo.components.Count} selected)");
            componentCountLabel.AddToClassList("component-count-label");

            childHeader.Add(expandBtn);
            childHeader.Add(nameLabel);
            childHeader.Add(componentCountLabel);

            // 组件列表容器
            var componentsContainer = new VisualElement();
            componentsContainer.AddToClassList("components-container");
            componentsContainer.style.display = childInfo.isExpanded ? DisplayStyle.Flex : DisplayStyle.None;

            if (childInfo.components.Count == 0)
            {
                var noComponentLabel = new Label("No components (will use GameObject)");
                noComponentLabel.AddToClassList("no-component-label");
                componentsContainer.Add(noComponentLabel);
            }
            else
            {
                foreach (var componentInfo in childInfo.components)
                {
                    var componentBtn = CreateComponentToggleButton(componentInfo, () =>
                    {
                        // 更新计数标签
                        int count = childInfo.components.Count(c => c.isSelected);
                        componentCountLabel.text = $"({count}/{childInfo.components.Count} selected)";
                        ValidateState();
                    });
                    componentsContainer.Add(componentBtn);
                }
            }

            // 展开/折叠逻辑
            expandBtn.clicked += () =>
            {
                childInfo.isExpanded = !childInfo.isExpanded;
                expandBtn.text = childInfo.isExpanded ? "▼" : "▶";
                componentsContainer.style.display = childInfo.isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            };

            childContainer.Add(childHeader);
            childContainer.Add(componentsContainer);
            _previewScrollView.Add(childContainer);
        }

        private Button CreateComponentToggleButton(ComponentInfo componentInfo, Action onToggled)
        {
            Button btn = null;
            btn = new Button(() =>
            {
                componentInfo.isSelected = !componentInfo.isSelected;
                UpdateComponentButtonState(btn, componentInfo.isSelected);
                onToggled?.Invoke();
            })
            { text = componentInfo.typeName };

            btn.AddToClassList("component-toggle-btn");
            UpdateComponentButtonState(btn, componentInfo.isSelected);

            return btn;
        }

        private void UpdateComponentButtonState(Button button, bool isSelected)
        {
            if (isSelected)
            {
                button.RemoveFromClassList("component-toggle-btn--inactive");
                button.AddToClassList("component-toggle-btn--active");
            }
            else
            {
                button.RemoveFromClassList("component-toggle-btn--active");
                button.AddToClassList("component-toggle-btn--inactive");
            }
        }

        private void LoadDefaultConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:AIConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                SelectConfig(AssetDatabase.LoadAssetAtPath<AIConfig>(path));
            }
        }

        private void OnSelectConfigClicked()
        {
            var menu = new GenericMenu();
            string[] guids = AssetDatabase.FindAssets("t:AIConfig");

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AIConfig config = AssetDatabase.LoadAssetAtPath<AIConfig>(path);
                if (config != null)
                {
                    menu.AddItem(new GUIContent(config.name), _currentConfig == config, () => SelectConfig(config));
                }
            }

            if (guids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No AIConfig found. Create one first."));
            }

            menu.ShowAsContext();
        }

        private void SelectConfig(AIConfig config)
        {
            _currentConfig = config;
            if (_configLabel != null)
            {
                _configLabel.text = config != null ? $"Config: {config.name} ({config.GetModelName()})" : "No Config Selected";
            }

            // Save config selection
            if (config != null)
            {
                string configPath = AssetDatabase.GetAssetPath(config);
                EditorPrefs.SetString(PREF_KEY_CONFIG, configPath);
            }

            ValidateState();
        }

        private async void OnGenerateClicked()
        {
            if (_currentConfig == null || _selectedParent == null) return;

            _isGenerating = true;
            ShowStatus("Collecting child objects info...", false);
            ValidateState();

            try
            {
                // 1. Use current children info (preserves user's component selection)
                if (_currentChildrenInfo == null || _currentChildrenInfo.Count == 0)
                {
                    ShowStatus("Error: No child objects found. Please select a parent GameObject.", true);
                    return;
                }

                // 2. Build Prompt
                string prompt = BuildPrompt(_currentChildrenInfo);

                ShowStatus("Calling AI API...", false);

                // 3. Call API
                string generatedCode = await CallAIAPI(prompt);

                if (string.IsNullOrEmpty(generatedCode))
                {
                    ShowStatus("Error: AI returned empty response", true);
                    return;
                }

                // 4. Clean Code
                generatedCode = CleanGeneratedCode(generatedCode);

                ShowStatus("Creating script file...", false);

                // 5. Create File
                bool success = CreateScriptFile(generatedCode);

                if (success)
                {
                    ShowStatus($"✓ Script '{_scriptName}.cs' created successfully!", false);
                }
                else
                {
                    ShowStatus("✗ Failed to create script file", true);
                }
            }
            catch (Exception e)
            {
                ShowStatus($"✗ Error: {e.Message}", true);
                Debug.LogError($"AI Script Generator Error: {e}");
            }
            finally
            {
                _isGenerating = false;
                ValidateState();
            }
        }

        private void ShowStatus(string message, bool isError)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = message;
                _statusLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
                _statusLabel.style.color = isError ? new StyleColor(Color.red) : new StyleColor(new Color(0.9f, 0.9f, 0.9f));
            }
        }

        // Logic from original file, adapted
        [Serializable]
        class ChildObjectInfo
        {
            public string name;
            public string translatedName; // AI翻译后的英文名称
            public List<ComponentInfo> components;
            public bool isExpanded = true; // 是否展开显示组件
        }

        [Serializable]
        class ComponentInfo
        {
            public string typeName;
            public bool isSelected = true; // 默认选中
        }

        List<ChildObjectInfo> GetChildrenInfo(GameObject parent)
        {
            var childrenInfo = new List<ChildObjectInfo>();
            foreach (Transform child in parent.transform)
            {
                var info = new ChildObjectInfo
                {
                    name = child.name,
                    translatedName = child.name, // 初始值，后续可能被AI翻译
                    components = new List<ComponentInfo>()
                };

                var components = child.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp != null && !(comp is Transform))
                    {
                        info.components.Add(new ComponentInfo
                        {
                            typeName = comp.GetType().Name,
                            isSelected = true
                        });
                    }
                }
                childrenInfo.Add(info);
            }
            return childrenInfo;
        }

        string BuildPrompt(List<ChildObjectInfo> childrenInfo)
        {
            // Debug: Log component selection status
            Debug.Log($"[AI Generator] Building prompt for {childrenInfo.Count} child objects:");
            foreach (var child in childrenInfo)
            {
                int selectedCount = child.components.Count(c => c.isSelected);
                Debug.Log($"  - {child.name}: {selectedCount}/{child.components.Count} components selected");
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Create a Unity C# script named '{_scriptName}' that holds references to UI child objects.");
            sb.AppendLine("Requirements:");

            if (_fieldStyle == FieldStyle.PublicField)
            {
                sb.AppendLine("- Use public fields");
                sb.AppendLine($"- Field naming: {(_namingConvention == NamingConvention.CamelCase ? "camelCase (first letter lowercase)" : "PascalCase (first letter uppercase)")}");
            }
            else
            {
                sb.AppendLine("- Use [SerializeField] private fields");
                sb.AppendLine("- Field names should start with underscore (_fieldName)");
            }

            if (!string.IsNullOrEmpty(_namespace))
            {
                sb.AppendLine($"- Wrap the class in namespace '{_namespace}'");
            }
            else
            {
                sb.AppendLine("- Do not use any namespace (global namespace)");
            }

            sb.AppendLine("- Inherit from MonoBehaviour");
            sb.AppendLine("- Only include field declarations, no methods");
            sb.AppendLine("- Add appropriate using statements. CRITICAL: You MUST include the namespace for any component used (e.g., 'using UnityEngine.UI;' for Image/Text, 'using TMPro;' for TextMeshPro).");
            sb.AppendLine("- If a child object name contains Chinese characters, translate it to a suitable English field name");
            sb.AppendLine();
            sb.AppendLine("Child objects information:");

            foreach (var child in childrenInfo)
            {
                // 获取选中的组件
                var selectedComponents = child.components.Where(c => c.isSelected).ToList();

                // 如果没有选中任何组件，跳过这个子物体
                if (selectedComponents.Count == 0)
                {
                    continue;
                }

                sb.AppendLine($"- GameObject Name: {child.name}");

                // 检查是否包含中文
                if (ContainsChinese(child.name))
                {
                    sb.AppendLine($"  (Contains Chinese - please translate to appropriate English field name)");
                }

                // 列出选中的组件
                sb.AppendLine($"  Components to generate fields for (ONLY these, one field per component):");
                foreach (var comp in selectedComponents)
                {
                    sb.AppendLine($"    • {comp.typeName} - Generate ONE field for this component");
                }

                // 建议主要组件类型
                if (selectedComponents.Count > 0)
                {
                    string primaryComponent = GetPrimaryComponent(selectedComponents.Select(c => c.typeName).ToList());
                    sb.AppendLine($"  Primary Component Type: {primaryComponent}");
                }

                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("CRITICAL FIELD GENERATION RULES:");
            sb.AppendLine("1. Generate EXACTLY ONE field for EACH component listed above");
            sb.AppendLine("2. Do NOT generate fields for components that are NOT listed");
            sb.AppendLine("3. If a child has multiple selected components, generate multiple fields (one per component)");
            sb.AppendLine("4. Field names should be: childName + componentType (e.g., 'startButtonButton', 'logoImage')");
            sb.AppendLine();
            sb.AppendLine("IMPORTANT CODE GENERATION REQUIREMENTS:");
            sb.AppendLine("1. Generate ONLY valid, complete C# code that can be directly compiled");
            sb.AppendLine("2. The code MUST be syntactically correct with proper formatting");
            sb.AppendLine("3. Include all necessary using statements at the top");
            sb.AppendLine("4. Ensure all braces { } are properly matched and closed");
            sb.AppendLine("5. Do NOT include any markdown code blocks (no ``` or ```csharp)");
            sb.AppendLine("6. Do NOT include any explanations, comments about the generation, or additional text");
            sb.AppendLine("7. The output should start with 'using' statements and end with the closing brace of the class");
            sb.AppendLine("8. Every field must have proper type declaration and semicolon");
            sb.AppendLine();
            sb.AppendLine("Generate the complete, ready-to-use C# script now:");

            return sb.ToString();
        }

        bool ContainsChinese(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            foreach (char c in text)
            {
                if (c >= 0x4E00 && c <= 0x9FFF) // CJK Unified Ideographs
                {
                    return true;
                }
            }
            return false;
        }

        string GetPrimaryComponent(List<string> componentNames)
        {
            string[] uiComponents = { "Button", "Text", "Image", "Slider", "Toggle", "InputField", "TMP_Text", "TextMeshProUGUI" };
            foreach (var uiComp in uiComponents)
            {
                if (componentNames.Contains(uiComp)) return uiComp;
            }
            if (componentNames.Count > 0) return componentNames[0];
            return "GameObject";
        }

        async Task<string> CallAIAPI(string prompt)
        {
            var requestData = new ChatRequest
            {
                model = _currentConfig.GetModelName(),
                messages = new List<Message>
                {
                    new Message { role = "system", content = "You are a Unity C# code generator. Generate clean, production-ready code." },
                    new Message { role = "user", content = prompt }
                },
                temperature = 0.3f, // Lower temperature for code generation
                max_tokens = 2000
            };

            string jsonData = JsonUtility.ToJson(requestData);

            using UnityWebRequest request = new(_currentConfig.apiUrl, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", _currentConfig.AuthToken);

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Delay(100);
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                var response = JsonUtility.FromJson<ChatResponse>(responseText);
                if (response.choices != null && response.choices.Count > 0)
                {
                    return response.choices[0].message.content;
                }
            }
            else
            {
                Debug.LogError($"API Error: {request.error}\n{request.downloadHandler.text}");
                ShowStatus($"API Error: {request.error}", true);
            }
            return null;
        }

        string CleanGeneratedCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return code;

            code = code.Trim();

            // 移除markdown代码块标记
            // 处理 ```csharp 或 ```c# 或 ``` 开头
            if (code.StartsWith("```"))
            {
                int firstNewLine = code.IndexOf('\n');
                if (firstNewLine > 0)
                {
                    code = code.Substring(firstNewLine + 1).TrimStart();
                }
            }

            // 移除末尾的 ```
            if (code.EndsWith("```"))
            {
                int lastBacktick = code.LastIndexOf("```");
                if (lastBacktick > 0)
                {
                    code = code.Substring(0, lastBacktick).TrimEnd();
                }
            }

            // 移除可能的额外说明文本（在代码之前）
            // 如果代码不是以 using 或 namespace 开头，尝试找到真正的代码开始位置
            if (!code.StartsWith("using") && !code.StartsWith("namespace"))
            {
                // 查找第一个 using 语句
                int usingIndex = code.IndexOf("using ");
                if (usingIndex > 0)
                {
                    code = code.Substring(usingIndex);
                }
                else
                {
                    // 查找第一个 namespace 语句
                    int namespaceIndex = code.IndexOf("namespace ");
                    if (namespaceIndex > 0)
                    {
                        code = code.Substring(namespaceIndex);
                    }
                }
            }

            // 移除可能的额外说明文本（在代码之后）
            // 检查最后一个 } 之后是否还有文本
            int lastBrace = code.LastIndexOf('}');
            if (lastBrace > 0 && lastBrace < code.Length - 1)
            {
                string afterBrace = code.Substring(lastBrace + 1).Trim();
                // 如果后面只有空白或简单的说明文字（不包含代码），则截断
                if (afterBrace.Length > 0 && !afterBrace.Contains("{") && !afterBrace.Contains("using"))
                {
                    code = code.Substring(0, lastBrace + 1);
                }
            }

            return code.Trim();
        }

        bool CreateScriptFile(string code)
        {
            try
            {
                string folderPath = string.IsNullOrEmpty(_outputPath) ? DEFAULT_OUTPUT_PATH : _outputPath;

                // Ensure the directory exists
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = $"{folderPath}/{_scriptName}.cs";
                File.WriteAllText(filePath, code);

                AssetDatabase.Refresh();

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath);
                if (script != null)
                {
                    EditorGUIUtility.PingObject(script);
                    Selection.activeObject = script;
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create script file: {e.Message}");
                return false;
            }
        }

        // Data Structures (Matching ChatRequest from AIDialogueWindow roughly, essentially standard OpenAI format)
        [Serializable]
        private class ChatRequest
        {
            public string model;
            public List<Message> messages;
            public float temperature;
            public int max_tokens;
        }

        [Serializable]
        private class Message
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class ChatResponse
        {
            public List<Choice> choices;
        }

        [Serializable]
        private class Choice
        {
            public Message message;
        }
    }
}