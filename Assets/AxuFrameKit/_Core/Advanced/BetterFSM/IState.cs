namespace AxuFrameKit._Core.Advanced.BetterFSM
{
    public interface IState
    {
        void OnEnter();
        void Update();
        void FixedUpdate();
        void OnExit();
    }
}
