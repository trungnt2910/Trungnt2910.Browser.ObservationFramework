namespace ObservationFramework.WebSocket.Messages;

class DisposeObjectData : MessageData
{
    public int ObjectHandle { get; set; }
}

class DisposeObjectResult : MessageResult
{
    public ExceptionMessage? Exception { get; set; }
}
