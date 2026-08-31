using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// AxuTestRunner 测试看板窗口（纯 UI Toolkit，布局语言仿 AxuKitBox，样式 token 独立自持、
    /// 不引用 AxuFrameKit.Editor）。左侧为测试套件树（按程序集/类分组，TestRunnerApi.RetrieveTestList，
    /// 用例行带勾选框、程序集/命名空间/类各级头部带三态勾选框：点击批量勾选/取消其下当前可见用例，
    /// 全选=打勾、部分=填充态、无=空；勾选集合按 FullName 键控、跨树重渲染保留），右侧为最近报告的
    /// 结果树（状态/耗时/失败消息与堆栈可展开），顶栏为模式切换 + Run 全部 / Run 选定 + 过滤输入 +
    /// 最近报告路径（点击 reveal）。左右分栏经 TwoPaneSplitView 可拖拽调比例（viewDataKey 持久化）。
    /// 运行直接调用 <see cref="AxuTestRunService"/>（不经 HTTP）；"Run 选定"经 testNames 精确通道
    /// 只跑勾选用例（按当前模式执行，勾选用例不属于当前模式时被预检 0 匹配拦下）。
    /// </summary>
    public class AxuTestRunnerWindow : EditorWindow
    {
        private const string UssResourcePath = "Packages/com.cielevel.axu-test-framework/Editor/UI/AxuTestTokens.uss";

        private string _mode = "editor";
        private bool _running;

        private Button _runAllButton;
        private Button _runSelectedButton;
        private Button _editorModeButton;
        private Button _playModeButton;
        private TextField _filterField;
        private Label _statusLabel;
        private Button _reportPathButton;
        private Label _summaryLabel;
        private ScrollView _testsScroll;
        private ScrollView _resultsScroll;

        /// <summary>左侧树勾选的用例全名集合（"Run 选定"的数据源；键控 FullName，与渲染分离，
        /// 过滤输入触发的树重建与模式切换都不清空——跨模式的失效名单由服务层预检拦下）。</summary>
        private readonly HashSet<string> _selectedTestNames = new HashSet<string>();

        /// <summary>树内层级勾选框（程序集/命名空间/类头部）与其可见叶子清单；用于勾选变化后重算三态。
        /// 叶子清单是渲染时的"过滤后可见"集合，批量勾选因此只作用于当前所见。</summary>
        private readonly List<(Toggle toggle, List<string> leaves)> _treeChecks = new List<(Toggle, List<string>)>();

        /// <summary>树内用例行勾选框与其全名；用于层级勾选变化后回填叶子勾选态。</summary>
        private readonly List<(Toggle toggle, string leafName)> _leafChecks = new List<(Toggle, string)>();

        /// <summary>本域最近一次成功加载的报告路径（供 reveal 与 axu_test_report latest 兜底对照）。</summary>
        private string _loadedReportPath;
        private AxuTestReport _report;

        /// <summary>左侧树当前数据（用于窗口重开时保留过滤输入等状态）。</summary>
        private List<ITestAdaptor> _cachedAssemblyRoots = new List<ITestAdaptor>();

        /// <summary>菜单入口：AxuFrameKit/AxuTestRunner（与 AxuKitBox 同级，仅字符串同前缀，无代码依赖）。</summary>
        [MenuItem("AxuFrameKit/AxuTestRunner")]
        public static void Open()
        {
            var window = GetWindow<AxuTestRunnerWindow>();
            window.titleContent = new GUIContent("AxuTestRunner");
            window.minSize = new Vector2(900f, 560f);
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.AddToClassList("axu-theme-ue5");

            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssResourcePath);
            if (uss != null)
                rootVisualElement.styleSheets.Add(uss);

            BuildTopBar();
            BuildMainArea();
            BuildStatusBar();

            _ = RefreshTestsAsync();
            TryLoadLatestReport();
        }

        // ------------------------------------------------------------------ UI 构建

        private void BuildTopBar()
        {
            var topBar = new VisualElement { name = "ax-topbar" };
            topBar.AddToClassList("ax-topbar");

            _editorModeButton = new Button(() => SetMode("editor")) { text = "EditMode", name = "ax-mode-editor" };
            _editorModeButton.AddToClassList("ax-mode-button");
            _playModeButton = new Button(() => SetMode("playmode")) { text = "PlayMode", name = "ax-mode-play" };
            _playModeButton.AddToClassList("ax-mode-button");

            _runAllButton = new Button(() => RunTests(runSelectedOnly: false))
            {
                text = "Run 全部",
                name = "ax-run-all-button"
            };
            _runAllButton.AddToClassList("ax-run-button");

            _runSelectedButton = new Button(() => RunTests(runSelectedOnly: true))
            {
                name = "ax-run-selected-button"
            };
            _runSelectedButton.AddToClassList("ax-run-button");

            var refreshButton = new Button(() => _ = RefreshTestsAsync()) { text = "刷新列表", name = "ax-refresh-button" };
            refreshButton.AddToClassList("ax-secondary-button");

            _filterField = new TextField { name = "ax-filter-field" };
            _filterField.AddToClassList("ax-filter-field");
            _filterField.tooltip = "测试全名包含匹配（大小写不敏感），只影响左侧树的显示与\"Run 全部\"的 testName 过滤；\"Run 选定\"按勾选集合执行";
            _filterField.RegisterValueChangedCallback(_ => RenderTestsTree());

            _reportPathButton = new Button(RevealReport) { text = "（尚无报告）", name = "ax-report-path" };
            _reportPathButton.AddToClassList("ax-report-path");
            _reportPathButton.tooltip = "点击在 Finder 中显示最近一次报告文件";
            _reportPathButton.SetEnabled(false);

            topBar.Add(_editorModeButton);
            topBar.Add(_playModeButton);
            topBar.Add(_runAllButton);
            topBar.Add(_runSelectedButton);
            topBar.Add(refreshButton);
            topBar.Add(_filterField);
            topBar.Add(_reportPathButton);

            rootVisualElement.Add(topBar);
            SetMode(_mode);
            UpdateSelectionUi();
        }

        private void BuildMainArea()
        {
            var main = new VisualElement { name = "ax-main" };
            main.AddToClassList("ax-main");

            // 左右分栏用 TwoPaneSplitView：中间分隔条可拖拽调比例，viewDataKey 让比例跨窗口重开保留。
            // Orientation.Horizontal = 两面板水平排列（左右布局、竖直分隔线）；Vertical 则是上下堆叠。
            // 首子元素（左栏）为固定宽度侧。viewDataKey 换新避免还原竖排实验期存下的脏状态。
            var split = new TwoPaneSplitView(0, 380f, TwoPaneSplitViewOrientation.Horizontal)
            {
                name = "ax-split",
                viewDataKey = "ax-split-h"
            };
            split.AddToClassList("ax-split");

            // 左：测试套件树
            var testsPane = new VisualElement { name = "ax-tests-pane" };
            testsPane.AddToClassList("ax-pane");
            testsPane.style.minWidth = 220f;
            var testsHeader = new Label("测试套件（按程序集/类分组，勾选后可 Run 选定）");
            testsHeader.AddToClassList("ax-pane-header");
            _testsScroll = new ScrollView { name = "ax-tests-scroll", viewDataKey = "ax-tests-scroll" };
            _testsScroll.AddToClassList("ax-scroll");
            testsPane.Add(testsHeader);
            testsPane.Add(_testsScroll);

            // 右：结果树
            var resultsPane = new VisualElement { name = "ax-results-pane" };
            resultsPane.AddToClassList("ax-pane");
            resultsPane.AddToClassList("ax-pane-last");
            resultsPane.style.minWidth = 260f;
            var resultsHeader = new Label("最近报告结果");
            resultsHeader.AddToClassList("ax-pane-header");
            _summaryLabel = new Label("（尚无结果——先 Run 一次）");
            _summaryLabel.AddToClassList("ax-summary");
            _resultsScroll = new ScrollView { name = "ax-results-scroll", viewDataKey = "ax-results-scroll" };
            _resultsScroll.AddToClassList("ax-scroll");
            resultsPane.Add(resultsHeader);
            resultsPane.Add(_summaryLabel);
            resultsPane.Add(_resultsScroll);

            split.Add(testsPane);
            split.Add(resultsPane);
            main.Add(split);
            rootVisualElement.Add(main);
        }

        private void BuildStatusBar()
        {
            var bar = new VisualElement { name = "ax-statusbar" };
            bar.AddToClassList("ax-statusbar");
            _statusLabel = new Label("就绪");
            _statusLabel.AddToClassList("ax-status-text");
            bar.Add(_statusLabel);
            rootVisualElement.Add(bar);
        }

        private void SetMode(string mode)
        {
            _mode = mode;
            if (_editorModeButton == null || _playModeButton == null)
                return;
            _editorModeButton.AddToClassList("ax-mode-active");
            _playModeButton.RemoveFromClassList("ax-mode-active");
            if (mode != "editor")
            {
                _playModeButton.AddToClassList("ax-mode-active");
                _editorModeButton.RemoveFromClassList("ax-mode-active");
            }
            _ = RefreshTestsAsync();
        }

        private void RevealReport()
        {
            if (!string.IsNullOrEmpty(_loadedReportPath) && File.Exists(_loadedReportPath))
                EditorUtility.RevealInFinder(_loadedReportPath);
        }

        // ------------------------------------------------------------------ 左侧：测试列表

        private async Task RefreshTestsAsync()
        {
            if (_testsScroll == null)
                return;

            UpdateStatus($"正在获取 {( _mode == "playmode" ? "PlayMode" : "EditMode")} 测试列表…");
            var testMode = _mode == "playmode" ? TestMode.PlayMode : TestMode.EditMode;
            var tcs = new TaskCompletionSource<ITestAdaptor>();
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RetrieveTestList(testMode, root => tcs.TrySetResult(root));

            // TestRunnerApi 为无状态门面（TestListJob 只持有 provider 与回调，不回引实例），用后即毁防泄漏。
            UnityEngine.Object.DestroyImmediate(api);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(10000));
            if (completed != tcs.Task)
            {
                UpdateStatus("获取测试列表超时（10s）——编辑器可能正在编译，稍后点\"刷新列表\"重试。");
                return;
            }

            _cachedAssemblyRoots.Clear();
            var root = tcs.Task.Result;
            if (root != null && root.Children != null)
                _cachedAssemblyRoots.AddRange(root.Children);
            if (root != null && root.IsSuite && (root.Children == null || !root.Children.Any()))
                _cachedAssemblyRoots.Add(root);

            RenderTestsTree();
            UpdateStatus($"测试列表已刷新（{_mode}，{CountLeafTests()} 个用例）。");
        }

        private int CountLeafTests()
        {
            var count = 0;
            foreach (var assembly in _cachedAssemblyRoots)
                CountLeafTests(assembly, ref count);
            return count;
        }

        private void CountLeafTests(ITestAdaptor node, ref int count)
        {
            if (node == null)
                return;
            if (!node.HasChildren && !node.IsSuite)
            {
                count++;
                return;
            }
            if (node.Children == null)
                return;
            foreach (var child in node.Children)
                CountLeafTests(child, ref count);
        }

        /// <summary>渲染左侧套件树（应用过滤输入的包含匹配；程序集 → 类 → 用例）。</summary>
        private void RenderTestsTree()
        {
            if (_testsScroll == null)
                return;
            _testsScroll.Clear();
            _treeChecks.Clear();
            _leafChecks.Clear();

            var needle = (_filterField.value ?? "").Trim();
            var excludedNote = AxuTestAssemblyScanner.ForbiddenAssemblyPrefix;

            foreach (var assembly in _cachedAssemblyRoots)
            {
                var foldout = new Foldout { text = assembly.Name, value = true };
                foldout.AddToClassList("ax-group-foldout");
                var (hasContent, leaves) = RenderFixtureLevel(assembly, foldout, needle);
                if (!hasContent)
                    continue;
                AttachTreeCheck(foldout, leaves);
                _testsScroll.Add(foldout);
            }

            if (_testsScroll.childCount == 0)
            {
                var empty = new Label(string.IsNullOrEmpty(needle) ? "（无测试）" : $"（无匹配 \"{needle}\" 的测试）");
                empty.AddToClassList("ax-empty-note");
                _testsScroll.Add(empty);
            }

            var note = new Label($"安全过滤：程序集名以 {excludedNote} 开头的测试始终被排除（防 pipeline 自带测试毒化本地 API 鉴权）。");
            note.AddToClassList("ax-footnote");
            _testsScroll.Add(note);

            var note2 = new Label("勾选用例后可\"Run 选定\"（按当前模式执行，经精确全名通道）；点类/程序集级勾选框批量勾选其下当前可见用例——全选态点击=全不选，其余态点击=全选。");
            note2.AddToClassList("ax-footnote");
            _testsScroll.Add(note2);

            RefreshTreeCheckStates();
        }

        /// <summary>递归渲染套件层级；返回该子树是否有可见内容与其可见叶子全名清单（供父级三态勾选框批量操作）。</summary>
        private (bool hasContent, List<string> leafNames) RenderFixtureLevel(ITestAdaptor node, Foldout parent, string needle)
        {
            if (node.Children == null)
                return (false, new List<string>());

            var hasContent = false;
            var leafNames = new List<string>();
            foreach (var child in node.Children)
            {
                if (!child.HasChildren && !child.IsSuite)
                {
                    if (needle.Length > 0 &&
                        (child.FullName == null ||
                         child.FullName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0))
                    {
                        continue;
                    }

                    // 勾选集合按 FullName 键控，树重建（过滤输入/模式切换）时从集合回填勾选态
                    var fullName = child.FullName ?? child.Name;
                    var row = new VisualElement { name = "ax-test-row" };
                    row.AddToClassList("ax-test-row");
                    row.tooltip = child.FullName + (child.RunState == RunState.Explicit ? "\n[Explicit]" : "");

                    var checkbox = new Toggle { value = _selectedTestNames.Contains(fullName) };
                    checkbox.AddToClassList("ax-test-check");
                    checkbox.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue)
                            _selectedTestNames.Add(fullName);
                        else
                            _selectedTestNames.Remove(fullName);
                        RefreshTreeCheckStates();
                        UpdateSelectionUi();
                    });

                    var testLabel = new Label(child.Name) { name = "ax-test-row-label" };
                    testLabel.AddToClassList("ax-test-row-label");

                    row.Add(checkbox);
                    row.Add(testLabel);
                    parent.Add(row);
                    _leafChecks.Add((checkbox, fullName));
                    leafNames.Add(fullName);
                    hasContent = true;
                    continue;
                }

                var childFoldout = new Foldout { text = GetDisplayName(child), value = false };
                childFoldout.AddToClassList("ax-fixture-foldout");
                var (childOk, childLeaves) = RenderFixtureLevel(child, childFoldout, needle);
                if (childOk)
                {
                    AttachTreeCheck(childFoldout, childLeaves);
                    parent.Add(childFoldout);
                    hasContent = true;
                    leafNames.AddRange(childLeaves);
                }
            }
            return (hasContent, leafNames);
        }

        /// <summary>在 Foldout 头部行挂三态勾选框：点击批量勾选/取消其下当前可见用例
        /// （全选态点击=全不选，部分/全无态点击=全选）；三态展示由 <see cref="RefreshTreeCheckStates"/> 统一回填。
        /// 勾选框上拦截 PointerDown/Click 冒泡，避免点勾选框时连带折叠/展开 Foldout。</summary>
        private void AttachTreeCheck(Foldout foldout, List<string> leaves)
        {
            if (leaves.Count == 0)
                return;

            var header = foldout.Q(className: "unity-foldout__toggle");
            if (header == null)
                return;

            var checkbox = new Toggle();
            checkbox.AddToClassList("ax-tree-check");
            checkbox.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            checkbox.RegisterCallback<ClickEvent>(e => e.StopPropagation());
            checkbox.RegisterValueChangedCallback(_ =>
            {
                if (leaves.All(_selectedTestNames.Contains))
                {
                    foreach (var name in leaves)
                        _selectedTestNames.Remove(name);
                }
                else
                {
                    foreach (var name in leaves)
                        _selectedTestNames.Add(name);
                }
                RefreshTreeCheckStates();
                UpdateSelectionUi();
            });

            header.Add(checkbox);
            _treeChecks.Add((checkbox, leaves));
        }

        /// <summary>按勾选集合重算全树勾选态：叶子行回填真值；层级框回填三态
        /// （全选=勾、部分=mixed 填充态、无=空），全部经 SetValueWithoutNotify 防递归触发回调。</summary>
        private void RefreshTreeCheckStates()
        {
            foreach (var (toggle, leafName) in _leafChecks)
                toggle.SetValueWithoutNotify(_selectedTestNames.Contains(leafName));

            foreach (var (toggle, leaves) in _treeChecks)
            {
                var selected = leaves.Count(_selectedTestNames.Contains);
                var all = selected >= leaves.Count;
                toggle.SetValueWithoutNotify(all);
                toggle.EnableInClassList("ax-check-mixed", selected > 0 && !all);
            }
        }

        private static string GetDisplayName(ITestAdaptor node)
        {
            if (string.IsNullOrEmpty(node.FullName))
                return node.Name;
            var lastDot = node.FullName.LastIndexOf('.');
            return lastDot >= 0 && lastDot < node.FullName.Length - 1
                ? node.FullName.Substring(lastDot + 1)
                : node.FullName;
        }

        // ------------------------------------------------------------------ 运行

        /// <summary>同步勾选相关 UI：按钮文案带选中计数，无勾选或运行中时"Run 选定"禁用。</summary>
        private void UpdateSelectionUi()
        {
            if (_runSelectedButton == null)
                return;
            _runSelectedButton.text = $"Run 选定（{_selectedTestNames.Count}）";
            _runSelectedButton.SetEnabled(!_running && _selectedTestNames.Count > 0);
        }

        private async void RunTests(bool runSelectedOnly)
        {
            if (_running)
                return;

            // 选定运行：快照勾选集合（运行期间用户仍可改勾选，不影响本次请求）
            var selectedNames = runSelectedOnly ? _selectedTestNames.ToList() : null;
            if (runSelectedOnly && (selectedNames == null || selectedNames.Count == 0))
                return;

            _running = true;
            _runAllButton.SetEnabled(false);
            UpdateSelectionUi();
            UpdateStatus(runSelectedOnly
                ? $"正在运行 {_mode} 选定用例（{selectedNames.Count} 个）…"
                : $"正在运行 {_mode} 全部测试…");

            try
            {
                var request = new AxuTestRunRequest
                {
                    Mode = _mode,
                    Filter = runSelectedOnly ? "" : (_filterField.value ?? ""),
                    FilterType = "testName",
                    TestNames = selectedNames,
                    ClearConsole = true,
                    Report = true,
                    TimeoutMs = 600000
                };

                var outcome = await AxuTestRunService.StartRunAsync(request);

                AxuTestRunStatus finalStatus = null;
                if (outcome.Status == "playmode_running")
                {
                    UpdateStatus("PlayMode 运行中（等待状态文件 completed）…");
                    finalStatus = await PollStatusUntilCompletedAsync(900000);
                    if (finalStatus == null)
                    {
                        UpdateStatus("PlayMode 运行等待超时（15 分钟）——可经 axu_test_status 或状态文件继续观察。");
                        return;
                    }
                }

                ShowOutcome(outcome, finalStatus);
            }
            catch (Exception ex)
            {
                UpdateStatus("运行失败: " + ex.Message);
                Debug.LogException(ex);
            }
            finally
            {
                _running = false;
                _runAllButton.SetEnabled(true);
                UpdateSelectionUi();
                TryLoadLatestReport();
            }
        }

        /// <summary>轮询运行状态文件直至 completed/error（步长 500ms，上限 15 分钟）。</summary>
        private static async Task<AxuTestRunStatus> PollStatusUntilCompletedAsync(int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                var status = AxuTestRunStatus.TryRead();
                if (status != null && status.Status != AxuTestRunStatus.Running)
                    return status;
                await Task.Delay(500);
            }
            return null;
        }

        /// <summary>渲染运行结果。PlayMode 异步段（StartRunAsync 返回 playmode_running）以轮询到的
        /// 最终状态文件渲染成功/失败，数据与 axu_test_status 的终值对齐。</summary>
        private void ShowOutcome(AxuTestRunOutcome outcome, AxuTestRunStatus finalStatus = null)
        {
            // PlayMode 异步段：StartRunAsync 返回时 outcome 停留在 playmode_running（Summary 为空），
            // 原样落入错误分支会把成功运行显示为"错误: playmode_running"——改用轮询终值渲染。
            if (outcome.Status == "playmode_running")
            {
                if (finalStatus == null)
                {
                    UpdateStatus("PlayMode 运行已启动（未见终态状态文件）。");
                    return;
                }
                outcome = MapFinalStatus(finalStatus);
            }

            if (outcome.Status == "completed")
            {
                var summary = outcome.Summary;
                if (summary != null)
                {
                    UpdateStatus($"完成: {summary.Passed}/{summary.Total} 通过, {summary.Failed} 失败, " +
                                 $"{summary.Skipped} 跳过" + (outcome.AllPassed ? " —— 全绿" : " —— 存在失败"));
                }
                else
                {
                    UpdateStatus(outcome.Message ?? "完成。");
                }
            }
            else if (outcome.Status == "timeout")
            {
                UpdateStatus("超时: " + outcome.Error);
            }
            else
            {
                UpdateStatus("错误: " + (outcome.Error ?? outcome.Status));
            }
        }

        /// <summary>把轮询到的最终状态文件映射为结果模型（与 AxuTestRunService 对 EditMode 的
        /// completed/error 映射对齐），供 PlayMode 异步段收尾时渲染。</summary>
        private static AxuTestRunOutcome MapFinalStatus(AxuTestRunStatus finalStatus)
        {
            if (finalStatus.Status == AxuTestRunStatus.Error)
            {
                return new AxuTestRunOutcome
                {
                    Success = false,
                    Status = "error",
                    Error = finalStatus.Message ?? "运行结束但报告流程出错，详见 Editor Console。"
                };
            }

            return new AxuTestRunOutcome
            {
                Success = true,
                Status = "completed",
                Summary = finalStatus.Summary,
                ReportPath = finalStatus.ReportPath,
                AllPassed = finalStatus.Summary != null && finalStatus.Summary.Total > 0 &&
                            finalStatus.Summary.Failed == 0 && finalStatus.Summary.Inconclusive == 0,
                Message = finalStatus.Summary == null ? "运行完成（无汇总数据）。" : null
            };
        }

        // ------------------------------------------------------------------ 右侧：报告结果

        /// <summary>加载最近可用的报告（优先本域 LastReportPath，其次报告目录里最新文件）。</summary>
        private void TryLoadLatestReport()
        {
            string path = null;

            if (AxuTestReportWriter.LastReportPath != null && File.Exists(AxuTestReportWriter.LastReportPath))
            {
                path = AxuTestReportWriter.LastReportPath;
            }
            else
            {
                var directory = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.ReportsDirectory);
                if (Directory.Exists(directory))
                {
                    var newest = new DirectoryInfo(directory)
                        .EnumerateFiles("*.json")
                        .OrderByDescending(f => f.LastWriteTimeUtc)
                        .FirstOrDefault();
                    path = newest?.FullName;
                }
            }

            LoadReport(path);
        }

        /// <summary>读报告 JSON 并渲染结果树；读不到时保留现状提示。</summary>
        private void LoadReport(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            try
            {
                _report = JsonConvert.DeserializeObject<AxuTestReport>(File.ReadAllText(path));
                _loadedReportPath = path;
            }
            catch (Exception ex)
            {
                UpdateStatus("报告解析失败: " + ex.Message);
                return;
            }

            _reportPathButton.text = Path.GetFileName(_loadedReportPath);
            _reportPathButton.SetEnabled(true);
            _reportPathButton.tooltip = _loadedReportPath;
            RenderResultsTree();
        }

        /// <summary>渲染右侧结果树（按程序集分组；每条用例可展开查看消息与堆栈）。</summary>
        private void RenderResultsTree()
        {
            _resultsScroll.Clear();

            if (_report?.Summary != null)
            {
                _summaryLabel.text =
                    $"[{_report.Mode}] {_report.Summary.Passed}/{_report.Summary.Total} 通过 · " +
                    $"{_report.Summary.Failed} 失败 · {_report.Summary.Skipped} 跳过 · " +
                    $"console 错误 {_report.Console?.ErrorCount ?? 0} 条";
            }

            var results = _report?.Results;
            if (results == null || results.Count == 0)
            {
                var empty = new Label("（报告无用例明细）");
                empty.AddToClassList("ax-empty-note");
                _resultsScroll.Add(empty);
                return;
            }

            foreach (var group in results.GroupBy(r => r.Assembly ?? "(未知程序集)").OrderBy(g => g.Key))
            {
                var foldout = new Foldout { text = $"{group.Key}（{group.Count()}）", value = group.Any(IsFailed) };
                foldout.AddToClassList("ax-group-foldout");

                foreach (var result in group.OrderByDescending(r => IsFailed(r)).ThenBy(r => r.FullName))
                    foldout.Add(CreateResultRow(result));

                _resultsScroll.Add(foldout);
            }
        }

        /// <summary>单条结果行：可展开 Foldout，头部为状态 + 名称 + 耗时，内容为消息与堆栈。</summary>
        private VisualElement CreateResultRow(ReportResult result)
        {
            var failed = IsFailed(result);
            var skipped = result.Status != null && result.Status.StartsWith("Skipped", StringComparison.OrdinalIgnoreCase);
            var header = $"{(failed ? "FAIL" : skipped ? "SKIP" : "PASS")}  {result.FullName}  ({result.DurationMs:0.#} ms)";

            var row = new Foldout { text = header, value = failed };
            row.AddToClassList("ax-result-row");
            row.AddToClassList(failed ? "ax-row-fail" : skipped ? "ax-row-skip" : "ax-row-pass");

            if (!string.IsNullOrEmpty(result.Message))
            {
                var messageLabel = new Label(result.Message) { name = "ax-result-message" };
                messageLabel.AddToClassList("ax-result-detail");
                row.Add(messageLabel);
            }
            if (!string.IsNullOrEmpty(result.StackTrace))
            {
                var stackLabel = new Label(result.StackTrace) { name = "ax-result-stack" };
                stackLabel.AddToClassList("ax-result-detail");
                row.Add(stackLabel);
            }
            if (result.Categories != null && result.Categories.Count > 0)
            {
                var categoriesLabel = new Label("Categories: " + string.Join(", ", result.Categories));
                categoriesLabel.AddToClassList("ax-result-detail");
                row.Add(categoriesLabel);
            }
            return row;
        }

        /// <summary>是否未通过：非 Passed 且非 Skipped 的终态（Failed/Error/Cancelled/Inconclusive）
        /// 都按未通过着色，防止 Error 之类的非绿色行被归入通过。</summary>
        private static bool IsFailed(ReportResult result)
        {
            if (result.Status == null)
                return false;
            if (result.Status.StartsWith("Passed", StringComparison.OrdinalIgnoreCase))
                return false;
            if (result.Status.StartsWith("Skipped", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        private void UpdateStatus(string message)
        {
            if (_statusLabel != null)
                _statusLabel.text = message;
        }
    }
}
