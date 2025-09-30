using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBox.CopierGroup
{
    public class CopierGroup : IKitGroup
    {
        public IKitPage[] Pages { get; } =
        {
            new ModelMaterialCopy(),
        };

        public bool IsExpanded { get; set; }
        public IKitBox KitBoxWindow { get; set; }

        public string Title => "拷贝器";

        public void Init(IKitBox kitBoxWindow) => KitBoxWindow = kitBoxWindow;

        public void Show(string groupName, int index)
        {
            IsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(IsExpanded, Title);

            if (IsExpanded)
            {
                for (int i = 0; i < Pages.Length; i++)
                {
                    GUI.backgroundColor = groupName == nameof(CopierGroup) && index == i ? Color.green : Color.white;
                    
                    if (GUILayout.Button(Pages[i].Title, GUILayout.Height(30)))
                        KitBoxWindow.OnSelectPage(nameof(CopierGroup), i);

                    GUI.backgroundColor = Color.white;
                }
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}