using Perpetuum.Reactive;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;

namespace Perpetuum.Services.Daytime
{
    /// <summary>
    /// Observer responsible for capturing GameTime events from the GameTimeService and sending messages to the EventListenerService
    /// </summary>
    public class GameTimeObserver : Observer<GameTimeInfo>
    {
        private readonly EventListenerService _listener;
        public GameTimeObserver(EventListenerService listener)
        {
            _listener = listener;
        }

        public override void OnNext(GameTimeInfo info)
        {
            _listener.PublishMessage(new GameTimeMessage(info));
        }
    }
}
