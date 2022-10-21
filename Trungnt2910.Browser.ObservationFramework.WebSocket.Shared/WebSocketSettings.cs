namespace Trungnt2910.Browser.ObservationFramework.WebSocket;

static class WebSocketSettings
{
    public const int Port = 35845;
    public const string Scheme = "ws://";
    public const string Host = "localhost";
    public const string Path = "Testing";
    public const string WebSocketPortQueryString = "webSocketPort";
    public static readonly TimeSpan WaitTime = TimeSpan.FromSeconds(1000000);
}
