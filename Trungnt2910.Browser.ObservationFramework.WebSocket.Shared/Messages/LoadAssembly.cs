using System.ComponentModel;

namespace Trungnt2910.Browser.ObservationFramework.WebSocket.Messages;

class LoadAssemblyData : MessageData
{
    public byte[] Assembly { get; set; } = Array.Empty<byte>();
    public byte[]? SymbolStore { get; set; }

    public LoadAssemblyData()
    {
        Op = MessageOperation.LoadAssembly;
    }
}

class LoadAssemblyResult : MessageResult
{
    public ExceptionMessage? Exception { get; set; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public LoadAssemblyResult()
    {
        Op = MessageOperation.LoadAssembly;
    }

    public LoadAssemblyResult(LoadAssemblyData origin)
    {
        Op = origin.Op;
        Id = origin.Id;
    }
}
