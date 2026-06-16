namespace AxuFrameKit.Common.Utility.Net
{
    public enum SocketDisconnectReason
    {
        Manual,
        RemoteClosed,
        ConnectFailed,
        ReceiveError,
        SendError,
        HeartbeatTimeout,
        ReconnectFailed,
        Disposed
    }
}
