using WebSocketSharp.Server;
using Xunit.Abstractions;

namespace Trungnt2910.Browser.ObservationFramework.WebSocket;

class WebSocketProcessServer : IDisposable
{
    private readonly WebSocketServer _server;
    private readonly int _serverPort = WebSocketSettings.Port;
    private readonly object _connectionLock = new();
    private TaskCompletionSource _connectionSource = new();
    private Queue<WebSocketProcessConnection> _pendingConnections = new();

    public int Port => _serverPort;

    public IMessageSink? DiagnosticMessageSink { get; set; }
    public IMessageSink? ExecutionMessageSink { get; set; }

    public WebSocketProcessServer()
        : this(TcpHelpers.FindUnusedPort(), WebSocketSettings.Path)
    {
        
    }

    public WebSocketProcessServer(int port, string path)
    {
        _serverPort = port;
        _server = new($"{WebSocketSettings.Scheme}{WebSocketSettings.Host}:{_serverPort}");
        _server.AddWebSocketService<WebSocketProcessConnection>($"/{path}", (connection) => connection._server = this);
        _server.WaitTime = WebSocketSettings.WaitTime;
        _server.Start();
    }

    public void Stop()
    {
        _server.Stop();
        lock (_connectionLock)
        {
            _connectionSource.TrySetCanceled();
            foreach (var connection in _pendingConnections)
            {
                connection.Dispose();
            }
            _pendingConnections.Clear();
        }
    }

    public async Task<WebSocketProcessConnection> WaitForNextProcessConnection()
    {
        lock (_connectionLock)
        {
            if (_pendingConnections.Count > 0)
            {
                return _pendingConnections.Dequeue();
            }
        }

        while (true)
        {
            Task task;

            lock (_connectionLock)
            {
                task = _connectionSource.Task;
            }

            await task;
            
            lock (_connectionLock)
            {
                if (_pendingConnections.Count > 0)
                {
                    return _pendingConnections.Dequeue();
                }
            }
        }
    }

    internal void NotifyProcessConnection(WebSocketProcessConnection connection)
    {
        lock (_connectionLock)
        {
            _pendingConnections.Enqueue(connection);
            _connectionSource.SetResult();
            _connectionSource = new();
        }
    }

    #region IDisposable
    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            Stop();
            _disposedValue = true;
        }
    }

    // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~WebSocketProcessServer()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
