using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBoxV2.Core
{
    /// <summary>
    /// KitBoxV2 分组抽象基类
    /// </summary>
    public abstract class KitGroupBase : IKitGroupV2
    {
        protected IKitBoxV2 KitBox { get; private set; }
        
        public abstract string Title { get; }
        public abstract string GroupName { get; }
        public abstract IKitPageV2[] Pages { get; }
        
        public bool IsExpanded { get; set; } = false;
        
        public virtual void Init(IKitBoxV2 kitBox)
        {
            KitBox = kitBox;
            foreach (var page in Pages)
            {
                page.Init(kitBox);
            }
        }
        
        public virtual void Draw(string currentGroupName, int currentPageIndex)
        {
            // 使用 FoldoutHeaderGroup 绘制可折叠的分组标题
            IsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(IsExpanded, Title);
            
            if (IsExpanded)
            {
                EditorGUI.indentLevel++;
                
                for (int i = 0; i < Pages.Length; i++)
                {
                    bool isSelected = currentGroupName == GroupName && currentPageIndex == i;
                    DrawPageButton(Pages[i], i, isSelected);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// 绘制单个页面按钮
        /// </summary>
        protected virtual void DrawPageButton(IKitPageV2 page, int index, bool isSelected)
        {
            Color originalBg = GUI.backgroundColor;
            
            if (isSelected)
            {
                GUI.backgroundColor = KitBoxStyles.SelectedColor;
            }
            
            if (GUILayout.Button(page.Title, KitBoxStyles.PageButtonStyle, GUILayout.Height(KitBoxStyles.ButtonHeight)))
            {
                KitBox?.OnSelectPage(GroupName, index);
            }
            
            GUI.backgroundColor = originalBg;
        }
    }
}
