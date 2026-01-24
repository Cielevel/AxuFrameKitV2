using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace AxuFrameKit.Modules.AITools
{
    public class AIDialogueWindow : EditorWindow
    {
        private const string UXML_PATH = "Assets/AxuFrameKit/Modules/AITools/Chat/AIDialogueWindow.uxml";

        private AIChatConfig _currentConfig;
        private Label _configLabel;
        private ScrollView _chatScrollView;
        private TextField _inputField;
        private Button _sendBtn;
        private Button _configBtn;

        private List<Message> _messageHistory = new List<Message>();

        [MenuItem("AxuFrameKit/AI Tools/Chat Window")]
        public static void ShowWindow()
        {
            AIDialogueWindow wnd = GetWindow<AIDialogueWindow>();
            wnd.titleContent = new GUIContent("AI Assistant");
            wnd.minSize = new Vector2(400, 500);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

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
        }

        private void SetupInteractions(VisualElement root)
        {
            _chatScrollView = root.Q<ScrollView>("ChatHistory");
            _inputField = root.Q<TextField>("InputField");
            _sendBtn = root.Q<Button>("SendButton");
            _configBtn = root.Q<Button>("ConfigButton");
            _configLabel = root.Q<Label>("CurrentConfigLabel");

            // Config Selection
            if (_configBtn != null)
            {
                _configBtn.clicked += OnSelectConfigClicked;
            }

            // Send Message
            if (_sendBtn != null && _inputField != null && _chatScrollView != null)
            {
                _sendBtn.clicked += OnSendClicked;
            }

            // Clear history initially or keep it? For now, clear UI but maybe we want persistence later.
            // _chatScrollView.Clear(); // Keep the UXML example ones for now or clear? Better clear for real use.
            _chatScrollView.Clear();
            _messageHistory.Clear();

            // Try load last used config or find one
            LoadDefaultConfig();
        }

        private void LoadDefaultConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:AIChatConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                SelectConfig(AssetDatabase.LoadAssetAtPath<AIChatConfig>(path));
            }
        }

        private void OnSelectConfigClicked()
        {
            var menu = new GenericMenu();
            string[] guids = AssetDatabase.FindAssets("t:AIChatConfig");

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AIChatConfig config = AssetDatabase.LoadAssetAtPath<AIChatConfig>(path);
                if (config != null)
                {
                    menu.AddItem(new GUIContent(config.name), _currentConfig == config, () => SelectConfig(config));
                }
            }

            if (guids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No AIChatConfig found. Create one first."));
            }

            menu.ShowAsContext();
        }

        private void SelectConfig(AIChatConfig config)
        {
            _currentConfig = config;
            if (_configLabel != null)
            {
                _configLabel.text = config != null ? $"Config: {config.name} ({config.GetModelName()})" : "No Config Selected";
            }

            // Reset history when changing config to ensure system prompt is fresh? 
            // Or keep history. Let's keep history for now, but usually system prompt is at the start.
        }

        private void OnSendClicked()
        {
            if (_currentConfig == null)
            {
                AddAIMessage(_chatScrollView, "Please select an AI Config first.");
                return;
            }

            string userMsg = _inputField.value;
            if (string.IsNullOrWhiteSpace(userMsg)) return;

            // 1. Show User Message
            AddUserMessage(_chatScrollView, userMsg);
            _inputField.value = "";

            // 2. Prepare Data
            if (_messageHistory.Count == 0 && !string.IsNullOrEmpty(_currentConfig.systemPrompt))
            {
                _messageHistory.Add(new Message { role = "system", content = _currentConfig.systemPrompt });
            }

            _messageHistory.Add(new Message { role = "user", content = userMsg });

            // 3. Send Request
            var requestData = new ChatRequest
            {
                model = _currentConfig.GetModelName(),
                messages = _messageHistory,
                stream = _currentConfig.stream,
                temperature = _currentConfig.temperature
            };

            string jsonBody = JsonUtility.ToJson(requestData);

            if (_currentConfig.stream)
            {
                PostRequestStream(_currentConfig.apiUrl, _currentConfig.AuthToken, jsonBody);
            }
            else
            {
                PostRequest(_currentConfig.apiUrl, _currentConfig.AuthToken, jsonBody);
            }
        }

        private void PostRequest(string url, string token, string jsonBody)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", token);

            // Handle async operation in Editor
            var operation = request.SendWebRequest();

            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                if (!operation.isDone) return;

                EditorApplication.update -= callback;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    AddAIMessage(_chatScrollView, $"Error: {request.error}\n{request.downloadHandler.text}");
                }
                else
                {
                    HandleResponse(request.downloadHandler.text);
                }
                request.Dispose();
            };

            EditorApplication.update += callback;
        }

        private void PostRequestStream(string url, string token, string jsonBody)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", token);

            var operation = request.SendWebRequest();

            // Prepare UI for streaming
            Label aiLabel = AddAIMessage(_chatScrollView, "...");
            StringBuilder fullContent = new StringBuilder();
            int lastReadIndex = 0;
            bool firstChunk = true;

            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                // Safety check: if window closed or req disposed
                if (request == null)
                {
                    EditorApplication.update -= callback;
                    return;
                }

                if (request.result != UnityWebRequest.Result.InProgress && request.result != UnityWebRequest.Result.Success)
                {
                    EditorApplication.update -= callback;
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        if (aiLabel != null) aiLabel.text = $"[Error: {request.error}]";
                    }
                    request.Dispose();
                    return;
                }

                // Process new data
                string currentText = request.downloadHandler.text;
                if (!string.IsNullOrEmpty(currentText) && currentText.Length > lastReadIndex)
                {
                    string newContent = currentText.Substring(lastReadIndex);
                    lastReadIndex = currentText.Length;

                    string[] lines = newContent.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("data: "))
                        {
                            string data = line.Substring(6).Trim();
                            if (data == "[DONE]")
                            {
                                // Stream finished
                                _messageHistory.Add(new Message { role = "assistant", content = fullContent.ToString() });
                                continue;
                            }

                            try
                            {
                                var streamObj = JsonUtility.FromJson<ChatStreamResponse>(data);
                                if (streamObj != null && streamObj.choices != null && streamObj.choices.Count > 0)
                                {
                                    var delta = streamObj.choices[0].delta;
                                    if (delta != null && delta.content != null) // Content can be empty string but not null check
                                    {
                                        if (firstChunk)
                                        {
                                            aiLabel.text = ""; // Clear "..."
                                            firstChunk = false;
                                        }
                                        fullContent.Append(delta.content);
                                        aiLabel.text = ConvertToRichText(fullContent.ToString());
                                        ScrollToBottom(_chatScrollView);
                                    }
                                }
                            }
                            catch
                            {
                                // Ignore json parse error for partial lines or invalid data
                            }
                        }
                    }
                }

                if (operation.isDone)
                {
                    EditorApplication.update -= callback;
                    request.Dispose();
                }
            };

            EditorApplication.update += callback;
        }

        private void HandleResponse(string jsonResponse)
        {
            try
            {
                // Parse GLM/OpenAI format
                var responseObj = JsonUtility.FromJson<ChatResponse>(jsonResponse);
                if (responseObj != null && responseObj.choices != null && responseObj.choices.Count > 0)
                {
                    var aiMsg = responseObj.choices[0].message;
                    if (aiMsg != null)
                    {
                        if (aiMsg != null)
                        {
                            AddAIMessage(_chatScrollView, aiMsg.content); // AddAIMessage handles conversion
                            _messageHistory.Add(aiMsg);
                        }
                    }
                }
                else
                {
                    AddAIMessage(_chatScrollView, $"Received empty or invalid response: {jsonResponse}");
                }
            }
            catch (Exception ex)
            {
                AddAIMessage(_chatScrollView, $"JSON Parse Error: {ex.Message}");
            }
        }

        private void AddUserMessage(ScrollView container, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("chat-message-row");
            row.AddToClassList("chat-message-row-user");

            var bubble = new VisualElement();
            bubble.AddToClassList("chat-bubble");
            bubble.AddToClassList("chat-bubble-user");

            var label = new Label(text);
            label.AddToClassList("chat-bubble-text");

            bubble.Add(label);
            row.Add(bubble);
            container.Add(row);

            ScrollToBottom(container);
        }

        private Label AddAIMessage(ScrollView container, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("chat-message-row");
            row.AddToClassList("chat-message-row-ai");

            var bubble = new VisualElement();
            bubble.AddToClassList("chat-bubble");
            bubble.AddToClassList("chat-bubble-ai");

            var label = new Label(ConvertToRichText(text));
            label.AddToClassList("chat-bubble-text");

            bubble.Add(label);
            row.Add(bubble);
            container.Add(row);

            ScrollToBottom(container);
            return label;
        }

        private void ScrollToBottom(ScrollView scroll)
        {
            scroll.schedule.Execute(() =>
            {
                if (scroll.contentContainer.layout.height > scroll.layout.height)
                {
                    scroll.scrollOffset = new Vector2(0, scroll.contentContainer.layout.height - scroll.layout.height);
                }
            }).ExecuteLater(50);
        }

        // Data Structures for JSON
        [Serializable]
        private class ChatRequest
        {
            public string model;
            public List<Message> messages;
            public bool stream;
            public float temperature;
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

        // Streaming Response Structures
        [Serializable]
        private class ChatStreamResponse
        {
            public List<StreamChoice> choices;
        }

        [Serializable]
        private class StreamChoice
        {
            public Message delta;
        }

        private string ConvertToRichText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            // 1. Code Blocks: ```code``` -> <color=...>Isolate...</color>
            // Note: Rich Text doesn't support background color blocks, so we use text color or just bold.
            // Let's use a nice orange for code: #FFAB40
            input = Regex.Replace(input, @"```([\s\S]*?)```", m =>
            {
                return $"<color=#FFAB40>{m.Groups[1].Value}</color>";
            });

            // 2. Inline Code: `code` -> <color=...>code</color>
            input = Regex.Replace(input, @"`([^`]+)`", "<color=#FFAB40>$1</color>");

            // 3. Bold: **text** -> <b>text</b>
            input = Regex.Replace(input, @"\*\*([^*]+)\*\*", "<b>$1</b>");

            // 4. Italic: *text* -> <i>text</i> (Basic implementation, might conflict if mixed carelessly but okay for chat)
            input = Regex.Replace(input, @"\*([^*]+)\*", "<i>$1</i>");

            // 5. Headers: # Header -> <b><size=14>Header</size></b>
            // We use Multiline option to match ^ at start of line
            input = Regex.Replace(input, @"^#{1,6}\s+(.+)$", "<b><size=14>$1</size></b>", RegexOptions.Multiline);

            return input;
        }
    }
}
