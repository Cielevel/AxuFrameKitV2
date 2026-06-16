using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace AxuFrameKit.Common.Editor.Hierarchy
{
    public class HierarchyIconWindow : EditorWindow
{
    private HierarchyIconConfig config;
    private VisualElement rulesContainer;
    private VisualElement builtInContainer;

    // Define the list of common built-in components to display
    private readonly string[] commonBuiltInTypes = new string[]
    {
        // Rendering
        "Camera", "Light", "MeshRenderer", "SkinnedMeshRenderer", "SpriteRenderer",
        "LineRenderer", "TrailRenderer", "ParticleSystem",
        
        // Physics 3D
        "Rigidbody", "BoxCollider", "SphereCollider", "CapsuleCollider",
        "MeshCollider", "CharacterController", "WheelCollider", "TerrainCollider",
        
        // Physics 2D
        "Rigidbody2D", "BoxCollider2D", "CircleCollider2D", "CapsuleCollider2D",
        "PolygonCollider2D", "EdgeCollider2D", "CompositeCollider2D",
        
        // Animation
        "Animator", "Animation",
        
        // Audio
        "AudioSource", "AudioListener", "AudioReverbZone",
        
        // UI
        "Canvas", "CanvasGroup", "RectTransform", "Image", "RawImage", "Text",
        "Button", "Toggle", "Slider", "Scrollbar", "Dropdown", "InputField",
        "ScrollRect", "Mask", "RectMask2D",
        
        // Effects
        "Projector", "Halo",
        
        // Navigation
        "NavMeshAgent", "NavMeshObstacle", "OffMeshLink",
        
        // Miscellaneous
        "Terrain", "WindZone", "LODGroup", "OcclusionArea", "OcclusionPortal",
        "ReflectionProbe", "LightProbeGroup", "LightProbeProxyVolume"
    };

    [MenuItem("AxuFrameKit/Hierarchy/Icon")]
    public static void ShowWindow()
    {
        var window = GetWindow<HierarchyIconWindow>("Hierarchy Icons");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        config = HierarchyIconConfig.Load();
    }

    public void CreateGUI()
    {
        // Load UXML and USS
        var monoScript = MonoScript.FromScriptableObject(this);
        var scriptPath = AssetDatabase.GetAssetPath(monoScript);
        var dirPath = System.IO.Path.GetDirectoryName(scriptPath);

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dirPath}/HierarchyIconWindow.uxml");
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dirPath}/HierarchyIconWindow.uss");

        if (visualTree == null || styleSheet == null)
        {
            Debug.LogError("Could not load UXML or USS for HierarchyIconWindow.");
            return;
        }

        visualTree.CloneTree(rootVisualElement);
        rootVisualElement.styleSheets.Add(styleSheet);

        // Query Elements
        builtInContainer = rootVisualElement.Q<VisualElement>("BuiltInContainer");
        rulesContainer = rootVisualElement.Q<ScrollView>("RulesContainer");
        var btnAddRule = rootVisualElement.Q<Button>("BtnAddRule");
        var btnSave = rootVisualElement.Q<Button>("BtnSave");
        var btnReset = rootVisualElement.Q<Button>("BtnReset");

        // Bind Actions
        btnAddRule.clicked += () =>
        {
            config.customRules.Add(new IconRule());
            RefreshCustomRuleList();
        };

        btnSave.clicked += () =>
        {
            config.Save();
            HierarchyIconDrawer.RefreshConfig();
            EditorApplication.RepaintHierarchyWindow();
            ShowNotification(new GUIContent("Settings Saved!"));
        };

        btnReset.clicked += () =>
        {
            if (EditorUtility.DisplayDialog("Reset Settings", "Are you sure you want to reset to default settings?", "Yes", "No"))
            {
                EditorPrefs.DeleteKey("HierarchyIcon_Config");
                config = HierarchyIconConfig.Load();
                HierarchyIconDrawer.RefreshConfig();
                EditorApplication.RepaintHierarchyWindow();
                RefreshBuiltInList();
                RefreshCustomRuleList();
            }
        };

        // Initialize Lists
        RefreshBuiltInList();
        RefreshCustomRuleList();
    }

    private void RefreshBuiltInList()
    {
        if (builtInContainer == null) return;
        builtInContainer.Clear();

        if (config.enabledBuiltInIcons == null)
            config.enabledBuiltInIcons = new List<string>();

        foreach (var typeName in commonBuiltInTypes)
        {
            var btn = CreateBuiltInToggle(typeName);
            builtInContainer.Add(btn);
        }
    }

    private Button CreateBuiltInToggle(string typeName)
    {
        bool isActive = config.enabledBuiltInIcons.Contains(typeName);

        var btn = new Button(() =>
        {
            // Toggle logic
            if (config.enabledBuiltInIcons.Contains(typeName))
            {
                config.enabledBuiltInIcons.Remove(typeName);
            }
            else
            {
                config.enabledBuiltInIcons.Add(typeName);
            }
            RefreshBuiltInList(); // Re-render to update state styles
        });

        // 清除默认文本，我们将手动添加图标和文本
        btn.text = "";

        // 创建一个容器来放置图标和文本
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;
        container.pickingMode = PickingMode.Ignore; // 让点击事件穿透到按钮

        // 获取组件图标
        var iconContent = EditorGUIUtility.IconContent($"d_{typeName} Icon");
        if (iconContent?.image == null)
        {
            // 尝试不带 "d_" 前缀的图标（非暗色主题）
            iconContent = EditorGUIUtility.IconContent($"{typeName} Icon");
        }

        // 如果找到图标，添加图标元素
        if (iconContent?.image != null)
        {
            var icon = new Image();
            icon.image = iconContent.image;
            icon.style.width = 16;
            icon.style.height = 16;
            icon.style.marginRight = 4;
            icon.pickingMode = PickingMode.Ignore;
            container.Add(icon);
        }

        // 添加文本标签
        var label = new Label(typeName);
        label.style.unityTextAlign = TextAnchor.MiddleLeft;
        label.pickingMode = PickingMode.Ignore;
        container.Add(label);

        btn.Add(container);
        btn.AddToClassList("component-toggle-btn");
        btn.AddToClassList("builtin-item");

        if (isActive)
            btn.AddToClassList("component-toggle-btn--active");
        else
            btn.AddToClassList("component-toggle-btn--inactive");

        return btn;
    }

    private void RefreshCustomRuleList()
    {
        if (rulesContainer == null) return;
        rulesContainer.Clear();

        if (config.customRules == null)
            config.customRules = new List<IconRule>();

        for (int i = 0; i < config.customRules.Count; i++)
        {
            rulesContainer.Add(CreateCustomRuleElement(i));
        }
    }

    private VisualElement CreateCustomRuleElement(int index)
    {
        var rule = config.customRules[index];
        var item = new VisualElement();
        item.AddToClassList("rule-item");

        var contentContainer = new VisualElement();
        contentContainer.AddToClassList("rule-content");
        contentContainer.SetEnabled(rule.enabled);

        // Header
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

        var titleLabel = new Label($"Custom Rule {index + 1}");
        titleLabel.AddToClassList("rule-title");
        headerRow.Add(titleLabel);

        var btnRemove = new Button(() =>
        {
            config.customRules.RemoveAt(index);
            RefreshCustomRuleList();
        });
        btnRemove.text = "×";
        btnRemove.AddToClassList("rule-remove-btn");
        headerRow.Add(btnRemove);

        item.Add(headerRow);

        // Component Name
        var rowName = new VisualElement();
        rowName.AddToClassList("rule-row");
        var labelName = new Label("Component Type:");
        labelName.AddToClassList("rule-label");
        rowName.Add(labelName);

        var textField = new TextField();
        textField.value = rule.componentName;
        textField.AddToClassList("rule-control");
        textField.isDelayed = true;
        textField.RegisterValueChangedCallback(evt => rule.componentName = evt.newValue);
        rowName.Add(textField);
        contentContainer.Add(rowName);

        // Offset X
        var rowOffset = new VisualElement();
        rowOffset.AddToClassList("rule-row");
        var labelOffset = new Label("Offset Right:");
        labelOffset.AddToClassList("rule-label");
        rowOffset.Add(labelOffset);

        var floatField = new FloatField();
        floatField.value = rule.offsetX;
        floatField.AddToClassList("rule-control");
        floatField.RegisterValueChangedCallback(evt => rule.offsetX = evt.newValue);
        rowOffset.Add(floatField);
        contentContainer.Add(rowOffset);

        item.Add(contentContainer);

        return item;
    }
}
}
