using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBox.CommonGroup
{
    public class CommonGroup : IKitGroup
    {
        public string Title => "通用";
        
        public IKitPage[] Pages { get; } =
        {
            new WelcomePage(),
            new SymbolsEditPage(),
        };
        
        public bool IsExpanded { get; set; }
        
        public IKitBox KitBoxWindow { get; set; }

        public void Init(IKitBox kitBoxWindow) => KitBoxWindow = kitBoxWindow;

        public void Show(string groupName, int index)
        {
            IsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(IsExpanded, Title);

            if (IsExpanded)
            {
                for (int i = 0; i < Pages.Length; i++)
                {
                    GUI.backgroundColor = groupName == nameof(CommonGroup) && index == i ? Color.green : Color.white;
                    
                    if (GUILayout.Button(Pages[i].Title, GUILayout.Height(30)))
                        KitBoxWindow.OnSelectPage(nameof(CommonGroup), i);

                    GUI.backgroundColor = Color.white;
                }
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}