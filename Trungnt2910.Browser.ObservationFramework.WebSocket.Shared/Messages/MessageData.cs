namespace Trungnt2910.Browser.ObservationFramework.WebSocket.Messages;

class MessageData
{
    public MessageOperation Op { get; set; }

    public Guid Id { get; set; } = Guid.NewGuid();
}