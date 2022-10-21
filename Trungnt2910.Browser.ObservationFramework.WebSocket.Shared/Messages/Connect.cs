namespace Trungnt2910.Browser.ObservationFramework.WebSocket.Messages;

class ConnectData : MessageData
{
    public string? RuntimeFrameworkEnvironment { get; set; }

    public ConnectData()
    {
        Op = MessageOperation.Connect;
    }
}
