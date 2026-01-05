using System.Collections.Generic;
using AxuFrameKit.Common.Editor.KitBoxV2.Core;
using AxuFrameKit.Common.Editor.KitBoxV2.Groups.CommonGroup;
using AxuFrameKit.Common.Editor.KitBoxV2.Groups.CopierGroup;
using AxuFrameKit.Common.Editor.KitBoxV2.Groups.TableGroup;
using AxuFrameKit.Common.Editor.KitBoxV2.Groups.UtilityGroup;
using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBoxV2
{
    /// <summary>
    /// KitBoxV2 主窗口
    /// </summary>
    public class AxuKitBoxV2Window : EditorWindow, IKitBoxV2
    {
        private readonly List<IKitGroupV2> _groups = new()
        {
            new CommonGroup(),
            new CopierGroup(),
            new TableGroup(),
            new UtilityGroup(),
        };
        
        private string _currentGroupName;
        private int _currentPageIndex = -1;
        private IKitPageV2 _currentPage;
        
        private Vector2 _sidebarScrollPos;
        private Vector2 _pageScrollPos;
        
        [MenuItem("AxuFrameKit/AxuEditor工具集V2")]
        public static void Open()
        {
            var window = GetWindow<AxuKitBoxV2Window>();
            window.titleContent = new GUIContent("Axu工具集 V2");
            window.minSize = new Vector2(700, 450);
            window.Show();
        }
        
        private void OnEnable()
        {
            InitializeGroups();
        }
        
        private void InitializeGroups()
        {
            foreach (var group in _groups)
            {
                group.Init(this);
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            
            // 左侧：侧边栏
            DrawSidebar();
            
            // 右侧：页面内容区域
            DrawPageArea();
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制侧边栏
        /// </summary>
        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, 
                GUILayout.Width(KitBoxStyles.SidebarWidth), 
                GUILayout.ExpandHeight(true));
            
            // 标题
            EditorGUILayout.LabelField("工具导航", KitBoxStyles.SectionHeaderStyle);
            KitBoxStyles.DrawHorizontalLine();
            
            // 滚动区域
            _sidebarScrollPos = EditorGUILayout.BeginScrollView(_sidebarScrollPos);
            
            foreach (var group in _groups)
            {
                group.Draw(_currentGroupName, _currentPageIndex);
                GUILayout.Space(2);
            }
            
            EditorGUILayout.EndScrollView();
            
            // 底部刷新按钮
            GUILayout.FlexibleSpace();
            KitBoxStyles.DrawHorizontalLine();
            
            if (GUILayout.Button("🔄 刷新", GUILayout.Height(KitBoxStyles.ButtonHeight)))
            {
                Refresh();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制页面内容区域
        /// </summary>
        private void DrawPageArea()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, 
                GUILayout.ExpandWidth(true), 
                GUILayout.ExpandHeight(true));
            
            if (_currentPage != null)
            {
                // 页面标题
                EditorGUILayout.LabelField(_currentPage.Title, KitBoxStyles.TitleStyle, GUILayout.Height(35));
                KitBoxStyles.DrawHorizontalLine(2f, 5f, 10f);
                
                // 页面内容滚动区域
                _pageScrollPos = EditorGUILayout.BeginScrollView(_pageScrollPos);
                _currentPage.Draw();
                EditorGUILayout.EndScrollView();
            }
            else
            {
                // 未选择页面时显示欢迎信息
                DrawWelcomeMessage();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制欢迎信息
        /// </summary>
        private void DrawWelcomeMessage()
        {
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginVertical();
            
            GUIStyle welcomeStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            
            EditorGUILayout.LabelField("欢迎使用 AxU 工具集 V2", welcomeStyle, GUILayout.Height(40));
            
            GUIStyle hintStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
            
            EditorGUILayout.LabelField("← 请从左侧导航选择一个工具", hintStyle, GUILayout.Height(25));
            
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
        }
        
        public void OnSelectPage(string groupName, int pageIndex)
        {
            foreach (var group in _groups)
            {
                if (group.GroupName == groupName && pageIndex >= 0 && pageIndex < group.Pages.Length)
                {
                    _currentGroupName = groupName;
                    _currentPageIndex = pageIndex;
                    _currentPage = group.Pages[pageIndex];
                    _pageScrollPos = Vector2.zero; // 重置滚动位置
                    Repaint();
                    return;
                }
            }
        }
        
        public void Refresh()
        {
            _currentPage?.Refresh();
            Repaint();
        }
    }
}
