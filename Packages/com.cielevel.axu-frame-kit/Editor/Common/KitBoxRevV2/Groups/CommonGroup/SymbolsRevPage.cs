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
            _root.AddToClassList("symbol-page-root");

            // ================== Left Panel - Rounded Card ==================
            var leftPanel = new VisualElement();
            leftPanel.AddToClassList("symbol-list-panel");

            // ScrollView with container for list items
            _leftScrollView = new ScrollView();
            _leftScrollView.AddToClassList("symbol-list-container");
            _leftScrollView.style.flexGrow = 1;
            leftPanel.Add(_leftScrollView);

            RefreshSymbolListUI(); // Initialize data and UI

            // Footer with Custom Mode Button
            var footer = new VisualElement();
            footer.AddToClassList("symbol-list-footer");

            _modeSwitchBtn = new Button(SwitchToCustomMode);
            _modeSwitchBtn.text = "自定义符号";
            _modeSwitchBtn.AddToClassList("btn-secondary");
            _modeSwitchBtn.style.height = 34;
            footer.Add(_modeSwitchBtn);

            leftPanel.Add(footer);
            _root.Add(leftPanel);

            // ================== Right Panel ==================
            var rightPanel = new VisualElement();
            rightPanel.AddToClassList("symbol-detail-area");

            // 1. Normal Detail Root - Using Section Panel
            _normalDetailRoot = new VisualElement();
            _normalDetailRoot.style.display = DisplayStyle.None;

            // Main Section Panel
            var normalPanel = new VisualElement();
            normalPanel.AddToClassList("section-panel");

            // Section Header
            var normalHeader = new VisualElement();
            normalHeader.AddToClassList("section-header");
            _detailTitle = new Label("符号");
            _detailTitle.AddToClassList("section-title");
            normalHeader.Add(_detailTitle);
            normalPanel.Add(normalHeader);

            // Section Content
            var normalContent = new VisualElement();
            normalContent.AddToClassList("section-content");

            _detailDesc = new Label();
            _detailDesc.AddToClassList("symbol-detail-desc");
            _detailDesc.style.marginBottom = 16;
            normalContent.Add(_detailDesc);

            // Status Sub-Section
            var statusSection = new VisualElement();
            statusSection.AddToClassList("sub-section");

            var statusRow = new VisualElement();
            statusRow.AddToClassList("form-row");
            _detailStatus = new Label("-");
            _detailStatus.AddToClassList("text-bold");
            _detailStatus.AddToClassList("form-value");
            statusRow.Add(_detailStatus);
            statusSection.Add(statusRow);

            normalContent.Add(statusSection);

            // Action Button Row
            var buttonRow = new VisualElement();
            buttonRow.AddToClassList("button-row");
            _actionButton = new Button();
            _actionButton.AddToClassList("button-row-first");
            _actionButton.style.height = 36;
            _actionButton.style.minWidth = 140;
            _actionButton.clicked += OnActionClicked;
            buttonRow.Add(_actionButton);
            normalContent.Add(buttonRow);

            normalPanel.Add(normalContent);
            _normalDetailRoot.Add(normalPanel);
            rightPanel.Add(_normalDetailRoot);

            // 2. Custom Detail Root - Using Section Panel
            _customDetailRoot = new VisualElement();
            _customDetailRoot.style.display = DisplayStyle.None;

            // Main Section Panel
            var customPanel = new VisualElement();
            customPanel.AddToClassList("section-panel");

            // Section Header
            var customHeader = new VisualElement();
            customHeader.AddToClassList("section-header");
            var customTitle = new Label("自定义宏管理");
            customTitle.AddToClassList("section-title");
            customHeader.Add(customTitle);
            customPanel.Add(customHeader);

            // Section Content
            var customContent = new VisualElement();
            customContent.AddToClassList("section-content");

            // Info Box
            var infoBox = new VisualElement();
            infoBox.AddToClassList("info-box");
            var infoText = new Label("输入预编译宏符号名称即可查询状态并添加或移除，例如 MY_CUSTOM_DEFINE");
            infoText.AddToClassList("info-box-text");
            infoBox.Add(infoText);
            customContent.Add(infoBox);

            // Input Sub-Section
            var inputSection = new VisualElement();
            inputSection.AddToClassList("sub-section");

            var inputTitle = new Label("符号输入");
            inputTitle.AddToClassList("sub-section-title");
            inputSection.Add(inputTitle);

            var inputRow = new VisualElement();
            inputRow.AddToClassList("form-row");
            var labelInfo = new Label("符号名称");
            labelInfo.AddToClassList("form-label");
            inputRow.Add(labelInfo);
            _customSymbolInput = new TextField();
            _customSymbolInput.AddToClassList("form-value");
            _customSymbolInput.RegisterValueChangedCallback(evt => CheckCustomSymbol(evt.newValue));
            inputRow.Add(_customSymbolInput);
            inputSection.Add(inputRow);

            // Status Row
            var customStatusRow = new VisualElement();
            customStatusRow.AddToClassList("form-row");
            customStatusRow.style.marginTop = 8;
            var customStatusPrefix = new Label("状态");
            customStatusPrefix.AddToClassList("form-label");
            customStatusRow.Add(customStatusPrefix);
            _customStatusLabel = new Label("输入后显示状态");
            _customStatusLabel.AddToClassList("text-bold");
            _customStatusLabel.AddToClassList("form-value");
            customStatusRow.Add(_customStatusLabel);
            inputSection.Add(customStatusRow);

            customContent.Add(inputSection);

            // Action Button Row
            var customButtonRow = new VisualElement();
            customButtonRow.AddToClassList("button-row");
            _customActionButton = new Button();
            _customActionButton.AddToClassList("button-row-first");
            _customActionButton.text = "添加/移除";
            _customActionButton.style.height = 36;
            _customActionButton.style.minWidth = 140;
            _customActionButton.clicked += OnCustomActionClicked;
            _customActionButton.SetEnabled(false);
            customButtonRow.Add(_customActionButton);
            customContent.Add(customButtonRow);

            customPanel.Add(customContent);
            _customDetailRoot.Add(customPanel);
            rightPanel.Add(_customDetailRoot);

            _root.Add(rightPanel);

            return _root;
        }

        private void RefreshSymbolListUI()
        {
            _leftScrollView.Clear();
            _currentSelectedRow = null;

            // 1. Tool Recommended (Inner)
            var group1Header = new Label("内置符号");
            group1Header.AddToClassList("symbol-list-header");
            _leftScrollView.Add(group1Header);

            foreach (var symbol in InnerSymbols.AllSymbols)
            {
                _leftScrollView.Add(CreateSymbolRow(symbol, true));
            }

            // 2. Project Symbols
            var defined = GetProjectDefinedSymbols();
            var projectSymbols = defined.Where(s => !InnerSymbols.AllSymbols.Contains(s)).ToList();

            var group2Header = new Label("工程符号");
            group2Header.AddToClassList("symbol-list-header");
            _leftScrollView.Add(group2Header);

            if (projectSymbols.Count == 0)
            {
                var emptyLabel = new Label("无自定义宏");
                emptyLabel.AddToClassList("symbol-empty-label");
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

        // 状态文本颜色统一走 USS 类（跟随当前主题的 --kb-* 变量），不在 C# 内联硬编码色值
        private static void SetStatusClass(VisualElement element, string colorClass)
        {
            element.RemoveFromClassList("status-added");
            element.RemoveFromClassList("status-removed");
            element.RemoveFromClassList("text-secondary");
            element.AddToClassList(colorClass);
        }

        private VisualElement CreateSymbolRow(string symbol, bool isInner)
        {
            var row = new VisualElement();
            row.AddToClassList("symbol-row");

            row.RegisterCallback<ClickEvent>(evt => SelectRow(row, symbol));

            // Status Icon (UE5 orange when enabled)
            bool isAdded = ScriptDefineSymbolSetter.IsSymbolAdded(symbol);
            var icon = new Label("●");
            icon.AddToClassList("symbol-row-icon");
            if (isAdded) icon.AddToClassList("symbol-row-icon--active");
            row.Add(icon);

            // Name
            var label = new Label(symbol);
            label.AddToClassList("symbol-row-label");
            row.Add(label);

            return row;
        }

        private void SelectRow(VisualElement row, string symbol)
        {
            // Reset previous selection (remove selected class)
            if (_currentSelectedRow != null)
            {
                _currentSelectedRow.RemoveFromClassList("symbol-row--selected");
            }

            _currentSelectedRow = row;
            // Add selected class for UE5 Orange selection
            _currentSelectedRow.AddToClassList("symbol-row--selected");

            _selectedSymbol = symbol;

            // Reset custom mode button style
            _modeSwitchBtn.RemoveFromClassList("btn-primary");
            _modeSwitchBtn.AddToClassList("btn-secondary");

            // Switch to normal mode
            _isCustomMode = false;
            UpdateViewVisibility();
        }

        private void SwitchToCustomMode()
        {
            // Clear list selection
            if (_currentSelectedRow != null)
            {
                _currentSelectedRow.RemoveFromClassList("symbol-row--selected");
                _currentSelectedRow = null;
                _selectedSymbol = null;
            }

            // Highlight custom mode button with UE5 primary style
            _modeSwitchBtn.RemoveFromClassList("btn-secondary");
            _modeSwitchBtn.AddToClassList("btn-primary");

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
                _detailDesc.text = "工程内定义的宏符号";
            }

            bool isAdded = ScriptDefineSymbolSetter.IsSymbolAdded(_selectedSymbol);
            _detailStatus.text = isAdded ? "✓ 已启用" : "○ 未启用";
            SetStatusClass(_detailStatus, isAdded ? "status-added" : "status-removed");

            // Clear previous button classes
            _actionButton.RemoveFromClassList("btn-success");
            _actionButton.RemoveFromClassList("btn-danger");

            if (isAdded)
            {
                _actionButton.text = "移除符号宏";
                _actionButton.AddToClassList("btn-danger");
            }
            else
            {
                _actionButton.text = "添加符号宏";
                _actionButton.AddToClassList("btn-success");
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
            // Clear previous button classes
            _customActionButton.RemoveFromClassList("btn-success");
            _customActionButton.RemoveFromClassList("btn-danger");
            _customActionButton.RemoveFromClassList("btn-secondary");

            if (string.IsNullOrEmpty(symbol))
            {
                _customStatusLabel.text = "等待输入";
                SetStatusClass(_customStatusLabel, "text-secondary");
                _customActionButton.text = "添加/移除";
                _customActionButton.SetEnabled(false);
                _customActionButton.AddToClassList("btn-secondary");
                return;
            }

            bool isAdded = ScriptDefineSymbolSetter.IsSymbolAdded(symbol);
            _customStatusLabel.text = isAdded ? "✓ 已存在" : "○ 未定义";
            SetStatusClass(_customStatusLabel, isAdded ? "status-added" : "status-removed");

            _customActionButton.SetEnabled(true);
            if (isAdded)
            {
                _customActionButton.text = "移除自定义宏";
                _customActionButton.AddToClassList("btn-danger");
            }
            else
            {
                _customActionButton.text = "添加自定义宏";
                _customActionButton.AddToClassList("btn-success");
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

        // 本页无全局事件订阅，生命周期钩子留空
        public void OnEnter() { }
        public void OnExit() { }

        public void DrawIMGUI() { }
    }
}
