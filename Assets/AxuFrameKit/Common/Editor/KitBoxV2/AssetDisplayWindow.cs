using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuFrameKit.Common.Editor.KitBoxV2
{
    public class AssetDisplayWindow : EditorWindow
    {
        // UXML文件引用
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset;
    
        private ScrollView scrollView;
        private List<Object> selectedAssets = new List<Object>();

        [MenuItem("Tools/Asset Display Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetDisplayWindow>("Asset Display");
            window.minSize = new Vector2(400, 300);
        }

        private void CreateGUI()
        {
            // 加载UXML文件
            if (m_VisualTreeAsset != null)
            {
                m_VisualTreeAsset.CloneTree(rootVisualElement);
            }
            else
            {
                // 如果没有在Inspector中指定，直接从路径加载
                var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/YourUXMLFileName.uxml");
                if (visualTree != null)
                {
                    visualTree.CloneTree(rootVisualElement);
                }
            }

            // === 关键：查询没有name的ScrollView ===
            // 因为您的ScrollView没有设置name属性，直接查询第一个ScrollView
            scrollView = rootVisualElement.Q<ScrollView>();
        
            if (scrollView == null)
            {
                Debug.LogError("ScrollView not found!");
                return;
            }

            // 清除UXML中预设的GroupBox（如果需要）
            scrollView.Clear();

            // 添加控制按钮到窗口顶部
            CreateControlButtons();

            Debug.Log($"ScrollView found! Current children count: {scrollView.childCount}");
        }

        // 创建控制按钮
        private void CreateControlButtons()
        {
            var buttonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    paddingTop    = 5,
                    paddingBottom = 5,
                    paddingLeft   = 5,
                    paddingRight  = 5
                }
            };

            var addButton = new Button(AddSelectedAssets)
            {
                text  = "Add Selected Assets",
                style = { flexGrow = 1, marginRight = 5, height = 30 }
            };
            buttonContainer.Add(addButton);

            var clearButton = new Button(ClearAssets)
            {
                text  = "Clear All",
                style = { flexGrow = 1, height = 30 }
            };
            buttonContainer.Add(clearButton);

            // 插入到ScrollView之前
            rootVisualElement.Insert(0, buttonContainer);

            // 添加计数标签
            var countLabel = new Label("Total: 0")
            {
                name = "CountLabel",
                style = 
                { 
                    paddingLeft             = 5,
                    paddingTop              = 5,
                    unityFontStyleAndWeight = FontStyle.Bold 
                }
            };
            rootVisualElement.Insert(1, countLabel);
        }

        // 添加选中的资产
        private void AddSelectedAssets()
        {
            if (scrollView == null)
            {
                Debug.LogError("ScrollView is null!");
                return;
            }

            var selection = Selection.objects;
        
            if (selection == null || selection.Length == 0)
            {
                Debug.LogWarning("No assets selected! Please select assets in Project window.");
                return;
            }

            Debug.Log($"Adding {selection.Length} selected assets...");

            foreach (var obj in selection)
            {
                if (!selectedAssets.Contains(obj))
                {
                    selectedAssets.Add(obj);
                    AddAssetToScrollView(obj);
                }
            }

            UpdateCountLabel();
        }

        // 向ScrollView添加单个资产的UI元素
        private void AddAssetToScrollView(Object asset)
        {
            // 创建资产条目容器
            var assetItem = new VisualElement
            {
                style =
                {
                    flexDirection           = FlexDirection.Row,
                    alignItems              = Align.Center,
                    marginBottom            = 5,
                    marginLeft              = 5,
                    marginRight             = 5,
                    paddingTop              = 8,
                    paddingBottom           = 8,
                    paddingLeft             = 8,
                    paddingRight            = 8,
                    backgroundColor         = new Color(0.25f, 0.25f, 0.25f, 0.5f),
                    borderTopLeftRadius     = 4,
                    borderTopRightRadius    = 4,
                    borderBottomLeftRadius  = 4,
                    borderBottomRightRadius = 4
                }
            };

            // 添加悬停效果
            assetItem.RegisterCallback<MouseEnterEvent>(evt =>
            {
                assetItem.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);
            });
            assetItem.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                assetItem.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
            });

            // 资产图标
            var icon = new Image
            {
                image = AssetPreview.GetMiniThumbnail(asset),
                style =
                {
                    width       = 40,
                    height      = 40,
                    marginRight = 10
                }
            };
            assetItem.Add(icon);

            // 资产信息容器
            var infoContainer = new VisualElement
            {
                style = { flexGrow = 1 }
            };

            // 资产名称
            var nameLabel = new Label(asset.name)
            {
                style = 
                { 
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize                = 12,
                    marginBottom            = 2
                }
            };
            infoContainer.Add(nameLabel);

            // 资产类型和路径
            var path = AssetDatabase.GetAssetPath(asset);
            var typeAndPathLabel = new Label($"{asset.GetType().Name} • {path}")
            {
                style =
                {
                    fontSize = 10,
                    color    = new Color(0.7f, 0.7f, 0.7f, 1f)
                }
            };
            infoContainer.Add(typeAndPathLabel);

            assetItem.Add(infoContainer);

            // 删除按钮
            var removeButton = new Button(() => RemoveAsset(asset, assetItem))
            {
                text = "×",
                style =
                {
                    width                   = 30,
                    height                  = 30,
                    fontSize                = 18,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            assetItem.Add(removeButton);

            // 双击打开资产
            assetItem.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2)
                {
                    AssetDatabase.OpenAsset(asset);
                    evt.StopPropagation();
                }
            });

            // === 关键：向ScrollView添加元素 ===
            scrollView.Add(assetItem);
        
            Debug.Log($"Added asset: {asset.name} to ScrollView");
        }

        // 移除资产
        private void RemoveAsset(Object asset, VisualElement element)
        {
            selectedAssets.Remove(asset);
            scrollView.Remove(element);
            UpdateCountLabel();
        
            Debug.Log($"Removed asset: {asset.name}");
        }

        // 清空所有资产
        private void ClearAssets()
        {
            if (scrollView == null) return;
        
            int count = selectedAssets.Count;
            selectedAssets.Clear();
            scrollView.Clear();
            UpdateCountLabel();
        
            Debug.Log($"Cleared {count} assets from ScrollView");
        }

        // 更新计数标签
        private void UpdateCountLabel()
        {
            var countLabel = rootVisualElement.Q<Label>("CountLabel");
            if (countLabel != null)
            {
                countLabel.text = $"Total: {selectedAssets.Count} asset(s)";
            }
        }

        // 可选：监听Selection变化自动添加
        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            // 可以在这里实现自动添加选中资产的逻辑
            // AddSelectedAssets();
        }
    }
}