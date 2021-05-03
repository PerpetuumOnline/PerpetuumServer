using Perpetuum.Services.EventServices.EventMessages;
using System;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public interface IEventProcessor : IObserver<IEventMessage>
    {
        void HandleMessage(IEventMessage value);
        EventType Type { get; }
    }

    public abstract class EventProcessor : IEventProcessor
    {
        public abstract EventType Type { get; }

        public abstract void HandleMessage(IEventMessage value);

        public void OnNext(IEventMessage value)
        {
            HandleMessage(value);
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
