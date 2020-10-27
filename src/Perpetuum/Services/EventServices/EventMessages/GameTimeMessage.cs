using Perpetuum.Services.Daytime;

namespace Perpetuum.Services.EventServices.EventMessages
{
    public class GameTimeMessage : EventMessage
    {
        public GameTimeInfo TimeInfo { get; private set; }
        public GameTimeMessage(GameTimeInfo time)
        {
            TimeInfo = time;
        }
    }
}
