using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBoxV2.Core
{
    /// <summary>
    /// KitBoxV2 样式工具类
    /// </summary>
    public static class KitBoxStyles
    {
        // ============ 颜色定义 ============
        
        /// <summary>侧边栏背景色</summary>
        public static readonly Color SidebarBgColor = new Color(0.22f, 0.22f, 0.22f, 1f);
        
        /// <summary>选中按钮高亮色</summary>
        public static readonly Color SelectedColor = new Color(0.3f, 0.7f, 0.4f, 1f);
        
        /// <summary>按钮悬停色</summary>
        public static readonly Color HoverColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        
        /// <summary>分组标题背景色</summary>
        public static readonly Color GroupHeaderColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        
        /// <summary>页面区域背景色</summary>
        public static readonly Color PageAreaBgColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        
        // ============ 尺寸常量 ============
        
        /// <summary>侧边栏宽度</summary>
        public const float SidebarWidth = 200f;
        
        /// <summary>按钮高度</summary>
        public const float ButtonHeight = 28f;
        
        /// <summary>分组标题高度</summary>
        public const float GroupHeaderHeight = 24f;
        
        /// <summary>内边距</summary>
        public const float Padding = 5f;
        
        // ============ GUIStyle 缓存 ============
        
        private static GUIStyle _titleStyle;
        private static GUIStyle _pageButtonStyle;
        private static GUIStyle _sectionHeaderStyle;
        
        /// <summary>
        /// 大标题样式
        /// </summary>
        public static GUIStyle TitleStyle
        {
            get
            {
                if (_titleStyle == null)
                {
                    _titleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 18,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                }
                return _titleStyle;
            }
        }
        
        /// <summary>
        /// 页面按钮样式
        /// </summary>
        public static GUIStyle PageButtonStyle
        {
            get
            {
                if (_pageButtonStyle == null)
                {
                    _pageButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(15, 5, 5, 5),
                        fontSize = 12
                    };
                }
                return _pageButtonStyle;
            }
        }
        
        /// <summary>
        /// 区块标题样式
        /// </summary>
        public static GUIStyle SectionHeaderStyle
        {
            get
            {
                if (_sectionHeaderStyle == null)
                {
                    _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 14,
                        alignment = TextAnchor.MiddleLeft,
                        normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                        padding = new RectOffset(5, 0, 5, 5)
                    };
                }
                return _sectionHeaderStyle;
            }
        }
        
        // ============ 辅助方法 ============
        
        /// <summary>
        /// 绘制水平分隔线
        /// </summary>
        public static void DrawHorizontalLine(float height = 1f, float topMargin = 5f, float bottomMargin = 5f)
        {
            GUILayout.Space(topMargin);
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            GUILayout.Space(bottomMargin);
        }
        
        /// <summary>
        /// 绘制区块标题
        /// </summary>
        public static void DrawSectionHeader(string title)
        {
            EditorGUILayout.LabelField(title, SectionHeaderStyle);
            DrawHorizontalLine(1f, 0f, 5f);
        }
        
        /// <summary>
        /// 绘制带背景的 Box
        /// </summary>
        public static void DrawColoredBox(Color bgColor, System.Action drawContent)
        {
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalBg;
            
            drawContent?.Invoke();
            
            EditorGUILayout.EndVertical();
        }
    }
}
