using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using AxuFrameKit.Common.CustomAttribute;
using AxuFrameKit.Common.Editor.UI;

namespace AxuFrameKit.Common.Editor.Window
{
    /// <summary>
    /// 通用 ScriptableObject 资产创建窗口：
    /// 左栏按 CreatorEntryAttribute 分组的类型树（未标注类型按所属程序集归入"未分组"），
    /// 右栏展示类型详情（InspectorTitle/Intro meta、字段预览、已有资产）并一键创建资产。
    /// 类型收集与过滤逻辑在 <see cref="ScriptableObjectCreatorScanner"/>，供单元测试复用。
    /// 分组折叠状态经 EditorPrefs 跨开窗持久化（<see cref="ScriptableObjectCreatorFoldoutState"/>，收起集合语义，未记录路径默认展开）。
    /// </summary>
    public class ScriptableObjectCreatorWindow : EditorWindow
    {
        // ───────────────────── 常量与静态状态 ─────────────────────

        /// <summary>右栏"已有资产"列表最多展示的行数（超出只提示总数）。</summary>
        private const int MaxAssetRows = 50;

        /// <summary>左栏分组折叠状态在 EditorPrefs 中的键（收起集合，见 <see cref="ScriptableObjectCreatorFoldoutState"/>）。</summary>
        private const string FoldoutStatePrefsKey = "AxuKit.ScriptableObjectCreator.FoldoutState";

        /// <summary>
        /// Project 右键入口传入的初始目录；由 CreateGUI 消费后清空。
        /// </summary>
        internal static string PendingFolder;

        // ───────────────────── 运行时状态 ─────────────────────

        private List<ScriptableObjectCreatorScanner.Entry> _allEntries = new List<ScriptableObjectCreatorScanner.Entry>();
        private List<ScriptableObjectCreatorScanner.Entry> _filteredEntries = new List<ScriptableObjectCreatorScanner.Entry>();
        private ScriptableObjectCreatorScanner.Entry _selected;
        private string _initialFolder;

        // UI refs
        private TextField _searchField;
        private Toggle _frameworkOnlyToggle;
        private Label _countLabel;
        private ScrollView _typeListScrollView;
        private VisualElement _detailPanel;
        private readonly Dictionary<string, Foldout> _foldoutCache = new Dictionary<string, Foldout>();

        /// <summary>被收起的分组路径集合（累计路径段间以 "/" 连接）；未记录路径默认展开。</summary>
        private HashSet<string> _collapsedGroups = new HashSet<string>();

        // ───────────────────── 菜单 ─────────────────────

        [MenuItem("AxuFrameKit/Asset/ScriptableObject Creator")]
        public static void ShowWindow()
        {
            var window = GetWindow<ScriptableObjectCreatorWindow>("ScriptableObject Creator");
            window.minSize = new Vector2(640, 420);
        }

        [MenuItem("Assets/Create/AxuFrameKit/ScriptableObject Creator", true, 100)]
        private static bool ValidateOpenFromSelection()
        {
            if (Selection.activeObject == null) return false;
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) && path.StartsWith("Assets/");
        }

        [MenuItem("Assets/Create/AxuFrameKit/ScriptableObject Creator", false, 100)]
        private static void OpenFromSelection()
        {
            // 文件夹用自身路径、文件用父目录，作为创建面板的初始目录
            PendingFolder = ResolveSelectionFolder();

            var window = GetWindow<ScriptableObjectCreatorWindow>("ScriptableObject Creator");
            window.minSize = new Vector2(640, 420);
            // 窗口已打开时 CreateGUI 不会再次执行，直接让当前实例消费初始目录
            window.ConsumePendingFolder();
        }

        // ───────────────────── GUI 构建 ─────────────────────

        private void OnEnable()
        {
            // EditorWindow 不可序列化字段（HashSet）在域重载后会置空，OnEnable 重跑即从 EditorPrefs 恢复折叠状态
            _collapsedGroups = ScriptableObjectCreatorFoldoutState.Load(FoldoutStatePrefsKey);
        }

        private void CreateGUI()
        {
            ConsumePendingFolder();

            // Load tokens (AxuKit theme) + shared component styles + local styles;
            // token 表由 AxuKitTheme 集中加载（规范化路径，勿用脚本目录拼 "../"）
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var dirPath = System.IO.Path.GetDirectoryName(scriptPath);
            AxuKitTheme.AttachTokens(rootVisualElement);

            var sharedStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dirPath}/CommonWindowStyles.uss");
            if (sharedStyles != null)
                rootVisualElement.styleSheets.Add(sharedStyles);
            var localStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dirPath}/ScriptableObjectCreatorWindow.uss");
            if (localStyles != null)
                rootVisualElement.styleSheets.Add(localStyles);

            rootVisualElement.AddToClassList("ue5-root");
            AxuKitTheme.Apply(rootVisualElement, AxuKitTheme.ResolveSavedClass());

            // ── Toolbar ──
            BuildToolbar();

            // ── Main split layout ──
            var mainContainer = new VisualElement();
            mainContainer.AddToClassList("soc-main-container");
            rootVisualElement.Add(mainContainer);

            BuildLeftPanel(mainContainer);
            BuildRightPanel(mainContainer);

            // Initial populate
            Rescan();
        }

        private void ConsumePendingFolder()
        {
            if (string.IsNullOrEmpty(PendingFolder)) return;
            _initialFolder = PendingFolder;
            PendingFolder = null;
        }

        private void BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar-panel");
            rootVisualElement.Add(toolbar);

            // Search field
            var searchLabel = new Label("搜索");
            searchLabel.AddToClassList("toolbar-label");
            toolbar.Add(searchLabel);

            _searchField = new TextField();
            _searchField.AddToClassList("ue5-text-field");
            _searchField.RegisterValueChangedCallback(evt => ApplyFilter(evt.newValue));
            toolbar.Add(_searchField);

            // Framework-only toggle（切换时重新扫描）
            _frameworkOnlyToggle = new Toggle("仅框架类型") { value = false };
            _frameworkOnlyToggle.AddToClassList("soc-toolbar-toggle");
            _frameworkOnlyToggle.tooltip = "仅显示 CustomScriptableObject 的派生类";
            _frameworkOnlyToggle.RegisterValueChangedCallback(_ => Rescan());
            toolbar.Add(_frameworkOnlyToggle);

            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            // Refresh button（重扫并清选中）
            var refreshBtn = new Button(OnRefreshClicked) { text = "刷新" };
            refreshBtn.AddToClassList("action-btn");
            toolbar.Add(refreshBtn);

            // Count badge（当前列表条数）
            _countLabel = new Label("0");
            _countLabel.AddToClassList("badge");
            toolbar.Add(_countLabel);
        }

        private void BuildLeftPanel(VisualElement parent)
        {
            var leftPanel = new VisualElement();
            leftPanel.AddToClassList("soc-left-panel");
            parent.Add(leftPanel);

            var container = new VisualElement();
            container.AddToClassList("group-container");
            container.style.flexGrow = 1;
            leftPanel.Add(container);

            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("可创建类型");
            title.AddToClassList("group-title");
            header.Add(title);

            // Scroll content
            var content = new VisualElement();
            content.AddToClassList("group-content");
            content.style.flexGrow = 1;
            container.Add(content);

            _typeListScrollView = new ScrollView(ScrollViewMode.Vertical);
            _typeListScrollView.AddToClassList("content-scroll");
            content.Add(_typeListScrollView);
        }

        private void BuildRightPanel(VisualElement parent)
        {
            var rightPanel = new VisualElement();
            rightPanel.AddToClassList("soc-right-panel");
            parent.Add(rightPanel);

            // Use a ScrollView so long field/asset lists don't overflow
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("content-scroll");
            rightPanel.Add(scroll);

            _detailPanel = new VisualElement();
            _detailPanel.style.flexGrow = 1;
            scroll.Add(_detailPanel);

            RefreshDetailPanel();
        }

        // ───────────────────── Scan / Filter ─────────────────────

        private void Rescan()
        {
            var frameworkOnly = _frameworkOnlyToggle != null && _frameworkOnlyToggle.value;
            _allEntries = ScriptableObjectCreatorScanner.Scan(frameworkOnly);

            // 选中类型不在新扫描结果中时清空右栏
            if (_selected != null && _allEntries.All(entry => entry.Type != _selected.Type))
            {
                _selected = null;
                RefreshDetailPanel();
            }

            ApplyFilter(_searchField?.value ?? string.Empty);
        }

        private void ApplyFilter(string search)
        {
            _filteredEntries = ScriptableObjectCreatorScanner.Filter(_allEntries, search);
            RefreshTypeList();
        }

        // ───────────────────── Type List（左栏分组树） ─────────────────────

        private void RefreshTypeList()
        {
            if (_typeListScrollView == null) return;
            _typeListScrollView.Clear();
            _foldoutCache.Clear();

            if (_countLabel != null)
                _countLabel.text = _filteredEntries.Count.ToString();

            if (_filteredEntries.Count == 0)
            {
                var emptyState = new VisualElement();
                emptyState.AddToClassList("empty-state");
                var emptyText = new Label("未找到匹配类型");
                emptyText.AddToClassList("empty-state-text");
                emptyState.Add(emptyText);
                _typeListScrollView.Add(emptyState);
                return;
            }

            // 按分组路径逐段建/复用 Foldout；CreatorEntry 单段重命名的条目放顶级，
            // 未标注条目经"未分组/程序集名"两段路径由通用嵌套逻辑渲染（"未分组"段共享复用）
            foreach (var entry in _filteredEntries)
            {
                var host = ResolveGroupHost(entry.GroupPath);
                host.Add(CreateTypeRow(entry));
            }

            HighlightSelection();
        }

        private VisualElement ResolveGroupHost(string[] groupPath)
        {
            // 空路径：CreatorEntry 单段重命名（如"道具"），行直接放顶级
            if (groupPath == null || groupPath.Length == 0)
                return _typeListScrollView;

            VisualElement parent = _typeListScrollView;
            Foldout foldout = null;
            var accumulated = string.Empty;
            foreach (var segment in groupPath)
            {
                accumulated = accumulated.Length == 0 ? segment : accumulated + "/" + segment;
                if (!_foldoutCache.TryGetValue(accumulated, out foldout))
                {
                    // 折叠状态持久化：收起集合中记录过的路径收起，未记录路径默认展开。
                    // accumulated 声明在循环外，回调捕获须用本次迭代的快照，否则所有 Foldout 共享末段路径；
                    // evt.target 须为折叠自身——子 Foldout 的变更事件会冒泡到祖先回调，不拦截会把
                    // 祖先路径错误写入收起集合并持久化
                    var capturedPath = accumulated;
                    var capturedFoldout = new Foldout { text = segment, value = !_collapsedGroups.Contains(capturedPath) };
                    capturedFoldout.AddToClassList("soc-group-foldout");
                    capturedFoldout.RegisterValueChangedCallback(evt =>
                    {
                        if (!ReferenceEquals(evt.target, capturedFoldout)) return;
                        ToggleFoldout(capturedPath, evt.newValue);
                    });
                    foldout = capturedFoldout;
                    parent.Add(foldout);
                    _foldoutCache[accumulated] = foldout;
                }
                parent = foldout;
            }
            return foldout;
        }

        /// <summary>
        /// 分组折叠切换：更新收起集合并即时写回 EditorPrefs（展开=移出集合，收起=加入集合）。
        /// 折叠树重建（搜索过滤/开关切换/手动刷新均走本路径）后按同一集合恢复，无需额外处理。
        /// </summary>
        private void ToggleFoldout(string path, bool expanded)
        {
            if (expanded)
                _collapsedGroups.Remove(path);
            else
                _collapsedGroups.Add(path);

            ScriptableObjectCreatorFoldoutState.Save(FoldoutStatePrefsKey, _collapsedGroups);
        }

        // 缓存 "cs Script Icon"，避免每行构建/每次详情重建都调 IconContent
        private static Texture2D _csScriptIcon;
        private static Texture2D CsScriptIcon => _csScriptIcon != null
            ? _csScriptIcon
            : (_csScriptIcon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D);

        private VisualElement CreateTypeRow(ScriptableObjectCreatorScanner.Entry entry)
        {
            var row = new VisualElement();
            row.AddToClassList("item-row");
            row.AddToClassList("soc-type-row");
            row.userData = entry; // 高亮/选中均以 userData 比对

            // Icon
            var icon = new VisualElement();
            icon.AddToClassList("item-icon");
            var tex = CsScriptIcon;
            if (tex != null) icon.style.backgroundImage = new StyleBackground(tex);
            row.Add(icon);

            // Display name
            var nameLabel = new Label(entry.DisplayName);
            nameLabel.AddToClassList("item-name");
            row.Add(nameLabel);

            // Namespace badge (short)
            if (!string.IsNullOrEmpty(entry.Type.Namespace))
            {
                var nsBadge = new Label(GetShortNamespace(entry.Type.Namespace));
                nsBadge.AddToClassList("soc-ns-badge");
                row.Add(nsBadge);
            }

            // 整行可点：选中条目查看详情
            row.AddManipulator(new Clickable(() => OnEntrySelected(entry)));

            return row;
        }

        private static string GetShortNamespace(string ns)
        {
            if (string.IsNullOrEmpty(ns)) return string.Empty;
            var parts = ns.Split('.');
            return parts.Length > 2 ? $"…{parts[^1]}" : ns;
        }

        private void OnEntrySelected(ScriptableObjectCreatorScanner.Entry entry)
        {
            _selected = entry;
            RefreshDetailPanel();
            HighlightSelection();
        }

        private void HighlightSelection()
        {
            if (_typeListScrollView == null) return;
            _typeListScrollView.Query<VisualElement>(className: "soc-type-row").ForEach(row =>
            {
                var isSelected = ReferenceEquals(row.userData, _selected);
                row.EnableInClassList("soc-item-selected", isSelected);
            });
        }

        // ───────────────────── Detail Panel（右栏） ─────────────────────

        private void RefreshDetailPanel()
        {
            if (_detailPanel == null) return;
            _detailPanel.Clear();

            if (_selected == null)
            {
                BuildEmptyDetail();
                return;
            }

            BuildEntryDetail(_selected);
        }

        private void BuildEmptyDetail()
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            container.style.flexGrow = 1;

            var header = new VisualElement();
            header.AddToClassList("group-header");
            var headerTitle = new Label("类型详情");
            headerTitle.AddToClassList("group-title");
            header.Add(headerTitle);
            container.Add(header);

            var content = new VisualElement();
            content.AddToClassList("group-content");
            content.style.flexGrow = 1;
            content.style.justifyContent = Justify.Center;

            var helpBox = new VisualElement();
            helpBox.AddToClassList("help-box");

            var helpText = new Label("从左侧选择一个 ScriptableObject 类型，\n即可查看字段、已有资产并创建新资产。");
            helpText.AddToClassList("help-box-text");
            helpBox.Add(helpText);
            content.Add(helpBox);

            container.Add(content);
            _detailPanel.Add(container);
        }

        private void BuildEntryDetail(ScriptableObjectCreatorScanner.Entry entry)
        {
            var type = entry.Type;

            // ── Outer container ──
            var container = new VisualElement();
            container.AddToClassList("group-container");
            container.style.flexGrow = 1;

            // ── Header with display name (+ class name subtitle if renamed) ──
            var header = new VisualElement();
            header.AddToClassList("group-header");

            var csIcon = new VisualElement();
            csIcon.AddToClassList("item-icon");
            csIcon.style.marginRight = 8;
            var tex = CsScriptIcon;
            if (tex != null) csIcon.style.backgroundImage = new StyleBackground(tex);
            header.Add(csIcon);

            var titleBlock = new VisualElement();
            titleBlock.AddToClassList("soc-title-block");
            header.Add(titleBlock);

            var headerTitle = new Label(entry.DisplayName);
            headerTitle.AddToClassList("group-title");
            headerTitle.AddToClassList("text-accent");
            titleBlock.Add(headerTitle);

            if (!string.Equals(entry.DisplayName, type.Name, StringComparison.Ordinal))
            {
                var subtitle = new Label(type.Name);
                subtitle.AddToClassList("soc-subtitle");
                titleBlock.Add(subtitle);
            }

            // Locate button in detail header
            var locateBtn = new Button(() => LocateScript(type)) { text = "定位脚本" };
            locateBtn.AddToClassList("action-btn");
            locateBtn.tooltip = "在 Project 窗口中定位脚本";
            header.Add(locateBtn);

            container.Add(header);

            // ── Content ──
            var content = new VisualElement();
            content.AddToClassList("group-content");
            content.style.flexGrow = 1;
            container.Add(content);

            // ── InspectorTitle / InspectorIntro meta ──
            BuildInspectorMeta(content, type);

            // ── Info rows ──
            AddInfoRow(content, "Full Name", type.FullName ?? type.Name);
            AddInfoRow(content, "Assembly", type.Assembly.GetName().Name);
            AddInfoRow(content, "创建面板条目", DescribeCreatorEntry(entry));

            // ── Fields section ──
            BuildFieldsSection(content, type);

            // Separator
            var sep = new VisualElement();
            sep.AddToClassList("soc-separator");
            content.Add(sep);

            // Create button (full width, prominent)
            var createBtn = new Button(OnCreateAsset) { text = "创建资产" };
            createBtn.AddToClassList("primary-btn");
            createBtn.AddToClassList("soc-create-btn");
            content.Add(createBtn);

            _detailPanel.Add(container);

            // ── Existing assets section (separate card below) ──
            BuildExistingAssetsSection(entry);
        }

        /// <summary>
        /// "创建面板条目"信息行（三分支）：未标注条目提示未标注；
        /// 单段标注（仅重命名）仅输出显示名；多段标注输出 分组路径 → 显示名。
        /// </summary>
        private static string DescribeCreatorEntry(ScriptableObjectCreatorScanner.Entry entry)
        {
            if (entry.IsUngrouped) return "未标注 CreatorEntry";

            return entry.GroupPath != null && entry.GroupPath.Length > 0
                ? string.Join("/", entry.GroupPath) + " → " + entry.DisplayName
                : entry.DisplayName;
        }

        // ─────────────────── Inspector Meta ─────────────────────────────

        private static void BuildInspectorMeta(VisualElement parent, Type type)
        {
            var titleAttr = type.GetCustomAttribute<InspectorTitleAttribute>();
            var introAttr = type.GetCustomAttribute<InspectorIntroAttribute>();

            if (titleAttr == null && introAttr == null) return;

            var metaBlock = new VisualElement();
            metaBlock.AddToClassList("soc-meta-block");
            parent.Add(metaBlock);

            // ── Title ──
            if (titleAttr != null)
            {
                var titleLbl = new Label(titleAttr.Title);
                titleLbl.AddToClassList("soc-meta-title");
                // Scale font relative to the attribute's requested size, clamped for the panel
                titleLbl.style.fontSize = Mathf.Clamp(titleAttr.FontSize / 2, 16, 32);
                metaBlock.Add(titleLbl);

                var titleDivider = new VisualElement();
                titleDivider.AddToClassList("soc-meta-divider");
                metaBlock.Add(titleDivider);
            }

            // ── Intro ──
            if (introAttr != null)
            {
                var introLbl = new Label(introAttr.Description);
                introLbl.AddToClassList("soc-meta-intro");
                metaBlock.Add(introLbl);

                var introDivider = new VisualElement();
                introDivider.AddToClassList("soc-meta-divider");
                introDivider.AddToClassList("soc-meta-divider--light");
                metaBlock.Add(introDivider);
            }
        }

        private static void AddInfoRow(VisualElement parent, string label, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("soc-info-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("soc-info-label");
            row.Add(labelEl);

            var valueEl = new Label(value);
            valueEl.AddToClassList("soc-info-value");
            row.Add(valueEl);

            parent.Add(row);
        }

        // ─────────────── Fields Section ────────────────

        /// <summary>
        /// 字段预览：可序列化字段（public 或标 [SerializeField]）、非静态、
        /// 非编译器生成、非 Obsolete、非自动属性 backing field。
        /// </summary>
        private static void BuildFieldsSection(VisualElement parent, Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

            var fields = new List<FieldInfo>();
            foreach (var field in type.GetFields(flags))
            {
                if (field.IsStatic) continue;
                if (!field.IsPublic && field.GetCustomAttribute<SerializeField>(false) == null) continue;
                if (field.GetCustomAttribute<CompilerGeneratedAttribute>(false) != null) continue;
                if (field.Name.StartsWith("<")) continue; // backing field 兜底（如 <Foo>k__BackingField）
                if (field.GetCustomAttribute<ObsoleteAttribute>(false) != null) continue;
                fields.Add(field);
            }

            if (fields.Count == 0) return;

            var sectionLabel = new Label($"字段（{fields.Count}）");
            sectionLabel.AddToClassList("soc-fields-section-title");
            parent.Add(sectionLabel);

            foreach (var field in fields)
                parent.Add(CreateFieldRow(GetCSharpTypeName(field.FieldType), field.Name));
        }

        private static VisualElement CreateFieldRow(string typeName, string fieldName)
        {
            var row = new VisualElement();
            row.AddToClassList("soc-field-row");

            // Left: field type
            var typeLabel = new Label(typeName);
            typeLabel.AddToClassList("soc-field-type");
            row.Add(typeLabel);

            // Right: field name
            var nameLabel = new Label(fieldName);
            nameLabel.AddToClassList("soc-field-name");
            row.Add(nameLabel);

            return row;
        }

        /// <summary>Maps CLR type names to C# keyword aliases where applicable.</summary>
        private static string GetCSharpTypeName(Type t)
        {
            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                var args = string.Join(", ", t.GetGenericArguments().Select(GetCSharpTypeName));
                var baseName = def.Name.Split('`')[0];
                return $"{baseName}<{args}>";
            }

            return Type.GetTypeCode(t) switch
            {
                TypeCode.Boolean => "bool",
                TypeCode.Byte => "byte",
                TypeCode.SByte => "sbyte",
                TypeCode.Char => "char",
                TypeCode.Decimal => "decimal",
                TypeCode.Double => "double",
                TypeCode.Single => "float",
                TypeCode.Int16 => "short",
                TypeCode.UInt16 => "ushort",
                TypeCode.Int32 => "int",
                TypeCode.UInt32 => "uint",
                TypeCode.Int64 => "long",
                TypeCode.UInt64 => "ulong",
                TypeCode.String => "string",
                TypeCode.Object => t.Name,
                _ => t.Name,
            };
        }

        // ─────────────────── Existing Assets ─────────────────────────────

        private void BuildExistingAssetsSection(ScriptableObjectCreatorScanner.Entry entry)
        {
            var type = entry.Type;

            // Find all .asset files of the given ScriptableObject type.
            // 注意：t:{类名} 会命中该类型及其所有子类（Unity 的 t: 按继承匹配），
            // 下面 LoadAssetAtPath 时再按目标类型加载校验，展示的是"该类型及其子类"的资产。
            var guids = AssetDatabase.FindAssets($"t:{type.Name}", new[] { "Assets" });
            var assets = new List<(string path, UnityEngine.Object obj)>();

            foreach (var guid in guids)
            {
                if (assets.Count >= MaxAssetRows) break; // 展示截断，超出部分不再逐个加载
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath(path, type);
                if (obj != null) assets.Add((path, obj));
            }

            // ── Outer card ──
            var card = new VisualElement();
            card.AddToClassList("group-container");
            card.style.marginTop = 6;

            // Header
            var header = new VisualElement();
            header.AddToClassList("group-header");
            card.Add(header);

            var title = new Label("现有资产");
            title.AddToClassList("group-title");
            header.Add(title);

            var badge = new Label(guids.Length.ToString());
            badge.AddToClassList("badge");
            if (assets.Count > 0) badge.AddToClassList("badge-build");
            header.Add(badge);

            // Content
            var content = new VisualElement();
            content.AddToClassList("group-content");
            card.Add(content);

            if (assets.Count == 0)
            {
                var empty = new VisualElement();
                empty.AddToClassList("empty-state");
                var emptyLbl = new Label("项目中未找到该类型的资产。");
                emptyLbl.AddToClassList("empty-state-text");
                empty.Add(emptyLbl);
                content.Add(empty);
            }
            else
            {
                var shownCount = Math.Min(assets.Count, MaxAssetRows);
                for (var i = 0; i < shownCount; i++)
                    content.Add(CreateAssetRow(assets[i].path, assets[i].obj));

                if (guids.Length > MaxAssetRows)
                {
                    var moreLbl = new Label($"共 {guids.Length} 条，仅显示前 {MaxAssetRows} 条");
                    moreLbl.AddToClassList("soc-asset-more");
                    content.Add(moreLbl);
                }
            }

            _detailPanel.Add(card);
        }

        private static VisualElement CreateAssetRow(string path, UnityEngine.Object obj)
        {
            var row = new VisualElement();
            row.AddToClassList("item-row");
            row.AddToClassList("soc-asset-row");
            row.tooltip = path;

            // Asset icon
            var icon = new VisualElement();
            icon.AddToClassList("item-icon");
            var preview = AssetPreview.GetMiniThumbnail(obj);
            if (preview != null) icon.style.backgroundImage = new StyleBackground(preview);
            row.Add(icon);

            // Asset name
            var nameLbl = new Label(obj.name);
            nameLbl.AddToClassList("item-name");
            row.Add(nameLbl);

            // Short path label
            var pathLbl = new Label(System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/") ?? path);
            pathLbl.AddToClassList("soc-asset-path");
            row.Add(pathLbl);

            // 整行可点：选中并 Ping 该资产
            row.AddManipulator(new Clickable(() =>
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }));

            return row;
        }

        // ─────────────────────── Actions ────────────────────────────────

        private void OnRefreshClicked()
        {
            _selected = null;
            RefreshDetailPanel();
            Rescan();
        }

        private void OnCreateAsset()
        {
            if (_selected == null) return;

            // 初始目录：Project 右键入口传入的目录优先，否则取当前选中项所在目录
            var dir = !string.IsNullOrEmpty(_initialFolder)
                ? _initialFolder
                : ResolveSelectionFolder();

            var path = EditorUtility.SaveFilePanelInProject(
                "创建 ScriptableObject",
                $"{_selected.Type.Name}.asset",
                "asset",
                $"创建 {_selected.DisplayName} 资产",
                dir);

            if (string.IsNullOrEmpty(path)) return;

            var instance = ScriptableObject.CreateInstance(_selected.Type);
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);

            Debug.Log($"[ScriptableObjectCreator] Created: {path}");

            // 成功后仅刷新右栏（重新构建详情，含"现有资产"区），左栏列表保持不动
            RefreshDetailPanel();
        }

        /// <summary>解析当前选中项所在目录：文件夹取自身、文件取父目录；不在 Assets 下回退 "Assets"。</summary>
        private static string ResolveSelectionFolder()
        {
            var active = Selection.activeObject;
            if (active == null) return "Assets";

            var path = AssetDatabase.GetAssetPath(active);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/")) return "Assets";
            if (AssetDatabase.IsValidFolder(path)) return path;

            var dir = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            return string.IsNullOrEmpty(dir) ? "Assets" : dir;
        }

        private static void LocateScript(Type type)
        {
            // Search project for MonoScript assets whose class matches the type exactly
            var guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null) continue;

                // MonoScript.GetClass() can return null for abstract/generic definitions;
                // fall back to a name comparison in that case.
                var scriptClass = script.GetClass();
                if (scriptClass == type || (scriptClass == null && script.name == type.Name))
                {
                    EditorGUIUtility.PingObject(script);
                    Selection.activeObject = script;
                    return;
                }
            }

            Debug.LogWarning($"[ScriptableObjectCreator] Could not locate script for: {type.FullName}");
        }
    }
}
