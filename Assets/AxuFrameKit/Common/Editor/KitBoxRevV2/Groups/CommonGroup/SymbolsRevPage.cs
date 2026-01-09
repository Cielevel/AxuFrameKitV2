using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UIElements;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using AxuFrameKit.Common.Editor.Common;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.CommonGroup
{
    public class SymbolsRevPage : IKitPageRevV2
    {
        public string Title => "定义符号宏";
        private IKitBoxRevV2 _kitBox;

        // Root
        private VisualElement _root;

        // Left Panel
        private ScrollView _leftScrollView;
        private Button _modeSwitchBtn;

        // Selection Logic
        private VisualElement _currentSelectedRow;
        private string _selectedSymbol;

        // Right Panel Roots
        private VisualElement _normalDetailRoot;
        private VisualElement _customDetailRoot;

        // Normal Detail Elements
        private Label _detailTitle;
        private Label _detailDesc;
        private Label _detailStatus;
        private Button _actionButton;

        // Custom Detail Elements
        private TextField _customSymbolInput;
        private Label _customStatusLabel;
        private Button _customActionButton;

        // State
        private bool _isCustomMode = false;

        public static class InnerSymbols
        {
            public const string DebugX = "DEBUG_X";
            public const string YooAssetX = "YOO_ASSET_X";
            public const string AddressableX = "ADDRESSABLE_X";

            public static readonly string[] AllSymbols = { DebugX, YooAssetX, AddressableX };
            public static readonly Dictionary<string, string> SymbolDesc = new Dictionary<string, string>()
            {
                { DebugX, "启用工具内的调试Debug扩展，提供更详细的日志输出。" },
                { YooAssetX, "启用针对 YooAsset 的深度集成与资源管理扩展。" },
                { AddressableX, "启用对 Unity Addressables 的一键集成支持。" }
            };
        }

        public void Init(IKitBoxRevV2 kitBox) => _kitBox = kitBox;

        public VisualElement CreateContent()
        {
            _root = new VisualElement();
            _root.AddToClassList("row");
            _root.AddToClassList("flex-grow");

            // ================== Left Panel ==================
            var leftPanel = new VisualElement();
            leftPanel.AddToClassList("symbol-list-panel");
            leftPanel.style.minWidth = 200;
            leftPanel.style.width = 200;
            leftPanel.style.borderRightWidth = 1;
            leftPanel.style.borderRightColor = new Color(0.15f, 0.15f, 0.15f);
            leftPanel.style.display = DisplayStyle.Flex;
            leftPanel.style.flexDirection = FlexDirection.Column;

            // Scroll View replacement for ListView to handle Grouping better
            _leftScrollView = new ScrollView();
            _leftScrollView.style.flexGrow = 1;
            leftPanel.Add(_leftScrollView);

            RefreshSymbolListUI(); // Initialize data and UI

            // Custom Mode Button at the bottom of Left Panel
            _modeSwitchBtn = new Button(SwitchToCustomMode);
            _modeSwitchBtn.text = "+ 自定义符号";
            _modeSwitchBtn.style.height = 36;
            _modeSwitchBtn.style.marginTop = 5;
            _modeSwitchBtn.style.marginBottom = 5;
            _modeSwitchBtn.style.marginLeft = 5;
            _modeSwitchBtn.style.marginRight = 5;
            leftPanel.Add(_modeSwitchBtn);

            _root.Add(leftPanel);

            // ================== Right Panel ==================
            var rightPanel = new VisualElement();
            rightPanel.style.flexGrow = 1;
            rightPanel.style.paddingLeft = 15;
            rightPanel.style.paddingRight = 15;
            rightPanel.style.paddingTop = 10;

            // 1. Normal Detail Root
            _normalDetailRoot = new VisualElement();
            _normalDetailRoot.style.display = DisplayStyle.None;

            _detailTitle = new Label("Symbols");
            _detailTitle.style.fontSize = 20;
            _detailTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _detailTitle.style.marginBottom = 10;
            _detailTitle.style.color = new Color(0.3f, 0.7f, 0.9f);
            _normalDetailRoot.Add(_detailTitle);

            _detailDesc = new Label();
            _detailDesc.style.whiteSpace = WhiteSpace.Normal;
            _detailDesc.style.marginBottom = 15;
            _detailDesc.style.color = new Color(0.7f, 0.7f, 0.7f);
            _normalDetailRoot.Add(_detailDesc);

            var statusRow = new VisualElement();
            statusRow.style.flexDirection = FlexDirection.Row;
            statusRow.style.marginBottom = 20;
            statusRow.Add(new Label("当前状态: "));
            _detailStatus = new Label("-");
            _detailStatus.style.unityFontStyleAndWeight = FontStyle.Bold;
            _detailStatus.style.marginLeft = 5;
            statusRow.Add(_detailStatus);
            _normalDetailRoot.Add(statusRow);

            _actionButton = new Button();
            _actionButton.style.height = 30;
            _actionButton.clicked += OnActionClicked;
            _normalDetailRoot.Add(_actionButton);

            rightPanel.Add(_normalDetailRoot);

            // 2. Custom Detail Root
            _customDetailRoot = new VisualElement();
            _customDetailRoot.style.display = DisplayStyle.None;

            var customHeader = new Label("自定义宏管理");
            customHeader.style.fontSize = 18;
            customHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            customHeader.style.marginBottom = 10;
            _customDetailRoot.Add(customHeader);

            var customDesc = new Label("在此处输入任意预编译宏符号名称，可查询当前状态并进行添加或移除操作。\n(例如: MY_CUSTOM_DEFINE)");
            customDesc.style.fontSize = 12;
            customDesc.style.color = new Color(0.7f, 0.7f, 0.7f);
            customDesc.style.marginBottom = 20;
            customDesc.style.whiteSpace = WhiteSpace.Normal;
            _customDetailRoot.Add(customDesc);

            // Input Area
            var inputRow = new VisualElement();
            inputRow.style.flexDirection = FlexDirection.Row;
            inputRow.style.alignItems = Align.Center;
            inputRow.style.marginBottom = 15;

            var labelInfo = new Label("Symbol Name:");
            labelInfo.style.width = 90;
            inputRow.Add(labelInfo);

            _customSymbolInput = new TextField();
            _customSymbolInput.style.flexGrow = 1;
            _customSymbolInput.RegisterValueChangedCallback(evt => CheckCustomSymbol(evt.newValue));
            inputRow.Add(_customSymbolInput);

            _customDetailRoot.Add(inputRow);

            // Status Area
            var customStatusRow = new VisualElement();
            customStatusRow.style.flexDirection = FlexDirection.Row;
            customStatusRow.style.alignItems = Align.Center;
            customStatusRow.style.marginBottom = 20;

            customStatusRow.Add(new Label("Status: "));
            _customStatusLabel = new Label("等待输入...");
            _customStatusLabel.style.marginLeft = 5;
            _customStatusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            customStatusRow.Add(_customStatusLabel);
            _customDetailRoot.Add(customStatusRow);

            // Action Button
            _customActionButton = new Button();
            _customActionButton.text = "添加/移除";
            _customActionButton.style.height = 35;
            _customActionButton.clicked += OnCustomActionClicked;
            _customActionButton.SetEnabled(false);
            _customDetailRoot.Add(_customActionButton);

            rightPanel.Add(_customDetailRoot);

            _root.Add(rightPanel);

            return _root;
        }

        private void RefreshSymbolListUI()
        {
            _leftScrollView.Clear();
            _currentSelectedRow = null;

            // 1. Tool Recommended (Inner)
            var group1Header = new Label("工具Symbol");
            group1Header.style.unityFontStyleAndWeight = FontStyle.Bold;
            group1Header.style.paddingLeft = 5;
            group1Header.style.paddingTop = 8;
            group1Header.style.paddingBottom = 4;
            group1Header.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            _leftScrollView.Add(group1Header);

            foreach (var symbol in InnerSymbols.AllSymbols)
            {
                _leftScrollView.Add(CreateSymbolRow(symbol, true));
            }

            // 2. Project Symbols
            var defined = GetProjectDefinedSymbols();
            var projectSymbols = defined.Where(s => !InnerSymbols.AllSymbols.Contains(s)).ToList();

            var group2Header = new Label("工程Symbol");
            group2Header.style.unityFontStyleAndWeight = FontStyle.Bold;
            group2Header.style.paddingLeft = 5;
            group2Header.style.paddingTop = 8;
            group2Header.style.paddingBottom = 4;
            group2Header.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            _leftScrollView.Add(group2Header);

            if (projectSymbols.Count == 0)
            {
                var emptyLabel = new Label("(无其他自定义宏)");
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                emptyLabel.style.paddingLeft = 10;
                emptyLabel.style.paddingTop = 5;
                _leftScrollView.Add(emptyLabel);
            }
            else
            {
                foreach (var symbol in projectSymbols)
                {
                    _leftScrollView.Add(CreateSymbolRow(symbol, false));
                }
            }
        }

        private List<string> GetProjectDefinedSymbols()
        {
            var currentTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var definedString = PlayerSettings.GetScriptingDefineSymbols(currentTarget);

            if (string.IsNullOrEmpty(definedString)) return new List<string>();

            return definedString.Split(';')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();
        }

        private VisualElement CreateSymbolRow(string symbol, bool isInner)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = 28;
            row.style.paddingLeft = 10;
            // row.style.borderBottomWidth = 1;
            // row.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f); // Optional separator?

            row.RegisterCallback<ClickEvent>(evt => SelectRow(row, symbol));

            // Hover effect
            row.RegisterCallback<MouseEnterEvent>(evt =>
            {
                if (row != _currentSelectedRow) row.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            });
            row.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (row != _currentSelectedRow) row.style.backgroundColor = Color.clear;
            });

            // Status Icon
            bool isAdded = ScriptDefineSymbolSetter.IsSymbolAdded(symbol);
            var icon = new Label("●");
            icon.style.marginRight = 6;
            icon.style.fontSize = 10;
            icon.style.color = isAdded ? Color.green : new Color(0.4f, 0.4f, 0.4f);
            row.Add(icon);

            // Name
            var label = new Label(symbol);
            label.style.flexGrow = 1;
            row.Add(label);

            return row;
        }

        private void SelectRow(VisualElement row, string symbol)
        {
            if (_currentSelectedRow != null)
            {
                _currentSelectedRow.style.backgroundColor = Color.clear;
            }

            _currentSelectedRow = row;
            _currentSelectedRow.style.backgroundColor = new Color(0.24f, 0.37f, 0.58f); // Classic selection blue

            _selectedSymbol = symbol;
            _modeSwitchBtn.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f); // reset custom btn visual if any

            // Switch to normal mode
            _isCustomMode = false;
            UpdateViewVisibility();
        }

        private void SwitchToCustomMode()
        {
            // Clear list selection
            if (_currentSelectedRow != null)
            {
                _currentSelectedRow.style.backgroundColor = Color.clear;
                _currentSelectedRow = null;
                _selectedSymbol = null;
            }

            _modeSwitchBtn.style.backgroundColor = new Color(0.24f, 0.37f, 0.58f); // Highlight button
            _isCustomMode = true;
            UpdateViewVisibility();
        }

        // Re-use visibility logic
        private void UpdateViewVisibility()
        {
            if (_isCustomMode)
            {
                _normalDetailRoot.style.display = DisplayStyle.None;
                _customDetailRoot.style.display = DisplayStyle.Flex;

                // Refresh custom state
                CheckCustomSymbol(_customSymbolInput.value);
            }
            else
            {
                _customDetailRoot.style.display = DisplayStyle.None;

                if (!string.IsNullOrEmpty(_selectedSymbol))
                {
                    _normalDetailRoot.style.display = DisplayStyle.Flex;
                    UpdateNormalDetail();
                }
                else
                {
                    _normalDetailRoot.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateNormalDetail()
        {
            if (string.IsNullOrEmpty(_selectedSymbol)) return;

            _detailTitle.text = _selectedSymbol;

            if (InnerSymbols.SymbolDesc.TryGetValue(_selectedSymbol, out var desc))
            {
                _detailDesc.text = desc;
            }
            else
            {
                _detailDesc.text = "项目内定义的宏符号 (Project Symbol)";
            }

            bool isAdded = ScriptDefineSymbolSetter.IsSymbolAdded(_selectedSymbol);
            _detailStatus.text = isAdded ? "✓ 已启用" : "○ 未启用";
            _detailStatus.style.color = isAdded ? Color.green : new Color(0.8f, 0.4f, 0.4f);

            if (isAdded)
            {
                _actionButton.text = "➖ 移除符号宏";
                _actionButton.style.backgroundColor = new Color(0.7f, 0.2f, 0.2f);
            }
            else
            {
                _actionButton.text = "➕ 添加符号宏";
                _actionButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.3f);
            }
        }

        private void OnActionClicked()
        {
            if (string.IsNullOrEmpty(_selectedSymbol)) return;

            if (ScriptDefineSymbolSetter.IsSymbolAdded(_selectedSymbol))
                ScriptDefineSymbolSetter.RemoveSymbol(_selectedSymbol);
            else
                ScriptDefineSymbolSetter.AddSymbol(_selectedSymbol);

            // Refresh UI list as status changed and symbols list might have changed
            RefreshSymbolListUI();

            // Try to re-select current if it still exists (it might have been removed if it was a project symbol)
            // But if we just toggled it, it's either in Inner or Project.

            // If it was a Project Symbol and we REMOVED it, it disappears from Project List!
            // So we should handle that gracefully.
            if (_selectedSymbol != null)
            {
                // Find row corresponding to _selectedSymbol? 
                // Since we rebuilt the list, the visual elements are new. We need to re-find and highlight.
                // Iterating children of _leftScrollView
                foreach (var child in _leftScrollView.Children())
                {
                    // Warning: Fragile check by casting label text
                    // The child is a Row or a Header.
                    if (child.childCount > 1 && child[1] is Label label && label.text == _selectedSymbol)
                    {
                        SelectRow(child, _selectedSymbol);
                        return;
                    }
                }

                // If not found (removed project symbol), switch to default state
                _selectedSymbol = null;
                UpdateViewVisibility();
            }
        }

        private void CheckCustomSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                _customStatusLabel.text = "等待输入";
                _customStatusLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                _customActionButton.text = "添加/移除";
                _customActionButton.SetEnabled(false);
                _customActionButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                return;
            }

            bool isAdded = ScriptDefineSymbolSetter.IsSymbolAdded(symbol);
            _customStatusLabel.text = isAdded ? "✓ 已存在" : "○ 未定义";
            _customStatusLabel.style.color = isAdded ? Color.green : new Color(0.8f, 0.4f, 0.4f);

            _customActionButton.SetEnabled(true);
            if (isAdded)
            {
                _customActionButton.text = "➖ 移除自定义宏";
                _customActionButton.style.backgroundColor = new Color(0.7f, 0.2f, 0.2f);
            }
            else
            {
                _customActionButton.text = "➕ 添加自定义宏";
                _customActionButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.3f);
            }
        }

        private void OnCustomActionClicked()
        {
            var symbol = _customSymbolInput?.value;
            if (string.IsNullOrEmpty(symbol)) return;

            if (ScriptDefineSymbolSetter.IsSymbolAdded(symbol))
                ScriptDefineSymbolSetter.RemoveSymbol(symbol);
            else
                ScriptDefineSymbolSetter.AddSymbol(symbol);

            RefreshSymbolListUI();
            CheckCustomSymbol(symbol);
        }

        public void Refresh()
        {
            RefreshSymbolListUI();
            UpdateViewVisibility();
        }

        public void DrawIMGUI() { }
    }
}
