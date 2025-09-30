namespace AxuFrameKit.Common.Editor.KitBox
{
    public interface IKitPage
    {
        string Title { get; }
        
        void Show();

        void Refresh();
    }
}