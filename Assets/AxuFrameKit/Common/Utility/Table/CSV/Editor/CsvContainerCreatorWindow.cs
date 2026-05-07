using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Common.Utility.Table.CSV.Editor
{
    public class CsvContainerCreatorWindow : EditorWindow
    {
        private List<Type> _csvContainerTypes = new List<Type>();
        private List<Type> _filteredTypes = new List<Type>();
        private Type _selectedType;

        // UI refs
        private ScrollView _typeListScrollView;
        private VisualElement _detailPanel;
        private TextField _searchField;
        private Label _countBadge;

        [MenuItem("AxuFrameKit/Table/Csv Container Creator")]
        public static void ShowWindow()
        {
            var window = GetWindow<CsvContainerCreatorWindow>("Csv Container Creator");
            window.minSize = new Vector2(600, 450);
        }

        private void OnEnable()
        {
            ScanCsvContainerTypes();
        }

        // ───────────────────────────── Scan ─────────────────────────────

        private void ScanCsvContainerTypes()
        {
            _csvContainerTypes.Clear();
            var csvContainerBaseType = typeof(CsvContainerSo<>);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsClass && !type.IsAbstract &&
                            type.BaseType != null &&
                            type.BaseType.IsGenericType &&
                            type.BaseType.GetGenericTypeDefinition() == csvContainerBaseType)
                        {
                            _csvContainerTypes.Add(type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CsvContainerCreator] Failed to scan assembly {assembly.FullName}: {ex.Message}");
                }
            }

            _csvContainerTypes = _csvContainerTypes.OrderBy(t => t.FullName).ToList();
            ApplyFilter(_searchField?.value ?? string.Empty);
        }

        private void ApplyFilter(string query)
        {
            query = query?.Trim() ?? string.Empty;
            _filteredTypes = string.IsNullOrEmpty(query)
                ? new List<Type>(_csvContainerTypes)
                : _csvContainerTypes.Where(t =>
                    t.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.FullName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            RefreshTypeList();
        }

        // ───────────────────────────── GUI ──────────────────────────────

        private void CreateGUI()
        {
            // Load CommonWindowStyles (shared across all AxuFrameKit windows)
            var commonStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/AxuFrameKit/Common/Editor/Window/CommonWindowStyles.uss");
            if (commonStyles != null)
                rootVisualElement.styleSheets.Add(commonStyles);

            // Also load local overrides / extras
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var dirPath = System.IO.Path.GetDirectoryName(scriptPath);
            var localStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dirPath}/CsvContainerCreatorWindow.uss");
            if (localStyles != null)
                rootVisualElement.styleSheets.Add(localStyles);

            rootVisualElement.AddToClassList("ue5-root");

            // ── Window Title ──
            var titleLabel = new Label("CSV Container Creator");
            titleLabel.AddToClassList("window-title");
            rootVisualElement.Add(titleLabel);

            // ── Toolbar ──
            BuildToolbar();

            // ── Main split layout ──
            var mainContainer = new VisualElement();
            mainContainer.AddToClassList("csv-main-container");
            rootVisualElement.Add(mainContainer);

            BuildLeftPanel(mainContainer);
            BuildRightPanel(mainContainer);

            // Initial populate
            ApplyFilter(string.Empty);
        }

        private void BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar-panel");
            rootVisualElement.Add(toolbar);

            // Search field
            var searchLabel = new Label("Filter:");
            searchLabel.AddToClassList("toolbar-label");
            toolbar.Add(searchLabel);

            _searchField = new TextField();
            _searchField.AddToClassList("ue5-text-field");
            _searchField.RegisterValueChangedCallback(evt => ApplyFilter(evt.newValue));
            toolbar.Add(_searchField);

            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            // Refresh button
            var refreshBtn = new Button(OnRefreshClicked) { text = "↺  Refresh" };
            refreshBtn.AddToClassList("action-btn");
            toolbar.Add(refreshBtn);
        }

        private void BuildLeftPanel(VisualElement parent)
        {
            var leftPanel = new VisualElement();
            leftPanel.AddToClassList("csv-left-panel");
            parent.Add(leftPanel);

            // Header
            var container = new VisualElement();
            container.AddToClassList("group-container");
            container.style.flexGrow = 1;
            leftPanel.Add(container);

            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("AVAILABLE TYPES");
            title.AddToClassList("group-title");
            header.Add(title);

            _countBadge = new Label("0");
            _countBadge.AddToClassList("badge");
            header.Add(_countBadge);

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
            rightPanel.AddToClassList("csv-right-panel");
            parent.Add(rightPanel);

            // Use a ScrollView so long field lists don't overflow
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("content-scroll");
            rightPanel.Add(scroll);

            _detailPanel = new VisualElement();
            _detailPanel.style.flexGrow = 1;
            scroll.Add(_detailPanel);

            RefreshDetailPanel();
        }

        // ─────────────────────── Type List ──────────────────────────────

        private void RefreshTypeList()
        {
            if (_typeListScrollView == null) return;
            _typeListScrollView.Clear();

            if (_countBadge != null)
                _countBadge.text = _filteredTypes.Count.ToString();

            if (_filteredTypes.Count == 0)
            {
                var emptyState = new VisualElement();
                emptyState.AddToClassList("empty-state");
                var emptyText = new Label(
                    _csvContainerTypes.Count == 0
                        ? "No CsvContainerSo<T> subtypes found.\nEnsure your classes are compiled."
                        : "No results match the filter.");
                emptyText.AddToClassList("empty-state-text");
                emptyState.Add(emptyText);
                _typeListScrollView.Add(emptyState);
                return;
            }

            foreach (var type in _filteredTypes)
            {
                _typeListScrollView.Add(CreateTypeRow(type));
            }
        }

        private VisualElement CreateTypeRow(Type type)
        {
            var row = new VisualElement();
            row.AddToClassList("item-row");
            if (_selectedType == type) row.AddToClassList("csv-item-selected");

            // Icon
            var icon = new VisualElement();
            icon.AddToClassList("item-icon");
            var tex = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
            if (tex != null) icon.style.backgroundImage = new StyleBackground(tex);
            row.Add(icon);

            // Name
            var nameLabel = new Label(type.Name);
            nameLabel.AddToClassList("item-name");
            row.Add(nameLabel);

            // Namespace badge (short)
            if (!string.IsNullOrEmpty(type.Namespace))
            {
                var nsBadge = new Label(GetShortNamespace(type.Namespace));
                nsBadge.AddToClassList("csv-ns-badge");
                row.Add(nsBadge);
            }

            // Select button
            var selectBtn = new Button(() => OnTypeSelected(type)) { text = "Select" };
            selectBtn.AddToClassList("action-btn");
            row.Add(selectBtn);

            // Locate button
            var locateBtn = new Button(() => LocateScript(type)) { text = "📍" };
            locateBtn.AddToClassList("action-btn");
            locateBtn.tooltip = "Locate script in Project window";
            row.Add(locateBtn);

            return row;
        }

        private static string GetShortNamespace(string ns)
        {
            if (string.IsNullOrEmpty(ns)) return string.Empty;
            var parts = ns.Split('.');
            return parts.Length > 2 ? $"…{parts[^1]}" : ns;
        }

        // ─────────────────────── Selection ──────────────────────────────

        private void OnTypeSelected(Type type)
        {
            _selectedType = type;
            RefreshDetailPanel();
            HighlightSelectedType(type);
        }

        private void HighlightSelectedType(Type selectedType)
        {
            if (_typeListScrollView == null) return;
            foreach (var child in _typeListScrollView.Children())
            {
                child.RemoveFromClassList("csv-item-selected");
                var lbl = child.Q<Label>(className: "item-name");
                if (lbl != null && lbl.text == selectedType.Name)
                    child.AddToClassList("csv-item-selected");
            }
        }

        // ─────────────────────── Detail Panel ───────────────────────────

        private void RefreshDetailPanel()
        {
            if (_detailPanel == null) return;
            _detailPanel.Clear();

            if (_selectedType == null)
            {
                BuildEmptyDetail();
                return;
            }

            BuildTypeDetail(_selectedType);
        }

        private void BuildEmptyDetail()
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            container.style.flexGrow = 1;

            var header = new VisualElement();
            header.AddToClassList("group-header");
            var headerTitle = new Label("TYPE DETAILS");
            headerTitle.AddToClassList("group-title");
            header.Add(headerTitle);
            container.Add(header);

            var content = new VisualElement();
            content.AddToClassList("group-content");
            content.style.flexGrow = 1;
            content.style.justifyContent = Justify.Center;

            var helpBox = new VisualElement();
            helpBox.AddToClassList("help-box");

            var helpText = new Label("← Select a type from the list\nto view details and create an asset.");
            helpText.AddToClassList("help-box-text");
            helpBox.Add(helpText);
            content.Add(helpBox);

            container.Add(content);
            _detailPanel.Add(container);
        }

        private void BuildTypeDetail(Type type)
        {
            // ── Outer container ──
            var container = new VisualElement();
            container.AddToClassList("group-container");
            container.style.flexGrow = 1;

            // ── Header with type name ──
            var header = new VisualElement();
            header.AddToClassList("group-header");

            var csIcon = new VisualElement();
            csIcon.AddToClassList("item-icon");
            csIcon.style.marginRight = 8;
            var tex = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
            if (tex != null) csIcon.style.backgroundImage = new StyleBackground(tex);
            header.Add(csIcon);

            var headerTitle = new Label(type.Name);
            headerTitle.AddToClassList("group-title");
            headerTitle.style.color = new StyleColor(new Color(1f, 0.58f, 0f)); // UE5 orange
            header.Add(headerTitle);

            // Locate button in detail header
            var detailLocateBtn = new Button(() => LocateScript(type)) { text = "📍  Locate" };
            detailLocateBtn.AddToClassList("action-btn");
            detailLocateBtn.tooltip = "Ping and select this script in the Project window";
            header.Add(detailLocateBtn);

            container.Add(header);

            // ── Content ──
            var content = new VisualElement();
            content.AddToClassList("group-content");
            content.style.flexGrow = 1;
            container.Add(content);

            // ── InspectorTitle / InspectorIntro meta ──
            BuildInspectorMeta(content, type);

            // ── Info rows ──
            var genericArg = type.BaseType!.GetGenericArguments()[0];

            AddInfoRow(content, "Full Name",    type.FullName ?? type.Name);
            AddInfoRow(content, "Assembly",     type.Assembly.GetName().Name);
            AddInfoRow(content, "Element Type", genericArg.FullName ?? genericArg.Name);

            // ── Fields section ──
            BuildFieldsSection(content, genericArg);

            // Separator
            var sep = new VisualElement();
            sep.AddToClassList("csv-separator");
            content.Add(sep);

            // Create button (full width, prominent)
            var createBtn = new Button(OnCreateInstance) { text = "✦  Create Asset" };
            createBtn.AddToClassList("primary-btn");
            createBtn.AddToClassList("csv-create-btn");
            content.Add(createBtn);

            _detailPanel.Add(container);

            // ── Existing assets section (separate card below) ──
            BuildExistingAssetsSection(type);
        }

        private void BuildExistingAssetsSection(Type type)
        {
            // Find all .asset files of the given ScriptableObject type
            var guids = AssetDatabase.FindAssets($"t:{type.Name}");
            var assets = new List<(string path, UnityEngine.Object obj)>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj  = AssetDatabase.LoadAssetAtPath(path, type);
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

            var title = new Label("EXISTING ASSETS");
            title.AddToClassList("group-title");
            header.Add(title);

            var badge = new Label(assets.Count.ToString());
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
                var emptyLbl = new Label("No assets of this type found in the project.");
                emptyLbl.AddToClassList("empty-state-text");
                empty.Add(emptyLbl);
                content.Add(empty);
            }
            else
            {
                foreach (var (path, obj) in assets)
                    content.Add(CreateAssetRow(path, obj));
            }

            _detailPanel.Add(card);
        }

        private static VisualElement CreateAssetRow(string path, UnityEngine.Object obj)
        {
            var row = new VisualElement();
            row.AddToClassList("item-row");
            row.tooltip = path;

            // Asset icon
            var icon = new VisualElement();
            icon.AddToClassList("item-icon");
            var preview = AssetPreview.GetMiniThumbnail(obj);
            if (preview != null) icon.style.backgroundImage = new StyleBackground(preview);
            row.Add(icon);

            // Asset name (clickable — opens inspector focus)
            var nameBtn = new Button(() =>
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            });
            nameBtn.text = obj.name;
            nameBtn.AddToClassList("item-name");
            nameBtn.style.backgroundColor = Color.clear;
            nameBtn.style.borderLeftWidth  = 0;
            nameBtn.style.borderRightWidth = 0;
            nameBtn.style.borderTopWidth   = 0;
            nameBtn.style.borderBottomWidth = 0;
            nameBtn.style.unityTextAlign   = TextAnchor.MiddleLeft;
            row.Add(nameBtn);

            // Short path label
            var pathLbl = new Label(System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/") ?? path);
            pathLbl.AddToClassList("csv-asset-path");
            row.Add(pathLbl);

            // Locate button
            var locateBtn = new Button(() =>
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }) { text = "📍" };
            locateBtn.AddToClassList("action-btn");
            locateBtn.tooltip = "Locate in Project window";
            row.Add(locateBtn);

            return row;
        }

        // ─────────────────── Inspector Meta ─────────────────────────────

        private static void BuildInspectorMeta(VisualElement parent, Type type)
        {
            var titleAttr = type.GetCustomAttribute<InspectorTitleAttribute>();
            var introAttr = type.GetCustomAttribute<InspectorIntroAttribute>();

            if (titleAttr == null && introAttr == null) return;

            var metaBlock = new VisualElement();
            metaBlock.AddToClassList("csv-meta-block");
            parent.Add(metaBlock);

            // ── Title ──
            if (titleAttr != null)
            {
                var titleLbl = new Label(titleAttr.Title);
                titleLbl.AddToClassList("csv-meta-title");
                // Scale font relative to the attribute's requested size, clamped for the panel
                titleLbl.style.fontSize = Mathf.Clamp(titleAttr.FontSize / 2, 16, 32);
                metaBlock.Add(titleLbl);

                var titleDivider = new VisualElement();
                titleDivider.AddToClassList("csv-meta-divider");
                metaBlock.Add(titleDivider);
            }

            // ── Intro ──
            if (introAttr != null)
            {
                var introLbl = new Label(introAttr.Description);
                introLbl.AddToClassList("csv-meta-intro");
                metaBlock.Add(introLbl);

                var introDivider = new VisualElement();
                introDivider.AddToClassList("csv-meta-divider");
                introDivider.AddToClassList("csv-meta-divider--light");
                metaBlock.Add(introDivider);
            }
        }

        private static void AddInfoRow(VisualElement parent, string label, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("csv-info-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("csv-info-label");
            row.Add(labelEl);

            var valueEl = new Label(value);
            valueEl.AddToClassList("csv-info-value");
            row.Add(valueEl);

            parent.Add(row);
        }

        // ─────────────── Fields Section ────────────────

        private static void BuildFieldsSection(VisualElement parent, Type elementType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            // Collect members: public fields + public properties
            var members = new List<(string typeName, string memberName, CsvColumnAttribute csvAttr, FinalParseAttribute finalAttr)>();

            foreach (var field in elementType.GetFields(flags))
            {
                var csvAttr   = field.GetCustomAttribute<CsvColumnAttribute>();
                var finalAttr = field.GetCustomAttribute<FinalParseAttribute>();
                members.Add((GetCSharpTypeName(field.FieldType), field.Name, csvAttr, finalAttr));
            }

            foreach (var prop in elementType.GetProperties(flags))
            {
                // Skip indexers
                if (prop.GetIndexParameters().Length > 0) continue;
                var csvAttr   = prop.GetCustomAttribute<CsvColumnAttribute>();
                var finalAttr = prop.GetCustomAttribute<FinalParseAttribute>();
                members.Add((GetCSharpTypeName(prop.PropertyType), prop.Name, csvAttr, finalAttr));
            }

            if (members.Count == 0) return;

            // Section separator label
            var sectionLabel = new Label($"FIELDS  ({members.Count})");
            sectionLabel.AddToClassList("csv-fields-section-title");
            parent.Add(sectionLabel);

            foreach (var (typeName, memberName, csvAttr, finalAttr) in members)
            {
                parent.Add(CreateFieldRow(typeName, memberName, csvAttr, finalAttr));
            }
        }

        private static VisualElement CreateFieldRow(
            string typeName, string memberName,
            CsvColumnAttribute csvAttr, FinalParseAttribute finalAttr)
        {
            var row = new VisualElement();
            row.AddToClassList("csv-field-row");

            // Left: «type name»
            var typeLabel = new Label(typeName);
            typeLabel.AddToClassList("csv-field-type");
            row.Add(typeLabel);

            // Middle: member name
            var nameLabel = new Label(memberName);
            nameLabel.AddToClassList("csv-field-name");
            row.Add(nameLabel);

            // CsvColumn badge + column name
            if (csvAttr != null)
            {
                var arrow = new Label("→");
                arrow.AddToClassList("csv-field-arrow");
                row.Add(arrow);

                var colLabel = new Label($"\"{csvAttr.ColumnName}\"");
                colLabel.AddToClassList("csv-field-colname");
                row.Add(colLabel);

                if (csvAttr.IsOptional)
                {
                    var optBadge = new Label("optional");
                    optBadge.AddToClassList("csv-field-badge");
                    optBadge.AddToClassList("csv-field-badge--optional");
                    row.Add(optBadge);
                }
            }
            else
            {
                // No CsvColumn — show a dim indicator
                var noBadge = new Label("no column");
                noBadge.AddToClassList("csv-field-badge");
                noBadge.AddToClassList("csv-field-badge--none");
                row.Add(noBadge);
            }

            // FinalParse badge
            if (finalAttr != null)
            {
                var fpBadge = new Label($"⚙ {finalAttr.ParseMethod}");
                fpBadge.AddToClassList("csv-field-badge");
                fpBadge.AddToClassList("csv-field-badge--final");
                row.Add(fpBadge);
            }

            return row;
        }

        /// <summary>Maps CLR type names to C# keyword aliases where applicable.</summary>
        private static string GetCSharpTypeName(Type t)
        {
            if (t.IsGenericType)
            {
                var def  = t.GetGenericTypeDefinition();
                var args = string.Join(", ", t.GetGenericArguments().Select(GetCSharpTypeName));
                var baseName = def.Name.Split('`')[0];
                return $"{baseName}<{args}>";
            }

            return Type.GetTypeCode(t) switch
            {
                TypeCode.Boolean  => "bool",
                TypeCode.Byte     => "byte",
                TypeCode.SByte    => "sbyte",
                TypeCode.Char     => "char",
                TypeCode.Decimal  => "decimal",
                TypeCode.Double   => "double",
                TypeCode.Single   => "float",
                TypeCode.Int16    => "short",
                TypeCode.UInt16   => "ushort",
                TypeCode.Int32    => "int",
                TypeCode.UInt32   => "uint",
                TypeCode.Int64    => "long",
                TypeCode.UInt64   => "ulong",
                TypeCode.String   => "string",
                TypeCode.Object   => t.Name,
                _                 => t.Name,
            };
        }

        // ─────────────────────── Actions ────────────────────────────────

        private void OnRefreshClicked()
        {
            _selectedType = null;
            ScanCsvContainerTypes();
            RefreshDetailPanel();
        }

        private void OnCreateInstance()
        {
            if (_selectedType == null) return;

            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(selectedPath))
                selectedPath = "Assets";
            else if (!AssetDatabase.IsValidFolder(selectedPath))
                selectedPath = System.IO.Path.GetDirectoryName(selectedPath);

            var path = EditorUtility.SaveFilePanelInProject(
                "Create CSV Container",
                $"{_selectedType.Name}_Data",
                "asset",
                "Choose a location to save the CSV Container asset",
                selectedPath);

            if (string.IsNullOrEmpty(path)) return;

            var instance = ScriptableObject.CreateInstance(_selectedType);
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);

            Debug.Log($"[CsvContainerCreator] Created: {path}");
        }

        private static void LocateScript(Type type)
        {
            // Search project for MonoScript assets whose class matches the type exactly
            var guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
            foreach (var guid in guids)
            {
                var path   = AssetDatabase.GUIDToAssetPath(guid);
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

            Debug.LogWarning($"[CsvContainerCreator] Could not locate script for: {type.FullName}");
        }

        // ─────────────────────── Context Menu ───────────────────────────

        [MenuItem("Assets/Create/AxuFrameKit/CSV Container (From Selected Folder)", true)]
        private static bool ValidateCreateFromFolder() => Selection.activeObject != null;

        [MenuItem("Assets/Create/AxuFrameKit/CSV Container (From Selected Folder)", false, 100)]
        private static void CreateFromFolder(MenuCommand command) => ShowWindow();
    }
}