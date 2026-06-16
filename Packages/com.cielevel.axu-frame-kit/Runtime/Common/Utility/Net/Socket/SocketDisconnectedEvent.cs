using System;

namespace AxuFrameKit.Common.Utility.Net
{
    public sealed class SocketDisconnectedEvent
    {
        public SocketDisconnectReason Reason { get; set; }
        public Exception Exception { get; set; }
        public bool IsRemote { get; set; }
        public bool WillReconnect { get; set; }
        public int ReconnectAttempt { get; set; }
    }
}
