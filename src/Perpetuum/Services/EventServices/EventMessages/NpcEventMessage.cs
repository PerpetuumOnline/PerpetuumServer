using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Services.EventServices.EventMessages
{

    /// <summary>
    /// EventMessage sent by an NPC
    /// </summary>
    public class NpcMessage : IEventMessage
    {
        public EventType Type => EventType.NpcChat;
        private string _content;
        private readonly Unit _source;

        public NpcMessage(string payload, Unit source)
        {
            _content = payload;
            _source = source;
        }

        public Player GetPlayerKiller()
        {
            return _source as Player;
        }

        public string GetMessage()
        {
            return _content;
        }
    }
}
