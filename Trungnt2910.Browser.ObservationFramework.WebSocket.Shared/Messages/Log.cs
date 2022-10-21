namespace Trungnt2910.Browser.ObservationFramework.WebSocket.Messages;

class LogData : MessageData
{
    public LogType Type { get; set; }
    public string Message { get; set; } = string.Empty;

    public LogData()
    {
        Op = MessageOperation.Log;
    }
}

enum LogType
{
    Diagnostic,
    Execution
}
