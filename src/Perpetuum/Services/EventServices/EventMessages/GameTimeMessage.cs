using Perpetuum.Services.Daytime;

namespace Perpetuum.Services.EventServices.EventMessages
{
    public class GameTimeMessage : IEventMessage
    {
        public EventType Type => EventType.Environmental;
        public GameTimeInfo TimeInfo { get; private set; }
        public GameTimeMessage(GameTimeInfo time)
        {
            TimeInfo = time;
        }
    }
}
