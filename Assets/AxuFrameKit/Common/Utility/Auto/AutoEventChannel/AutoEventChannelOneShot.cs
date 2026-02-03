namespace AxuFrameKit.Common.Utility.Auto
{
    /// <summary>
    /// 自动注册并执行一次事件的 Channel
    /// </summary>
    public class AutoEventChannelOneShot : AutoEventChannelBase
    {
        private void Awake()
        {
            _eventChannel.Register(OnEventRaised);
        }

        private void OnEventRaised()
        {
            _onEventRaised?.Invoke();
            _eventChannel.Unregister(OnEventRaised);
        }
    }
}