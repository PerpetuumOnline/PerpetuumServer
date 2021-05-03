namespace Perpetuum.Services.EventServices.EventMessages
{
    public interface IEventMessage
    {
        EventType Type { get; }
    }
}
