namespace Trungnt2910.Browser.ObservationFramework.WebSocket.Messages;

class QueryAssemblyNameData : MessageData
{
    public string AssemblyName { get; set; } = string.Empty;
}

class QueryAssemblyNameResult : MessageResult
{
    public bool Exists { get; set; }
}
