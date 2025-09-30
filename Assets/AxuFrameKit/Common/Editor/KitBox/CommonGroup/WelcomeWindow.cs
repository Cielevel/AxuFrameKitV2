using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBox.CommonGroup
{
    public class WelcomePage : IKitPage
    {
        public string Title => "工具集介绍";
        
        public void Show()
        {
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 24
            };
            GUILayout.Label("欢迎使用AxU-NextGen", style, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        }
        
        public void Refresh() { }
    }
}