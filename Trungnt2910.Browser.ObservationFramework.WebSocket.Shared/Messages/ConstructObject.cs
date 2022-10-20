namespace ObservationFramework.WebSocket.Messages;

class ConstructObjectData : MessageData
{
    public string TypeAssemblyName { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
}

class ConstructObjectResult : MessageResult
{
    public int ObjectHandle { get; set; }
    public ExceptionMessage? Exception { get; set; }
}