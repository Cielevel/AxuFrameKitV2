using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Net
{
    public sealed class SocketClient : IDisposable
    {
        private readonly object _stateLock = new();
        private readonly SemaphoreSlim _sendSemaphore = new(1, 1);
        private readonly SynchronizationContext _syncContext;
        private readonly SocketClientOptions _options;
        private readonly ISocketPacketCodec _codec;

        private CancellationTokenSource _disposeCts = new();
        private CancellationTokenSource _sessionCts;

        private TcpClient _tcpClient;
        private NetworkStream _stream;

        private Task _receiveTask;
        private Task _heartbeatTask;
        private Task _reconnectTask;

        private string _host;
        private int _port;
        private int _reconnectAttempt;
        private long _lastReceiveTicks = DateTime.UtcNow.Ticks;

        private volatile bool _disposed;
        private volatile bool _manualDisconnectRequested;
        private int _state = (int)SocketConnectionState.Disconnected;

        public SocketConnectionState State => (SocketConnectionState)Volatile.Read(ref _state);
        public bool IsConnected => State == SocketConnectionState.Connected;

        public event Action Connected;
        public event Action<byte[]> MessageReceived;
        public event Action<SocketDisconnectedEvent> Disconnected;
        public event Action<Exception> Error;
        public event Action<SocketConnectionState> StateChanged;

        public SocketClient(SocketClientOptions options = null, ISocketPacketCodec codec = null)
        {
            _options = options ?? new SocketClientOptions();
            _options.Validate();

            _codec = codec ?? new LengthPrefixedPacketCodec(_options.MaxPacketSizeBytes);
            _syncContext = SynchronizationContext.Current;
        }

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("host is null or empty", nameof(host));
            if (port <= 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            ThrowIfDisposed();

            lock (_stateLock)
            {
                if (State is SocketConnectionState.Connected or SocketConnectionState.Connecting or SocketConnectionState.Reconnecting)
                    return;

                _host = host;
                _port = port;
                _manualDisconnectRequested = false;
            }

            await ConnectCoreAsync(host, port, isReconnect: false, cancellationToken).ConfigureAwait(false);
        }

        public async Task DisconnectAsync()
        {
            if (_disposed)
                return;

            _manualDisconnectRequested = true;
            await CloseAndNotifyAsync(SocketDisconnectReason.Manual, null, false).ConfigureAwait(false);
        }

        public async Task SendAsync(byte[] payload, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var stream = _stream;

            if (!IsConnected || stream == null)
                throw new InvalidOperationException("Socket is not connected.");

            byte[] frame = _codec.Encode(payload);
            await _sendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await stream.WriteAsync(frame, 0, frame.Length, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                HandleError(ex);
                _ = CloseAndNotifyAsync(SocketDisconnectReason.SendError, ex, false);
                throw;
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        public Task SendTextAsync(string text, CancellationToken cancellationToken = default)
        {
            byte[] bytes = string.IsNullOrEmpty(text) ? Array.Empty<byte>() : System.Text.Encoding.UTF8.GetBytes(text);
            return SendAsync(bytes, cancellationToken);
        }

        private async Task ConnectCoreAsync(string host, int port, bool isReconnect, CancellationToken externalToken)
        {
            SetState(isReconnect ? SocketConnectionState.Reconnecting : SocketConnectionState.Connecting);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token, externalToken);
            TcpClient client = null;

            try
            {
                client = new TcpClient
                {
                    NoDelay = _options.NoDelay
                };
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, _options.KeepAlive);

                Task connectTask = client.ConnectAsync(host, port);
                Task timeoutTask = Task.Delay(_options.ConnectTimeoutMs, linkedCts.Token);
                Task completed = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);

                if (completed == timeoutTask)
                    throw new TimeoutException($"Socket connect timeout: {_options.ConnectTimeoutMs}ms");

                await connectTask.ConfigureAwait(false);

                var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);

                lock (_stateLock)
                {
                    _tcpClient = client;
                    _stream = client.GetStream();
                    _sessionCts?.Dispose();
                    _sessionCts = sessionCts;
                    Volatile.Write(ref _lastReceiveTicks, DateTime.UtcNow.Ticks);
                    _reconnectAttempt = 0;
                }

                SetState(SocketConnectionState.Connected);
                Raise(() => Connected?.Invoke());
                Log($"Connected to {host}:{port}");

                StartBackgroundLoops(sessionCts.Token);
            }
            catch (Exception ex)
            {
                try { client?.Close(); } catch { Log("Ignored exception while closing socket after failed connect."); }
                HandleError(ex);

                if (isReconnect)
                {
                    SetState(SocketConnectionState.Disconnected);
                    throw;
                }

                await CloseAndNotifyAsync(SocketDisconnectReason.ConnectFailed, ex, false).ConfigureAwait(false);
                throw;
            }
        }

        private void StartBackgroundLoops(CancellationToken token)
        {
            _receiveTask = Task.Run(() => ReceiveLoopAsync(token), token);

            if (_options.HeartbeatEnabled)
                _heartbeatTask = Task.Run(() => HeartbeatLoopAsync(token), token);
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var stream = _stream;
                    if (stream == null)
                        break;

                    byte[] payload = await _codec.ReadFrameAsync(stream, cancellationToken).ConfigureAwait(false);
                    if (payload == null)
                    {
                        await CloseAndNotifyAsync(SocketDisconnectReason.RemoteClosed, null, true).ConfigureAwait(false);
                        return;
                    }

                    Volatile.Write(ref _lastReceiveTicks, DateTime.UtcNow.Ticks);

                    if (ShouldIgnoreHeartbeatPacket(payload))
                        continue;

                    Raise(() => MessageReceived?.Invoke(payload));
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                HandleError(ex);
                await CloseAndNotifyAsync(SocketDisconnectReason.ReceiveError, ex, false).ConfigureAwait(false);
            }
        }

        private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_options.HeartbeatIntervalMs, cancellationToken).ConfigureAwait(false);

                    if (!IsConnected)
                        return;

                    long lastTicks = Volatile.Read(ref _lastReceiveTicks);
                    var lastReceiveUtc = new DateTime(lastTicks, DateTimeKind.Utc);
                    if ((DateTime.UtcNow - lastReceiveUtc).TotalMilliseconds > _options.HeartbeatTimeoutMs)
                    {
                        await CloseAndNotifyAsync(SocketDisconnectReason.HeartbeatTimeout, null, false).ConfigureAwait(false);
                        return;
                    }

                    if (_options.HeartbeatRequestPayload is { Length: > 0 })
                    {
                        try
                        {
                            await SendAsync(_options.HeartbeatRequestPayload, cancellationToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private bool ShouldIgnoreHeartbeatPacket(byte[] payload)
        {
            if (!_options.FilterHeartbeatPackets)
                return false;

            if (_options.HeartbeatResponsePayload == null || _options.HeartbeatResponsePayload.Length == 0)
                return false;

            if (payload == null || payload.Length != _options.HeartbeatResponsePayload.Length)
                return false;

            for (int i = 0; i < payload.Length; i++)
            {
                if (payload[i] != _options.HeartbeatResponsePayload[i])
                    return false;
            }

            return true;
        }

        private Task CloseAndNotifyAsync(SocketDisconnectReason reason, Exception exception, bool remote)
        {
            bool shouldReconnect;
            int attempt;
            bool stateChangedToDisconnecting;

            lock (_stateLock)
            {
                if (State == SocketConnectionState.Disconnected || State == SocketConnectionState.Disconnecting)
                    return Task.CompletedTask;

                stateChangedToDisconnecting = TrySetStateNoLock(SocketConnectionState.Disconnecting);
            }

            if (stateChangedToDisconnecting)
                Raise(() => StateChanged?.Invoke(SocketConnectionState.Disconnecting));

            try
            {
                _sessionCts?.Cancel();

                if (_stream != null)
                {
                    try { _stream.Close(); } catch { Log("Ignored exception while closing stream."); }
                    _stream = null;
                }

                if (_tcpClient != null)
                {
                    try { _tcpClient.Close(); } catch { Log("Ignored exception while closing tcp client."); }
                    _tcpClient = null;
                }
            }
            finally
            {
                bool stateChangedToDisconnected;
                lock (_stateLock)
                {
                    stateChangedToDisconnected = TrySetStateNoLock(SocketConnectionState.Disconnected);

                    shouldReconnect = !_manualDisconnectRequested
                                      && !_disposed
                                      && _options.AutoReconnect
                                      && reason != SocketDisconnectReason.Disposed;

                    attempt = shouldReconnect ? ++_reconnectAttempt : _reconnectAttempt;
                }

                if (stateChangedToDisconnected)
                    Raise(() => StateChanged?.Invoke(SocketConnectionState.Disconnected));
            }

            Raise(() => Disconnected?.Invoke(new SocketDisconnectedEvent
            {
                Reason = reason,
                Exception = exception,
                IsRemote = remote,
                WillReconnect = shouldReconnect,
                ReconnectAttempt = attempt
            }));

            Log($"Disconnected. Reason={reason}, Remote={remote}, Reconnect={shouldReconnect}");

            if (shouldReconnect)
                StartReconnectLoop();

            return Task.CompletedTask;
        }

        private void StartReconnectLoop()
        {
            if (_reconnectTask != null && !_reconnectTask.IsCompleted)
                return;

            _reconnectTask = Task.Run(async () =>
            {
                try
                {
                    while (!_disposed && !_manualDisconnectRequested && _options.AutoReconnect)
                    {
                        if (_options.MaxReconnectAttempts > 0 && _reconnectAttempt > _options.MaxReconnectAttempts)
                        {
                            Raise(() => Disconnected?.Invoke(new SocketDisconnectedEvent
                            {
                                Reason = SocketDisconnectReason.ReconnectFailed,
                                Exception = null,
                                IsRemote = false,
                                WillReconnect = false,
                                ReconnectAttempt = _reconnectAttempt - 1
                            }));
                            Log($"Reconnect failed after {_reconnectAttempt - 1} attempts.");
                            return;
                        }

                        int delayMs = CalculateReconnectDelayMs(_reconnectAttempt);
                        Log($"Reconnect attempt {_reconnectAttempt}, wait {delayMs}ms");
                        await Task.Delay(delayMs, _disposeCts.Token).ConfigureAwait(false);

                        if (_disposed || _manualDisconnectRequested)
                            return;

                        try
                        {
                            await ConnectCoreAsync(_host, _port, isReconnect: true, _disposeCts.Token).ConfigureAwait(false);
                            return;
                        }
                        catch (Exception ex)
                        {
                            Log($"Reconnect attempt {_reconnectAttempt} failed: {ex.Message}");

                            lock (_stateLock)
                            {
                                _reconnectAttempt++;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
            }, _disposeCts.Token);
        }

        private int CalculateReconnectDelayMs(int attempt)
        {
            double delay = _options.ReconnectInitialDelayMs * System.Math.Pow(_options.ReconnectBackoffFactor, System.Math.Max(0, attempt - 1));
            return (int)System.Math.Min(_options.ReconnectMaxDelayMs, delay);
        }

        private void SetState(SocketConnectionState newState)
        {
            bool changed;
            lock (_stateLock)
            {
                changed = TrySetStateNoLock(newState);
            }

            if (changed)
                Raise(() => StateChanged?.Invoke(newState));
        }

        private bool TrySetStateNoLock(SocketConnectionState newState)
        {
            if (State == newState)
                return false;

            Volatile.Write(ref _state, (int)newState);
            return true;
        }

        private void Raise(Action action)
        {
            if (action == null) return;

            void SafeInvoke()
            {
                try { action.Invoke(); }
                catch (Exception ex) { LogException(ex); }
            }

            if (_options.DispatchEventsOnUnityMainThread)
            {
                if (_syncContext != null)
                {
                    _syncContext.Post(_ => SafeInvoke(), null);
                    return;
                }

                if (SocketUnityMainThreadDispatcher.IsMainThread)
                {
                    SafeInvoke();
                    return;
                }

                if (SocketUnityMainThreadDispatcher.IsReady)
                {
                    SocketUnityMainThreadDispatcher.Post(SafeInvoke);
                    return;
                }

                // Dispatcher is not ready yet (e.g. early startup or edit mode); fallback to current thread.
                SafeInvoke();
                return;
            }

            if (_syncContext != null)
            {
                _syncContext.Post(_ => SafeInvoke(), null);
                return;
            }

            SafeInvoke();
        }

        private void HandleError(Exception ex)
        {
            if (ex == null) return;
            Raise(() => Error?.Invoke(ex));
            Log($"Error: {ex.GetType().Name} - {ex.Message}");
        }

        private void Log(string message)
        {
            if (!_options.VerboseLogging)
                return;

            if (_options.Logger != null)
            {
                _options.Logger.Invoke($"[SocketClient] {message}");
                return;
            }

            Debug.Log($"[SocketClient] {message}");
        }

        private void LogException(Exception ex)
        {
            if (ex == null)
                return;

            if (_options.Logger != null)
            {
                _options.Logger.Invoke($"[SocketClient] Callback exception: {ex}");
                return;
            }

            Debug.LogException(ex);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SocketClient));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _manualDisconnectRequested = true;

            try
            {
                _disposeCts.Cancel();
            }
            catch
            {
                // ignore
            }

            CloseAndNotifyAsync(SocketDisconnectReason.Disposed, null, false)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            WaitBackgroundTasks();
            _sessionCts?.Dispose();
            _disposeCts.Dispose();
            _sendSemaphore.Dispose();
        }

        private void WaitBackgroundTasks()
        {
            Task[] tasks = { _receiveTask, _heartbeatTask, _reconnectTask };
            int timeoutMs = _options.DisposeWaitTimeoutMs;

            try
            {
                int activeTaskCount = 0;
                for (int i = 0; i < tasks.Length; i++)
                {
                    if (tasks[i] != null)
                        activeTaskCount++;
                }

                if (activeTaskCount == 0)
                    return;

                var activeTasks = new Task[activeTaskCount];
                int index = 0;
                for (int i = 0; i < tasks.Length; i++)
                {
                    if (tasks[i] != null)
                        activeTasks[index++] = tasks[i];
                }

                if (timeoutMs == 0)
                    Task.WaitAll(activeTasks);
                else
                    Task.WaitAll(activeTasks, timeoutMs);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
    }
}
