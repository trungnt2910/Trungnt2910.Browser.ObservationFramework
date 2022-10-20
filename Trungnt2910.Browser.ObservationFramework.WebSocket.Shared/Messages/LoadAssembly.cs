namespace ObservationFramework.WebSocket.Messages;

class LoadAssemblyData : MessageData
{
    public byte[] Assembly { get; set; } = Array.Empty<byte>();
}

class LoadAssemblyResult : MessageResult
{
    public ExceptionMessage? Exception { get; set; }
}
