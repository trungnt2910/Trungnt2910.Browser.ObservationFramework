namespace Trungnt2910.Browser.ObservationFramework.WebSocket.Messages;

class InvokeMethodData : MessageData
{
    public string MethodAssemblyName { get; set; } = string.Empty;
    public string MethodTypeName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public int ObjectHandle { get; set; }
}

class InvokeMethodResult : MessageResult
{
    public ExceptionMessage? Exception { get; set; }
}