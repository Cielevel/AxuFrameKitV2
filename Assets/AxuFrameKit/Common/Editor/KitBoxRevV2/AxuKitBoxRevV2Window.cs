using System;
using System.Collections.Generic;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.CommonGroup;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.CopierGroup;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.TableGroup;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.UtilityGroup;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2
{
    /// <summary>
    /// KitBoxRevV2 主窗口 - 纯 UI Toolkit 实现
    /// </summary>
    public class AxuKitBoxRevV2Window : EditorWindow, IKitBoxRevV2
    {
        private VisualElement _root;
        private VisualElement _navContainer;
        private Label _contentTitle;
        private VisualElement _contentContainer;
        private ScrollView _contentScrollView;

        private readonly List<IKitGroupRevV2> _groups = new List<IKitGroupRevV2>();
        private string _currentGroupName;
        private int _currentPageIndex = -1;
        private IKitPageRevV2 _currentPage;

        [MenuItem("AxuFrameKit/AxuEditor工具集V2-Rev %#m")]
        public static void Open()
        {
            var window = GetWindow<AxuKitBoxRevV2Window>();
            window.titleContent = new GUIContent("Axu工具集V2-Rev");
            window.minSize = new Vector2(1000, 650);
            window.Show();
        }

        private void OnEnable()
        {
            SetupGroups();
            InitializeGroups();
        }

        private void CreateGUI()
        {
            _root = rootVisualElement;

            // Load UXML & USS
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/AxuFrameKit/Common/Editor/KitBoxRevV2/UI/AxuKitBoxV2.uxml");
            if (uxml != null) uxml.CloneTree(_root);

            // Theme Selection - Switch between default and UE5 theme
            // Default Theme:
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/AxuFrameKit/Common/Editor/KitBoxRevV2/UI/AxuKitBoxV2.uss");

            // UE5 Theme (uncomment to use):
            // var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/AxuFrameKit/Common/Editor/KitBoxRevV2/UI/AxuKitBoxV2_UE5Theme.uss");

            if (uss != null) _root.styleSheets.Add(uss);

            // References
            _navContainer = _root.Q<VisualElement>("nav-container");
            _contentTitle = _root.Q<Label>("content-title");
            _contentContainer = _root.Q<VisualElement>("content-container");
            _contentScrollView = _root.Q<ScrollView>("content-scroll-view");

            var refreshButton = _root.Q<Button>("refresh-button");
            refreshButton.clicked += Refresh;

            RenderSidebar();

            // Set default page
            if (_currentPage == null && _groups.Count > 0 && _groups[0].Pages.Length > 0)
            {
                OnSelectPage(_groups[0].GroupName, 0);
            }
            else if (_currentPage != null)
            {
                DisplayPage(_currentPage);
            }
        }

        private void SetupGroups()
        {
            _groups.Clear();

            _groups.Add(new KitGroupRevImpl("通用工具", "Common", new IKitPageRevV2[] {
                new WelcomeRevPage(),
                new SymbolsRevPage()
            }));

            _groups.Add(new KitGroupRevImpl("材质工具", "Copier", new IKitPageRevV2[] {
                new MaterialCopierRevPage()
            }));

            _groups.Add(new KitGroupRevImpl("表格工具", "Table", new IKitPageRevV2[] {
                new CsvEditViewLiteRevPage(),
                new SelectionToCsvRevPage()
            }));

            _groups.Add(new KitGroupRevImpl("实用工具", "Utility", new IKitPageRevV2[] {
                new SceneJumpRevPage()
            }));
        }

        private void InitializeGroups()
        {
            foreach (var group in _groups) group.Init(this);
        }

        private void RenderSidebar()
        {
            if (_navContainer == null) return;
            _navContainer.Clear();

            foreach (var group in _groups)
            {
                var foldout = new Foldout
                {
                    text = group.Title,
                    value = group.IsExpanded,
                };
                foldout.AddToClassList("group-foldout");
                foldout.RegisterValueChangedCallback(evt => group.IsExpanded = evt.newValue);

                for (int i = 0; i < group.Pages.Length; i++)
                {
                    var page = group.Pages[i];
                    int index = i;
                    string gName = group.GroupName;

                    var pageBtn = new Button(() => OnSelectPage(gName, index)) { text = page.Title };
                    pageBtn.AddToClassList("nav-item");
                    if (gName == _currentGroupName && index == _currentPageIndex)
                        pageBtn.AddToClassList("nav-item--selected");

                    foldout.Add(pageBtn);
                }
                _navContainer.Add(foldout);
            }
        }

        public void OnSelectPage(string groupName, int pageIndex)
        {
            _currentGroupName = groupName;
            _currentPageIndex = pageIndex;

            foreach (var group in _groups)
            {
                if (group.GroupName == groupName)
                {
                    _currentPage = group.Pages[pageIndex];
                    break;
                }
            }

            RenderSidebar();
            DisplayPage(_currentPage);
        }

        private void DisplayPage(IKitPageRevV2 page)
        {
            if (page == null || _contentContainer == null) return;

            _contentTitle.text = page.Title;
            _contentContainer.Clear();
            _contentScrollView.scrollOffset = Vector2.zero;

            var content = page.CreateContent();
            if (content != null) _contentContainer.Add(content);
        }

        public void Refresh()
        {
            _currentPage?.Refresh();
            RenderSidebar();
        }

        private class KitGroupRevImpl : IKitGroupRevV2
        {
            public string Title { get; }
            public string GroupName { get; }
            public IKitPageRevV2[] Pages { get; }
            public bool IsExpanded { get; set; } = true;

            public KitGroupRevImpl(string title, string groupName, IKitPageRevV2[] pages)
            {
                Title = title; GroupName = groupName; Pages = pages;
            }

            public void Init(IKitBoxRevV2 kitBox)
            {
                foreach (var p in Pages) p.Init(kitBox);
            }
        }
    }
}
