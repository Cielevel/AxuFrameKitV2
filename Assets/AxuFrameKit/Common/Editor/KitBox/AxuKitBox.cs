using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBox
{
    public class AxuKitBoxWindow : EditorWindow, IKitBox
    {
        private readonly Dictionary<string, IKitGroup> _groupDict = new()
        {
            { nameof(CommonGroup.CommonGroup), new CommonGroup.CommonGroup() },
            { nameof(CopierGroup.CopierGroup), new CopierGroup.CopierGroup() },
            { nameof(UtilityGroup), new UtilityGroup.UtilityGroup() },
            { nameof(TableGroup), new TableGroup.TableGroup() },
        };

        private string _currentGroupName = null;
        private int _currentPageIndex = -1;

        private IKitPage _currentPage;

        [MenuItem("AxuFrameKit/AxuEditor工具集")]
        public static void Open()
        {
            var window = GetWindow<AxuKitBoxWindow>();

            window.titleContent = new GUIContent("Axu工具集");

            window.Show();
        }

        private void OnGUI()
        {
            // 整体横向布局：左边是导航按钮，右边是页面内容
            EditorGUILayout.BeginHorizontal();

            // 左侧：一列按钮
            DrawSidebar();

            // 右侧：根据当前选择的 page 显示不同内容
            DrawPageArea();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSidebar()
        {
            GUI.backgroundColor = Color.cyan;
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.Width(180)); // 固定宽度

            foreach (var group in _groupDict.Values)
            {
                group.Init(this);
                group.Show(_currentGroupName, _currentPageIndex);
            }

            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
        }

        private void DrawPageArea()
        {
            GUI.backgroundColor = Color.yellow;
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            _currentPage?.Show();

            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
        }

        public void OnSelectPage(string groupName, int pageIndex)
        {
            if (_groupDict.TryGetValue(groupName, out var kitGroup) == false) 
                return;
            
            var kitPages = kitGroup.Pages;
            if (pageIndex >= 0 && pageIndex < kitPages.Length)
            {
                _currentPage      = kitPages[pageIndex];
                _currentGroupName = groupName;
                _currentPageIndex = pageIndex;
                Repaint();
            }
        }
    }
}