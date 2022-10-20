using ObservationFramework.WebSocket.Messages;

namespace ObservationFramework.WebSocket;

class HostExecutionException : Exception
{
    public HostExecutionException(ExceptionMessage message)
        : base($"{message.Name} from execution host: {message.Message}\n{message.StackTrace}", message.InnerException != null ? new HostExecutionException(message.InnerException) : null)
    {
    }

    public HostExecutionException(string messasge)
        : base(messasge)
    {

    }
}
