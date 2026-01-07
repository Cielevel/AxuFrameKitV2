using System.Collections.Generic;
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

        private VisualElement _root;
        private ListView _symbolListView;
        private VisualElement _detailArea;
        private Label _detailTitle;
        private Label _detailDesc;
        private Label _detailStatus;
        private Button _actionButton;

        private string _selectedSymbol;

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

            // --- Left: ListView ---
            var leftPanel = new VisualElement();
            leftPanel.AddToClassList("symbol-list-panel");

            var listHeader = new Label("可用宏定义");
            listHeader.AddToClassList("symbol-list-header");
            leftPanel.Add(listHeader);

            _symbolListView = new ListView(InnerSymbols.AllSymbols, 32, MakeItem, BindItem);
            _symbolListView.selectionType = SelectionType.Single;
            _symbolListView.selectionChanged += OnSymbolSelected;
            _symbolListView.style.flexGrow = 1;
            leftPanel.Add(_symbolListView);

            _root.Add(leftPanel);

            // --- Right: Details ---
            _detailArea = new VisualElement();
            _detailArea.AddToClassList("symbol-detail-area");

            _detailTitle = new Label("请选择符号宏");
            _detailTitle.AddToClassList("symbol-detail-title");
            _detailArea.Add(_detailTitle);

            _detailDesc = new Label();
            _detailDesc.AddToClassList("symbol-detail-desc");
            _detailArea.Add(_detailDesc);

            var statusRow = new VisualElement();
            statusRow.AddToClassList("symbol-status-row");

            statusRow.Add(new Label("当前状态: "));
            _detailStatus = new Label("-");
            _detailStatus.style.unityFontStyleAndWeight = FontStyle.Bold;
            _detailStatus.style.marginLeft = 5;
            statusRow.Add(_detailStatus);
            _detailArea.Add(statusRow);

            _actionButton = new Button();
            _actionButton.AddToClassList("btn-danger");
            _actionButton.clicked += OnActionClicked;
            _detailArea.Add(_actionButton);

            _detailArea.style.display = DisplayStyle.None;
            _root.Add(_detailArea);

            return _root;
        }

        private VisualElement MakeItem()
        {
            var box = new VisualElement();
            box.style.flexDirection = FlexDirection.Row;
            box.style.alignItems = Align.Center;
            box.style.paddingLeft = 10;

            var icon = new Label("●");
            icon.name = "status-icon";
            icon.style.marginRight = 5;
            box.Add(icon);

            var label = new Label();
            label.name = "symbol-name";
            box.Add(label);

            return box;
        }

        private void BindItem(VisualElement element, int index)
        {
            var symbol = InnerSymbols.AllSymbols[index];
            element.Q<Label>("symbol-name").text = symbol;

            bool isAdded = ScriptDefineSymbolSetter.IsSymbolAdded(symbol);
            var icon = element.Q<Label>("status-icon");
            icon.style.color = isAdded ? Color.green : new Color(0.4f, 0.4f, 0.4f);
        }

        private void OnSymbolSelected(IEnumerable<object> selection)
        {
            foreach (var item in selection)
            {
                _selectedSymbol = item as string;
                UpdateDetail();
                break;
            }
        }

        private void UpdateDetail()
        {
            if (string.IsNullOrEmpty(_selectedSymbol))
            {
                _detailArea.style.display = DisplayStyle.None;
                return;
            }

            _detailArea.style.display = DisplayStyle.Flex;
            _detailTitle.text = _selectedSymbol;
            _detailDesc.text = InnerSymbols.SymbolDesc.TryGetValue(_selectedSymbol, out var desc) ? desc : "暂无描述。";

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

            UpdateDetail();
            _symbolListView.RefreshItems();
        }

        public void Refresh()
        {
            _symbolListView?.RefreshItems();
            UpdateDetail();
        }

        public void DrawIMGUI() { }
    }
}
