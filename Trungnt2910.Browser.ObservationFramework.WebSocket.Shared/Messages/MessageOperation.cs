namespace ObservationFramework.WebSocket.Messages;

enum MessageOperation
{
    Connect,
    QueryAssemblyName,
    LoadAssembly,
    InvokeMethod,
    ConstructObject,
    DisposeObject,
}
