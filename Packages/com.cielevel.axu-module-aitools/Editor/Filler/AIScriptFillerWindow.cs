using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace AxuFrameKit.Modules.AITools
{
    public class AIScriptFillerWindow : EditorWindow
    {
        // UXML (and its USS, referenced from the UXML) sit next to this script; resolve the
        // path relative to the script so the window works under Assets or as a package.
        private string UXML_PATH =>
            System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)))
                .Replace('\\', '/') + "/AIScriptFillerWindow.uxml";
        private const string PREF_KEY_CONFIG = "AIScriptFiller_LastConfig";

        private AIConfig _currentConfig;

        // UI Elements
        private Button _configBtn;
        private Label _configLabel;
        private ObjectField _targetObjectField;
        private ListView _scriptListView;
        private ScrollView _fieldsPreviewScrollView;
        private Button _analyzeBtn;
        private Button _fillBtn;
        private Label _statusLabel;

        // Data
        private GameObject _targetObject;
        private List<MonoBehaviour> _availableScripts = new List<MonoBehaviour>();
        private MonoBehaviour _selectedScript;
        private List<FieldMapping> _fieldMappings = new List<FieldMapping>();
        private bool _isProcessing = false;

        [Serializable]
        class FieldMapping
        {
            public FieldInfo fieldInfo;
            public string fieldName;
            public Type fieldType;
            public Component suggestedComponent;
            public bool isConfirmed = false;
        }

        [MenuItem("AxuFrameKit/Modules/AI Tools/Script Filler")]
        public static void ShowWindow()
        {
            AIScriptFillerWindow wnd = GetWindow<AIScriptFillerWindow>();
            wnd.titleContent = new GUIContent("AI Script Filler");
            wnd.minSize = new Vector2(500, 600);
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

            VisualElement labelFromUXML = visualTree.Instantiate();
            labelFromUXML.style.flexGrow = 1;
            root.Add(labelFromUXML);

            SetupInteractions(root);
            LoadSavedConfig();
        }

        private void SetupInteractions(VisualElement root)
        {
            _configBtn = root.Q<Button>("ConfigButton");
            _configLabel = root.Q<Label>("CurrentConfigLabel");
            _analyzeBtn = root.Q<Button>("AnalyzeButton");
            _fillBtn = root.Q<Button>("FillButton");
            _statusLabel = root.Q<Label>("StatusLabel");

            // Target Object Field
            var targetContainer = root.Q<VisualElement>("TargetObjectContainer");
            if (targetContainer != null)
            {
                _targetObjectField = new ObjectField("Target GameObject")
                {
                    objectType = typeof(GameObject),
                    allowSceneObjects = true
                };
                targetContainer.Add(_targetObjectField);

                _targetObjectField.RegisterValueChangedCallback(evt =>
                {
                    _targetObject = evt.newValue as GameObject;
                    OnTargetObjectChanged();
                });
            }

            // Script List
            _scriptListView = root.Q<ListView>("ScriptListView");
            if (_scriptListView != null)
            {
                _scriptListView.makeItem = MakeScriptListItem;
                _scriptListView.bindItem = BindScriptListItem;
                _scriptListView.selectionChanged += OnScriptSelectionChanged;
            }

            // Fields Preview
            _fieldsPreviewScrollView = root.Q<ScrollView>("FieldsPreviewScrollView");

            // Buttons
            if (_configBtn != null) _configBtn.clicked += OnSelectConfigClicked;
            if (_analyzeBtn != null) _analyzeBtn.clicked += OnAnalyzeClicked;
            if (_fillBtn != null) _fillBtn.clicked += OnFillClicked;

            ValidateState();
        }

        private VisualElement MakeScriptListItem()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;

            var label = new Label();
            label.name = "ScriptName";
            label.style.flexGrow = 1;
            container.Add(label);

            return container;
        }

        private void BindScriptListItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _availableScripts.Count) return;

            var script = _availableScripts[index];
            var label = element.Q<Label>("ScriptName");
            if (label != null)
            {
                label.text = script.GetType().Name;
            }
        }

        private void OnScriptSelectionChanged(IEnumerable<object> selection)
        {
            _selectedScript = selection.FirstOrDefault() as MonoBehaviour;
            UpdateFieldsPreview();
            ValidateState();
        }

        private void OnTargetObjectChanged()
        {
            _availableScripts.Clear();
            _selectedScript = null;
            _fieldMappings.Clear();

            if (_targetObject != null)
            {
                var scripts = _targetObject.GetComponents<MonoBehaviour>();
                _availableScripts.AddRange(scripts.Where(s => s != null));
            }

            if (_scriptListView != null)
            {
                _scriptListView.itemsSource = _availableScripts;
                _scriptListView.Rebuild();
            }

            UpdateFieldsPreview();
            ValidateState();
        }

        private void UpdateFieldsPreview()
        {
            if (_fieldsPreviewScrollView == null) return;

            _fieldsPreviewScrollView.Clear();

            if (_selectedScript == null)
            {
                var emptyLabel = new Label("Select a script to see its fields");
                emptyLabel.AddToClassList("preview-empty-label");
                _fieldsPreviewScrollView.Add(emptyLabel);
                return;
            }

            // Get all serialized fields
            var fields = GetSerializedFields(_selectedScript);

            if (fields.Count == 0)
            {
                var emptyLabel = new Label("No serializable fields found in this script");
                emptyLabel.AddToClassList("preview-empty-label");
                _fieldsPreviewScrollView.Add(emptyLabel);
                return;
            }

            var headerLabel = new Label($"Found {fields.Count} field(s) in {_selectedScript.GetType().Name}");
            headerLabel.AddToClassList("preview-header-label");
            _fieldsPreviewScrollView.Add(headerLabel);

            foreach (var field in fields)
            {
                CreateFieldPreviewUI(field);
            }
        }

        private List<FieldInfo> GetSerializedFields(MonoBehaviour script)
        {
            var fields = new List<FieldInfo>();
            var type = script.GetType();

            var allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in allFields)
            {
                // Check if field is serializable
                bool isSerializable = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;

                // Skip if NonSerialized
                if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
                    continue;

                // Only include Component or GameObject types
                if (isSerializable && (typeof(Component).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(GameObject)))
                {
                    fields.Add(field);
                }
            }

            return fields;
        }

        private void CreateFieldPreviewUI(FieldInfo field)
        {
            var fieldContainer = new VisualElement();
            fieldContainer.AddToClassList("field-preview-container");

            var fieldHeader = new VisualElement();
            fieldHeader.AddToClassList("field-preview-header");

            var fieldName = new Label($"{field.Name} ({field.FieldType.Name})");
            fieldName.AddToClassList("field-preview-name");
            fieldHeader.Add(fieldName);

            // Current value
            var currentValue = field.GetValue(_selectedScript) as UnityEngine.Object;
            var valueLabel = new Label(currentValue != null ? $"Current: {currentValue.name}" : "Current: null");
            valueLabel.AddToClassList("field-preview-value");
            fieldHeader.Add(valueLabel);

            fieldContainer.Add(fieldHeader);
            _fieldsPreviewScrollView.Add(fieldContainer);
        }

        private void ValidateState()
        {
            bool canAnalyze = _currentConfig != null && _selectedScript != null && !_isProcessing;
            bool canFill = canAnalyze && _fieldMappings.Count > 0;

            if (_analyzeBtn != null) _analyzeBtn.SetEnabled(canAnalyze);
            if (_fillBtn != null) _fillBtn.SetEnabled(canFill);
        }

        private void OnSelectConfigClicked()
        {
            GenericMenu menu = new GenericMenu();
            string[] guids = AssetDatabase.FindAssets("t:AIConfig");

            if (guids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No AI Configs found"));
            }
            else
            {
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var config = AssetDatabase.LoadAssetAtPath<AIConfig>(path);
                    if (config != null)
                    {
                        menu.AddItem(new GUIContent(config.name), config == _currentConfig, () => SelectConfig(config));
                    }
                }
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

            if (config != null)
            {
                string configPath = AssetDatabase.GetAssetPath(config);
                EditorPrefs.SetString(PREF_KEY_CONFIG, configPath);
            }

            ValidateState();
        }

        private void LoadSavedConfig()
        {
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

            // Fallback
            string[] guids = AssetDatabase.FindAssets("t:AIConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var config = AssetDatabase.LoadAssetAtPath<AIConfig>(path);
                if (config != null)
                {
                    SelectConfig(config);
                }
            }
        }

        private async void OnAnalyzeClicked()
        {
            if (_currentConfig == null || _selectedScript == null) return;

            _isProcessing = true;
            ValidateState();
            ShowStatus("Analyzing script and children...", false);

            try
            {
                // Build prompt
                string prompt = BuildAnalysisPrompt();

                // Call AI
                string response = await CallAIAPI(prompt);

                if (string.IsNullOrEmpty(response))
                {
                    ShowStatus("Error: AI returned empty response", true);
                    return;
                }

                // Parse response
                ParseAIResponse(response);

                ShowStatus($"✓ Analysis complete! Found {_fieldMappings.Count} field mapping(s)", false);
            }
            catch (Exception e)
            {
                ShowStatus($"✗ Error: {e.Message}", true);
                Debug.LogError($"AI Script Filler Error: {e}");
            }
            finally
            {
                _isProcessing = false;
                ValidateState();
            }
        }

        private string BuildAnalysisPrompt()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"I have a Unity GameObject with a MonoBehaviour script '{_selectedScript.GetType().Name}' attached.");
            sb.AppendLine("I need you to analyze the script fields and suggest which child GameObjects/Components should be assigned to each field.");
            sb.AppendLine();

            // Script fields
            var fields = GetSerializedFields(_selectedScript);
            sb.AppendLine("Script Fields:");
            foreach (var field in fields)
            {
                sb.AppendLine($"- {field.Name} (Type: {field.FieldType.Name})");
            }
            sb.AppendLine();

            // Child objects
            sb.AppendLine("Available Child Objects:");
            foreach (Transform child in _targetObject.transform)
            {
                var components = child.GetComponents<Component>();
                var componentNames = components.Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name);
                sb.AppendLine($"- GameObject: {child.name}");
                sb.AppendLine($"  Components: {string.Join(", ", componentNames)}");
            }
            sb.AppendLine();

            sb.AppendLine("IMPORTANT RESPONSE FORMAT:");
            sb.AppendLine("Return ONLY a JSON array in this exact format:");
            sb.AppendLine("[");
            sb.AppendLine("  {\"fieldName\": \"fieldName\", \"childPath\": \"ChildName\", \"componentType\": \"ComponentType\"},");
            sb.AppendLine("  ...");
            sb.AppendLine("]");
            sb.AppendLine();
            sb.AppendLine("Rules:");
            sb.AppendLine("1. Match field names with child object names intelligently (consider naming conventions, Chinese translation if needed)");
            sb.AppendLine("2. If field type is a specific Component, use that component type");
            sb.AppendLine("3. If field type is GameObject, use null for componentType");
            sb.AppendLine("4. childPath should be the direct child name (not full path)");
            sb.AppendLine("5. Only include fields that have a confident match");
            sb.AppendLine();
            sb.AppendLine("Generate the JSON array now:");

            return sb.ToString();
        }

        private async Task<string> CallAIAPI(string prompt)
        {
            var requestData = new ChatRequest
            {
                model = _currentConfig.GetModelName(),
                messages = new List<Message>
                {
                    new Message { role = "system", content = _currentConfig.systemPrompt },
                    new Message { role = "user", content = prompt }
                },
                temperature = 0.3f,
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

        private void ParseAIResponse(string response)
        {
            _fieldMappings.Clear();

            // Clean response
            response = response.Trim();

            // Remove markdown code blocks
            if (response.StartsWith("```"))
            {
                int firstNewLine = response.IndexOf('\n');
                if (firstNewLine > 0) response = response.Substring(firstNewLine + 1).TrimStart();
            }
            if (response.EndsWith("```"))
            {
                int lastBacktick = response.LastIndexOf("```");
                if (lastBacktick > 0) response = response.Substring(0, lastBacktick).TrimEnd();
            }

            // Find JSON array
            int jsonStart = response.IndexOf('[');
            int jsonEnd = response.LastIndexOf(']');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                response = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            // Parse JSON manually (simple approach)
            var matches = Regex.Matches(response, @"\{[^}]+\}");
            var fields = GetSerializedFields(_selectedScript);

            foreach (Match match in matches)
            {
                string json = match.Value;

                // Extract values
                var fieldNameMatch = Regex.Match(json, @"""fieldName""\s*:\s*""([^""]+)""");
                var childPathMatch = Regex.Match(json, @"""childPath""\s*:\s*""([^""]+)""");
                var componentTypeMatch = Regex.Match(json, @"""componentType""\s*:\s*""([^""]+)""");

                if (fieldNameMatch.Success && childPathMatch.Success)
                {
                    string fieldName = fieldNameMatch.Groups[1].Value;
                    string childPath = childPathMatch.Groups[1].Value;
                    string componentType = componentTypeMatch.Success ? componentTypeMatch.Groups[1].Value : null;

                    // Find matching field
                    var field = fields.FirstOrDefault(f => f.Name == fieldName);
                    if (field != null)
                    {
                        // Find child object
                        Transform childTransform = _targetObject.transform.Find(childPath);
                        if (childTransform != null)
                        {
                            Component suggestedComponent = null;

                            if (field.FieldType == typeof(GameObject))
                            {
                                // Will assign GameObject directly
                            }
                            else if (!string.IsNullOrEmpty(componentType) && componentType != "null")
                            {
                                suggestedComponent = childTransform.GetComponent(componentType);
                            }
                            else
                            {
                                // Try to get component of the field type
                                suggestedComponent = childTransform.GetComponent(field.FieldType);
                            }

                            // For GameObject fields, store the Transform as Component (we'll convert when filling)
                            Component componentToStore = suggestedComponent;
                            if (field.FieldType == typeof(GameObject) && suggestedComponent == null)
                            {
                                componentToStore = childTransform; // Store Transform, will convert to GameObject when filling
                            }

                            _fieldMappings.Add(new FieldMapping
                            {
                                fieldInfo = field,
                                fieldName = fieldName,
                                fieldType = field.FieldType,
                                suggestedComponent = componentToStore
                            });
                        }
                    }
                }
            }

            UpdateFieldMappingsPreview();
        }

        private void UpdateFieldMappingsPreview()
        {
            if (_fieldsPreviewScrollView == null) return;

            _fieldsPreviewScrollView.Clear();

            if (_fieldMappings.Count == 0)
            {
                var emptyLabel = new Label("No field mappings found. Click 'Analyze with AI' to generate suggestions.");
                emptyLabel.AddToClassList("preview-empty-label");
                _fieldsPreviewScrollView.Add(emptyLabel);
                return;
            }

            var headerLabel = new Label($"AI Suggested {_fieldMappings.Count} field mapping(s):");
            headerLabel.AddToClassList("preview-header-label");
            _fieldsPreviewScrollView.Add(headerLabel);

            foreach (var mapping in _fieldMappings)
            {
                CreateFieldMappingUI(mapping);
            }
        }

        private void CreateFieldMappingUI(FieldMapping mapping)
        {
            var container = new VisualElement();
            container.AddToClassList("field-mapping-container");

            var header = new VisualElement();
            header.AddToClassList("field-mapping-header");

            var fieldLabel = new Label($"Field: {mapping.fieldName} ({mapping.fieldType.Name})");
            fieldLabel.AddToClassList("field-mapping-field");
            header.Add(fieldLabel);

            container.Add(header);

            var suggestionLabel = new Label();
            suggestionLabel.AddToClassList("field-mapping-suggestion");

            if (mapping.suggestedComponent != null)
            {
                suggestionLabel.text = $"→ {mapping.suggestedComponent.name} ({mapping.suggestedComponent.GetType().Name})";
            }
            else
            {
                suggestionLabel.text = "→ No match found";
                suggestionLabel.style.color = new StyleColor(Color.red);
            }

            container.Add(suggestionLabel);

            _fieldsPreviewScrollView.Add(container);
        }

        private void OnFillClicked()
        {
            if (_selectedScript == null || _fieldMappings.Count == 0) return;

            Undo.RecordObject(_selectedScript, "AI Fill Script Fields");

            int successCount = 0;
            foreach (var mapping in _fieldMappings)
            {
                if (mapping.suggestedComponent != null)
                {
                    try
                    {
                        if (mapping.fieldType == typeof(GameObject))
                        {
                            mapping.fieldInfo.SetValue(_selectedScript, mapping.suggestedComponent.gameObject);
                        }
                        else
                        {
                            mapping.fieldInfo.SetValue(_selectedScript, mapping.suggestedComponent);
                        }
                        successCount++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to set field {mapping.fieldName}: {e.Message}");
                    }
                }
            }

            EditorUtility.SetDirty(_selectedScript);
            ShowStatus($"✓ Successfully filled {successCount}/{_fieldMappings.Count} field(s)!", false);

            UpdateFieldsPreview();
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

        // Data Structures for API Communication
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
        class ChatResponse
        {
            public List<Choice> choices;
        }

        [Serializable]
        class Choice
        {
            public Message message;
        }
    }
}
