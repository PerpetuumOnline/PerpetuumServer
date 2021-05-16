using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.EventServices.EventMessages
{
    /// <summary>
    /// Message for sending a message to a character
    /// </summary>
    public class DirectMessage : IEventMessage
    {
        public EventType Type => EventType.DMEcho;
        public Character TargetCharacter { get; private set; }
        public string Message { get; private set; }
        public DirectMessage(Character target, string message)
        {
            TargetCharacter = target;
            Message = message;
        }
    }
}
