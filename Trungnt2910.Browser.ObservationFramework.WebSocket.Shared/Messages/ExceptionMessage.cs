namespace Trungnt2910.Browser.ObservationFramework.WebSocket.Messages;

class ExceptionMessage
{
    public string Name { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string Message { get; set; } = string.Empty;
    public ExceptionMessage? InnerException { get; set; }

    public ExceptionMessage()
    {
    }

    public ExceptionMessage(Exception exception)
    {
        Name = exception.GetType().FullName!;
        StackTrace = exception.StackTrace;
        Message = exception.Message;
        InnerException = (exception.InnerException != null) ? new ExceptionMessage(exception.InnerException) : null;
    }
}