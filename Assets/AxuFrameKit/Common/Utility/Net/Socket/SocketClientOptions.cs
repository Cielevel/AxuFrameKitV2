using System;

namespace AxuFrameKit.Common.Utility.Net
{
    [Serializable]
    public sealed class SocketClientOptions
    {
        public int ConnectTimeoutMs { get; set; } = 8000;

        public bool AutoReconnect { get; set; } = true;
        public int ReconnectInitialDelayMs { get; set; } = 1000;
        public int ReconnectMaxDelayMs { get; set; } = 15000;
        public double ReconnectBackoffFactor { get; set; } = 1.8;
        public int MaxReconnectAttempts { get; set; } = 0;

        public bool HeartbeatEnabled { get; set; }
        public int HeartbeatIntervalMs { get; set; } = 5000;
        public int HeartbeatTimeoutMs { get; set; } = 12000;
        public byte[] HeartbeatRequestPayload { get; set; }
        public byte[] HeartbeatResponsePayload { get; set; }
        public bool FilterHeartbeatPackets { get; set; } = true;

        public bool NoDelay { get; set; } = true;
        public bool KeepAlive { get; set; } = true;

        public int MaxPacketSizeBytes { get; set; } = 2 * 1024 * 1024;

        public bool VerboseLogging { get; set; }
        public Action<string> Logger { get; set; }
        public bool DispatchEventsOnUnityMainThread { get; set; } = true;
        public int DisposeWaitTimeoutMs { get; set; } = 3000;

        public void Validate()
        {
            if (ConnectTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(ConnectTimeoutMs));

            if (ReconnectInitialDelayMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(ReconnectInitialDelayMs));

            if (ReconnectMaxDelayMs < ReconnectInitialDelayMs)
                throw new ArgumentOutOfRangeException(nameof(ReconnectMaxDelayMs));

            if (ReconnectBackoffFactor < 1d)
                throw new ArgumentOutOfRangeException(nameof(ReconnectBackoffFactor));

            if (MaxReconnectAttempts < 0)
                throw new ArgumentOutOfRangeException(nameof(MaxReconnectAttempts));

            if (HeartbeatIntervalMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(HeartbeatIntervalMs));

            if (HeartbeatTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(HeartbeatTimeoutMs));

            if (HeartbeatTimeoutMs < HeartbeatIntervalMs)
                throw new ArgumentOutOfRangeException(nameof(HeartbeatTimeoutMs), "HeartbeatTimeoutMs must be >= HeartbeatIntervalMs.");

            if (MaxPacketSizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaxPacketSizeBytes));

            if (DisposeWaitTimeoutMs < 0)
                throw new ArgumentOutOfRangeException(nameof(DisposeWaitTimeoutMs));
        }
    }
}
