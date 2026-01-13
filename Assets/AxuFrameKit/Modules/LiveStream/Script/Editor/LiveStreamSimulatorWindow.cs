using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuFrameKit.Modules.LiveStream.Editor
{
    public class LiveStreamSimulatorWindow : EditorWindow
    {
        [MenuItem("AxuFrameKit/LiveStream/Simulator")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<LiveStreamSimulatorWindow>();
            wnd.titleContent = new GUIContent("LiveStream Simulator");
            wnd.minSize = new Vector2(900, 600);
        }

        // === Events (Collection mode for multi-selection) ===
        public static event Action<IReadOnlyList<UserDataGift>> OnGiftsSent;
        public static event Action<IReadOnlyList<UserDataComment>> OnCommentsSent;

        #region Simulator Fields
        // Config Lists
        private ListView _userConfigListView;
        private ListView _giftConfigListView;

        // Data Lists
        private ListView _userListView;
        private ListView _giftListView;

        // Load Buttons
        private Button _loadUserBtn;
        private Button _loadGiftBtn;

        // Action Elements
        private TextField _commentField;
        private Button _sendCommentBtn;
        private Button _sendGiftBtn;

        // Gift quantity storage (index -> quantity)
        private Dictionary<int, int> _giftQuantities = new Dictionary<int, int>();

        // Data Sources
        private List<UserConfigSo> _foundUserConfigs = new List<UserConfigSo>();
        private List<GiftConfigSo> _foundGiftConfigs = new List<GiftConfigSo>();
        private List<UserDataUnit> _currentDisplayedUsers = new List<UserDataUnit>();
        private List<GiftDataUnit> _currentDisplayedGifts = new List<GiftDataUnit>();

        // Selection
        private UserConfigSo _selectedUserConfig;
        private GiftConfigSo _selectedGiftConfig;
        private List<UserDataUnit> _selectedUsers = new List<UserDataUnit>();
        private GiftDataUnit _selectedGift;
        private int _selectedGiftIndex = -1;
        #endregion

        #region Debugger Fields
        // Log entry structure
        public enum LogType { Info, Warning, Error, Gift, Comment }

        public struct LogEntry
        {
            public DateTime Timestamp;
            public LogType Type;
            public string Message;
            public string Details;
            public Sprite Avatar;
            public string PlayerName;
            public string PlayerId;
        }

        // Log storage
        private List<LogEntry> _logs = new List<LogEntry>();
        private HashSet<int> _expandedLogs = new HashSet<int>();
        private const int MaxLogCount = 500;

        // Debugger UI Elements
        private ScrollView _logScrollView;
        private Label _logStatusLabel;

        // Filters
        private bool _showInfo = true;
        private bool _showWarning = true;
        private bool _showError = true;
        private bool _showGift = true;
        private bool _showComment = true;
        private bool _autoScroll = true;

        // Static instance for logging
        private static LiveStreamSimulatorWindow _instance;
        #endregion

        private void OnEnable()
        {
            _instance = this;
        }

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;
        }

        public void CreateGUI()
        {
            _instance = this;

            // Load and add USS
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/AxuFrameKit/Modules/LiveStream/Script/Editor/LiveStreamSimulatorWindow.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            // Apply Root Style
            rootVisualElement.AddToClassList("ue5-root");

            // Main vertical split view (Simulator on top, Debugger on bottom) - Draggable separator
            // fixedPaneIndex=1 means Debugger (second pane) is fixed, Simulator fills remaining space
            var mainSplitView = new TwoPaneSplitView(1, 200, TwoPaneSplitViewOrientation.Vertical);
            mainSplitView.AddToClassList("main-vertical-split");
            mainSplitView.style.flexGrow = 1;
            rootVisualElement.Add(mainSplitView);

            // Set initial split ratio (60% Simulator : 40% Debugger) after layout
            mainSplitView.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.oldRect.height == 0 && evt.newRect.height > 0)
                {
                    // First layout - set Debugger to 40% of total height
                    float debuggerHeight = evt.newRect.height * 0.4f;
                    mainSplitView.fixedPaneInitialDimension = debuggerHeight;
                }
            });

            // === Top Area: Simulator ===
            var simulatorArea = CreateSimulatorArea();
            simulatorArea.style.flexGrow = 1;
            simulatorArea.style.minHeight = 200;
            mainSplitView.Add(simulatorArea);

            // === Bottom Area: Debugger ===
            var debuggerArea = CreateDebuggerArea();
            debuggerArea.style.flexGrow = 1;
            debuggerArea.style.minHeight = 150;
            mainSplitView.Add(debuggerArea);

            // Initial Search
            SearchForConfigs();
        }


        #region Simulator Area
        private VisualElement CreateSimulatorArea()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.minHeight = 200;

            // Horizontal Split for Simulator content
            var mainSplitView = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            mainSplitView.style.flexGrow = 1;
            container.Add(mainSplitView);

            // Left Pane: Config Selection
            var leftPane = CreateLeftPane();
            mainSplitView.Add(leftPane);

            // Right Area: Data Lists
            var rightSplitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
            rightSplitView.style.flexGrow = 1;
            mainSplitView.Add(rightSplitView);

            // Middle Pane: User List
            var middlePane = CreateUserListPane();
            rightSplitView.Add(middlePane);

            // Right Pane: Gift List
            var rightPane = CreateGiftListPane();
            rightSplitView.Add(rightPane);

            return container;
        }

        private VisualElement CreateLeftPane()
        {
            var leftPane = new VisualElement();
            leftPane.style.flexGrow = 1;
            leftPane.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f));
            leftPane.style.minWidth = 180;

            // === User Config Section ===
            var userConfigSection = new VisualElement();
            userConfigSection.style.flexGrow = 1;
            userConfigSection.style.flexBasis = new StyleLength(StyleKeyword.Auto);
            leftPane.Add(userConfigSection);

            var userConfigHeader = new Label("USER CONFIG");
            userConfigHeader.AddToClassList("header-label");
            userConfigSection.Add(userConfigHeader);

            _userConfigListView = new ListView();
            _userConfigListView.makeItem = () =>
            {
                var label = new Label();
                label.style.paddingLeft = 8;
                label.style.paddingTop = 4;
                label.style.paddingBottom = 4;
                return label;
            };
            _userConfigListView.bindItem = (element, index) =>
            {
                var label = element as Label;
                if (index >= 0 && index < _foundUserConfigs.Count)
                {
                    label.text = _foundUserConfigs[index].name;
                }
            };
            _userConfigListView.itemsSource = _foundUserConfigs;
            _userConfigListView.selectionType = SelectionType.Single;
            _userConfigListView.selectionChanged += OnUserConfigSelectionChanged;
            _userConfigListView.AddToClassList("green-selected-list");
            _userConfigListView.style.flexGrow = 1;
            userConfigSection.Add(_userConfigListView);

            _loadUserBtn = new Button(OnLoadUserButtonClicked) { text = "加载 User 配置" };
            _loadUserBtn.AddToClassList("ue5-button");
            _loadUserBtn.SetEnabled(false);
            userConfigSection.Add(_loadUserBtn);

            // === Separator ===
            var separator = new VisualElement();
            separator.AddToClassList("ue5-separator");
            separator.style.marginTop = 8;
            separator.style.marginBottom = 8;
            leftPane.Add(separator);

            // === Gift Config Section ===
            var giftConfigSection = new VisualElement();
            giftConfigSection.style.flexGrow = 1;
            giftConfigSection.style.flexBasis = new StyleLength(StyleKeyword.Auto);
            leftPane.Add(giftConfigSection);

            var giftConfigHeader = new Label("GIFT CONFIG");
            giftConfigHeader.AddToClassList("header-label");
            giftConfigSection.Add(giftConfigHeader);

            _giftConfigListView = new ListView();
            _giftConfigListView.makeItem = () =>
            {
                var label = new Label();
                label.style.paddingLeft = 8;
                label.style.paddingTop = 4;
                label.style.paddingBottom = 4;
                return label;
            };
            _giftConfigListView.bindItem = (element, index) =>
            {
                var label = element as Label;
                if (index >= 0 && index < _foundGiftConfigs.Count)
                {
                    label.text = _foundGiftConfigs[index].name;
                }
            };
            _giftConfigListView.itemsSource = _foundGiftConfigs;
            _giftConfigListView.selectionType = SelectionType.Single;
            _giftConfigListView.selectionChanged += OnGiftConfigSelectionChanged;
            _giftConfigListView.AddToClassList("green-selected-list");
            _giftConfigListView.style.flexGrow = 1;
            giftConfigSection.Add(_giftConfigListView);

            _loadGiftBtn = new Button(OnLoadGiftButtonClicked) { text = "加载 Gift 配置" };
            _loadGiftBtn.AddToClassList("ue5-button");
            _loadGiftBtn.SetEnabled(false);
            giftConfigSection.Add(_loadGiftBtn);

            return leftPane;
        }

        private VisualElement CreateUserListPane()
        {
            var pane = new VisualElement();
            pane.style.flexGrow = 1;
            pane.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f));
            pane.style.minWidth = 220;

            var header = new Label("USER LIST");
            header.AddToClassList("header-label");
            pane.Add(header);

            _userListView = new ListView();
            _userListView.makeItem = () => CreateUserListItem();
            _userListView.bindItem = BindUserListItem;
            _userListView.itemsSource = _currentDisplayedUsers;
            _userListView.selectionType = SelectionType.Multiple;
            _userListView.selectionChanged += OnUserSelectionChanged;
            _userListView.AddToClassList("green-selected-list");
            _userListView.style.flexGrow = 1;
            _userListView.fixedItemHeight = 36;
            pane.Add(_userListView);

            // === Comment Section ===
            var commentSection = new VisualElement();
            commentSection.style.flexDirection = FlexDirection.Column;
            commentSection.style.paddingLeft = 8;
            commentSection.style.paddingRight = 8;
            commentSection.style.paddingTop = 8;
            commentSection.style.paddingBottom = 8;
            commentSection.style.borderTopWidth = 1;
            commentSection.style.borderTopColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            pane.Add(commentSection);

            var commentLabel = new Label("评论发送");
            commentLabel.AddToClassList("section-title");
            commentLabel.style.marginBottom = 6;
            commentLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            commentLabel.style.fontSize = 11;
            commentSection.Add(commentLabel);

            var commentRow = new VisualElement();
            commentRow.style.flexDirection = FlexDirection.Row;
            commentRow.style.alignItems = Align.Center;
            commentSection.Add(commentRow);

            _commentField = new TextField();
            _commentField.style.flexGrow = 1;
            _commentField.style.marginRight = 6;
            _commentField.AddToClassList("ue5-text-field");
            commentRow.Add(_commentField);

            _sendCommentBtn = new Button(OnSendCommentClicked) { text = "发送" };
            _sendCommentBtn.AddToClassList("ue5-button");
            _sendCommentBtn.AddToClassList("ue5-button-small");
            _sendCommentBtn.style.marginLeft = 0;
            _sendCommentBtn.style.marginRight = 0;
            _sendCommentBtn.style.marginTop = 0;
            _sendCommentBtn.style.marginBottom = 0;
            _sendCommentBtn.style.width = 60;
            commentRow.Add(_sendCommentBtn);

            return pane;
        }

        private VisualElement CreateUserListItem()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.paddingLeft = 6;
            container.style.paddingRight = 6;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;

            // Avatar Circle
            var avatarContainer = new VisualElement();
            avatarContainer.name = "avatar";
            avatarContainer.AddToClassList("user-avatar");
            container.Add(avatarContainer);

            // Nickname Label
            var nicknameLabel = new Label();
            nicknameLabel.name = "nickname";
            nicknameLabel.style.marginLeft = 10;
            nicknameLabel.style.flexGrow = 1;
            container.Add(nicknameLabel);

            // UID Label (right side)
            var uidLabel = new Label();
            uidLabel.name = "uid";
            uidLabel.AddToClassList("user-uid-label");
            container.Add(uidLabel);

            return container;
        }

        private void BindUserListItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _currentDisplayedUsers.Count) return;

            var user = _currentDisplayedUsers[index];

            var avatar = element.Q<VisualElement>("avatar");
            var nickname = element.Q<Label>("nickname");
            var uid = element.Q<Label>("uid");

            if (nickname != null)
            {
                nickname.text = user.Nickname ?? "(No Name)";
            }

            if (uid != null)
            {
                uid.text = user.Uid ?? "";
            }

            if (avatar != null)
            {
                if (user.Avatar != null)
                {
                    avatar.style.backgroundImage = new StyleBackground(user.Avatar);
                    avatar.RemoveFromClassList("user-avatar-empty");
                }
                else
                {
                    avatar.style.backgroundImage = StyleKeyword.None;
                    avatar.AddToClassList("user-avatar-empty");
                }
            }
        }

        private VisualElement CreateGiftListPane()
        {
            var pane = new VisualElement();
            pane.style.flexGrow = 1;
            pane.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f));
            pane.style.minWidth = 240;

            var header = new Label("GIFT LIST");
            header.AddToClassList("header-label");
            pane.Add(header);

            _giftListView = new ListView();
            _giftListView.makeItem = () => CreateGiftListItem();
            _giftListView.bindItem = BindGiftListItem;
            _giftListView.itemsSource = _currentDisplayedGifts;
            _giftListView.selectionType = SelectionType.Single;
            _giftListView.selectionChanged += OnGiftSelectionChanged;
            _giftListView.AddToClassList("green-selected-list");
            _giftListView.style.flexGrow = 1;
            _giftListView.fixedItemHeight = 40;
            pane.Add(_giftListView);

            // === Send Gift Button ===
            _sendGiftBtn = new Button(OnSendGiftClicked) { text = "赠送礼物" };
            _sendGiftBtn.AddToClassList("ue5-button");
            _sendGiftBtn.AddToClassList("ue5-button-accent");
            pane.Add(_sendGiftBtn);

            return pane;
        }

        private VisualElement CreateGiftListItem()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.paddingLeft = 6;
            container.style.paddingRight = 6;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;

            // Gift Image
            var imgContainer = new VisualElement();
            imgContainer.name = "giftImg";
            imgContainer.AddToClassList("gift-img");
            container.Add(imgContainer);

            // Info Container
            var infoContainer = new VisualElement();
            infoContainer.style.marginLeft = 10;
            infoContainer.style.flexGrow = 1;
            container.Add(infoContainer);

            // Gift Name
            var nameLabel = new Label();
            nameLabel.name = "giftName";
            nameLabel.style.fontSize = 12;
            infoContainer.Add(nameLabel);

            // Gift Price
            var priceLabel = new Label();
            priceLabel.name = "giftPrice";
            priceLabel.style.fontSize = 10;
            priceLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            infoContainer.Add(priceLabel);

            // Quantity Input Field
            var quantityField = new TextField();
            quantityField.name = "quantityField";
            quantityField.value = "1";
            quantityField.AddToClassList("gift-quantity-field");
            quantityField.style.width = 50;
            quantityField.style.marginLeft = 8;

            // Register callback to filter non-integer input
            quantityField.RegisterValueChangedCallback(evt =>
            {
                // Filter to allow only digits
                string filtered = "";
                foreach (char c in evt.newValue)
                {
                    if (char.IsDigit(c))
                        filtered += c;
                }

                // Ensure at least "1" if empty
                if (string.IsNullOrEmpty(filtered))
                    filtered = "1";

                // Parse and store
                if (int.TryParse(filtered, out int qty))
                {
                    if (qty < 1) qty = 1;
                    filtered = qty.ToString();
                }

                // Update field if changed
                if (evt.newValue != filtered)
                {
                    quantityField.SetValueWithoutNotify(filtered);
                }
            });

            container.Add(quantityField);

            return container;
        }

        private void BindGiftListItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _currentDisplayedGifts.Count) return;

            var gift = _currentDisplayedGifts[index];

            var giftImg = element.Q<VisualElement>("giftImg");
            var giftName = element.Q<Label>("giftName");
            var giftPrice = element.Q<Label>("giftPrice");
            var quantityField = element.Q<TextField>("quantityField");

            if (giftName != null)
            {
                giftName.text = gift.Name ?? "(No Name)";
            }

            if (giftPrice != null)
            {
                giftPrice.text = $"¥ {gift.Price}";
            }

            if (giftImg != null)
            {
                if (gift.Img != null)
                {
                    giftImg.style.backgroundImage = new StyleBackground(gift.Img);
                    giftImg.RemoveFromClassList("gift-img-empty");
                }
                else
                {
                    giftImg.style.backgroundImage = StyleKeyword.None;
                    giftImg.AddToClassList("gift-img-empty");
                }
            }

            // Initialize quantity if not exists
            if (quantityField != null)
            {
                if (!_giftQuantities.ContainsKey(index))
                {
                    _giftQuantities[index] = 1;
                }
                quantityField.SetValueWithoutNotify(_giftQuantities[index].ToString());

                // Store quantity on change
                int currentIndex = index; // Capture for closure
                quantityField.RegisterValueChangedCallback(evt =>
                {
                    if (int.TryParse(evt.newValue, out int qty))
                    {
                        _giftQuantities[currentIndex] = qty;
                    }
                });
            }
        }
        #endregion

        #region Simulator Logic
        private void SearchForConfigs()
        {
            // Search UserConfigSo
            _foundUserConfigs.Clear();
            string[] userGuids = AssetDatabase.FindAssets("t:UserConfigSo");
            foreach (var guid in userGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<UserConfigSo>(path);
                if (so != null)
                {
                    _foundUserConfigs.Add(so);
                }
            }
            _userConfigListView?.Rebuild();

            // Search GiftConfigSo
            _foundGiftConfigs.Clear();
            string[] giftGuids = AssetDatabase.FindAssets("t:GiftConfigSo");
            foreach (var guid in giftGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<GiftConfigSo>(path);
                if (so != null)
                {
                    _foundGiftConfigs.Add(so);
                }
            }
            _giftConfigListView?.Rebuild();
        }

        private void OnUserConfigSelectionChanged(IEnumerable<object> selection)
        {
            _selectedUserConfig = null;
            foreach (var item in selection)
            {
                _selectedUserConfig = item as UserConfigSo;
                break;
            }
            _loadUserBtn?.SetEnabled(_selectedUserConfig != null);
        }

        private void OnGiftConfigSelectionChanged(IEnumerable<object> selection)
        {
            _selectedGiftConfig = null;
            foreach (var item in selection)
            {
                _selectedGiftConfig = item as GiftConfigSo;
                break;
            }
            _loadGiftBtn?.SetEnabled(_selectedGiftConfig != null);
        }

        private void OnUserSelectionChanged(IEnumerable<object> selection)
        {
            _selectedUsers.Clear();
            foreach (var item in selection)
            {
                if (item is UserDataUnit user)
                {
                    _selectedUsers.Add(user);
                }
            }
        }

        private void OnGiftSelectionChanged(IEnumerable<object> selection)
        {
            _selectedGift = null;
            _selectedGiftIndex = -1;
            foreach (var item in selection)
            {
                _selectedGift = item as GiftDataUnit;
                _selectedGiftIndex = _currentDisplayedGifts.IndexOf(_selectedGift);
                break;
            }
        }

        private void OnLoadUserButtonClicked()
        {
            if (_selectedUserConfig == null) return;

            var field = typeof(UserConfigSo).GetField("_userDataUnitList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var list = field.GetValue(_selectedUserConfig) as List<UserDataUnit>;
                _currentDisplayedUsers.Clear();
                _selectedUsers.Clear();
                if (list != null)
                {
                    _currentDisplayedUsers.AddRange(list);
                }
                _userListView.Rebuild();
                Log($"已加载 {_currentDisplayedUsers.Count} 个用户");
            }
            else
            {
                LogError("Could not find field '_userDataUnitList' in UserConfigSo.");
            }
        }

        private void OnLoadGiftButtonClicked()
        {
            if (_selectedGiftConfig == null) return;

            var field = typeof(GiftConfigSo).GetField("_giftDataUnitList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var list = field.GetValue(_selectedGiftConfig) as List<GiftDataUnit>;
                _currentDisplayedGifts.Clear();
                _giftQuantities.Clear();
                _selectedGift = null;
                _selectedGiftIndex = -1;
                if (list != null)
                {
                    _currentDisplayedGifts.AddRange(list);
                    // Initialize quantities
                    for (int i = 0; i < _currentDisplayedGifts.Count; i++)
                    {
                        _giftQuantities[i] = 1;
                    }
                }
                _giftListView.Rebuild();
                Log($"已加载 {_currentDisplayedGifts.Count} 个礼物");
            }
            else
            {
                LogError("Could not find field '_giftDataUnitList' in GiftConfigSo.");
            }
        }

        private void OnSendGiftClicked()
        {
            // Validate user selection
            if (_selectedUsers.Count == 0)
            {
                LogWarning("请先选择至少一个用户！");
                // EditorUtility.DisplayDialog("提示", "请先选择至少一个用户！", "确定");
                return;
            }

            // Validate gift selection
            if (_selectedGift == null || _selectedGiftIndex < 0)
            {
                LogWarning("请先选择一个礼物！");
                // EditorUtility.DisplayDialog("提示", "请先选择一个礼物！", "确定");
                return;
            }

            // Get quantity
            int quantity = 1;
            if (_giftQuantities.ContainsKey(_selectedGiftIndex))
            {
                quantity = _giftQuantities[_selectedGiftIndex];
            }

            // Validate quantity
            if (quantity < 1)
            {
                LogWarning("礼物数量不能小于 1！");
                // EditorUtility.DisplayDialog("提示", "礼物数量不能小于 1！", "确定");
                return;
            }

            // Build gift data for all selected users
            var giftDataList = new List<UserDataGift>();
            foreach (var user in _selectedUsers)
            {
                var giftData = new UserDataGift
                {
                    Uid = user.Uid,
                    Nickname = user.Nickname,
                    AvatarUrl = user.AvatarUrl,
                    Avatar = user.Avatar,
                    GiftName = _selectedGift.Name,
                    GiftIndex = _selectedGiftIndex,
                    GiftCount = quantity
                };
                giftDataList.Add(giftData);

                // Log each gift
                LogGift(
                    $"赠送 {quantity} 个 {_selectedGift.Name}",
                    $"礼物索引: {_selectedGiftIndex}\n礼物价值: ¥{_selectedGift.Price * quantity}",
                    user.Avatar,
                    user.Nickname,
                    user.Uid
                );
            }

            // Fire event with collection
            OnGiftsSent?.Invoke(giftDataList.AsReadOnly());
            Log($"已向 {_selectedUsers.Count} 个用户发送礼物");
        }

        private void OnSendCommentClicked()
        {
            // Validate user selection
            if (_selectedUsers.Count == 0)
            {
                LogWarning("请先选择至少一个用户！");
                EditorUtility.DisplayDialog("提示", "请先选择至少一个用户！", "确定");
                return;
            }

            // Validate comment
            string comment = _commentField?.value?.Trim();
            if (string.IsNullOrEmpty(comment))
            {
                LogWarning("评论不能为空！");
                EditorUtility.DisplayDialog("提示", "评论不能为空！", "确定");
                return;
            }

            // Build comment data for all selected users
            var commentDataList = new List<UserDataComment>();
            foreach (var user in _selectedUsers)
            {
                var commentData = new UserDataComment
                {
                    Uid = user.Uid,
                    Nickname = user.Nickname,
                    AvatarUrl = user.AvatarUrl,
                    Avatar = user.Avatar,
                    Comment = comment
                };
                commentDataList.Add(commentData);

                // Log each comment
                LogComment(
                    comment,
                    null,
                    user.Avatar,
                    user.Nickname,
                    user.Uid
                );
            }

            // Fire event with collection
            OnCommentsSent?.Invoke(commentDataList.AsReadOnly());
            // Log($"已向 {_selectedUsers.Count} 个用户发送评论");

            // Clear comment field
            if (_commentField != null)
            {
                _commentField.value = "";
            }
        }
        #endregion

        #region Debugger Area
        private VisualElement CreateDebuggerArea()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.minHeight = 150;
            container.style.flexDirection = FlexDirection.Row;

            // === Left: Log List ===
            var leftPane = new VisualElement();
            leftPane.AddToClassList("debugger-left-pane");
            container.Add(leftPane);

            // Toolbar
            var toolbar = CreateDebuggerToolbar();
            leftPane.Add(toolbar);

            // Log ScrollView
            _logScrollView = new ScrollView(ScrollViewMode.Vertical);
            _logScrollView.AddToClassList("debugger-log-scroll");
            leftPane.Add(_logScrollView);

            // Status Bar
            var statusBar = new VisualElement();
            statusBar.AddToClassList("ue5-status-bar");
            statusBar.style.flexDirection = FlexDirection.Row;
            statusBar.style.justifyContent = Justify.SpaceBetween;
            leftPane.Add(statusBar);

            _logStatusLabel = new Label("日志数量: 0");
            statusBar.Add(_logStatusLabel);

            // === Right: Filter Panel ===
            var rightPane = CreateDebuggerFilterPanel();
            container.Add(rightPane);

            return container;
        }

        private VisualElement CreateDebuggerToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("debugger-toolbar");

            // Header
            var header = new Label("DEBUGGER");
            header.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            header.style.fontSize = 11;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginRight = 15;
            toolbar.Add(header);

            // Clear Button
            var clearBtn = new Button(ClearLogs) { text = "清空" };
            clearBtn.AddToClassList("ue5-button");
            clearBtn.AddToClassList("ue5-button-small");
            clearBtn.style.marginRight = 10;
            toolbar.Add(clearBtn);

            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            // Auto scroll toggle - button style (no checkbox)
            Button autoScrollBtn = null;
            autoScrollBtn = new Button(() =>
            {
                _autoScroll = !_autoScroll;
                UpdateButtonToggleState(autoScrollBtn, _autoScroll);
            })
            { text = "自动滚动" };
            autoScrollBtn.AddToClassList("debugger-btn-toggle");
            UpdateButtonToggleState(autoScrollBtn, _autoScroll);
            toolbar.Add(autoScrollBtn);

            return toolbar;
        }

        private void UpdateButtonToggleState(Button button, bool isActive)
        {
            if (isActive)
            {
                button.RemoveFromClassList("debugger-btn-toggle--inactive");
                button.AddToClassList("debugger-btn-toggle--active");
            }
            else
            {
                button.RemoveFromClassList("debugger-btn-toggle--active");
                button.AddToClassList("debugger-btn-toggle--inactive");
            }
        }

        private VisualElement CreateDebuggerFilterPanel()
        {
            var panel = new VisualElement();
            panel.AddToClassList("debugger-filter-panel");

            // Header
            var header = new Label("筛选");
            header.AddToClassList("header-label");
            panel.Add(header);

            // Filter buttons (button-style toggles, no checkbox)
            var toggleContainer = new VisualElement();
            toggleContainer.AddToClassList("debugger-filter-container");
            panel.Add(toggleContainer);

            toggleContainer.Add(CreateFilterButton("ℹ 信息", _showInfo, v => { _showInfo = v; RefreshLogView(); }));
            toggleContainer.Add(CreateFilterButton("⚠ 警告", _showWarning, v => { _showWarning = v; RefreshLogView(); }));
            toggleContainer.Add(CreateFilterButton("✖ 错误", _showError, v => { _showError = v; RefreshLogView(); }));
            toggleContainer.Add(CreateFilterButton("🎁 礼物", _showGift, v => { _showGift = v; RefreshLogView(); }));
            toggleContainer.Add(CreateFilterButton("💬 评论", _showComment, v => { _showComment = v; RefreshLogView(); }));

            return panel;
        }

        private Button CreateFilterButton(string label, bool initialValue, Action<bool> onChanged)
        {
            bool isActive = initialValue;
            Button btn = null;
            btn = new Button(() =>
            {
                isActive = !isActive;
                onChanged(isActive);
                UpdateButtonToggleState(btn, isActive);
            })
            { text = label };

            btn.AddToClassList("debugger-filter-btn");
            UpdateButtonToggleState(btn, isActive);

            return btn;
        }
        #endregion

        #region Logging API
        public static void Log(string message, string details = null)
        {
            AddLogEntry(LogType.Info, message, details, null, null, null);
        }

        public static void LogWarning(string message, string details = null)
        {
            AddLogEntry(LogType.Warning, message, details, null, null, null);
        }

        public static void LogError(string message, string details = null)
        {
            AddLogEntry(LogType.Error, message, details, null, null, null);
        }

        public static void LogGift(string message, string details = null, Sprite avatar = null, string playerName = null, string playerId = null)
        {
            AddLogEntry(LogType.Gift, message, details, avatar, playerName, playerId);
        }

        public static void LogComment(string message, string details = null, Sprite avatar = null, string playerName = null, string playerId = null)
        {
            AddLogEntry(LogType.Comment, message, details, avatar, playerName, playerId);
        }

        private static void AddLogEntry(LogType type, string message, string details, Sprite avatar, string playerName, string playerId)
        {
            if (_instance == null) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Type = type,
                Message = message,
                Details = details,
                Avatar = avatar,
                PlayerName = playerName,
                PlayerId = playerId
            };

            _instance._logs.Add(entry);

            // Limit log count
            while (_instance._logs.Count > MaxLogCount)
            {
                _instance._logs.RemoveAt(0);
                var newExpanded = new HashSet<int>();
                foreach (var idx in _instance._expandedLogs)
                {
                    if (idx > 0) newExpanded.Add(idx - 1);
                }
                _instance._expandedLogs = newExpanded;
            }

            _instance.RefreshLogView();
        }

        private void ClearLogs()
        {
            _logs.Clear();
            _expandedLogs.Clear();
            RefreshLogView();
        }

        private void RefreshLogView()
        {
            if (_logScrollView == null) return;

            _logScrollView.Clear();

            int visibleCount = 0;
            for (int i = 0; i < _logs.Count; i++)
            {
                var log = _logs[i];
                if (!ShouldShowLog(log.Type)) continue;

                visibleCount++;
                bool isExpanded = _expandedLogs.Contains(i);
                var logElement = CreateLogElement(log, i, isExpanded);
                _logScrollView.Add(logElement);
            }

            _logStatusLabel.text = $"日志: {visibleCount} / {_logs.Count}";

            if (_autoScroll && _logScrollView.contentContainer.childCount > 0)
            {
                _logScrollView.schedule.Execute(() =>
                {
                    _logScrollView.scrollOffset = new Vector2(0, _logScrollView.contentContainer.layout.height);
                });
            }
        }

        private bool ShouldShowLog(LogType type)
        {
            return type switch
            {
                LogType.Info => _showInfo,
                LogType.Warning => _showWarning,
                LogType.Error => _showError,
                LogType.Gift => _showGift,
                LogType.Comment => _showComment,
                _ => true
            };
        }

        private VisualElement CreateLogElement(LogEntry log, int index, bool isExpanded)
        {
            var wrapper = new VisualElement();

            var container = new VisualElement();
            container.AddToClassList("log-entry");
            wrapper.Add(container);

            string typeIcon = GetLogTypeIcon(log.Type);
            string colorClass = GetLogTypeColorClass(log.Type);

            // Expand Button
            var expandBtn = new Button(() => ToggleExpand(index));
            expandBtn.text = isExpanded ? "▼" : "▶";
            expandBtn.AddToClassList("log-expand-btn");
            container.Add(expandBtn);

            // Avatar
            if (log.Avatar != null || !string.IsNullOrEmpty(log.PlayerName))
            {
                var avatarContainer = new VisualElement();
                avatarContainer.AddToClassList("log-avatar");
                if (log.Avatar != null)
                    avatarContainer.style.backgroundImage = new StyleBackground(log.Avatar);
                else
                    avatarContainer.AddToClassList("log-avatar-empty");
                container.Add(avatarContainer);
            }

            // Type Icon
            var typeLabel = new Label(typeIcon);
            typeLabel.AddToClassList("log-type-icon");
            typeLabel.AddToClassList(colorClass);
            container.Add(typeLabel);

            // Timestamp
            var timeLabel = new Label(log.Timestamp.ToString("HH:mm:ss"));
            timeLabel.AddToClassList("log-timestamp");
            container.Add(timeLabel);

            // Player Name
            if (!string.IsNullOrEmpty(log.PlayerName))
            {
                var nameLabel = new Label(log.PlayerName);
                nameLabel.AddToClassList("log-player-name");
                container.Add(nameLabel);
            }

            // Message
            var messageLabel = new Label(log.Message);
            messageLabel.AddToClassList("log-message");
            messageLabel.AddToClassList(colorClass);
            container.Add(messageLabel);

            // Double-click to expand
            container.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2) ToggleExpand(index);
            });

            // Expanded details
            if (isExpanded)
            {
                var detailsSection = new VisualElement();
                detailsSection.AddToClassList("log-details");

                if (!string.IsNullOrEmpty(log.PlayerId))
                    detailsSection.Add(CreateDetailRow("用户ID", log.PlayerId));

                detailsSection.Add(CreateDetailRow("时间", log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")));

                if (!string.IsNullOrEmpty(log.Details))
                {
                    foreach (var line in log.Details.Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                            detailsSection.Add(CreateDetailRow(parts[0].Trim(), parts[1].Trim()));
                        else
                            detailsSection.Add(CreateDetailRow("详情", line));
                    }
                }

                var copyBtn = new Button(() =>
                {
                    string fullLog = $"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] [{log.Type}]";
                    if (!string.IsNullOrEmpty(log.PlayerName)) fullLog += $" [{log.PlayerName}]";
                    if (!string.IsNullOrEmpty(log.PlayerId)) fullLog += $" (ID: {log.PlayerId})";
                    fullLog += $" {log.Message}";
                    if (!string.IsNullOrEmpty(log.Details)) fullLog += $"\n{log.Details}";
                    EditorGUIUtility.systemCopyBuffer = fullLog;
                })
                { text = "复制" };
                copyBtn.AddToClassList("ue5-button");
                copyBtn.AddToClassList("ue5-button-small");
                copyBtn.style.marginTop = 6;
                copyBtn.style.alignSelf = Align.FlexStart;
                detailsSection.Add(copyBtn);

                wrapper.Add(detailsSection);
            }

            return wrapper;
        }

        private VisualElement CreateDetailRow(string label, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("log-detail-row");

            var labelElement = new Label(label + ":");
            labelElement.AddToClassList("log-detail-label");
            row.Add(labelElement);

            var valueElement = new Label(value);
            valueElement.AddToClassList("log-detail-value");
            row.Add(valueElement);

            return row;
        }

        private void ToggleExpand(int index)
        {
            if (_expandedLogs.Contains(index))
                _expandedLogs.Remove(index);
            else
                _expandedLogs.Add(index);
            RefreshLogView();
        }

        private string GetLogTypeColorClass(LogType type) => type switch
        {
            LogType.Info => "log-message-info",
            LogType.Warning => "log-message-warning",
            LogType.Error => "log-message-error",
            LogType.Gift => "log-message-gift",
            LogType.Comment => "log-message-comment",
            _ => "log-message-info"
        };

        private string GetLogTypeIcon(LogType type) => type switch
        {
            LogType.Info => "ℹ",
            LogType.Warning => "⚠",
            LogType.Error => "✖",
            LogType.Gift => "🎁",
            LogType.Comment => "💬",
            _ => "●"
        };
        #endregion
    }
}
