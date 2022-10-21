using Trungnt2910.Browser.ObservationFramework.WebSocket.Messages;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Trungnt2910.Browser.ObservationFramework.WebSocket;

class WebSocketProcessConnection : WebSocketBehavior, IDisposable
{
    internal WebSocketProcessServer _server = null!;
    private Dictionary<Guid, TaskCompletionSource<MessageResult>> _pendingResults = new();
    private bool _disposedValue;

    protected override void OnMessage(MessageEventArgs e)
    {
        var data = WebSocketMessageSerializer.Deserialize<MessageData>(e.RawData)!;

        switch (data.Op)
        {
            case MessageOperation.Connect:
                _server?.NotifyProcessConnection(this);
            break;
            default:
                lock (_pendingResults)
                {
                    _pendingResults.GetValueOrDefault(data.Id)?.SetResult((MessageResult)data);
                }
            break;
        }
    }

    public async Task<TResult> Send<TResult, TMessage>(TMessage message)
        where TResult: MessageResult
        where TMessage : MessageData
    {
        var tcs = new TaskCompletionSource<MessageResult>();

        lock (_pendingResults)
        {
            _pendingResults.Add(message.Id, tcs);
        }

        Send(WebSocketMessageSerializer.Serialize(message));

        return (TResult)await tcs.Task;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            foreach (var result in _pendingResults.Values)
            {
                result.TrySetCanceled();
            }
            _disposedValue = true;
        }
    }

    // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~WebSocketProcessConnection()
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
}
