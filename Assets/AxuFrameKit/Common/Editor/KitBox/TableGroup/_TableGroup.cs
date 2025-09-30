using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBox.TableGroup
{
    public class TableGroup : IKitGroup
    {
        public string Title => "表格工具";
        
        public bool IsExpanded { get; set; }

        public IKitPage[] Pages { get; } =
        {
            new CsvEditViewLitePage(),
            new SelectionToCsvPage(),
        };
        
        public IKitBox KitBoxWindow { get; set; }
        
        public void Init(IKitBox kitBoxWindow) => KitBoxWindow = kitBoxWindow;

        public void Show(string groupName, int index)
        {
            IsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(IsExpanded, Title);

            if (IsExpanded)
            {
                for (int i = 0; i < Pages.Length; i++)
                {
                    GUI.backgroundColor = groupName == nameof(TableGroup) && index == i ? Color.green : Color.white;
                    
                    if (GUILayout.Button(Pages[i].Title, GUILayout.Height(30)))
                        KitBoxWindow.OnSelectPage(nameof(TableGroup), i);

                    GUI.backgroundColor = Color.white;
                }
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}