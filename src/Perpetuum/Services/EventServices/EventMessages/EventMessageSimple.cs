namespace Perpetuum.Services.EventServices.EventMessages
{
    /// <summary>
    /// A Simple EventMessage stub with string content, mostly to demonstrate usage
    /// </summary>
    public class EventMessageSimple : IEventMessage
    {
        public EventType Type => EventType.undefined;
        private string _content;
        public EventMessageSimple(string payload)
        {
            _content = payload;
        }
        public string GetMessage()
        {
            return _content;
        }
    }
}
