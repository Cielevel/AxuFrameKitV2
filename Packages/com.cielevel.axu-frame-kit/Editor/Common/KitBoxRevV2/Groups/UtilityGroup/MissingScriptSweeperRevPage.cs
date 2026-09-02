using System;
using System.Collections.Generic;
using System.Linq;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.UtilityGroup
{
    /// <summary>
    /// 缺脚本清扫器页面：扫描 Assets 下 Prefab/Scene 资产中的 Missing Script（业务逻辑在 MissingScriptScanner），
    /// 报告列表 + 明细列表两级展示，支持双击/按钮定位与一键清扫（确认后落盘并自动重扫）。
    /// </summary>
    public class MissingScriptSweeperRevPage : IKitPageRevV2
    {
        public string Title => "缺脚本清扫器";

        private IKitBoxRevV2 _kitBox;

        // —— 扫描状态（页面实例存活期间保留，切页不丢）——
        private readonly List<MissingScriptReport> _reports = new List<MissingScriptReport>();
        private MissingScriptScanSummary _lastSummary;
        private MissingScriptReport _selectedReport;
        private bool _busy;

        // —— UI 引用 ——
        private TextField _foldersField;
        private Toggle _includePrefabsToggle;
        private Toggle _includeScenesToggle;
        private Button _scanButton;
        private Label _statusLabel;
        private ListView _reportListView;
        private ListView _entryListView;
        private Label _entryPlaceholder;
        private Button _sweepButton;

        public void Init(IKitBoxRevV2 kitBox)
        {
            _kitBox = kitBox;
            // 本页无需全局事件订阅：数据仅在扫描/清扫时刷新
        }

        public VisualElement CreateContent()
        {
            var root = new VisualElement();
            root.AddToClassList("page-container");

            // === 配置区域 ===
            var configBox = new VisualElement();
            configBox.AddToClassList("config-box");
            root.Add(configBox);

            var configTitle = new Label("扫描范围");
            configTitle.AddToClassList("section-header");
            configTitle.style.marginTop = 0;
            configBox.Add(configTitle);

            // 目录行
            var folderRow = new VisualElement();
            folderRow.AddToClassList("row");
            folderRow.style.alignItems = Align.Center;
            configBox.Add(folderRow);

            _foldersField = new TextField("目录") { value = "Assets" };
            _foldersField.tooltip = "工程内相对目录（Assets 开头），可用 ; 分隔多个；Packages 下资产不参与扫描";
            _foldersField.style.flexGrow = 1;
            folderRow.Add(_foldersField);

            var pickBtn = new Button(OnPickFolder) { text = "选择目录" };
            pickBtn.AddToClassList("btn-secondary");
            pickBtn.style.marginLeft = 6;
            folderRow.Add(pickBtn);

            // 选项 + 扫描按钮行
            var optionsRow = new VisualElement();
            optionsRow.AddToClassList("row");
            optionsRow.style.alignItems = Align.Center;
            optionsRow.style.marginTop = 5;
            configBox.Add(optionsRow);

            _includePrefabsToggle = new Toggle("预制体") { value = true };
            _includePrefabsToggle.AddToClassList("option-toggle");
            optionsRow.Add(_includePrefabsToggle);

            _includeScenesToggle = new Toggle("场景") { value = true };
            _includeScenesToggle.AddToClassList("option-toggle");
            optionsRow.Add(_includeScenesToggle);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            optionsRow.Add(spacer);

            _scanButton = new Button(RunScan) { text = "开始扫描" };
            _scanButton.AddToClassList("btn-primary");
            optionsRow.Add(_scanButton);

            // === 状态行 ===
            _statusLabel = new Label("尚未扫描。设置范围后点击 [开始扫描]。");
            _statusLabel.AddToClassList("text-muted");
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            _statusLabel.style.marginBottom = 5;
            root.Add(_statusLabel);

            // === 结果区：资产报告列表 ===
            var reportHeader = new VisualElement();
            reportHeader.AddToClassList("row-space-between");
            reportHeader.AddToClassList("mb-5");
            root.Add(reportHeader);

            var reportTitle = new Label("发现报告");
            reportTitle.AddToClassList("text-bold");
            reportHeader.Add(reportTitle);

            var pingBtn = new Button(PingSelectedReport) { text = "定位" };
            pingBtn.AddToClassList("helper-text-btn");
            reportHeader.Add(pingBtn);

            _reportListView = new ListView(new List<MissingScriptReport>(), 30, MakeReportRow, BindReportRow);
            _reportListView.selectionType = UnityEngine.UIElements.SelectionType.Single;
            _reportListView.AddToClassList("asset-list");
            _reportListView.style.flexGrow = 1;
            _reportListView.selectionChanged += OnReportSelectionChanged;
            _reportListView.RegisterCallback<MouseDownEvent>(OnReportListMouseDown);
            root.Add(_reportListView);

            // === 结果区：缺失明细列表 ===
            var entryTitle = new Label("缺失明细");
            entryTitle.AddToClassList("text-bold");
            entryTitle.style.marginTop = 10;
            root.Add(entryTitle);

            _entryPlaceholder = new Label("（选择上方资产行查看明细）");
            _entryPlaceholder.AddToClassList("text-muted");
            root.Add(_entryPlaceholder);

            _entryListView = new ListView(new List<MissingScriptEntry>(), 26, MakeEntryRow, BindEntryRow);
            _entryListView.selectionType = UnityEngine.UIElements.SelectionType.None;
            _entryListView.AddToClassList("asset-list");
            // 覆盖 .asset-list 的 flexGrow/min-height：明细区固定高度，报告区占剩余空间
            _entryListView.style.flexGrow = 0;
            _entryListView.style.minHeight = 0;
            _entryListView.style.height = 140;
            _entryListView.style.display = DisplayStyle.None;
            root.Add(_entryListView);

            // === 底部：一键清扫 ===
            _sweepButton = new Button(StartSweep) { text = "一键清扫" };
            _sweepButton.AddToClassList("btn-danger");
            _sweepButton.AddToClassList("mt-10");
            _sweepButton.SetEnabled(false);
            root.Add(_sweepButton);

            // 页面实例可能保留有上次扫描结果，重建 UI 后恢复展示
            UpdateResultsUI();

            return root;
        }

        // ───────────────────── 扫描 ─────────────────────

        /// <summary>执行扫描：可取消进度条，取消则中断并保留已扫部分。</summary>
        private void RunScan()
        {
            if (_busy) return;

            var folders = ParseFolders(_foldersField != null ? _foldersField.value : null);
            if (folders.Count == 0 || !folders.Any(f => f == "Assets" || f.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                EditorUtility.DisplayDialog("提示", "没有有效的扫描目录（仅支持工程 Assets 下目录，Packages 不参与扫描）。", "确定");
                return;
            }

            bool includePrefabs = _includePrefabsToggle != null && _includePrefabsToggle.value;
            bool includeScenes = _includeScenesToggle != null && _includeScenesToggle.value;
            if (!includePrefabs && !includeScenes)
            {
                EditorUtility.DisplayDialog("提示", "请至少勾选一种资产类型（预制体 / 场景）。", "确定");
                return;
            }

            _busy = true;
            _scanButton.SetEnabled(false);
            _sweepButton.SetEnabled(false);

            try
            {
                bool cancelRequested = false;
                var reports = MissingScriptScanner.Scan(
                    folders, includePrefabs, includeScenes,
                    out var summary,
                    (cur, total, msg) =>
                    {
                        if (EditorUtility.DisplayCancelableProgressBar("缺脚本清扫器",
                                $"[{cur}/{total}] {msg}", total == 0 ? 0f : (float)cur / total))
                            cancelRequested = true;
                    },
                    () => cancelRequested);

                _reports.Clear();
                _reports.AddRange(reports);
                _lastSummary = summary;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _busy = false;
                if (_scanButton != null) _scanButton.SetEnabled(true);
            }

            UpdateResultsUI();
        }

        // ───────────────────── 清扫 ─────────────────────

        /// <summary>一键清扫：确认 → RemoveMissingScripts 落盘 → 自动重扫 → 结果弹窗。</summary>
        private void StartSweep()
        {
            if (_busy) return;

            int removableTotal = _reports.Sum(r => r.RemovableCount);
            if (removableTotal == 0) return;

            string message = $"确定要清扫 {_reports.Count} 个资产中的 {removableTotal} 处缺失脚本吗？\n\n" +
                             "该操作不可撤销，Prefab/场景修改将直接落盘保存。";
            if (!EditorUtility.DisplayDialog("确认一键清扫", message, "清扫", "取消")) return;

            _busy = true;
            MissingScriptRemoveSummary removeSummary;
            try
            {
                removeSummary = MissingScriptScanner.RemoveMissingScripts(_reports, (cur, total, msg) =>
                    EditorUtility.DisplayProgressBar("缺脚本清扫器",
                        $"[{cur}/{total}] {msg}", total == 0 ? 0f : (float)cur / total));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _busy = false;
            }

            // 自动重扫刷新结果（清扫器已清除对应缓存，重扫为真实状态）
            RunScan();

            EditorUtility.DisplayDialog("清扫结果",
                $"已处理 {removeSummary.ProcessedAssetCount} 个资产\n" +
                $"移除 {removeSummary.RemovedCount} 处缺失脚本\n" +
                $"跳过 {removeSummary.SkippedCount} 个资产（无可移除项）\n\n" +
                "已完成自动重扫，请查看最新结果。",
                "确定");
        }

        // ───────────────────── 列表行 ─────────────────────

        private VisualElement MakeReportRow()
        {
            var row = new VisualElement();
            row.AddToClassList("list-item-row");
            row.style.alignItems = Align.Center;

            var icon = new Label();
            icon.name = "icon";
            icon.style.width = 20;
            icon.style.marginRight = 5;
            icon.style.fontSize = 14;
            row.Add(icon);

            var path = new Label();
            path.name = "path";
            path.style.flexGrow = 1;
            path.style.overflow = Overflow.Hidden;
            path.style.whiteSpace = WhiteSpace.NoWrap;
            row.Add(path);

            var badge = new Label();
            badge.name = "badge";
            badge.style.marginLeft = 5;
            badge.AddToClassList("text-accent");
            badge.AddToClassList("text-bold");
            row.Add(badge);

            var note = new Label();
            note.name = "note";
            note.style.marginLeft = 5;
            note.AddToClassList("text-warn");
            row.Add(note);

            return row;
        }

        private void BindReportRow(VisualElement element, int index)
        {
            var reports = _reportListView.itemsSource as List<MissingScriptReport>;
            if (reports == null || index < 0 || index >= reports.Count) return;
            var report = reports[index];

            var icon = element.Q<Label>("icon");
            var path = element.Q<Label>("path");
            var badge = element.Q<Label>("badge");
            var note = element.Q<Label>("note");

            if (icon != null) icon.text = report.AssetType == MissingScriptScanner.TypeScene ? "🎬" : "🎮";
            if (path != null)
            {
                path.text = report.AssetPath;
                path.tooltip = report.AssetPath;
            }
            if (badge != null) badge.text = $"{report.Entries.Count} 处";
            if (note != null)
            {
                note.text = report.NestedCount > 0 ? $"({report.NestedCount} 处在嵌套实例)" : "";
                note.tooltip = report.NestedCount > 0 ? "嵌套 prefab 实例上的缺失组件由其源资产负责清理" : "";
            }
        }

        private VisualElement MakeEntryRow()
        {
            var row = new VisualElement();
            row.AddToClassList("list-item-row");
            row.style.alignItems = Align.Center;

            var path = new Label();
            path.name = "path";
            path.style.flexGrow = 1;
            path.style.overflow = Overflow.Hidden;
            path.style.whiteSpace = WhiteSpace.NoWrap;
            row.Add(path);

            var status = new Label();
            status.name = "status";
            status.style.marginLeft = 5;
            row.Add(status);

            return row;
        }

        private void BindEntryRow(VisualElement element, int index)
        {
            var entries = _entryListView.itemsSource as List<MissingScriptEntry>;
            if (entries == null || index < 0 || index >= entries.Count) return;
            var entry = entries[index];

            var path = element.Q<Label>("path");
            var status = element.Q<Label>("status");

            if (path != null)
            {
                path.text = $"{entry.GameObjectPath}  ·  组件 #{entry.ComponentIndex}";
                path.tooltip = entry.GameObjectPath;
            }
            if (status != null)
            {
                status.text = entry.Removable ? "✓ 可移除" : "嵌套实例";
                status.EnableInClassList("text-ok", entry.Removable);
                status.EnableInClassList("text-warn", !entry.Removable);
                status.tooltip = entry.Removable ? "归属当前资产，可一键移除" : $"归属源资产：{entry.OwningAssetPath}";
            }
        }

        // ───────────────────── 展示刷新 ─────────────────────

        private void UpdateResultsUI()
        {
            if (_reportListView == null) return;

            _reportListView.itemsSource = _reports;
            _reportListView.Rebuild();
            _selectedReport = null;
            UpdateEntryList();
            UpdateStatus();
            UpdateSweepButton();
        }

        private void UpdateEntryList()
        {
            if (_entryListView == null) return;

            if (_selectedReport == null)
            {
                _entryListView.itemsSource = new List<MissingScriptEntry>();
                _entryListView.Rebuild();
                _entryListView.style.display = DisplayStyle.None;
                _entryPlaceholder.style.display = DisplayStyle.Flex;
                return;
            }

            _entryListView.itemsSource = _selectedReport.Entries;
            _entryListView.Rebuild();
            _entryListView.style.display = DisplayStyle.Flex;
            _entryPlaceholder.style.display = DisplayStyle.None;
        }

        private void UpdateStatus()
        {
            if (_statusLabel == null) return;

            if (_lastSummary == null)
            {
                _statusLabel.text = "尚未扫描。设置范围后点击 [开始扫描]。";
                _statusLabel.EnableInClassList("text-ok", false);
                _statusLabel.EnableInClassList("text-muted", true);
                return;
            }

            int totalEntries = _reports.Sum(r => r.Entries.Count);

            if (_reports.Count == 0)
            {
                // 全干净（或取消后无发现）
                _statusLabel.text = _lastSummary.Cancelled ? "扫描已取消，未发现 Missing Script" : "未发现 Missing Script ✓";
                _statusLabel.EnableInClassList("text-ok", !_lastSummary.Cancelled);
                _statusLabel.EnableInClassList("text-muted", _lastSummary.Cancelled);
                return;
            }

            string cancelledMark = _lastSummary.Cancelled ? " · 已取消" : "";
            _statusLabel.text = $"扫描 {_lastSummary.ScannedCount} 个资产（缓存命中 {_lastSummary.CacheHitCount}）" +
                                $"· 耗时 {FormatElapsed(_lastSummary)} · 发现 {_reports.Count} 个资产 {totalEntries} 处{cancelledMark}";
            _statusLabel.EnableInClassList("text-ok", false);
            _statusLabel.EnableInClassList("text-muted", true);
        }

        private void UpdateSweepButton()
        {
            if (_sweepButton == null) return;

            int removableTotal = _reports.Sum(r => r.RemovableCount);
            _sweepButton.text = _reports.Count == 0
                ? "一键清扫"
                : $"一键清扫（{_reports.Count} 个资产 / {removableTotal} 处可移除）";
            _sweepButton.SetEnabled(removableTotal > 0 && !_busy);
        }

        private static string FormatElapsed(MissingScriptScanSummary summary)
        {
            return summary.ElapsedSeconds < 1d
                ? $"{summary.ElapsedSeconds * 1000d:0} ms"
                : $"{summary.ElapsedSeconds:0.##} s";
        }

        // ───────────────────── 交互 ─────────────────────

        /// <summary>选择目录：限工程 Assets 下，转为相对路径填入。</summary>
        private void OnPickFolder()
        {
            var picked = EditorUtility.OpenFolderPanel("选择扫描目录（限工程 Assets 下）", Application.dataPath, "");
            if (string.IsNullOrEmpty(picked)) return;
            picked = picked.Replace('\\', '/').TrimEnd('/');

            var assetsAbs = Application.dataPath.Replace('\\', '/');

            string rel;
            if (picked == assetsAbs)
            {
                rel = "Assets";
            }
            else if (picked.StartsWith(assetsAbs + "/", StringComparison.OrdinalIgnoreCase))
            {
                rel = "Assets/" + picked.Substring(assetsAbs.Length + 1);
            }
            else
            {
                EditorUtility.DisplayDialog("无效目录", "请选择工程 Assets 目录下的文件夹（Packages 与工程外目录不支持扫描）。", "确定");
                return;
            }

            _foldersField.value = rel;
        }

        private void OnReportSelectionChanged(IEnumerable<object> items)
        {
            _selectedReport = items?.FirstOrDefault() as MissingScriptReport;
            UpdateEntryList();
        }

        private void OnReportListMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount != 2) return;
            PingSelectedReport();
        }

        /// <summary>Project 窗口定位并选中当前报告资产。</summary>
        private void PingSelectedReport()
        {
            if (_selectedReport == null) return;

            var asset = AssetDatabase.LoadMainAssetAtPath(_selectedReport.AssetPath);
            if (asset == null) return;

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        /// <summary>解析目录输入（支持 ; 分隔多个，统一斜杠）。</summary>
        private static List<string> ParseFolders(string raw)
        {
            var folders = new List<string>();
            if (string.IsNullOrWhiteSpace(raw)) return folders;

            foreach (var part in raw.Split(';'))
            {
                var folder = part.Trim().Replace('\\', '/').TrimEnd('/');
                if (folder.Length > 0) folders.Add(folder);
            }
            return folders;
        }

        public void Refresh() { }

        public void DrawIMGUI() { }
    }
}
