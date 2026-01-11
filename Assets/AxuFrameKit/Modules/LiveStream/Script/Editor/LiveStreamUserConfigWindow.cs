using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using AxuFrameKit.Modules.LiveStream;

namespace AxuFrameKit.Modules.LiveStream.Editor
{
    public class LiveStreamUserConfigWindow : EditorWindow
    {
        [MenuItem("AxuFrameKit/LiveStream/User Config Window")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<LiveStreamUserConfigWindow>();
            wnd.titleContent = new GUIContent("LiveStream Config");
            wnd.minSize = new Vector2(500, 300);
        }

        private ListView _configListView;
        private ListView _userListView;
        private Button _loadBtn;

        // Data
        private List<UserConfigSo> _foundConfigs = new List<UserConfigSo>();
        private List<UserDataUnit> _currentDisplayedUsers = new List<UserDataUnit>();

        // Selection
        private UserConfigSo _selectedConfig;

        public void CreateGUI()
        {
            // Load and add Value (USS)
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/AxuFrameKit/Modules/LiveStream/Script/Editor/LiveStreamUserConfigWindow.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            // Apply Root Style for UE5 Background
            rootVisualElement.AddToClassList("ue5-root");

            // Root Container - TwoPaneSplitView
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1;
            rootVisualElement.Add(splitView);

            var leftPane = new VisualElement();
            leftPane.style.flexGrow = 1;
            leftPane.style.backgroundColor = new StyleColor(new Color(0.125f, 0.125f, 0.125f)); // #202020
            splitView.Add(leftPane);

            var rightPane = new VisualElement();
            rightPane.style.flexGrow = 1;
            rightPane.style.backgroundColor = new StyleColor(new Color(0.125f, 0.125f, 0.125f)); // #202020
            splitView.Add(rightPane);

            // --- Left Pane: Config List & Load Button ---

            // Header
            var leftHeader = new Label("Found UserConfigSo Assets");
            leftHeader.AddToClassList("header-label");
            leftPane.Add(leftHeader);

            // List
            _configListView = new ListView();
            _configListView.makeItem = () =>
            {
                var label = new Label();
                label.style.paddingLeft = 5;
                return label;
            };
            _configListView.bindItem = (element, index) =>
            {
                var label = element as Label;
                if (index >= 0 && index < _foundConfigs.Count)
                {
                    label.text = _foundConfigs[index].name;
                }
            };
            _configListView.itemsSource = _foundConfigs;
            _configListView.selectionType = SelectionType.Single;
            _configListView.onSelectionChange += OnConfigSelectionChanged;
            _configListView.AddToClassList("green-selected-list");
            _configListView.style.flexGrow = 1;

            leftPane.Add(_configListView);

            // Button (Moved to Left Pane as requested)
            _loadBtn = new Button(OnLoadButtonClicked) { text = "加载User配置" };
            _loadBtn.AddToClassList("ue5-button");
            _loadBtn.SetEnabled(false);
            leftPane.Add(_loadBtn);

            // --- Right Pane: User List ---
            var rightHeader = new Label("User List (Nickname)");
            rightHeader.AddToClassList("header-label");
            rightPane.Add(rightHeader);

            _userListView = new ListView();
            _userListView.makeItem = () =>
            {
                var label = new Label();
                label.style.paddingLeft = 5;
                return label;
            };
            _userListView.bindItem = (element, index) =>
            {
                var label = element as Label;
                if (index >= 0 && index < _currentDisplayedUsers.Count)
                {
                    label.text = _currentDisplayedUsers[index].Nickname;
                }
            };
            _userListView.itemsSource = _currentDisplayedUsers;
            _userListView.selectionType = SelectionType.Single;
            _userListView.AddToClassList("green-selected-list");
            _userListView.style.flexGrow = 1;

            rightPane.Add(_userListView);

            // Initial Search
            SearchForConfigs();
        }

        private void OnEnable()
        {
            // If window is kept open, refresh logic could vary. 
            // For now, CreateGUI calls SearchForConfigs.
        }

        private void SearchForConfigs()
        {
            _foundConfigs.Clear();
            string[] guids = AssetDatabase.FindAssets("t:UserConfigSo");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<UserConfigSo>(path);
                if (so != null)
                {
                    _foundConfigs.Add(so);
                }
            }
            // Refresh list wrapper
            if (_configListView != null)
                _configListView.Rebuild();
        }

        private void OnConfigSelectionChanged(IEnumerable<object> selection)
        {
            // Update selected config
            _selectedConfig = null;
            foreach (var item in selection)
            {
                _selectedConfig = item as UserConfigSo;
                break; // Single selection
            }

            // Enable/Disable Load button
            _loadBtn?.SetEnabled(_selectedConfig != null);
        }

        private void OnLoadButtonClicked()
        {
            if (_selectedConfig == null) return;

            // Read private list via Reflection
            // Field: _userDataUnitList
            var field = typeof(UserConfigSo).GetField("_userDataUnitList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var list = field.GetValue(_selectedConfig) as List<UserDataUnit>;

                _currentDisplayedUsers.Clear();
                if (list != null)
                {
                    _currentDisplayedUsers.AddRange(list);
                }

                _userListView.Rebuild();
            }
            else
            {
                Debug.LogError("[LiveStreamUserConfigWindow] Could not find field '_userDataUnitList' in UserConfigSo.");
            }
        }
    }
}
