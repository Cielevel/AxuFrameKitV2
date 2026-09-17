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

        private void OnDestroy()
        {
            // 与 Awake 的注册成对：拆除时反注册，防止 Channel 残留对已销毁对象的订阅
            if (_eventChannel != null)
                _eventChannel.Unregister(OnEventRaised);
        }
    }
}
