using AxuFrameKit.Common.Editor.KitBoxV2.Core;
using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBoxV2.Groups.CommonGroup
{
    /// <summary>
    /// 欢迎页面
    /// </summary>
    public class WelcomePage : IKitPageV2
    {
        public string Title => "工具集介绍";
        
        private IKitBoxV2 _kitBox;
        
        public void Init(IKitBoxV2 kitBox)
        {
            _kitBox = kitBox;
        }
        
        public void Draw()
        {
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginVertical();
            
            // 主标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 0.7f, 0.9f) }
            };
            EditorGUILayout.LabelField("AxU-NextGen", titleStyle, GUILayout.Height(50));
            
            // 副标题
            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };
            EditorGUILayout.LabelField("Unity 开发工具集合 V2", subtitleStyle, GUILayout.Height(30));
            
            GUILayout.Space(30);
            
            // 功能介绍
            GUIStyle infoStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
                wordWrap = true
            };
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(400));
            GUILayout.Space(10);
            EditorGUILayout.LabelField("📦 通用工具 - 定义符号宏管理", infoStyle);
            EditorGUILayout.LabelField("📋 拷贝器 - 材质球批量关联复制", infoStyle);
            EditorGUILayout.LabelField("📊 表格工具 - CSV 编辑与资产导出", infoStyle);
            EditorGUILayout.LabelField("🔧 实用工具 - 场景快速跳转", infoStyle);
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(20);
            
            // 版本信息
            GUIStyle versionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.4f, 0.4f, 0.4f) }
            };
            EditorGUILayout.LabelField("Version 2.0", versionStyle);
            
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
        }
        
        public void Refresh() { }
    }
}
