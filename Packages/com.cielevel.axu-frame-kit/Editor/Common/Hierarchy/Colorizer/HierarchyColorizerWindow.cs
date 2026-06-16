using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace AxuFrameKit.Common.Editor.Hierarchy
{
    public class HierarchyColorizerWindow : EditorWindow
{
    private HierarchyColorizerConfig config;
    private VisualElement rulesContainer;

    [MenuItem("AxuFrameKit/Hierarchy/Colorizer")]
    public static void ShowWindow()
    {
        var window = GetWindow<HierarchyColorizerWindow>("Hierarchy Colorizer");
        window.minSize = new Vector2(400, 400);
    }

    private void OnEnable()
    {
        config = HierarchyColorizerConfig.Load();
    }

    public void CreateGUI()
    {
        // Load UXML and USS
        var monoScript = MonoScript.FromScriptableObject(this);
        var scriptPath = AssetDatabase.GetAssetPath(monoScript);
        var dirPath = System.IO.Path.GetDirectoryName(scriptPath);

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dirPath}/HierarchyColorizerWindow.uxml");
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dirPath}/HierarchyColorizerWindow.uss");

        if (visualTree == null || styleSheet == null)
        {
            Debug.LogError("Could not load UXML or USS for HierarchyColorizerWindow. Make sure they are in the same directory as the script.");
            return;
        }

        visualTree.CloneTree(rootVisualElement);
        rootVisualElement.styleSheets.Add(styleSheet);

        // Query Elements
        rulesContainer = rootVisualElement.Q<ScrollView>("RulesContainer");
        var btnAddRule = rootVisualElement.Q<Button>("BtnAddRule");
        var btnSave = rootVisualElement.Q<Button>("BtnSave");
        var btnReset = rootVisualElement.Q<Button>("BtnReset");

        // Bind Actions
        btnAddRule.clicked += () =>
        {
            config.rules.Add(new ColorRule());
            RefreshRuleList();
        };

        btnSave.clicked += () =>
        {
            config.Save();
            HierarchyColorizer.RefreshConfig();
            EditorApplication.RepaintHierarchyWindow();
            ShowNotification(new GUIContent("Settings Saved!"));
        };

        btnReset.clicked += () =>
        {
            if (EditorUtility.DisplayDialog("Reset Settings", "Are you sure you want to reset to default settings?", "Yes", "No"))
            {
                EditorPrefs.DeleteKey("HierarchyColorizer_Config");
                config = HierarchyColorizerConfig.Load();
                HierarchyColorizer.RefreshConfig();
                EditorApplication.RepaintHierarchyWindow();
                RefreshRuleList();
            }
        };

        // Initialize List
        RefreshRuleList();
    }

    private void RefreshRuleList()
    {
        if (rulesContainer == null) return;

        rulesContainer.Clear();

        for (int i = 0; i < config.rules.Count; i++)
        {
            rulesContainer.Add(CreateRuleElement(i));
        }
    }

    private VisualElement CreateRuleElement(int index)
    {
        var rule = config.rules[index];
        var item = new VisualElement();
        item.AddToClassList("rule-item");

        // Content Container
        var contentContainer = new VisualElement();
        contentContainer.AddToClassList("rule-content");
        contentContainer.SetEnabled(rule.enabled);

        // Header Row: Toggle | Title | Remove
        var headerRow = new VisualElement();
        headerRow.AddToClassList("rule-header");

        var toggle = new Toggle();
        toggle.value = rule.enabled;
        toggle.RegisterValueChangedCallback(evt =>
        {
            rule.enabled = evt.newValue;
            contentContainer.SetEnabled(evt.newValue);
        });
        headerRow.Add(toggle);

        var titleLabel = new Label($"Rule {index + 1}");
        titleLabel.AddToClassList("rule-title");
        headerRow.Add(titleLabel);

        var btnRemove = new Button(() =>
        {
            config.rules.RemoveAt(index);
            RefreshRuleList();
        });
        btnRemove.text = "×";
        btnRemove.AddToClassList("rule-remove-btn");
        headerRow.Add(btnRemove);

        item.Add(headerRow);



        // Match Type
        var rowType = new VisualElement();
        rowType.AddToClassList("rule-row");
        var labelType = new Label("Match Type:");
        labelType.AddToClassList("rule-label");
        rowType.Add(labelType);

        var enumField = new EnumField(rule.matchType);
        enumField.AddToClassList("rule-control");
        enumField.RegisterValueChangedCallback(evt => rule.matchType = (ColorRule.MatchType)evt.newValue);
        rowType.Add(enumField);
        contentContainer.Add(rowType);

        // Pattern
        var rowPattern = new VisualElement();
        rowPattern.AddToClassList("rule-row");
        var labelPattern = new Label("Pattern:");
        labelPattern.AddToClassList("rule-label");
        rowPattern.Add(labelPattern);

        var textField = new TextField();
        textField.value = rule.pattern;
        textField.AddToClassList("rule-control");
        textField.isDelayed = true; // Update on Enter or Blur
        textField.RegisterValueChangedCallback(evt => rule.pattern = evt.newValue);
        rowPattern.Add(textField);
        contentContainer.Add(rowPattern);

        // Color
        var rowColor = new VisualElement();
        rowColor.AddToClassList("rule-row");
        var labelColor = new Label("Color:");
        labelColor.AddToClassList("rule-label");
        rowColor.Add(labelColor);

        var colorField = new ColorField();
        colorField.value = rule.color;
        colorField.AddToClassList("rule-control");
        colorField.RegisterValueChangedCallback(evt => rule.color = evt.newValue);
        rowColor.Add(colorField);
        contentContainer.Add(rowColor);

        item.Add(contentContainer);

        return item;
    }
}
}