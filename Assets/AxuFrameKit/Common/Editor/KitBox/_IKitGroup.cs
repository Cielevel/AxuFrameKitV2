namespace AxuFrameKit.Common.Editor.KitBox
{
    public interface IKitGroup
    {
        string Title { get; }
        
        bool IsExpanded { get; set; }

        IKitPage[] Pages { get; }
        
        IKitBox KitBoxWindow { get; set; }
        
        void Init(IKitBox kitBoxWindow);
        
        void Show(string groupName, int index);
    }
}