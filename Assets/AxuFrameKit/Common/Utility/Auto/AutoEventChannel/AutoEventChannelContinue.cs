namespace AxuFrameKit.Common.Utility.Auto
{
    /// <summary>
    /// 自动注册并持续执行事件的 Channel
    /// </summary>
    public class AutoEventChannelContinue : AutoEventChannelBase
    {
        private void Awake()
        {
            _eventChannel.Register(OnEventRaised);
        }

        private void OnEventRaised()
        {
            _onEventRaised?.Invoke();
        }
    }
}