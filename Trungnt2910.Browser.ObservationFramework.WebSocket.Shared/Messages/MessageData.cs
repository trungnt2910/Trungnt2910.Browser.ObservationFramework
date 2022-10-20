namespace ObservationFramework.WebSocket.Messages;

class MessageData
{
    public MessageOperation Op { get; set; }

    public Guid Id { get; set; } = Guid.NewGuid();
}